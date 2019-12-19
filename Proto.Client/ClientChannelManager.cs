using System;
using System.Threading.Tasks;
using Proto.Remote;

namespace Proto.Client
{
    internal class ClientChannelManager : IActor
    {
        private RemoteConfig config;
        private TimeSpan connectionTimeout;

        public ClientChannelManager(RemoteConfig config, TimeSpan connectionTimeout)
        {
            this.config = config;
            this.connectionTimeout = connectionTimeout;
        }

        public Task ReceiveAsync(IContext context)
        {
            if(context.Message is String address){
                // context.
            }
            return Actor.Done;
        }
    }
}