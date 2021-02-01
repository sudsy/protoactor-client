using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Client.ClientHost
{
    internal class ClientHostEndpointManager : IEndpointManager
    {
        private readonly ILogger Logger = Log.CreateLogger<ClientHostEndpointManager>();
        private static readonly ConcurrentDictionary<string, PID> _connections = new();
        private ActorSystem _system;
        private RemoteConfigBase _remoteConfig;
        private int _clientActorRootLength;

        public ClientHostEndpointManager(ActorSystem system, RemoteConfigBase remoteConfig)
        {
            _system = system;
            _remoteConfig = remoteConfig;
        }

        public PID? GetEndpoint(PID destination)
        {
            
            if(_clientActorRootLength == 0 || _clientActorRootLength > destination.Id.Length){
                return null;
            }
            var destinationPrefix = destination.Id.Substring(0, _clientActorRootLength);
            PID? clientProxy;
            Logger.LogDebug("[ClientHostEndpointManager] Getting endpoint for {PID}, prefix {prefix}", destination, destinationPrefix);
            Logger.LogDebug("[ClientHostEndpointManager] connection count {count}", _connections.Count);
            if(_connections.TryGetValue(destinationPrefix, out clientProxy)){
                return clientProxy;
            }
            return null;
        }

        public void Start()
        {
            //Start is not really required for the host - not sure if it should be part of IEndpointManager, perhaps another interface
            throw new System.NotImplementedException();
        }

        public async Task RegisterClient(ClientDetails request, IServerStreamWriter<MessageBatch> responseStream, ServerCallContext context)
        {
            var clientActorRoot = request.ClientActorRoot;
            _clientActorRootLength = clientActorRoot.Length;
            var props = Props
                .FromProducer(() => new ClientProxyActor(request, responseStream, _remoteConfig))
                .WithMailbox(() => new EndpointWriterMailbox(_system,
                        _remoteConfig.EndpointWriterOptions.EndpointWriterBatchSize, clientActorRoot
                    )
                )
                .WithGuardianSupervisorStrategy(new EndpointSupervisorStrategy(clientActorRoot, _remoteConfig, _system));
            var endpointActorPid = _system.Root.SpawnNamed(props, $"clientproxy-{clientActorRoot}");
            Logger.LogDebug("[ClientHostEndpointManager] Created new endpoint for {Address}", clientActorRoot);
            if(!_connections.TryAdd(clientActorRoot, endpointActorPid)){
                //Failed to add so immediately shutdown // probably need to clean up the actor
                return;
            };
            Logger.LogDebug("[ClientHostEndpointManager] Added Connection - total count is now {count}", _connections.Count);
            await Task.Delay(-1); //Wait indefinitely for now. // Should be linked to remote temrninate message inside ClientPRoxyActor
        }
    }
}