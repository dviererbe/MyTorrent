using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ConsumerProducerLocking;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MyTorrent.DistributionServices.Events;
using MyTorrent.DistributionServices.PersistentDistributionState;
using MyTorrent.FragmentStorageProviders;
using MyTorrent.HashingServiceProviders;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// An <see cref="IDistributionServiceSubscriber"/> implementation that uses MQTT as the underlying distribution communication protocol.
    /// </summary>
    public partial class MqttDistributionServiceSubscriber : IDistributionServiceSubscriber
    {
#if DEBUG
        private static readonly TimeSpan TimeoutTimeSpan = TimeSpan.FromSeconds(999999);
#else
        private static readonly TimeSpan timeoutTimeSpan = TimeSpan.FromSeconds(10);
#endif

        //TODO: replace by dependency injected random service.
        private static readonly Random RandomNumberGenerator = new Random();

        private readonly IEventIdCreationSource _eventIdCreationSource;
        private readonly ILogger<MqttDistributionServiceSubscriber> _logger;
        
        private readonly IHashingServiceProvider _hashingServiceProvider;
        private readonly IFragmentStorageProvider _fragmentStorageProvider;
        private readonly IPersistentDistributionState _persistentDistributionState;
        private readonly IMqttEndpoint _mqttEndpoint;

        private readonly DistributionMap _distributionMap = new DistributionMap();
        private readonly ConsumerProducerLock _lock = new ConsumerProducerLock();
        private readonly HashSet<Guid> _eventIds = new HashSet<Guid>();
        
        private long? _fragmentSize = null;
        private IEnumerable<Uri>? _endpoints = null;
        
        private volatile bool _disposed = false;
        private IMqttDistributionServiceSubscriberState _state = new InitializingState();

        private TaskCompletionSource<object?> _WaitForStateChangesToIdleTask;
        
        /// <summary>
        /// Initializes a new <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </summary>
        /// <param name="logger">
        /// The logger that should be used for this <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </param>
        /// <param name="eventIdCreationSource">
        /// The source for creating unique event Id's that should be used by this <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </param>
        /// <param name="hashingServiceProvider">
        /// The service provider that validates und normalizes hashes and should be used by this <see cref="MqttDistributionServiceSubscriber"/> instance.
        /// </param>
        /// <param name="fragmentStorageProvider">
        /// The storage provider where the fragments are stored to and retrieved from.
        /// </param>
        /// <param name="persistentDistributionState">
        /// TODO:
        /// </param>
        /// <param name="mqttEndpoint">
        /// The MQTT endpoint to publish and receive messages.
        /// </param>
        public MqttDistributionServiceSubscriber(
            ILogger<MqttDistributionServiceSubscriber> logger,
            IEventIdCreationSource eventIdCreationSource,
            IHashingServiceProvider hashingServiceProvider,
            IFragmentStorageProvider fragmentStorageProvider,
            IPersistentDistributionState persistentDistributionState,
            IMqttEndpoint mqttEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventIdCreationSource = eventIdCreationSource ?? throw new ArgumentNullException(nameof(eventIdCreationSource));
            _hashingServiceProvider = hashingServiceProvider ?? throw new ArgumentNullException(nameof(hashingServiceProvider));
            _fragmentStorageProvider = fragmentStorageProvider ?? throw new ArgumentNullException(nameof(fragmentStorageProvider));
            _persistentDistributionState = persistentDistributionState ?? throw new ArgumentNullException(nameof(persistentDistributionState));
            _mqttEndpoint = mqttEndpoint ?? throw new ArgumentNullException(nameof(mqttEndpoint));

            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, "Initializing Mqtt Distribution-Service-Subscriber.");

            _WaitForStateChangesToIdleTask = new TaskCompletionSource<object?>();
            _WaitForStateChangesToIdleTask.SetCanceled();

            _mqttEndpoint.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(HandleApplicationMessageReceivedAsync);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MqttDistributionServiceSubscriber" /> when the
        /// Garbage Collector finalize it. 
        /// </summary>
        ~MqttDistributionServiceSubscriber()
        {
            Dispose(false);
        }

        #region Properties

        /// <summary>
        /// Gets the size of a whole fragment in bytes.
        /// </summary>
        /// <remarks>
        /// No Fragment is allowed to be larger than this <see cref="FragmentSize"/>, but 
        /// the last fragment of an file is allowed to be smaller, but not empty.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Mqtt Client has not received this information yet.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public long FragmentSize
        {
            get
            {
                EnsureNotDisposed();

                using (ReadSession readSession = _lock.CreateReadSession())
                {
                    EnsureRegistered();
                    return _fragmentSize ?? throw new InvalidOperationException("Client is not registered yet."); 
                }
            }
        }

        /// <summary>
        /// Gets the uris of the endpoints where the fragments are distributed to and can be retrieved from.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public IReadOnlyCollection<Uri> DistributionEndPoints
        {
            get
            {
                EnsureNotDisposed();

                using (ReadSession readSession = _lock.CreateReadSession())
                {
                    EnsureRegistered();
                    return _distributionMap.Endpoints;
                }   
            }
        }

        /// <summary>
        /// Gets the <see cref="IFragmentStorageProvider"/> where the fragments are stored which this <see cref="IDistributionServiceSubscriber"/> decided to hold.
        /// </summary>
        /// <remarks>
        /// An <see cref="IDistributionServiceSubscriber"/> of course don't have to hold any fragment that are published by the <see cref="IDistributionServicePublisher"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public IFragmentStorageProvider FragmentStorage 
        { 
            get
            {
                EnsureNotDisposed();
                return _fragmentStorageProvider;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the next unique event id.
        /// </summary>
        /// <param name="name">
        /// The name of the event (optional).
        /// </param>
        /// <returns>
        /// The unique event id.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventId GetNextEventId(string? name = null) => _eventIdCreationSource.GetNextId(name);

        /// <summary>
        /// Ensures that the distribution service subscriber was not disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this distribution service subscriber was disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task GetRandomDelayTask() => Task.Delay(RandomNumberGenerator.Next(minValue: 0, maxValue: 2000));

        /// <summary>
        /// Ensures that the distribution service subscriber was not disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this distribution service subscriber was disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    objectName: GetType().FullName,
                    message: "Mqtt distribution service publisher  was already disposed.");
        }

        /// <summary>
        /// Ensures that the distribution service subscriber is a registered client.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Distribution service subscriber is currently not registered.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureRegistered()
        {
            if (!_state.IsRegistered)
                throw new InvalidOperationException("Client is not registered.");
        }

        /// <summary>
        /// Ensures that a specific hash value is valid and normalizes it if it is valid.
        /// </summary>
        /// <param name="hashValue">
        /// The hash value to validate and normalize.
        /// </param>
        /// <param name="errorMessage">
        /// The error message that the <see cref="FormatException"/> should contain if the hash value is not valid.
        /// </param>
        /// <exception cref="FormatException">
        /// Specified <paramref name="hashValue"/> is not valid.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureHashValueIsValidAndNormalize(ref string hashValue, string errorMessage = "Invalid hash format.")
        {
            if (!_hashingServiceProvider.Validate(hashValue))
            {
                throw new FormatException(errorMessage)
                {
                    Data = { { "HashValue", hashValue } }
                };
            }

            hashValue = _hashingServiceProvider.Normalize(hashValue);
        }

        private void CancelCancellationTokenSourceSafely(CancellationTokenSource cancellationTokenSource, EventId eventId)
        {
            try
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
            catch (ObjectDisposedException)
            {
                //that's ok... ignore it    
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, $"[{eventId.Id:X8}] Error occured while disposing timeout cancellation source.");
            }
        }

        private bool ShouldTakeFragment(long size)
        {
            //TODO: IMPLEMENT ME!
            return true;
        }

        /// <summary>
        /// Tries to validate a given hash value and normalizes it if it is valid.
        /// </summary>
        /// <param name="hashValue">
        /// The hash value to validate and normalize.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the hash value is valid and was normalized; otherwise <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryValidateAndNormalizeHashValue(ref string hashValue)
        {
            if (_hashingServiceProvider.Validate(hashValue))
            {
                hashValue = _hashingServiceProvider.Normalize(hashValue);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetStateToIdle()
        {
#if TRACE
            _logger.LogTrace("Reset State to Idle.");
#endif
            _state = new IdleState();
            _WaitForStateChangesToIdleTask.TrySetResult(null);
        }

        private async ValueTask<bool> TryPublishEventAndLogResultAsync<TEventType>(EventId eventId, TEventType @event, LogLevel errorLogLevel = LogLevel.Warning) where TEventType : EventBase
        {
            string eventTypeName = typeof(TEventType).Name;
#if DEBUG
            _logger.LogDebug(eventId, $"Sending {eventTypeName}.");
#endif
            try
            {
                MqttClientPublishResult result = await _mqttEndpoint.PublishEventAsync(@event).ConfigureAwait(false);

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new Exception("Expected Mqtt-Client PublishReasonCode Success. Actual: " + result.ReasonCode)
                    {
                        Data =
                        {
                            {"Reason", result.ReasonString},
                            {"ReasonCode", result.ReasonCode}
                        }
                    };
                }
#if TRACE
                else
                {
                    _logger.LogTrace(eventId, $"{eventTypeName} sent successfully.");
                }
#endif
                return true;
            }
            catch (Exception exception)
            {
                _logger.Log(errorLogLevel, eventId, exception, $"Failed to send {eventTypeName}.");
                return false;
            }
        }

        public async Task ResetToInitialStateAsync()
        {
            _state = new InitializingState();

            _WaitForStateChangesToIdleTask = new TaskCompletionSource<object?>();
            _WaitForStateChangesToIdleTask.SetResult(null);

            _distributionMap.Clear();
            await _persistentDistributionState.RevertAsync().ConfigureAwait(false);
            _fragmentSize = _persistentDistributionState.FragmentSize;

            foreach (IFragmentedFileInfo fileInfo in _persistentDistributionState.FileInfos.Values)
            {
                _distributionMap.TryAddFileInfo(fileInfo);
            }

            foreach (string fragmentHash in _fragmentStorageProvider.Fragments)
            {
                long fragmentSize = await _fragmentStorageProvider.GetFragmentStorageSizeAsync(fragmentHash).ConfigureAwait(false);

                _distributionMap.TryAddFragmentInfo(fragmentHash, fragmentSize);
            }

            _distributionMap.TryAddClient(_mqttEndpoint.ClientId, _endpoints!, _fragmentStorageProvider.Fragments);

            _state = new InitializingState();
        }

        private async Task StartJoiningDistributionServiceAsync(EventId? eventId = default)
        {
            eventId ??= GetNextEventId();

            Dictionary<string, FileMetadata> knownFiles = new Dictionary<string, FileMetadata>();

            foreach (FragmentedFileInfo fileInfo in _distributionMap.Files)
            {
                knownFiles.Add(fileInfo.Hash, new FileMetadata(fileInfo.Size, fileInfo.FragmentSequence));
            }

            Dictionary<string, FragmentMetadata> storedFragments = new Dictionary<string, FragmentMetadata>();

            foreach (IFragmentInfo fragmentInfo in _distributionMap.Fragments)
            {
                storedFragments.Add(fragmentInfo.Hash, new FragmentMetadata(fragmentInfo.Size));
            }

            ClientJoinRequestedEvent clientJoinRequestedEvent = new ClientJoinRequestedEvent(
                knownFiles, storedFragments, _endpoints.ToHashSet(), _hashingServiceProvider.AlgorithmName, _fragmentSize);

            if (await TryPublishEventAndLogResultAsync(eventId.Value, clientJoinRequestedEvent, LogLevel.Error).ConfigureAwait(false))
            {
                WaitForJoinResponseState newState = new WaitForJoinResponseState(eventId.Value);

                _state = newState;
                _ = newState.TimeoutTask.ContinueWith(HandleTimeoutAsync);
            }
            else
            {
                _state = new InitializedState();
            }
        }

        #endregion

        #region Event Handler

        private async Task<WriteSession> WaitForNextIdleStateAsync(WriteSession writeSession)
        {
            while (!(_state is IdleState))
            {
                if (_state.IsRegistered)
                {
                    Task waitTask = _WaitForStateChangesToIdleTask.Task;
                    await writeSession.DisposeAsync().ConfigureAwait(false);

                    await waitTask;
                    writeSession = await _lock.CreateWriteSessionAsync().ConfigureAwait(false);
                }
                else
                {
                    await writeSession.DisposeAsync().ConfigureAwait(false);
                    throw new OperationCanceledException("Client is not registered.");
                }
            }

            return writeSession;
        }

        private async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (_state.IsInvalid)
                return;

            EventId eventId = GetNextEventId();

            string topic = eventArgs.ApplicationMessage.Topic;
#if DEBUG
            _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Received message. (Topic: {topic}; ClientId: {eventArgs.ClientId})");
#endif
            if (topic.Equals(MqttTopics.ClientRegistered))
                _ = HandleEventAsyncCore<ClientRegisteredEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleClientRegisteredEventAsync);
            else if (topic.Equals(MqttTopics.TrackerHello))
                _ = HandleEventAsyncCore<TrackerHelloEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleTrackerHelloEventAsync);
            else if (topic.Equals(MqttTopics.TrackerGoodbye))
                _ = HandleEventAsyncCore<TrackerGoodbyeEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleTrackerGoodbyeEventAsync);
            else if (_state.IsRegistered)
            {
                if (topic.Equals(MqttTopics.ClientGoodbye))
                    _ = HandleEventAsyncCore<ClientGoodbyeEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleClientGoodbyeEventAsync);
                else if (topic.Equals(MqttTopics.FileInfoPublished))
                    _ = HandleEventAsyncCore<FileInfoPublishedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleFileInfoPublishedEventAsync);
                else if (topic.Equals(MqttTopics.FragmentDistributionStarted))
                    _ = HandleEventAsyncCore<FragmentDistributionStartedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleFragmentDistributionStartedEventAsync);
                else if (topic.Equals(MqttTopics.FragmentDistributionDelivered))
                    _ = HandleEventAsyncCore<FragmentDistributionDeliveredEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleFragmentDistributionDeliveredEventAsync);
                else if (topic.Equals(MqttTopics.FragmentDistributionEnded))
                    _ = HandleEventAsyncCore<FragmentDistributionEndedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleFragmentDistributionEndedEventAsync);
            }
            else
            {
                if (topic.Equals(MqttTopics.ClientJoinAccepted))
                    _ = HandleEventAsyncCore<ClientJoinAcceptedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleClientJoinAcceptedEventAsync).ConfigureAwait(false);
                else if (topic.Equals(MqttTopics.ClientJoinDenied))
                    _ = HandleEventAsyncCore<ClientJoinDeniedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleClientJoinDeniedEventAsync).ConfigureAwait(false);
            }
        }

        private async Task HandleEventAsyncCore<TEventType>(
            EventId eventId,
            string senderIdentifier,
            byte[] payload,
            Func<EventId, WriteSession, string, TEventType, Task> eventHandler)
            where TEventType : EventBase
        {
            TEventType @event;
            await using WriteSession writeSession = await _lock.CreateWriteSessionAsync().ConfigureAwait(false);

            try
            {
                @event = EventBase.FromUtf8Bytes<TEventType>(payload);

                //Check if event was already received
                if (!_eventIds.Add(@event.EventId))
                    return;
            }
            catch (Exception exception)
            {
                _logger.LogError(eventId, exception, $"[{eventId.Id:X8}] Failed deserializing payload.");
                return;
            }

            try
            {
#if DEBUG
                _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Start processing received message.");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
#endif
                await eventHandler(eventId, writeSession, senderIdentifier, @event).ConfigureAwait(false);
#if DEBUG
                stopwatch.Stop();
                _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Finished processing received message after {stopwatch.ElapsedMilliseconds} ms.");
#endif
            }
            catch (OperationCanceledException exception)
            {
#if DEBUG
                _logger.LogDebug(eventId, exception, $"[{eventId.Id:X8}] Canceled processing received message.");
#endif
            }
            catch (Exception exception)
            {
                _logger.LogError(eventId, exception, $"[{eventId.Id:X8}] Failed processing received message.");
            }
        }

        private async Task HandleTimeoutAsync(Task timeoutTask)
        {
            if (timeoutTask.IsCompletedSuccessfully)
            {
                EventId? eventId = null;

                try
                {
                    await _lock.EnterWriteAsync().ConfigureAwait(false);
                    await _lock.EnableReadLockAsync().ConfigureAwait(false);

                    switch (_state)
                    {
                        case WaitForJoinResponseState state:
                        {
                            eventId = state.EventId;

                            if (!state.TimeoutCancellationTokenSource.IsCancellationRequested)
                            {
                                _logger.LogInformation(eventId.Value, $"[{eventId.Value.Id:X8}] Waiting for join response timed out.");

                                await ResetToInitialStateAsync().ConfigureAwait(false);

                                state.TimeoutCancellationTokenSource.Dispose();
                            }
                            
                            break;
                        }
                        case WaitForRegistrationState state:
                        {
                            eventId = state.EventId;

                            if (!state.TimeoutCancellationTokenSource.IsCancellationRequested)
                            {
                                _logger.LogInformation(eventId.Value, $"[{eventId.Value.Id:X8}] Waiting for registration timed out.");

                                await ResetToInitialStateAsync().ConfigureAwait(false);

                                state.TimeoutCancellationTokenSource.Dispose();
                            }

                            break;
                        }
                        case WaitForFragmentDeliveryState state:
                        {
                            eventId = state.EventId;

                            if (!state.TimeoutCancellationTokenSource.IsCancellationRequested)
                            {
                                _logger.LogInformation(eventId.Value, $"[{eventId.Value.Id:X8}] Client join request timed out.");

                                ResetStateToIdle();

                                state.TimeoutCancellationTokenSource.Dispose();
                            }

                            break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error occured while handling timeout.");
                }
                finally
                {
                    await _lock.DisableReadLockAsync().ConfigureAwait(false);
                    _lock.ExitWrite();
                }
            }
        }

        private async Task HandleTrackerHelloEventAsync(
            EventId eventId, 
            WriteSession writeSession, 
            string clientIdentifier, 
            TrackerHelloEvent trackerHelloEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(TrackerHelloEvent)} (ClientId: {clientIdentifier}).");
#endif
            if (_state.IsInvalid)
                return;

            await writeSession.EnableReadLockAsync().ConfigureAwait(false);

            CancellationTokenSource? cancellationTokenSource = null;

            switch (_state)
            {
                case WaitForJoinResponseState state:
                    cancellationTokenSource = state.TimeoutCancellationTokenSource;
                    break;
                case WaitForRegistrationState state:
                    cancellationTokenSource = state.TimeoutCancellationTokenSource;
                    break;
                case WaitForFragmentDeliveryState state:
                    cancellationTokenSource = state.TimeoutCancellationTokenSource;
                    break;
            }

            if (!(cancellationTokenSource is null))
            {
                try
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    //that's ok... ignore it    
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(eventId, exception, $"[{eventId.Id:X8}] Error occured while disposing timeout cancellation source.");
                }
            }

            await ResetToInitialStateAsync().ConfigureAwait(false);
            await StartJoiningDistributionServiceAsync(eventId);
        }

        private async Task HandleTrackerGoodbyeEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            TrackerGoodbyeEvent trackerGoodbyeEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(TrackerGoodbyeEvent)} (ClientId: {clientIdentifier}).");
