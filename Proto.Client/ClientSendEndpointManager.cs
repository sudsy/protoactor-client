using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Client
{
    public class ClientSendEndpointManager : IEndpointManager
    {
        private static readonly ILogger Logger = Log.CreateLogger<ClientSendEndpointManager>();
        private readonly ActorSystem _system;
        private Client_RemoteProcess _remoteClientHostProcessSingleton;
        private PID _endpointActorPid;

        public ClientSendEndpointManager(ActorSystem system, RemoteConfigBase remoteConfig, IChannelProvider channelProvider, string clientHostAddress){
            _system = system;
            var nullPID = new PID();
            _remoteClientHostProcessSingleton = new Client_RemoteProcess(_system, this, nullPID);
            
            Logger.LogDebug("[ClientEndpointManager] Requesting new endpoint for {Address}", clientHostAddress);
            var props = Props
                .FromProducer(() => new EndpointActor(clientHostAddress, remoteConfig, channelProvider))
                .WithMailbox(() => new EndpointWriterMailbox(_system,
                        remoteConfig.EndpointWriterOptions.EndpointWriterBatchSize, clientHostAddress
                    )
                )
                .WithGuardianSupervisorStrategy(new EndpointSupervisorStrategy(clientHostAddress, remoteConfig, _system));
            _endpointActorPid = _system.Root.SpawnNamed(props, $"endpoint-{clientHostAddress}");
            _system.ProcessRegistry.RegisterHostResolver(pid => _remoteClientHostProcessSingleton);
        }

        public PID? GetEndpoint(PID destination)
        {
            //It doesn't matter which address we are sending to, we send everything through the clienthost

            return _endpointActorPid;
        }

        public void Start(){
            //This does nothing at the moment
        }
    }
}