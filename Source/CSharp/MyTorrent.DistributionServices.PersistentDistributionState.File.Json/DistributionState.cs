#define CREATE_DEFENSIVE_COPY_OF_IFragmentedFileInfo

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;


namespace MyTorrent.DistributionServices.PersistentDistributionState
{
    [JsonConverter(typeof(DistributionStateJsonConverter))]
    public partial class DistributionState
    {
        public DistributionState(DistributionMetadata metadata, IEnumerable<IFragmentedFileInfo> fileInfos)
        {
            Metadata = metadata;

            Dictionary<string, IFragmentedFileInfo> _fileInfos = new Dictionary<string, IFragmentedFileInfo>();

            foreach (IFragmentedFileInfo fileInfo in fileInfos)
            {
#if CREATE_DEFENSIVE_COPY_OF_IFragmentedFileInfo
                if (fileInfo is FragmentedFileInfo) //is already immutable
                {
                    _fileInfos.Add(fileInfo.Hash, fileInfo);
                }
                else
                {
                    _fileInfos.Add(fileInfo.Hash, new FragmentedFileInfo(fileInfo.Hash, fileInfo.Size, fileInfo.FragmentSequence));
                }
#else
                _fileInfos.Add(fileInfo.Hash, fileInfo);
#endif
            }

            FileInfos = _fileInfos;
        }

        public DistributionState(DistributionMetadata metadata, IReadOnlyDictionary<string, IFragmentedFileInfo> fileInfos)
        {
            Metadata = metadata;

            Dictionary<string, IFragmentedFileInfo> _fileInfos = new Dictionary<string, IFragmentedFileInfo>();

            foreach (var entry in fileInfos)
            {
                foreach ((string fileHash, IFragmentedFileInfo fileInfo) in FileInfos)
                {
                    if (!fileHash.Equals(fileInfo.Hash))
                    {
                        throw new ArgumentException("In one or more entry in file info dictionary does key and value file hash not match.", nameof(fileInfo));
                    }
#if CREATE_DEFENSIVE_COPY_OF_IFragmentedFileInfo
                    if (fileInfo is FragmentedFileInfo) //is already immutable
                    {
                        _fileInfos.Add(fileInfo.Hash, fileInfo);
                    }
                    else
                    {
                        _fileInfos.Add(fileInfo.Hash, new FragmentedFileInfo(fileInfo.Hash, fileInfo.Size, fileInfo.FragmentSequence));
                    }
#else
                    _fileInfos.Add(fileInfo.Hash, fileInfo);
#endif
                }
            }

            FileInfos = _fileInfos;
        }

        public DistributionMetadata Metadata { get; }

        public IReadOnlyDictionary<string, IFragmentedFileInfo> FileInfos { get; } = new Dictionary<string, IFragmentedFileInfo>();

        public async Task SaveToFileAsync(string path, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            //Point of no return. From here the task must not be canceled.

            await using FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fileStream, this, jsonSerializerOptions);
        }

        public static async Task<DistributionState> LoadFromFileAsync(string path, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            return await JsonSerializer.DeserializeAsync<DistributionState>(fileStream, jsonSerializerOptions, cancellationToken);
        }

    }
}
