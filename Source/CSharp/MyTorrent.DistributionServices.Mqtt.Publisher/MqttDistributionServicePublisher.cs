using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConsumerProducerLocking;
using MyTorrent.HashingServiceProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MyTorrent.DistributionServices.Events;
using MQTTnet.Client.Publishing;
using System.Diagnostics;
using System.Linq;

namespace MyTorrent.DistributionServices
{
    /// <summary>
    /// An <see cref="IDistributionServiceSubscriber"/> implementation that uses MQTT as the underlying distribution communication protocol.
    /// </summary>
    public partial class MqttDistributionServicePublisher : IDistributionServicePublisher
    {
        #region Private Variables

        private readonly int _desiredReplicas;
        private readonly TimeSpan _timeoutTimeSpan;

        private readonly IEventIdCreationSource _eventIdCreationSource;
        private readonly ILogger<MqttDistributionServicePublisher> _logger;
        private readonly IHashingServiceProvider _hashingServiceProvider;
        private readonly IMqttEndpoint _mqttEndpoint;
        
        private readonly DistributionMap _distributionMap = new DistributionMap();
        private readonly ConsumerProducerLock _lock = new ConsumerProducerLock();
        private readonly HashSet<Guid> _eventIds = new HashSet<Guid>();
        private readonly long _fragmentSize;

        private IMqttDistributionServicePublisherState _state;

        private TaskCompletionSource<object?> _WaitForStateChangesToIdleTask;
        
        private volatile bool _disposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="MqttDistributionServicePublisher"/> instance.
        /// </summary>
        /// <param name="logger">
        /// The logger that should be used for this <see cref="MqttDistributionServicePublisher"/> instance.
        /// </param>
        /// <param name="eventIdCreationSource">
        /// The source for creating unique event Id's that should be used by this <see cref="MqttDistributionServicePublisher"/> instance.
        /// </param>
        /// <param name="hashingServiceProvider">
        /// The service provider that validates und normalizes hashes and should be used by this <see cref="MqttDistributionServicePublisher"/> instance.
        /// </param>
        /// <param name="mqttEndpoint">
        /// The MQTT endpoint to publish and receive messages.
        /// </param>
        /// <param name="options">
        /// The options to configure this <see cref="MqttDistributionServicePublisher"/> instance.
        /// </param>
        public MqttDistributionServicePublisher(
            ILogger<MqttDistributionServicePublisher> logger,
            IEventIdCreationSource eventIdCreationSource,
            IHashingServiceProvider hashingServiceProvider,
            IMqttEndpoint mqttEndpoint,
            IOptions<DistributionServicePublisherOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventIdCreationSource = eventIdCreationSource ?? throw new ArgumentNullException(nameof(eventIdCreationSource));
            _hashingServiceProvider = hashingServiceProvider ?? throw new ArgumentNullException(nameof(hashingServiceProvider));
            _mqttEndpoint = mqttEndpoint ?? throw new ArgumentNullException(nameof(mqttEndpoint));

            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, "Initializing Mqtt Distribution-Service-Publisher.");

            DistributionServicePublisherOptions distributionOptions = options?.Value 
                                                                      ?? DistributionServicePublisherOptions.Default;
            if (distributionOptions.Timeout < 1)
                throw new ArgumentOutOfRangeException(nameof(distributionOptions.Timeout), distributionOptions.Timeout, "Timeout timespan too small.");
            else if (distributionOptions.Timeout > 99999)
                throw new ArgumentOutOfRangeException(nameof(distributionOptions.Timeout), distributionOptions.Timeout, "Timeout timespan too large.");

            if (distributionOptions.FragmentSize < 1L)
                throw new ArgumentOutOfRangeException(nameof(distributionOptions.FragmentSize), distributionOptions.FragmentSize, "Fragment Size too small.");

            if (distributionOptions.DesiredReplicas < 1L)
                throw new ArgumentOutOfRangeException(nameof(distributionOptions.DesiredReplicas), distributionOptions.DesiredReplicas, "Desired Replica count too small.");

            _timeoutTimeSpan = TimeSpan.FromMilliseconds(distributionOptions.Timeout);
            _logger.LogInformation(eventId, $"Distribution Timeout Timespan: {distributionOptions.Timeout} ms.");

            _fragmentSize = distributionOptions.FragmentSize;
            _logger.LogInformation(eventId, $"Configured Fragment Size: {_fragmentSize}");

            _desiredReplicas = distributionOptions.DesiredReplicas;
            _logger.LogInformation(eventId, $"Configured Desired Replica Count: {_desiredReplicas}");

            _state = new IdleState();
            _WaitForStateChangesToIdleTask = new TaskCompletionSource<object?>();
            _WaitForStateChangesToIdleTask.SetResult(null);

            _mqttEndpoint.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(HandleReceivedApplicationMessage);