#endif
            if (_state.IsInvalid)
                return;

            await writeSession.EnableReadLockAsync().ConfigureAwait(false);

            CancellationTokenSource? cancellationTokenSource = null;

            switch (_state)
            {
                case WaitForJoinResponseState state:
                    cancellationTokenSource = state.TimeoutCancellationTokenSource;
                    break;
                case WaitForRegistrationState state:
                    cancellationTokenSource = state.TimeoutCancellationTokenSource;
                    break;
                case WaitForFragmentDeliveryState state:
                    cancellationTokenSource = state.TimeoutCancellationTokenSource;
                    break;
            }

            if (!(cancellationTokenSource is null))
            {
                try
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    //that's ok... ignore it    
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Error occured while disposing timeout cancellation source.");
                }
            }

            await ResetToInitialStateAsync().ConfigureAwait(false);
        }

        private async Task HandleClientJoinAcceptedEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            ClientJoinAcceptedEvent clientJoinAcceptedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(ClientJoinAcceptedEvent)} (ClientId: {clientJoinAcceptedEvent.ClientIdentifier}).");
#endif
            if (clientJoinAcceptedEvent.ClientIdentifier.Equals(_mqttEndpoint.ClientId, StringComparison.OrdinalIgnoreCase))
            {
                if (_state is WaitForJoinResponseState waitForJoinResponseState)
                {
                    await writeSession.EnableReadLockAsync().ConfigureAwait(false);

                    try
                    {
                        foreach (string fileHash in clientJoinAcceptedEvent.RemoveFileInfos)
                        {
                            if (!_persistentDistributionState.FileInfos.Remove(fileHash))
                                throw new Exception($"Failed to remove file info from persistent state (Hash: {fileHash})");

                            if (!_distributionMap.RemoveFile(fileHash))
                                throw new Exception($"Failed to remove file info from distribution map (Hash: {fileHash})");
                        }

                        if (waitForJoinResponseState.TimeoutTask.IsCompleted) 
                            throw new TimeoutException();

                        foreach (string fragmentHash in clientJoinAcceptedEvent.RemoveFragmentInfos)
                        {
                            if (!_distributionMap.RemoveFragment(fragmentHash))
                                throw new Exception(
                                    $"Failed to remove fragment info from distribution map (Hash: {fragmentHash})");
                        }

                        if (waitForJoinResponseState.TimeoutTask.IsCompleted)
                            throw new TimeoutException();

                        foreach ((string fileHash, FileMetadata fileMetadata) in clientJoinAcceptedEvent.AddFileInfos)
                        {
                            FragmentedFileInfo fragmentedFileInfo =
                                new FragmentedFileInfo(fileHash, fileMetadata.Size, fileMetadata.FragmentSequence);

                            if (!_persistentDistributionState.FileInfos.TryAdd(fileHash, fragmentedFileInfo))
                                throw new Exception($"Failed to add file info to persistent state (Hash: {fileHash})");

                            if (!_distributionMap.TryAddFileInfo(fragmentedFileInfo))
                                throw new Exception($"Failed to add file info to distribution map (Hash: {fileHash})");
                        }

                        if (waitForJoinResponseState.TimeoutTask.IsCompleted)
                            throw new TimeoutException();

                        foreach ((string fragmentHash, FragmentMetadata fragmentMetadata) in clientJoinAcceptedEvent
                            .AddFragmentInfos)
                        {
                            if (!_distributionMap.TryAddFragmentInfo(fragmentHash, fragmentMetadata.Size))
                                throw new Exception(
                                    $"Failed to add fragment info to distribution map (Hash: {fragmentHash})");
                        }

                        if (waitForJoinResponseState.TimeoutTask.IsCompleted)
                            throw new TimeoutException();

                        foreach ((string clientId, ClientMetadata clientMetadata) in clientJoinAcceptedEvent.Clients)
                        {
                            if (!_distributionMap.TryAddClient(clientId, clientMetadata.Endpoints, clientMetadata.StoredFragments))
                                throw new Exception($"Failed to add client info to distribution map (ClientId: {clientIdentifier})");
                        }

                        if (waitForJoinResponseState.TimeoutTask.IsCompleted)
                            throw new TimeoutException();

                        _persistentDistributionState.FragmentSize = _fragmentSize = clientJoinAcceptedEvent.FragmentSize;
                        _persistentDistributionState.HashAlgorithm = _hashingServiceProvider.AlgorithmName;

                        await TryPublishEventAndLogResultAsync(eventId, new ClientJoinSucceededEvent())
                            .ConfigureAwait(false);

                        _state = new WaitForRegistrationState(eventId, waitForJoinResponseState.TimeoutTask, waitForJoinResponseState.TimeoutCancellationTokenSource, clientJoinAcceptedEvent.RemoveFragmentInfos);
                    }
                    catch (Exception exception)
                    {
                        if (exception is TimeoutException)
                        
                            _logger.LogInformation(eventId, $"[{eventId.Id:X8}] Joining distribution service timed out.");
                        else if (exception is OperationCanceledException)
                            _logger.LogInformation(eventId, $"[{eventId.Id:X8}] Joining distribution service canceled.");
                        else
                            _logger.LogError(eventId, exception,
                                $"[{eventId.Id:X8}] Joining distribution service aborted! Failed to add changes to distribution view.");

                        waitForJoinResponseState.TimeoutCancellationTokenSource.Cancel();
                        waitForJoinResponseState.TimeoutCancellationTokenSource.Dispose();

                        await TryPublishEventAndLogResultAsync(eventId, new ClientJoinFailedEvent()).ConfigureAwait(false);
                        await writeSession.EnableReadLockAsync().ConfigureAwait(false);
                        await ResetToInitialStateAsync().ConfigureAwait(false);
                    }
                }
                else if (_state.IsRegistered)
                {
                    _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(ClientJoinAcceptedEvent)} that is ignored because of the current state ({_state}).");
                }
            }
        }

        private async Task HandleClientJoinDeniedEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            ClientJoinDeniedEvent clientJoinDeniedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(ClientJoinDeniedEvent)} (ClientId: {clientJoinDeniedEvent.ClientIdentifier}).");
