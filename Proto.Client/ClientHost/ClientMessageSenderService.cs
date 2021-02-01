using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Client.ClientHost
{
    public class ClientMessageSenderService : ClientRemoting.ClientRemotingBase
    {
        private readonly ILogger Logger = Log.CreateLogger<ClientMessageSenderService>();
        private ActorSystem _system;
        private ClientHostEndpointManager _clientHostEndpointManager;

        public ClientMessageSenderService(ActorSystem system, RemoteConfigBase remoteConfig)
        {
            
            this._system = system;
            _clientHostEndpointManager = new ClientHostEndpointManager(system, remoteConfig);
            _system.ProcessRegistry.RegisterClientResolver(pid => {
                if(String.IsNullOrEmpty(pid.Id)){
                    return null;
                }
                if(!pid.Id.StartsWith("$client")){
                    return null;
                }
                Logger.LogDebug("Running clienthost resolver on server for {pid}", pid);
                return new Client_RemoteProcess(system, _clientHostEndpointManager, pid );
            });
        }


        public override Task ClientMessageSender(ClientDetails request, IServerStreamWriter<MessageBatch> responseStream, ServerCallContext context)
        {
            
            Logger.LogDebug("[ClientMessageSenderService] registered client {prefix}", request.ClientActorRoot);
            return _clientHostEndpointManager.RegisterClient(request, responseStream, context);
            
        }
    }
}