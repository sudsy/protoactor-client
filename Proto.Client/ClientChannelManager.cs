using System;
using System.Threading.Tasks;
using Grpc.Core;
using Proto.Remote;

namespace Proto.Client
{
    internal class ClientChannelManager : IActor
    {
        private RemoteConfig config;
        private TimeSpan connectionTimeout;
        private string _clientId;
        private PID _requestor;
        private Channel _channel;
        

        public ClientChannelManager(RemoteConfig config, TimeSpan connectionTimeout)
        {
            this.config = config;
            this.connectionTimeout = connectionTimeout;
            this._clientId = Guid.NewGuid().ToString();
        }

        public Task ReceiveAsync(IContext context)
        {
            switch(context.Message){
                case String address:
                    _requestor = context.Sender;
                    _channel = new Channel(address, config.ChannelCredentials, config.ChannelOptions);
                    // _logger.LogDebug("Creating Remoting Client");
                    var clientChannel = context.Spawn(Props.FromProducer(() => new ClientStreamManager(_channel, _clientId, connectionTimeout)));
                    
                    
                    
                    break;
                case EndpointConnectedEvent connectedEvent:
                    context.Send(_requestor, connectedEvent);
                    break;
            }
            return Actor.Done;
        }
    }
}