            try
            {
                var result = _mqttEndpoint.PublishEvent(new TrackerHelloEvent());

                if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                {
                    throw new Exception("Expected Mqtt-Client PublishReasonCode Success. Actual: " + result.ReasonCode);
                }
#if TRACE
                else
                {
                    _logger.LogTrace(eventId, $"{nameof(MqttTopics.TrackerHello)} Event sent successfully.");
                }
#endif
            }
            catch (Exception exception)
            {
                _logger.LogWarning(eventId, exception, $"Failed to publish {nameof(MqttTopics.TrackerHello)} Event.");
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MqttDistributionServicePublisher" /> when the
        /// Garbage Collector finalize it. 
        /// </summary>
        ~MqttDistributionServicePublisher()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the size of a whole fragment in bytes.
        /// </summary>
        /// <remarks>
        /// No Fragment is allowed to be larger than this <see cref="FragmentSize"/>, but 
        /// the last fragment of an file is allowed to be smaller, but not empty.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public long FragmentSize 
        { 
            get
            {
                EnsureNotDisposed();
                return _fragmentSize;
            }
        }

        /// <summary>
        /// Gets the uris of the endpoints where the fragments are distributed to and can be retrieved from.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public IReadOnlyCollection<Uri> DistributionEndPoints
        {
            get
            {
                EnsureNotDisposed();
                return _distributionMap.Endpoints;
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
        /// Ensures that the distribution service publisher in a valid state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Method were called when this distribution service publisher wasn't in a valid state.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureValidState()
        {
            if (!_state.IsValid)
                throw new InvalidOperationException($"Mqtt distribution service publisher is in a invalid state. (Current State: {_state})");
        }

        /// <summary>
        /// Ensures that the distribution service publisher was not disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Method were called after this distribution service publisher was disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    objectName: GetType().FullName,
                    message: "Mqtt distribution service publisher was already disposed.");
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
        private void EnsureHashValueIsValidAndNormalized(ref string hashValue, string errorMessage = "Invalid hash format.")
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
                MqttClientPublishResult result = await _mqttEndpoint.PublishEventAsync(@event);
                
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
        
        private async Task<WriteSession> WaitForNextIdleStateAsync(WriteSession? writeSession = null)
        {
            if (writeSession is null)
                writeSession = await _lock.CreateWriteSessionAsync();

            while (!(_state is IdleState))
            {
                if (_state.IsValid)
                {
                    Task waitTask = _WaitForStateChangesToIdleTask.Task;
                    await writeSession.DisposeAsync().ConfigureAwait(false);

                    await waitTask;
                    writeSession = await _lock.CreateWriteSessionAsync().ConfigureAwait(false);
                }
                else
                {
                    await writeSession.DisposeAsync().ConfigureAwait(false);
                    throw new OperationCanceledException("Client is not in an valid state.");
                }
            }

            return writeSession;
        }

        #endregion

        #region Event Handler

        private void HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (!_state.IsValid)
                return;

            //Ignore messages from loopback client
            if (eventArgs.ClientId == null || eventArgs.ClientId.Equals(_mqttEndpoint.ClientId))
                return;

            EventId eventId = GetNextEventId();
            
            string topic = eventArgs.ApplicationMessage.Topic;
#if DEBUG
            _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Received message. (Topic: {topic}; ClientId: {eventArgs.ClientId})");
#endif
            // ReSharper disable AssignmentIsFullyDiscarded
            if (topic.Equals(MqttTopics.FragmentDistributionRequested))
                _ = HandleEventAsyncCore<FragmentDistributionRequestedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleFragmentDistributionRequestedEventAsync);
            else if (topic.Equals(MqttTopics.FragmentDistributionObtained))
                _ = HandleEventAsyncCore<FragmentDistributionObtainedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleFragmentDistributionObtainedEventAsync);
            else if (topic.Equals(MqttTopics.FragmentDistributionFailed))
                _ = HandleEventAsyncCore<FragmentDistributionFailedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleFragmentDistributionFailedEventAsync);
            else if (topic.Equals(MqttTopics.ClientJoinRequested))
                _ = HandleEventAsyncCore<ClientJoinRequestedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleClientJoinRequestedEventAsync);
            else if (topic.Equals(MqttTopics.ClientJoinSucceeded))
                _ = HandleEventAsyncCore<ClientJoinSucceededEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleClientJoinSucceededEventAsync);
            else if (topic.Equals(MqttTopics.ClientJoinFailed))
                _ = HandleEventAsyncCore<ClientJoinFailedEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleClientJoinFailedEventAsync);
            else if (topic.Equals(MqttTopics.ClientGoodbye))
                _ = HandleEventAsyncCore<ClientGoodbyeEvent>(eventId, eventArgs.ClientId, eventArgs.ApplicationMessage.Payload, HandleClientGoodbyeEventAsync);
            else
                _logger.LogWarning(eventId, $"[{eventId.Id:X8}] Received message was not handled.");
            // ReSharper restore AssignmentIsFullyDiscarded
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
                        case WaitForClientJoinResponseState state:
                        {
                            eventId = state.EventId;

                            if (!state.TimeoutCancellationTokenSource.IsCancellationRequested)
                            {
                                _logger.LogInformation(eventId.Value, $"[{eventId.Value.Id:X8}] Client join request timed out.");

                                state.TimeoutCancellationTokenSource.Dispose();
                                await TryPublishEventAndLogResultAsync(eventId.Value, new ClientJoinDeniedEvent(
                                    state.ClientIdentifier, ClientJoinDeniedCode.Other,
                                    "Timeout exceeded."));

                                ResetStateToIdle();
                            }

                            break;
                        }
                        case WaitForDistributionRequestsState state:
                        {
                            if (!state.TimeoutCancellationTokenSource.IsCancellationRequested)
                            {
                                eventId = state.EventId;
                                _logger.LogInformation(eventId.Value, $"[{eventId.Value.Id:X8}] Waiting for distribution requests timed out.");

                                await HandleFragmentDistributionAsyncCore(eventId.Value, state).ConfigureAwait(false);
                            }

                            break;
                        }
                        case WaitForDistributionDeliveryResponseState state:
                        {
                            if (!state.TimeoutCancellationTokenSource.IsCancellationRequested)
                            {
                                eventId = state.EventId;
                                _logger.LogInformation(eventId.Value, $"[{eventId.Value.Id:X8}] Waiting for distribution delivery responses timed out.");

                                await FinishDistributionAsync(eventId.Value, state).ConfigureAwait(false);
                            }

                            break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (eventId.HasValue)
                        _logger.LogError(exception, $"[{eventId.Value.Id:X8}] Error occured while handling timeout.");
                    else
                        _logger.LogError(exception, $"Error occured while handling timeout.");
                }
                finally
                {
                    await _lock.DisableReadLockAsync().ConfigureAwait(false);
                    _lock.ExitWrite();
                }
            }
        }

        private async Task HandleClientJoinRequestedEventAsync(EventId eventId, WriteSession writeSession, string clientIdentifier, ClientJoinRequestedEvent clientJoinRequestedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Handling {nameof(ClientJoinRequestedEvent)} (ClientId: {clientIdentifier}).");
#endif
            
            writeSession = await WaitForNextIdleStateAsync(writeSession).ConfigureAwait(false);

            WaitForClientJoinResponseState? newState = null;

            try
            {
                if (clientJoinRequestedEvent.FragmentSize.HasValue &&
                    clientJoinRequestedEvent.FragmentSize.Value != _fragmentSize)
                {
                    await TryPublishEventAndLogResultAsync(eventId,
                            new ClientJoinDeniedEvent(clientIdentifier, ClientJoinDeniedCode.WrongFragmentSize))
                        .ConfigureAwait(false);

                    throw new OperationCanceledException("Client uses wrong fragment size.");
                }

                if (!clientJoinRequestedEvent.HashAlgorithm.Equals(_hashingServiceProvider.AlgorithmName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    await TryPublishEventAndLogResultAsync(eventId,
                            new ClientJoinDeniedEvent(clientIdentifier, ClientJoinDeniedCode.WrongHashAlgorithm))
                        .ConfigureAwait(false);

                    throw new OperationCanceledException("Client uses wrong hashing algorithm.");
                }

                if (_distributionMap.OverlapsEntpoint(clientJoinRequestedEvent.Endpoints))
                {
                    await TryPublishEventAndLogResultAsync(
                            eventId,
                            new ClientJoinDeniedEvent(clientIdentifier, ClientJoinDeniedCode.EndpointConflict))
                        .ConfigureAwait(false);

                    throw new OperationCanceledException("Client has conflicting endpoints.");
                }

                newState = new WaitForClientJoinResponseState(
                    eventId, 
                    clientIdentifier, 
                    new ClientMetadata(clientJoinRequestedEvent.Endpoints, clientJoinRequestedEvent.StoredFragments.Keys),
                    _timeoutTimeSpan);

                var addFileInfos = new Dictionary<string, FileMetadata>();
                var addFragmentInfos = new Dictionary<string, FragmentMetadata>();
                var removeFileInfos = new HashSet<string>();
                var removeFragmentInfos = new HashSet<string>();
                var clients = new Dictionary<string, ClientMetadata>();

                #region Check if request can be accepted an generate ClientJoinAccept parameter data.

                foreach ((string fileHash, FileMetadata fileMetadata) in clientJoinRequestedEvent.KnownFileInfos)
                {
                    if (_distributionMap.TryGetFragmentedFileInfo(fileHash,
                        out FragmentedFileInfo? fragmentedFileInfo))
                    {
                        //remote file info does not match local file info
                        if (fragmentedFileInfo.Size != fileMetadata.Size ||
                            !fragmentedFileInfo.FragmentSequence.SequenceEqual(fileMetadata.FragmentSequence))
                        {
                            //delete remote file info and replace by local file info
                            removeFileInfos.Add(fileHash);
                            addFileInfos.Add(fileHash,
                                new FileMetadata(fragmentedFileInfo.Size, fragmentedFileInfo.FragmentSequence));
                        }
                    }
                    else
                    {
                        //add unknown file info to local distribution view
                        newState.AddFiles.Add(fileHash, fileMetadata);
                        newState.AddFileInfosToDistributionView.Add(new FragmentedFileInfo(fileHash, fileMetadata.Size,
                            fileMetadata.FragmentSequence));
                    }
                }

                if (newState.TimeoutTask.IsCompleted)
                    throw new TimeoutException();

                foreach (FragmentedFileInfo fragmentedFileInfo in _distributionMap.Files)
                {
                    if (!clientJoinRequestedEvent.KnownFileInfos.ContainsKey(fragmentedFileInfo.Hash))
                    {
                        addFileInfos.Add(fragmentedFileInfo.Hash,
                            new FileMetadata(fragmentedFileInfo.Size, fragmentedFileInfo.FragmentSequence));
                    }
                }

                if (newState.TimeoutTask.IsCompleted)
                    throw new TimeoutException();

                foreach ((string fragmentHash, FragmentMetadata fragmentMetadata) in clientJoinRequestedEvent
                    .StoredFragments)
                {
                    if (_distributionMap.TryGetFragmentInfo(fragmentHash, out IFragmentInfo? fragmentInfo))
                    {
                        //remote fragment info does not match local fragment info
                        if (fragmentInfo.Size != fragmentMetadata.Size)
                        {
                            //delete remote fragment info and replace by local fragment info
                            removeFragmentInfos.Add(fragmentHash);
                            addFragmentInfos.Add(fragmentHash, new FragmentMetadata(fragmentInfo.Size));
                        }
                    }
                    else
                    {
                        //add unknown fragment info to local distribution view
                        newState.AddFragments.Add(fragmentHash, fragmentMetadata);
                        newState.AddFragmentInfosToDistributionView.Add(new FragmentInfo(fragmentHash,
                            fragmentMetadata.Size));
                    }
                }

                if (newState.TimeoutTask.IsCompleted)
                    throw new TimeoutException();

                foreach (IFragmentInfo fragmentInfo in _distributionMap.Fragments)
                {
                    if (!clientJoinRequestedEvent.StoredFragments.ContainsKey(fragmentInfo.Hash))
                    {
                        addFragmentInfos.Add(fragmentInfo.Hash, new FragmentMetadata(fragmentInfo.Size));
                    }
                }

                if (newState.TimeoutTask.IsCompleted)
                    throw new TimeoutException();

                foreach (IClientInfo clientInfo in _distributionMap.Clients)
                {
                    clients.Add(clientInfo.Id, new ClientMetadata(clientInfo.Endpoints, clientInfo.Fragments));
                }

                if (newState.TimeoutTask.IsCompleted)
                    throw new TimeoutException();

                #endregion

                ClientJoinAcceptedEvent clientJoinAcceptedEvent = new ClientJoinAcceptedEvent(
                    clientIdentifier, _fragmentSize, addFileInfos, addFragmentInfos, removeFileInfos,
                    removeFragmentInfos, clients);

                if (!await TryPublishEventAndLogResultAsync(eventId, clientJoinAcceptedEvent).ConfigureAwait(false))
                    throw new OperationCanceledException();

                await writeSession.EnableReadLockAsync().ConfigureAwait(false);

                _state = newState;
                _WaitForStateChangesToIdleTask = new TaskCompletionSource<object?>();
                _ = newState.TimeoutTask.ContinueWith(HandleTimeoutAsync);
            }
            catch (Exception exception)
            {
                newState?.TimeoutCancellationTokenSource.Cancel();
                await writeSession.DisposeAsync().ConfigureAwait(false);

                if (exception is TimeoutException)
                {
                    _logger.LogInformation(eventId, $"[{eventId.Id:X8}] Client join request timed out.");
                    await TryPublishEventAndLogResultAsync(eventId, new ClientJoinDeniedEvent(clientIdentifier, ClientJoinDeniedCode.Other, "Timeout exceeded."))
                        .ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }   
        }

        private async Task HandleClientJoinSucceededEventAsync(EventId eventId, WriteSession writeSession, string clientIdentifier, ClientJoinSucceededEvent clientJoinSucceededEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(ClientJoinSucceededEvent)} (ClientId: {clientIdentifier}).");
#endif
            if (_state is WaitForClientJoinResponseState state) //else ignore event
            {
                if (clientIdentifier.Equals(state.ClientIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    //cancel timeout
                    state.TimeoutCancellationTokenSource.Cancel();
                    state.TimeoutCancellationTokenSource.Dispose();

                    ClientRegisteredEvent clientRegisteredEvent = new ClientRegisteredEvent(
                        clientIdentifier: state.ClientIdentifier,
                        info: state.ClientMetadata,
                        state.AddFiles, state.AddFragments);

                    await TryPublishEventAndLogResultAsync(eventId, clientRegisteredEvent, LogLevel.Error)
                        .ConfigureAwait(false);

                    foreach (FragmentedFileInfo fileInfo in state.AddFileInfosToDistributionView)
                    {
                        _distributionMap.TryAddFileInfo(fileInfo);
                    }

                    foreach (FragmentInfo fragmentInfo in state.AddFragmentInfosToDistributionView)
                    {
                        _distributionMap.TryAddFragmentInfo(fragmentInfo);
                    }

                    _distributionMap.TryAddClient(
                        clientIdentifier: clientIdentifier,
                        endpoints: state.ClientMetadata.Endpoints,
                        storedFragments: state.ClientMetadata.StoredFragments);

                    _logger.LogInformation(eventId, $"[{eventId.Id:X8}] Client registered. Client join request ({state.EventId.Id:X8}) succeeded.");

                    ResetStateToIdle();
                }
                else
                {
                    _logger.LogWarning(eventId, $"Received {nameof(ClientJoinSucceededEvent)} of unknown client.");
                }
            }
        }

        private Task HandleClientJoinFailedEventAsync(EventId eventId, WriteSession writeSession, string clientIdentifier, ClientJoinFailedEvent clientJoinFailedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(ClientJoinFailedEvent)} (ClientId: {clientIdentifier}).");
#endif
            if (_state is WaitForClientJoinResponseState state) //else ignore event
            {
                if (clientIdentifier.Equals(state.ClientIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    //cancel timeout
                    state.TimeoutCancellationTokenSource.Cancel();
                    state.TimeoutCancellationTokenSource.Dispose();

                    _logger.LogInformation(eventId, $"[{eventId.Id:X8}] Client join request ({state.EventId.Id:X8}) failed.");

                    ResetStateToIdle();
                }
                else
                {
                    _logger.LogWarning(eventId, $"Received {nameof(ClientJoinFailedEvent)} of unknown client.");
                }
            }

            return Task.CompletedTask;
        }

        private async Task HandleClientGoodbyeEventAsync(EventId eventId, WriteSession writeSession, string clientIdentifier, ClientGoodbyeEvent clientGoodbyeEvent)
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

        private async Task HandleFragmentDistributionRequestedEventAsync(EventId eventId, WriteSession writeSession, string clientIdentifier, FragmentDistributionRequestedEvent fragmentDistributionRequestedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(FragmentDistributionRequestedEvent)} (ClientId: {clientIdentifier}, Hash: {fragmentDistributionRequestedEvent.Hash}).");
#endif
            if (_state is WaitForDistributionRequestsState state)
            {
                if (fragmentDistributionRequestedEvent.Hash.Equals(state.FragmentHash))
                {
                    state.Requestors.Add(clientIdentifier);
#if DEBUG
                    _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Added Client ({clientIdentifier}) to requestor set.");
#endif
                    if (state.Requestors.Count >= _desiredReplicas) //else ignore
                    {
                        await HandleFragmentDistributionAsyncCore(eventId, state);
                    }
                }
                else
                {
                    _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(FragmentDistributionRequestedEvent)} that is not subject of the current distribution (Hash: {fragmentDistributionRequestedEvent.Hash}).");
                }
            }
            else
            {
                _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(FragmentDistributionRequestedEvent)} that is ignored because of the current state ({_state}).");
            }
        }

        private async Task HandleFragmentDistributionAsyncCore(EventId eventId, WaitForDistributionRequestsState state)
        {
#if DEBUG
            _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Start delivering fragment (Hash: {state.FragmentHash}) to torrent servers.");
#endif
            try
            {
                if (state.Requestors.Count > 0)
                {
                    bool wasEventSent = await TryPublishEventAndLogResultAsync(
                        eventId,
                        @event: new FragmentDistributionDeliveredEvent(
                            state.FragmentHash,
                            state.FragmentData,
                            state.Requestors));

                    if (wasEventSent)
                    {
                        WaitForDistributionDeliveryResponseState newState = new WaitForDistributionDeliveryResponseState(
                            eventId, 
                            state.FragmentHash,
                            state.FragmentSize,
                            state.Requestors,
                            state.TaskCompletionSource,
                            _timeoutTimeSpan);

                        _state = newState;
                        _ = newState.TimeoutTask.ContinueWith(HandleTimeoutAsync);
                    }
                    else
                    {
                        throw new OperationCanceledException(
                            $"Failed to send {nameof(FragmentDistributionDeliveredEvent)}.");
                    }
                }
                else
                {
                    throw new OperationCanceledException("No torrent server requested fragment.");
                }
            }
            catch (Exception exception)
            {
                state.TaskCompletionSource.SetException(exception);
                ResetStateToIdle();

                throw;
            }
            finally
            {
                try
                {
#if TRACE
                    _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Canceling timeout.");
#endif
                    state.TimeoutCancellationTokenSource.Cancel();
                    state.TimeoutCancellationTokenSource.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    //ignore this... that's ok
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(eventId, exception, $"[{eventId.Id:X8}] An Error occured while canceling timeout.");
                }
            }
        }

        private async Task HandleFragmentDistributionObtainedEventAsync(EventId eventId, WriteSession writeSession, string clientIdentifier, FragmentDistributionObtainedEvent fragmentDistributionObtainedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(FragmentDistributionObtainedEvent)} (ClientId: {clientIdentifier}, Hash: {fragmentDistributionObtainedEvent.Hash}).");
