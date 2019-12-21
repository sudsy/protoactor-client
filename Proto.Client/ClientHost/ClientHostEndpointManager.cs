using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;
using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Client
{
    public class ClientHostEndpointManager: ClientRemoting.ClientRemotingBase
    {
        
        private static readonly ILogger _logger = Log.CreateLogger<ClientHostEndpointManager>();
        
        public ClientHostEndpointManager()
        {
            
          
            
        }
        
        
        private readonly string _clientHostAddress;

        public override async Task ConnectClient(IAsyncStreamReader<ClientMessageBatch> requestStream,
            IServerStreamWriter<MessageBatch> responseStream, ServerCallContext context)
        {
            
            _logger.LogDebug($"Spawning Client EndpointWriter");

            _logger.LogDebug($"Request headers count is {context.RequestHeaders.Count} - {context.RequestHeaders.Select(entry => entry.Key + ":" + entry.Value).Aggregate((acc, value) => acc + "," + value)}");
            var clientIdHeader = context.RequestHeaders.FirstOrDefault(entry => entry.Key == "clientid");
            var clientId = clientIdHeader?.Value;
            if (clientId == null)
            {
                clientId = Guid.NewGuid().ToString();
                _logger.LogWarning($"clientId header is not set - generating random client id {clientId}");
                
            }

          
            var clientHostEndpointWriter = await SpawnClientHostEndpointWriter(responseStream, clientId);
            
            
            try
            {
                while (await requestStream.MoveNext())
                {
                    var clientMessageBatch = requestStream.Current;

                    var targetAddress = clientMessageBatch.Address;

                    _logger.LogDebug($"Received Batch for {targetAddress}");

                    foreach (var envelope in clientMessageBatch.Batch.Envelopes)
                    {

                        var message = Serialization.Deserialize(clientMessageBatch.Batch.TypeNames[envelope.TypeId],
                            envelope.MessageData, envelope.SerializerId);

                        _logger.LogDebug($"Batch Message {message.GetType()}");

                        var target = new PID(targetAddress, clientMessageBatch.Batch.TargetNames[envelope.Target]);

                      
                        _logger.LogDebug($"Target is {target}");

                       

                        //Forward the message to the correct endpoint
                        Proto.MessageHeader header = null;
                        if (envelope.MessageHeader != null)
                        {
                            header = new Proto.MessageHeader(envelope.MessageHeader.HeaderData);
                        }

                        var forwardingEnvelope = new Proto.MessageEnvelope(message, envelope.Sender, header);

                        _logger.LogDebug($"Sending message {message.GetType()} to target {target} from {envelope.Sender}");
                        
                        RootContext.Empty.Send(target, forwardingEnvelope);



                    }
                }

                _logger.LogDebug("Finished Request Stream - stopping connection manager");
                
                await clientHostEndpointWriter.PoisonAsync();
                _logger.LogDebug("Client Endpoint manager shut down");
                
            }   
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Exception on Client Host");
                throw ex;
            }

            

              
        }

      

        private PID SpawnClientHostAddressResponder()
        {
            return RootContext.Empty.Spawn(Props.FromFunc(context =>
            {
//                if (context.Message is ClientHostAddressRequest)
//                {
//                    context.Respond(new ClientHostAddressResponse(){Address = ProcessRegistry.Instance.Address});
//                }

                return Actor.Done;
            }));
        }


        private static async Task<PID> SpawnClientHostEndpointWriter(IServerStreamWriter<MessageBatch> responseStream, string clientId)
        {
            try
            {
                var endpointWriter = RootContext.Empty.SpawnNamed(
                    Props.FromProducer(() => new ClientHostEndpointWriter(responseStream))
                        .WithGuardianSupervisorStrategy(Supervision.AlwaysRestartStrategy), clientId);

                return endpointWriter;

            }
            catch (ProcessNameExistException)
            {
                _logger.LogDebug("Existing endpointwriter found - waiting for shutdown");
                //Still hanging around from last connection
                var endpointWriterPID = new PID {Address = ProcessRegistry.Instance.Address, Id = clientId};
                endpointWriterPID.Stop();
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                _logger.LogDebug("Paused for 100 msec to allow shutdown");
                return await SpawnClientHostEndpointWriter(responseStream, clientId);
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex, "Exception while spawning endpoint writer");
                throw ex;
            }
            
            
//            var props = Props.FromProducer(() => new ProxyActivator(endpointWriter)).WithGuardianSupervisorStrategy(Supervision.AlwaysRestartStrategy);
//            return RootContext.Empty.Spawn(props);
        }

        
    }
}