#endif
            if (clientJoinDeniedEvent.ClientIdentifier.Equals(_mqttEndpoint.ClientId, StringComparison.OrdinalIgnoreCase))
            {
                if (_state is WaitForJoinResponseState waitForJoinResponseState)
                {
                    waitForJoinResponseState.TimeoutCancellationTokenSource.Cancel();
                    waitForJoinResponseState.TimeoutCancellationTokenSource.Dispose();

                    await ResetToInitialStateAsync().ConfigureAwait(false);
                }
                else if (_state is WaitForRegistrationState waitForRegistrationState)
                {
                    waitForRegistrationState.TimeoutCancellationTokenSource.Cancel();
                    waitForRegistrationState.TimeoutCancellationTokenSource.Dispose();

                    await ResetToInitialStateAsync().ConfigureAwait(false);
                }
                else if (!_state.IsRegistered)
                {
                    _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(ClientJoinDeniedEvent)} that is ignored because of the current state.");
                }
            }
        }

        private async Task HandleClientRegisteredEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            ClientRegisteredEvent clientRegisteredEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(ClientRegisteredEvent)} (ClientId: {clientRegisteredEvent.ClientIdentifier}).");
#endif
            if (_state.IsRegistered)
            {
                await using (writeSession = await WaitForNextIdleStateAsync(writeSession).ConfigureAwait(false))
                {
                    await writeSession.EnableReadLockAsync().ConfigureAwait(false);

                    foreach ((string fileHash, FileMetadata fileMetadata) in clientRegisteredEvent.AddedFiles)
                    {
                        _distributionMap.TryAddFileInfo(fileHash, fileMetadata.Size, fileMetadata.FragmentSequence);
                    }

                    foreach ((string fragmentHash, FragmentMetadata fragmentMetadata) in clientRegisteredEvent.AddedFragments)
                    {
                        _distributionMap.TryAddFragmentInfo(fragmentHash, fragmentMetadata.Size);
                    }

                    _distributionMap.TryAddClient(
                        clientIdentifier: clientRegisteredEvent.ClientIdentifier,
                        endpoints: clientRegisteredEvent.Info.Endpoints,
                        storedFragments: clientRegisteredEvent.Info.StoredFragments);

                    _logger.LogInformation(eventId, $"[{eventId.Id:X8}] Client ({clientRegisteredEvent.ClientIdentifier}) registered.");
                }
            }
            else if (_state is WaitForRegistrationState waitForRegistrationState &&
                     clientRegisteredEvent.ClientIdentifier.Equals(_mqttEndpoint.ClientId)) //else ignore event
            {
                try
                {
                    await writeSession.EnableReadLockAsync().ConfigureAwait(false);

                    await _persistentDistributionState.CommitAsync().ConfigureAwait(false);
                    await Task.WhenAll(waitForRegistrationState.RemoveFragments.Select(
                        fragmentHash => FragmentStorage.DeleteFragmentAsync(fragmentHash, wait: true)))
                        .ConfigureAwait(false);

                    ResetStateToIdle();
                }
                catch (Exception exception)
                {
                    _logger.LogError(eventId, exception, $"[{eventId.Id:X8}] Failed to persist changes to distribution view.");

                    await TryPublishEventAndLogResultAsync(eventId, new ClientGoodbyeEvent());
                    await ResetToInitialStateAsync();
                }
            }
        }

        private async Task HandleClientGoodbyeEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            ClientGoodbyeEvent clientGoodbyeEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(ClientGoodbyeEvent)} (ClientId: {clientIdentifier}).");