#endif
            if (_state is WaitForDistributionDeliveryResponseState state)
            {
                if (fragmentDistributionObtainedEvent.Hash.Equals(state.FragmentHash))
                {
                    state.OpenRequestors.Remove(clientIdentifier);
                    state.ConfirmedRequestors.Add(clientIdentifier);

                    if (state.OpenRequestors.Count == 0)
                        await FinishDistributionAsync(eventId, state);
                }
                else
                {
                    _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(FragmentDistributionObtainedEvent)} that is not subject of the current distribution (Hash: {fragmentDistributionObtainedEvent.Hash}).");
                }
            }
            else
            {
                _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(FragmentDistributionObtainedEvent)} that is ignored because of the current state ({_state}).");
            }
        }

        private async Task HandleFragmentDistributionFailedEventAsync(EventId eventId, WriteSession writeSession, string clientIdentifier, FragmentDistributionFailedEvent fragmentDistributionFailedEvent)
        {
#if TRACE
            _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Start handling {nameof(FragmentDistributionFailedEvent)} (ClientId: {clientIdentifier}, Hash: {fragmentDistributionFailedEvent.Hash}).");
#endif
            if (_state is WaitForDistributionDeliveryResponseState state)
            {
                if (fragmentDistributionFailedEvent.Hash.Equals(state.FragmentHash))
                {
                    state.OpenRequestors.Remove(clientIdentifier);

                    if (state.OpenRequestors.Count == 0)
                    {
                        await writeSession.EnableReadLockAsync().ConfigureAwait(false);
                        await FinishDistributionAsync(eventId, state).ConfigureAwait(false);
                    }
                }
                else
                {
                    _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(FragmentDistributionFailedEvent)} that is not subject of the current distribution (Hash: {fragmentDistributionFailedEvent.Hash}).");
                }
            }
            else
            {
                _logger.LogWarning($"[{eventId.Id:X8}] Received {nameof(FragmentDistributionFailedEvent)} that is ignored because of the current state ({_state}).");
            }
        }

        private async Task FinishDistributionAsync(EventId eventId, WaitForDistributionDeliveryResponseState state)
        {
#if DEBUG
                _logger.LogDebug(eventId, $"[{eventId.Id:X8}] Start delivering fragment Data to ");
#endif
            try
            {
                if (state.ConfirmedRequestors.Count > 0)
                {
                    _distributionMap.TryAddFragmentInfo(state.FragmentHash, state.FragmentSize);
                    _distributionMap.TryAddFragmentToClients(state.FragmentHash, state.ConfirmedRequestors);

                    List<Uri> endpoints = new List<Uri>();

                    foreach (string clientId in state.ConfirmedRequestors)
                    {
                        if (_distributionMap.TryGetClientInfo(clientId, out IClientInfo? clientInfo))
                        {
                            foreach (Uri clientEndpoint in clientInfo.Endpoints)
                            {
                                endpoints.Add(new Uri(clientEndpoint, state.FragmentHash));
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"[{eventId.Id:X8}] An unknown client (ClientId: {clientId}) sent a {nameof(FragmentDistributionObtainedEvent)}.");
                        }
                    }

                    if (endpoints.Count == 0)
                    {
                        throw new OperationCanceledException("No torrent server requested fragment.");
                    }

                    state.TaskCompletionSource.TrySetResult(endpoints);

                    await TryPublishEventAndLogResultAsync(
                        eventId, //This is the log event id not the id of the published event
                        @event: new FragmentDistributionEndedEvent(
                            state.FragmentHash,
                            state.FragmentSize,
                            state.ConfirmedRequestors),
                        errorLogLevel: LogLevel.Error)
                        .ConfigureAwait(false);
                }
                else
                {
                    throw new OperationCanceledException("No torrent server requested fragment.");
                }
            }
            catch (Exception exception)
            {
                state.TaskCompletionSource.SetException(exception);

                throw;
            }
            finally
            {
                try
                {
#if TRACE
                    _logger.LogTrace(eventId, $"[{eventId.Id:X8}] Canceling timeout.");
#endif
                    state.TimeoutCancellationTokenSource.Cancel();
                    state.TimeoutCancellationTokenSource.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    //ignore this... that's ok
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(eventId, exception, $"[{eventId.Id:X8}] An Error occured while canceling timeout.");
                }

                ResetStateToIdle();
            }
        }

        #endregion

        /// <summary>
        /// Checks if the distribution network contains a file with a specific hash value.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the file to search for.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the distribution network contains a file with the 
        /// specified <paramref name="fileHash"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool ExistsFile(string fileHash)
        {
            EnsureNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fileHash))
            {
                try
                {
                    _lock.EnterRead();
                    EnsureValidState();

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
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool ExistsFragment(string fragmentHash)
        {
            EnsureNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fragmentHash))
            {
                try
                {
                    _lock.EnterRead();
                    EnsureValidState();

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
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool TryGetFileInfo(string fileHash, [NotNullWhen(true)] out IFragmentedFileInfo? fileInfo)
        {
            EnsureNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fileHash))
            {
                try
                {
                    _lock.EnterRead();
                    EnsureValidState();

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
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public bool TryGetFragmentDistribution(string fragmentHash, out IEnumerable<Uri> fragmentUris)
        {
            EnsureNotDisposed();

            if (TryValidateAndNormalizeHashValue(ref fragmentHash))
            {
                try
                {
                    _lock.EnterRead();
                    EnsureValidState();

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
        /// Asynchronously publishes information about an fragmented file.
        /// </summary>
        /// <param name="fileHash">
        /// The hash value of the entire file content of the fragmented file whose information should be published.
        /// </param>
        /// <param name="fileSize">
        /// The size of the entire file content of the fragmented file whose information should be published.
        /// </param>
        /// <param name="fragmentHashSequence">
        /// Sequence of hash values of the fragments the file consists of. 
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileHash"/>, <paramref name="fragmentHashSequence"/> or one of 
        /// the elements of <paramref name="fragmentHashSequence"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="fileSize"/> is negative or zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// A file with the specified <paramref name="fileHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fileHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to publish the file info, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public async Task PublishFileInfoAsync(string fileHash, long fileSize, IEnumerable<string> fragmentHashSequence, CancellationToken? cancellationToken = null)
        {
            EnsureNotDisposed();

            cancellationToken ??= CancellationToken.None;
            cancellationToken.Value.ThrowIfCancellationRequested();

            if (fileHash == null)
                throw new ArgumentNullException(nameof(fileHash));
            
            if (fragmentHashSequence == null)
                throw new ArgumentNullException(nameof(fileHash));

            if (fileSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(fileSize), fileSize, "File size has to be positive.");

            EnsureHashValueIsValidAndNormalized(ref fileHash);

            EventId eventId = GetNextEventId();

            await using (WriteSession writeSession = await WaitForNextIdleStateAsync())
            {
                EnsureValidState();

                if (_distributionMap.ContainsFile(fileHash))
                    throw new ArgumentException("A file with the specified file hash already exists.");

                try
                {
                    await writeSession.EnableReadLockAsync();
                    var result = await _mqttEndpoint.PublishEventAsync(new FileInfoPublishedEvent(fileHash, fileSize, fragmentHashSequence));

                    if (result.ReasonCode != MqttClientPublishReasonCode.Success)
                    {
                        throw new IOException(result.ReasonString)
                        {
                            Data = { { "MqttClientPublishingResult", result } }
                        };
                    }
                }
                catch (Exception exception)
                {
                    const string errorMessage = "Failed to publish file info.";
                    _logger.LogError(eventId, exception, errorMessage);
                    throw new IOException(errorMessage, exception);
                }

                _distributionMap.TryAddFileInfo(fileHash, fileSize, fragmentHashSequence);
            }
        }

        /// <summary>
        /// Asynchronously distributes a file fragment to the distribution network.
        /// </summary>
        /// <param name="fragmentHash">
        /// Hash value of the file fragment that should be distributed.
        /// </param>
        /// <param name="fragmentData">
        /// Content of the file fragment that should be distributed.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous distribution operation and wraps the 
        /// uris of the fragment where it was distributed to.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fragmentHash"/> or <paramref name="fragmentData"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Length of the <paramref name="fragmentData"/> is larger than the maximum allowed <see cref="IDistributionServiceObserver.FragmentSize"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An fragment with the specified <paramref name="fragmentHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to distribute the fragment, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public async Task<IEnumerable<Uri>> DistributeFragmentAsync(string fragmentHash, byte[] fragmentData, CancellationToken? cancellationToken = null)
        {
            EnsureNotDisposed();

            cancellationToken ??= CancellationToken.None;
            cancellationToken.Value.ThrowIfCancellationRequested();

            if (fragmentHash == null)
                throw new ArgumentNullException(nameof(fragmentHash));

            if (fragmentData == null)
                throw new ArgumentNullException(nameof(fragmentData));

            if (fragmentData.LongLength == 0L)
                throw new ArgumentOutOfRangeException(nameof(fragmentData), fragmentData, "Fragment data is empty.");
            
            if (fragmentData.LongLength > _fragmentSize)
                throw new ArgumentOutOfRangeException(nameof(fragmentData), fragmentData, "Fragment data larger than maximum allowed fragment size.");

            EnsureHashValueIsValidAndNormalized(ref fragmentHash);

            return await DistributeFragmentAsyncCore(fragmentHash, fragmentData, cancellationToken);
        }

        /// <summary>
        /// Asynchronously distributes a file fragment to the distribution network.
        /// </summary>
        /// <param name="fragmentHash">
        /// Hash value of the file fragment that should be distributed.
        /// </param>
        /// <param name="fragmentStream">
        /// Content of the file fragment that should be distributed, that can be read from this <see cref="Stream"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous distribution operation and wraps the 
        /// uris of the fragment where it was distributed to.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fragmentHash"/> or <paramref name="fragmentStream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Length of the <paramref name="fragmentStream"/> is larger than the maximum allowed <see cref="IDistributionServiceObserver.FragmentSize"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An fragment with the specified <paramref name="fragmentHash"/> already exists.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="fragmentHash"/> stores an value that represents no valid hash.
        /// </exception>
        /// <exception cref="IOException">
        /// Failed to distribute the fragment, because of the underlying I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// This method was called after the <see cref="MqttDistributionServicePublisher"/> was disposed.
        /// </exception>
        public async Task<IEnumerable<Uri>> DistributeFragmentAsync(string fragmentHash, Stream fragmentStream, CancellationToken? cancellationToken = null)
        {
            EnsureNotDisposed();

            cancellationToken ??= CancellationToken.None;
            cancellationToken.Value.ThrowIfCancellationRequested();

            if (fragmentHash == null)
                throw new ArgumentNullException(nameof(fragmentHash));

            if (fragmentStream == null)
                throw new ArgumentNullException(nameof(fragmentStream));

            //FIXME: Can throw NotSupportedException
            if (fragmentStream.Length == 0L)
                throw new ArgumentOutOfRangeException(nameof(fragmentStream), fragmentStream, "Fragment stream is empty.");

            //FIXME: Can throw NotSupportedException
            if (fragmentStream.Length > _fragmentSize)
                throw new ArgumentOutOfRangeException(nameof(fragmentStream), fragmentStream, "Fragment stream larger than maximum allowed fragment size.");

            EnsureHashValueIsValidAndNormalized(ref fragmentHash);

            byte[] fragmentData;

            await using (MemoryStream memoryStream = new MemoryStream())
            {
                await fragmentStream.CopyToAsync(memoryStream, cancellationToken.Value);
                fragmentData = memoryStream.ToArray();
            }

            return await DistributeFragmentAsyncCore(fragmentHash, fragmentData, cancellationToken);
        }

        private async Task<IEnumerable<Uri>> DistributeFragmentAsyncCore(
            string fragmentHash, 
            byte[] fragmentData,
            CancellationToken? cancellationToken)
        {
            EventId eventId = GetNextEventId();
            TaskCompletionSource<IEnumerable<Uri>> taskCompletionSource;

            using (WriteSession writeSession = await WaitForNextIdleStateAsync())
            {
                taskCompletionSource = new TaskCompletionSource<IEnumerable<Uri>>();

                bool wasEventSent = await TryPublishEventAndLogResultAsync(
                        eventId, //This is the log event id not the id of the published event
                        @event: new FragmentDistributionStartedEvent(
                            hash: fragmentHash,
                            size: fragmentData.LongLength),
                        errorLogLevel: LogLevel.Error)
                    .ConfigureAwait(false);

                if (wasEventSent)
                {
                    await writeSession.EnableReadLockAsync().ConfigureAwait(false);

                    WaitForDistributionRequestsState newState = new WaitForDistributionRequestsState(
                        eventId,
                        fragmentHash,
                        fragmentData,
                        taskCompletionSource,
                        _timeoutTimeSpan);

                    _state = newState;
                    _WaitForStateChangesToIdleTask = new TaskCompletionSource<object?>();
                    _ = newState.TimeoutTask.ContinueWith(HandleTimeoutAsync);
                }
                else
                {
                    throw new IOException($"Failed to publish {nameof(FragmentDistributionStartedEvent)}.");
                }
            }

            try
            {
                return await taskCompletionSource.Task.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new IOException($"Failed to publish {nameof(FragmentDistributionStartedEvent)}.", exception);
            }
        }

        /// <summary>
        /// Releases all allocated resources associated to this <see cref="MqttDistributionServicePublisher" /> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MqttDistributionServicePublisher" /> and
        /// optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
        /// release only unmanaged resources.
        /// </param>
        public void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
            
            EventId eventId = GetNextEventId();
            _logger.LogInformation(eventId, "Disposing Mqtt Distribution-Service-Publisher.");

            using (WriteSession writeSession = _lock.CreateWriteSession())
            {
                writeSession.EnableReadLock();

                _state = new DisposedState();
                _mqttEndpoint.ApplicationMessageReceivedHandler = null;
                Task.Run(() => TryPublishEventAndLogResultAsync(eventId, new TrackerGoodbyeEvent())).GetAwaiter().GetResult();
            }
            
            if (disposing)
            {
                _distributionMap.Clear();
                _eventIds.Clear();
                _lock.Dispose();
            }

            _logger.LogInformation(eventId, "Mqtt Distribution-Service-Publisher Disposed.");
        }
    }
}
