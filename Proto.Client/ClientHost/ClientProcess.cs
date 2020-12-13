// using Microsoft.Extensions.Logging;
// using Proto.Remote;

// namespace Proto.Client
// {
//     internal class ClientProcess : Process
//     {
//         private static readonly ILogger _logger = Log.CreateLogger<ClientProcess>();

//         private PID _clientTargetPID;
//         private PID _endpointWriterPID;

//         public ClientProcess(PID pid)
//         {
//             _clientTargetPID = pid;
            
//             //Assume that this is a proper client process of the form $client/guid/...
//             var addressParts = pid.Id.Split('/');
//             var endpointID = $"{addressParts[0]}/{addressParts[1]}";
//             _endpointWriterPID = new PID(ProcessRegistry.Instance.Address, endpointID);
            
            
//         }

//         protected override void SendUserMessage(PID _, object message) => Send(message);

//         protected override void SendSystemMessage(PID _, object message) => Send(message);
        
//         private void Send(object envelope)
//         {
            
//             var (message, sender, header) = MessageEnvelope.Unwrap(envelope);
            
//             _logger.LogDebug($"Sending Client Message {message.GetType()} to {_clientTargetPID} via {_endpointWriterPID}");
            
            
//             var env = new RemoteDeliver(header, message, _clientTargetPID, sender, Serialization.DefaultSerializerId);
           
//             var messageBatch = new ClientMessageBatch
//             {
//                 Address = _endpointWriterPID.Address,
//                 Batch = env.getMessageBatch()
//             };
//             RootContext.Empty.Send(_endpointWriterPID, messageBatch);

           
            

            
            
            
            
//         }
//     }
// }