#endif
            await using (writeSession = await WaitForNextIdleStateAsync(writeSession).ConfigureAwait(false))
            {
                await writeSession.EnableReadLockAsync().ConfigureAwait(false);

                if (_distributionMap.RemoveClient(clientIdentifier))
                {
                    _logger.LogInformation(eventId, $"[{eventId.Id:X8}] Client unregistered.");
                }
                else
                {
                    _logger.LogWarning(eventId, $"[{eventId.Id:X8}] Received {nameof(ClientGoodbyeEvent)} of unknown client.");
                }
            }
        }

        private async Task HandleFileInfoPublishedEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            FileInfoPublishedEvent fileInfoPublishedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(FileInfoPublishedEvent)} (Hash: {fileInfoPublishedEvent.Hash}).");
#endif
            await using (writeSession = await WaitForNextIdleStateAsync(writeSession))
            {
                if (_distributionMap.TryGetFragmentedFileInfo(fileInfoPublishedEvent.Hash, out FragmentedFileInfo? existingFile))
                {
                    if (!existingFile.Equals(fileInfoPublishedEvent))
                    {
                        _logger.LogError(eventId, $"[{eventId.Id:X8}] Failed adding new file info, because of file info already exists. (Hash: {fileInfoPublishedEvent.Hash})");
                    }
                }
                else
                {
                    await writeSession.EnableReadLockAsync();

                    try
                    {
                        _persistentDistributionState.FileInfos.Add(fileInfoPublishedEvent.Hash, fileInfoPublishedEvent);
                        await _persistentDistributionState.CommitAsync().ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(eventId, exception, $"[{eventId.Id:X8}] Failed commiting new file info to persisted distribution state. (Hash: {fileInfoPublishedEvent.Hash})");
                        return;
                    }

                    //This should never return false, because we checked before that the distribution map contains no such file
                    _distributionMap.TryAddFileInfo(fileInfoPublishedEvent);
#if DEBUG
                    _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Added new file info. (Hash: {fileInfoPublishedEvent.Hash})");
#endif
                }
            }
        }

        private async Task HandleFragmentDistributionStartedEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            FragmentDistributionStartedEvent fragmentDistributionStartedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(FragmentDistributionStartedEvent)} (Hash: {fragmentDistributionStartedEvent.Hash}).");
