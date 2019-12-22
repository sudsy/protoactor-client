using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Client
{
    public class ClientStreamManager : IActor, ISupervisorStrategy
    {
        private static readonly ILogger _logger = Log.CreateLogger<ClientStreamManager>();
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

        public void HandleFailure(ISupervisor supervisor, PID child, RestartStatistics rs, Exception cause, object message)
        {
            var errorMsg = cause.Message;
            //Escalate all failures
            supervisor.EscalateFailure(cause, message);
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch(context.Message){
                case Started _:
                    var connectionHeaders = new Metadata() {{"clientid", clientId}};
                    _logger.LogDebug("Connecting Streams");
                    var client = new ClientRemoting.ClientRemotingClient(channel);
                    _clientStreams = client.ConnectClient(connectionHeaders);//, DateTime.UtcNow.Add(connectionTimeout));
                    //Assign a reader to the connection
                    
                    _endpointReader = context.Spawn(Props.FromProducer(() => new ClientEndpointReader(_clientStreams.ResponseStream)));
                        //Start this in a new process so the loop is not affected by parent processes shuttting down (eg. Orleans)
                        
                    context.SetReceiveTimeout(connectionTimeout);
                        
                        
                    
                    break;
                case EndpointConnectedEvent _:
                    context.Forward(context.Parent);
                    break;
                case RemoteDeliver rd:
                    var batch = rd.getMessageBatch();
            
                    _logger.LogDebug($"Sending RemoteDeliver message {rd.Message.GetType()} to {rd.Target.Id} address {rd.Target.Address} from {rd.Sender}");
                
                    var clientBatch = new ClientMessageBatch()
                    {
                        Address = rd.Target.Address,
                        Batch = batch
                    };
                    try
                    {

                        await _clientStreams.RequestStream.WriteAsync(clientBatch);
                    }
                    catch (Exception ex)
                    {
                        context.Stash();
                        throw ex;
                    }
                    
                    
                    _logger.LogDebug($"Sent RemoteDeliver message {rd.Message.GetType()} to {rd.Target.Id} address {rd.Target.Address} from {rd.Sender}");
                    break;
            }
            
        }
    }
}