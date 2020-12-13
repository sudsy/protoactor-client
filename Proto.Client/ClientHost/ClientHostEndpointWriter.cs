// using System;
// using System.Net.NetworkInformation;
// using System.Threading.Tasks;
// using Grpc.Core;
// using Microsoft.Extensions.Logging;
// using Proto.Remote;

// namespace Proto.Client
// {
//     public class ClientHostEndpointWriter :IActor
//     {
//         private readonly IServerStreamWriter<MessageBatch> _responseStream;
//         private static readonly ILogger _logger = Log.CreateLogger<ClientHostEndpointWriter>();

//         public ClientHostEndpointWriter(IServerStreamWriter<MessageBatch> responseStream)
//         {
//             _responseStream = responseStream;
//         }
//         public async Task ReceiveAsync(IContext context)
//         {
//             _logger.LogDebug($"ClientHostEndpointwriter received {context.Message.GetType()}");
            
//             switch (context.Message)
//             {
//                 case Started started:
                    
//                     //Send a connection started message to be delivered over this response stream
//                     _logger.LogDebug($"{context.Self} returning CLientHostPIDResponse");
                    
//                     context.Send(context.Self, new  RemoteDeliver(null, new ClientHostPIDResponse(){HostProcess = context.Self}, context.Self, context.Self, Serialization.DefaultSerializerId));
//                     break;

//                 case ClientMessageBatch cmb:
                    
//                     await _responseStream.WriteAsync(cmb.Batch);
//                     break;

//                 case RemoteDeliver rd:
                    
//                     _logger.LogDebug($"Sending RemoteDeliver message {rd.Message.GetType()} to {rd.Target.Id} address {rd.Target.Address} from {rd.Sender}");

//                     var batch = rd.getMessageBatch();

//                     await _responseStream.WriteAsync(batch);
            
//                     _logger.LogDebug($"Sent RemoteDeliver message {rd.Message.GetType()} to {rd.Target.Id}");
                    
//                     break;
//             }

            
           

//         }

       
//     }
// }