#endif
            await using (writeSession = await WaitForNextIdleStateAsync(writeSession).ConfigureAwait(false))
            {
                if (ShouldTakeFragment(fragmentDistributionStartedEvent.Size))
                {
                    FragmentDistributionRequestedEvent fragmentDistributionRequestedEvent = 
                        new FragmentDistributionRequestedEvent(fragmentDistributionStartedEvent.Hash);

                    bool wasEventSent = await TryPublishEventAndLogResultAsync(
                            eventId, //This is the log event id not the id of the published event
                            @event: fragmentDistributionRequestedEvent,
                            errorLogLevel: LogLevel.Error)
                        .ConfigureAwait(false);

                    if (wasEventSent)
                    {
                        WaitForFragmentDeliveryState newState = new WaitForFragmentDeliveryState(
                            eventId, fragmentDistributionStartedEvent.Hash, fragmentDistributionStartedEvent.Size);

                        _state = newState;
                        _WaitForStateChangesToIdleTask = new TaskCompletionSource<object?>();
                        _ = newState.TimeoutTask.ContinueWith(HandleTimeoutAsync);

                        return;
                    }
                }

                ResetStateToIdle();
            }
        }

        private async Task HandleFragmentDistributionDeliveredEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            FragmentDistributionDeliveredEvent fragmentDistributionDeliveredEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(FragmentDistributionDeliveredEvent)} (Hash: {fragmentDistributionDeliveredEvent.Hash}).");
