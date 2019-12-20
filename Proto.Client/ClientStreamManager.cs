using System;
using System.Threading.Tasks;
using Grpc.Core;
using Proto.Remote;

namespace Proto.Client
{
    public class ClientStreamManager : IActor
    {
        private Channel channel;
        private string clientId;
        private TimeSpan connectionTimeout;
        private AsyncDuplexStreamingCall<ClientMessageBatch, MessageBatch> _clientStreams;
        private PID _endpointReader;

        public ClientStreamManager(Channel channel, string clientId, TimeSpan connectionTimeout)
        {
            this.channel = channel;
            this.clientId = clientId;
            this.connectionTimeout = connectionTimeout;
        }

        public Task ReceiveAsync(IContext context)
        {
            switch(context.Message){
                case Started _:
                    var connectionHeaders = new Metadata() {{"clientid", clientId}};
                    // _logger.LogDebug("Connectiing Streams");
                    var client = new ClientRemoting.ClientRemotingClient(channel);
                    _clientStreams = client.ConnectClient(connectionHeaders, DateTime.Now.Add(connectionTimeout));
                    //Assign a reader to the connection
                    
                    _endpointReader = context.Spawn(Props.FromProducer(() => new ClientEndpointReader(_clientStreams.ResponseStream)));
                        //Start this in a new process so the loop is not affected by parent processes shuttting down (eg. Orleans)
                        
                    context.SetReceiveTimeout(connectionTimeout);
                        
                        
                    
                    break;
            }
            return Actor.Done;
        }
    }
}