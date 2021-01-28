using System;
using System.Threading.Tasks;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Microsoft.Extensions.Logging;
using Grpc.Core;
using System.Linq;
using Proto.Mailbox;

namespace Proto.Client
{

    public class ClientReceiveEndpointReader
    {
        private readonly ILogger Logger = Log.CreateLogger<ClientReceiveEndpointReader>();
        private ActorSystem _system;
        private RemoteConfigBase _remoteConfig;
        private GrpcNetChannelProvider _channelProvider;
        private string _address;
        private IEndpointManager _endpointManager;
        private String _clientActorRoot;
        private ChannelBase? _channel;
        private Remoting.RemotingClient? _protoRemoteClient;
        private int _serializerId;
        private ClientRemoting.ClientRemotingClient? _clientRemotingClient;
        private AsyncServerStreamingCall<MessageBatch>? _receiveMessagesStreamingCall;
        

        public ClientReceiveEndpointReader(ActorSystem system, RemoteConfigBase config, GrpcNetChannelProvider channelProvider, IEndpointManager endpointManager, string address, string clientActorRoot)
        {
            this._system = system;
            this._remoteConfig = config;
            this._channelProvider = channelProvider;
            this._address = address;
            this._endpointManager = endpointManager;
            this._clientActorRoot = clientActorRoot;
        }

        public async Task ConnectAsync()
        {
            Logger.LogDebug("[ClientReceiveEndpointReader] Connecting to address {Address}", _address);
            try
            {
                _channel = _channelProvider.GetChannel(_address);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "[ClientReceiveEndpointReader] Error connecting to {_address}.", _address);
                throw;
            }

            _protoRemoteClient = new Remoting.RemotingClient(_channel);

            Logger.LogDebug("[ClientReceiveEndpointReader] Created channel and client for address {Address}", _address);

            var res = await _protoRemoteClient.ConnectAsync(new ConnectRequest());
            _serializerId = res.DefaultSerializerId;
            
            _clientRemotingClient = new ClientRemoting.ClientRemotingClient(_channel);

            _receiveMessagesStreamingCall = _clientRemotingClient.ClientMessageSender(new ClientDetails{ClientActorRoot = _clientActorRoot});

            Logger.LogDebug("[ClientReceiveEndpointReader] Connected client for address {Address}", _address);

            this.ReceiveMessages();

        }

        private void ReceiveMessages()
        {
            //This is almost identical to Proto.Remote.EndPointReader
            var responseStream = _receiveMessagesStreamingCall?.ResponseStream ?? throw new ArgumentNullException("ResponseStream");
            _ = Task.Run(
                async () =>
                {
                    try{
                        var targets = new PID[100];
                        while (await responseStream.MoveNext().ConfigureAwait(false))
                        {
                            // if (_endpointManager.CancellationToken.IsCancellationRequested)
                            // {
                            //     // We read all the messages ignoring them to gracefully end the request
                            //     continue;
                            // }

                            var batch = responseStream.Current;

                            // Logger.LogDebug("[EndpointReader] Received a batch of {Count} messages from {Remote}",
                            //     batch.TargetNames.Count, context.Peer
                            // );

                            //only grow pid lookup if needed
                            if (batch.TargetNames.Count > targets.Length) targets = new PID[batch.TargetNames.Count];

                            for (var i = 0; i < batch.TargetNames.Count; i++)
                            {
                                targets[i] = PID.FromAddress(_system.Address, batch.TargetNames[i]);
                            }

                            var typeNames = batch.TypeNames.ToArray();

                            foreach (var envelope in batch.Envelopes)
                            {
                                var target = targets[envelope.Target];
                                var typeName = typeNames[envelope.TypeId];
                                var message =
                                    _remoteConfig.Serialization.Deserialize(typeName, envelope.MessageData, envelope.SerializerId);

                                switch (message)
                                {
                                    case Terminated msg:
                                        Terminated(msg, target);
                                        break;
                                    case SystemMessage sys:
                                        SystemMessage(sys, target);
                                        break;
                                    default:
                                        ReceiveMessages(envelope, message, target);
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception x)
                    {
                        Logger.LogError(x, "[ClientReceiveEndpointReader] Lost connection to address {Address}", _address);
                        var endpointError = new EndpointErrorEvent
                        {
                            Address = _address,
                            Exception = x
                        };
                        _system.EventStream.Publish(endpointError);
                    }
                    
                   
                }
            );
            
        }

         private void ReceiveMessages(Proto.Remote.MessageEnvelope envelope, object message, PID target)
        {
            Proto.MessageHeader? header = null;

            if (envelope.MessageHeader is not null) header = new Proto.MessageHeader(envelope.MessageHeader.HeaderData);

            // Logger.LogDebug("[EndpointReader] Forwarding remote user message {@Message}", message);
            var localEnvelope = new Proto.MessageEnvelope(message, envelope.Sender, header);
            _system.Root.Send(target, localEnvelope);
        }

        private void SystemMessage(SystemMessage sys, PID target)
        {
            // Logger.LogDebug(
            //     "[EndpointReader] Forwarding remote system message {@MessageType}:{@Message}",
            //     sys.GetType().Name, sys
            // );

            target.SendSystemMessage(_system, sys);
        }

        private void Terminated(Terminated msg, PID target)
        {
            // Logger.LogDebug(
            //     "[EndpointReader] Forwarding remote endpoint termination request for {Who}", msg.Who
            // );

            var rt = new RemoteTerminate(target, msg.Who);
            var endpoint = _endpointManager.GetEndpoint(rt.Watchee);
            if (endpoint is null) return;
            _system.Root.Send(endpoint, rt);
        }
    }

   
}