#endif
            if (_state is WaitForFragmentDeliveryState state)
            {
                if (fragmentDistributionDeliveredEvent.Hash == state.FragmentHash)
                {
                    if (fragmentDistributionDeliveredEvent.Receivers.Contains(_mqttEndpoint.ClientId))
                    {
                        try
                        {
                            if (fragmentDistributionDeliveredEvent.Data.LongLength != state.FragmentSize)
                                throw new ArgumentException(
                                    message:
                                    $"Fragment length does not match with in {nameof(FragmentDistributionStartedEvent)} described fragment.",
                                    paramName: nameof(fragmentDistributionDeliveredEvent.Data));

                            string computedHash =
                                _hashingServiceProvider.ComputeHash(fragmentDistributionDeliveredEvent.Data);

                            if (!fragmentDistributionDeliveredEvent.Hash.Equals(computedHash))
                                throw new ArgumentException(
                                    message: $"Fragment hash does not match data.",
                                    paramName: nameof(fragmentDistributionDeliveredEvent.Hash));

                            if (!fragmentDistributionDeliveredEvent.Hash.Equals(state.FragmentHash))
                                throw new ArgumentException(
                                    message:
                                    $"Fragment hash does not match with in {nameof(FragmentDistributionStartedEvent)} described fragment.",
                                    paramName: nameof(fragmentDistributionDeliveredEvent.Hash));

                            await writeSession.EnableReadLockAsync();

                            await FragmentStorage.StoreFragmentAsync(
                                fragmentHash: fragmentDistributionDeliveredEvent.Hash,
                                data: fragmentDistributionDeliveredEvent.Data).ConfigureAwait(false);

                            _distributionMap.TryAddFragmentInfo(fragmentDistributionDeliveredEvent.Hash,
                                fragmentDistributionDeliveredEvent.Data.LongLength);
                            _distributionMap.TryAddFragmentToClient(fragmentDistributionDeliveredEvent.Hash,
                                _mqttEndpoint.ClientId);

                            if (!await TryPublishEventAndLogResultAsync(eventId, new FragmentDistributionObtainedEvent(
                                hash: state.FragmentHash), LogLevel.Error))
                                throw new Exception(
                                    $"[{eventId.Id:X8}] Failed to publish {nameof(FragmentDistributionObtainedEvent)}.");

                            return;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(eventId, exception,
                                $"[{eventId.Id:X8}] Failed to store delivered fragment.");
                        }

                        CancelCancellationTokenSourceSafely(state.TimeoutCancellationTokenSource, eventId);
                        ResetStateToIdle();
                    }
                }

                await TryPublishEventAndLogResultAsync(
                    eventId, //This is the log event id not the id of the published event
                    @event: new FragmentDistributionFailedEvent(fragmentDistributionDeliveredEvent.Hash));
            }
        }

        private async Task HandleFragmentDistributionEndedEventAsync(
            EventId eventId, 
            WriteSession writeSession,
            string clientIdentifier,
            FragmentDistributionEndedEvent fragmentDistributionEndedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(FragmentDistributionEndedEvent)} (Hash: {fragmentDistributionEndedEvent.Hash}).");
