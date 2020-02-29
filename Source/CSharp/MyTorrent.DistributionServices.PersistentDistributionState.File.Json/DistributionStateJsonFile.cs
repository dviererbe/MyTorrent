using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyTorrent.DistributionServices.PersistentDistributionState
{
    public class DistributionStateJsonFile : IPersistentDistributionState
    {
        public static readonly JsonSerializerOptions DefaultJsonSerializerOptions;

        private readonly ILogger<DistributionStateJsonFile> _logger;
        private readonly IEventIdCreationSource _eventIdCreationSource;

        private DistributionState? _persistedDistributionState;

        private readonly FileInfo _jsonFile;
        
        private string? _hashAlgorithm;
        private long? _fragmentSize;
        private IDictionary<string, IFragmentedFileInfo> _fileInfos;

        static DistributionStateJsonFile()
        {
            DefaultJsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                IgnoreNullValues = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            DefaultJsonSerializerOptions.Converters.Add(new FragmentedFileInfoJsonConverter());
        }

        public DistributionStateJsonFile(
            ILogger<DistributionStateJsonFile> logger,
            IEventIdCreationSource eventIdCreationSource,
            IOptions<DistributionStateJsonFileOptions>? options = null)
        {
            _logger = logger;
            _eventIdCreationSource = eventIdCreationSource;

            EventId eventId = _eventIdCreationSource.GetNextId();

            DistributionStateJsonFileOptions distributionStateJsonFileOptions = options?.Value ?? DistributionStateJsonFileOptions.Default;

            _jsonFile = new FileInfo(distributionStateJsonFileOptions.FilePath ?? Path.Combine(Directory.GetCurrentDirectory(), "DistributionState.json"));

            if (!_jsonFile.Exists)
            {
                try
                {
                    //Test if this application has need rights to perform read/write operations to the filesystem:
                    byte[] testData = Encoding.UTF8.GetBytes("Test");

                    FileStream fileStream = new FileStream(_jsonFile.FullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Delete);
                    fileStream.Write(Encoding.UTF8.GetBytes("Test"));
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.Read(testData);
                    fileStream.Dispose();

                    File.Delete(_jsonFile.FullName);
                }
                catch (Exception exception)
                {
                    string errorMessage = "Failed to read/write from/to specified path.";
                    _logger.LogCritical(eventId, exception, errorMessage);
                    throw new Exception(errorMessage, exception);
                }
            }

            _logger.LogInformation(eventId, "Loding persisted distribution state.");

            try
            {
                LoadPersistedDistributionStateAsync().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                string errorMessage = "Failed to load persisted distribution state from specified path.";
                _logger.LogCritical(eventId, exception, errorMessage);
                throw new Exception(errorMessage, exception);
            }
        }

        public bool Commited { get; private set; } = false;

        public string? HashAlgorithm
        {
            get => _hashAlgorithm;
            set
            {
                if (Commited && value != _hashAlgorithm)
                    Commited = false;
                
                _hashAlgorithm = value;
            }
        }

        public long? FragmentSize
        {
            get => _fragmentSize;
            set
            {
                if (Commited && value != _fragmentSize)
                    Commited = false;

                _fragmentSize = value;
            }
        }

        public IDictionary<string, IFragmentedFileInfo> FileInfos 
        {
            get => _fileInfos; 
            set
            {
                Commited = false;
                _fileInfos = value;
            }
        }

        public async Task CommitAsync(bool autoRevert = true, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Commited)
                return;

            EventId eventId = _eventIdCreationSource.GetNextId();
            _logger.LogInformation(eventId, "Commiting current distribution state.");

            if (_fileInfos.Count == 0)
            {
                if (_jsonFile.Exists)
                {
                    try
                    {
                        _jsonFile.Delete();
                    }
                    catch (Exception exception)
                    {
                        string errorMessage = "Failed to delete json file.";
                        _logger.LogError(eventId, exception, "Commit aborted. " + errorMessage);
                        
                        if (autoRevert)
                            await RevertAsync();

                        throw new IOException(errorMessage, exception);
                    }
                }

                _persistedDistributionState = null;

                _fragmentSize = null;
                _hashAlgorithm = null;
            }
            else if (!(_hashAlgorithm is null) && _fragmentSize.HasValue)
            {
                DistributionState state;

                try
                {
                    state = new DistributionState(new DistributionMetadata(_hashAlgorithm, _fragmentSize.Value), (IReadOnlyDictionary<string, IFragmentedFileInfo>)_fileInfos);
                }
                catch (Exception exception)
                {
                    string errorMessage = "Current distribution state is corrupted.";
                    _logger.LogError(eventId, exception, "Commit aborted. " + errorMessage);

                    if (autoRevert)
                        await RevertAsync();

                    throw new InvalidOperationException(errorMessage, exception);
                }
                
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await state.SaveToFileAsync(_jsonFile.FullName, DefaultJsonSerializerOptions);
                }
                catch (Exception exception)
                {
                    string errorMessage = "Failed to save state to filesystem.";
                    _logger.LogError(eventId, exception, "Commit aborted. " + errorMessage);

                    if (autoRevert)
                        await RevertAsync();

                    throw new InvalidOperationException(errorMessage, exception);
                }
                
                _persistedDistributionState = state;
            }
            else
            {
                string errorMessage = "Uncommitable state.";
                _logger.LogError(eventId, "Commit aborted. " + errorMessage);

                if (autoRevert)
                    await RevertAsync();

                throw new InvalidOperationException(errorMessage);
            }

            Commited = true;
#if DEBUG
            _logger.LogDebug(eventId, "Commited current distribution state successfull.");
#endif
        }

        public Task RevertAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            if (Commited)
                return Task.CompletedTask;

            EventId eventId = _eventIdCreationSource.GetNextId();
            _logger.LogInformation(eventId, "Reverting current distribution state.");

            RevertCore();

#if DEBUG
            _logger.LogDebug(eventId, "Reverted distribution state to last commited distribution state successfull.");
#endif
            return Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RevertCore()
        {
            if (_persistedDistributionState is null)
            {
                _hashAlgorithm = null;
                _fragmentSize = null;
                _fileInfos = new Dictionary<string, IFragmentedFileInfo>();
            }
            else
            {
                _hashAlgorithm = _persistedDistributionState.Metadata.HashAlgorithm;
                _fragmentSize = _persistedDistributionState.Metadata.FragmentSize;
                _fileInfos = new Dictionary<string, IFragmentedFileInfo>(_persistedDistributionState.FileInfos);
            }

            Commited = true;
        }

        private async Task LoadPersistedDistributionStateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_jsonFile.Exists)
            {
                _persistedDistributionState = await DistributionState.LoadFromFileAsync(_jsonFile.FullName, DefaultJsonSerializerOptions, cancellationToken);
            }
            else
            {
                _logger.LogWarning("No persisted distribution state found.");
                _persistedDistributionState = null;
            }

            RevertCore();
        }
    }
}
