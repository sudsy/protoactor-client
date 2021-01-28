using System.Threading.Tasks;
using Grpc.Core;
using Proto.Remote;

namespace Proto.Client.ClientHost
{
    internal class ClientEndpointActor : IActor
    {
        private IServerStreamWriter<MessageBatch> responseStream;

        public ClientEndpointActor(IServerStreamWriter<MessageBatch> responseStream)
        {
            this.responseStream = responseStream;
        }

        public Task ReceiveAsync(IContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}