#endif
            if (_state is WaitForFragmentDeliveryState state)
            {
                CancelCancellationTokenSourceSafely(state.TimeoutCancellationTokenSource, eventId);
                ResetStateToIdle();
            }
            else if (_state.IsInvalid || !_state.IsRegistered)
            {
                _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(FragmentDistributionEndedEvent)} that is ignored because of the current state (`{_state}`)");

                return;
            }

            _distributionMap.TryAddFragmentInfo(
                fragmentDistributionEndedEvent.Hash,
                fragmentDistributionEndedEvent.Size);

            _distributionMap.TryAddFragmentToClients(
                fragmentDistributionEndedEvent.Hash,
                fragmentDistributionEndedEvent.Receivers);
#if DEBUG
            _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Added fragment to distribution view.");
#endif
        }

        #endregion

        public async Task InitializeAsync(IEnumerable<Uri> endpoints)
        {
            EnsureNotDisposed();

            if (!endpoints.Any())
                throw new ArgumentException(" No endpoint were set.", nameof(endpoints));

            await using (WriteSession writeSession = await _lock.CreateWriteSessionAsync().ConfigureAwait(false))
            {
                if (_endpoints is null)
                {
                    await writeSession.EnableReadLockAsync().ConfigureAwait(false);
                    
                    _endpoints = endpoints.ToArray();

                    await ResetToInitialStateAsync().ConfigureAwait(false);
                    await StartJoiningDistributionServiceAsync().ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException("Endpoints already set.");
                }
            }
        }

        /// <summary>
        /// Checks if the distribution network contains a file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the file to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution network conatins a file with the 
        /// specified <paramref name="fileHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public bool ExistsFile(string fileHash)
        {
            EnsureNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fileHash))
            {
                try
                {
                    _lock.EnterRead();
                    EnsureRegistered();

                    return _distributionMap.ContainsFile(fileHash);
                }
                finally
                {
                    _lock.ExitRead();
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the distribution network contains a fragment with a specific hash value.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the fragment to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution network conatins a fragment with the 
        /// specified <paramref name="fragmentHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public bool ExistsFragment(string fragmentHash)
        {
            EnsureNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fragmentHash))
            {
                try
                {
                    _lock.EnterRead();
                    EnsureRegistered();

                    return _distributionMap.ContainsFragment(fragmentHash);
                }
                finally
                {
                    _lock.ExitRead();
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to get the information about an fragmented file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the content of the file to get the information for.
        /// </param>
        /// <param name="fileInfo">
        /// When this method returns, contains the <see cref="IFragmentedFileInfo"/> about the file with the
        /// specified <paramref name="fileHash"/>, if the operation succeeded, or <see langword="null"/> if the 
        /// operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation succeeded; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The operation succeeds if information about the specified file was published to the distribution network.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public bool TryGetFileInfo(string fileHash, [NotNullWhen(true)] out IFragmentedFileInfo? fileInfo)
        {
            EnsureNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fileHash))
            {
                try
                {
                    _lock.EnterRead();
                    EnsureRegistered();

                    if (_distributionMap.TryGetFragmentedFileInfo(fileHash, out FragmentedFileInfo? fragmentedFileInfo))
                    {
                        fileInfo = fragmentedFileInfo;
                        return true;
                    }
                }
                finally
                {
                    _lock.ExitRead();
                }
            }

            fileInfo = null;
            return false;
        }

        /// <summary>
        /// Tries to get the uris of an fragment with a specific hash value where the fragment was distributed to.
        /// </summary>
        /// <param name="fragmentHash">
        /// The hash value of the content of the fragment to get the information for.
        /// </param>
        /// <param name="fragmentUris">
        /// When this method returns, contains the enumeration of uris where the fragment with the 
        /// specified <paramref name="fragmentHash"/> was distributed to, if the operation succeeded, or 
        /// an empty array if the operation failed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation succeeded; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The operation succeeds if information about the specified fragment was published to the distribution network.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServiceSubscriber"/> was disposed.
        /// </exception>
        public bool TryGetFragmentDistribution(string fragmentHash, out IEnumerable<Uri> fragmentUris)
        {
            EnsureNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fragmentHash))
            {
                try
                {
                    _lock.EnterRead();
                    EnsureRegistered();

                    if (_distributionMap.TryGetFragmentInfo(fragmentHash, out IFragmentInfo? fragmentInfo))
                    {
                        List<Uri> fragmentUriList = new List<Uri>();

                        foreach (IClientInfo clientInfo in fragmentInfo.FragmentOwner)
                        {
                            foreach (Uri clientEndpoint in clientInfo.Endpoints)
                            {
                                fragmentUriList.Add(new Uri(clientEndpoint, fragmentHash));
                            }
                        }

                        fragmentUris = fragmentUriList;
                        return true;
                    }
                }
                finally
                {
                    _lock.ExitRead();
                }
            }

            fragmentUris = Array.Empty<Uri>();
            return false;
        }

        /// <summary>
        /// Releases all allocated resources associated to this <see cref="MqttDistributionServiceSubscriber" /> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MqttDistributionServiceSubscriber" /> and
        /// optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
        /// release only unmanaged resources.
        /// </param>
        protected void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, "Disposing Mqtt Distribution-Service-Subscriber.");

            using (WriteSession writeSession = _lock.CreateWriteSession())
            {
                Task enableReadLockTask = writeSession.EnableReadLockAsync();
#if DEBUG
                _logger.LogDebug(eventId, $"Sending {MqttTopics.ClientGoodbye} Event.");
#endif
                enableReadLockTask.GetAwaiter().GetResult();

                //Try Send Goodbye Message
                try
                {
                    var result = _mqttEndpoint.PublishEventAsync(new ClientGoodbyeEvent()).GetAwaiter().GetResult();
                    _mqttEndpoint.ApplicationMessageReceivedHandler = null;

                    if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                    {
                        throw new Exception("Expected Mqtt-Client PublishReasonCode Success. Actual: " + result.ReasonCode);
                    }
#if TRACE
                    else
                    {
                        _logger.LogTrace(eventId, $"{nameof(MqttTopics.ClientGoodbye)} Event sent successfully.");
                    }
#endif
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(eventId, exception, $"Failed to send {nameof(MqttTopics.ClientGoodbye)} Event.");
                }
            }

            if (disposing)
            {
                _distributionMap.Clear();
                _eventIds.Clear();
                _lock.Dispose();

                _fragmentSize = null;
            }

            _logger.LogInformation(eventId, "Mqtt Distribution-Service-Subscriber Disposed.");
        }
    }
}
