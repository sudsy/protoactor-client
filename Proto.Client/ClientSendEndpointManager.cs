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
        private readonly object _synLock = new();

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
            _system.ProcessRegistry.RegisterHostResolver(pid => {
                Logger.LogDebug("Host Resolver looking up {pid}", pid);
                return new Client_RemoteProcess(_system, this, pid);
            }); //This triggers for anything with a different address than us
            _system.ProcessRegistry.RegisterClientResolver(pid => {
                Logger.LogDebug("Client Resolver looking up {pid}", pid);
                return new Client_RemoteProcess(_system, this, pid);
            }); //This is for anything that is not registered locally - there shouldn't be any duplication between local and remot pid names 
        }

        public PID? GetEndpoint(PID destination)
        {
            //It doesn't matter which address we are sending to, we send everything through the clienthost

            return _endpointActorPid;
        }

        public void Start(){
            //This does nothing at the moment
        }

        public void Stop(){
            lock (_synLock)
            {
                _system.Root.StopAsync(_endpointActorPid).GetAwaiter().GetResult();
            }
        }
    }
}