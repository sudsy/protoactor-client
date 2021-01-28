using System;
using System.Threading.Tasks;
using Grpc.Core;
using Proto.Remote;

namespace Proto.Client.ClientHost
{
    public class ClientMessageSenderService : ClientRemoting.ClientRemotingBase
    {
        private ActorSystem _system;
        private ClientHostEndpointManager _clientHostEndpointManager;

        public ClientMessageSenderService(ActorSystem system, RemoteConfigBase remoteConfig)
        {
            this._system = system;
            _clientHostEndpointManager = new ClientHostEndpointManager(system, remoteConfig);
            _system.ProcessRegistry.RegisterClientResolver(pid => new Client_RemoteProcess(system, _clientHostEndpointManager, pid ));
        }


        public override Task ClientMessageSender(ClientDetails request, IServerStreamWriter<MessageBatch> responseStream, ServerCallContext context)
        {
            return _clientHostEndpointManager.RegisterClient(request, responseStream, context);
            
        }
    }
}