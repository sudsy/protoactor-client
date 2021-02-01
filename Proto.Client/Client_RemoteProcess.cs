using Proto.Remote;
using Microsoft.Extensions.Logging;

namespace Proto.Client
{
    public class Client_RemoteProcess : Process
    {
        private readonly ILogger Logger = Log.CreateLogger<Client_RemoteProcess>();
        private IEndpointManager _endpointManager;
        private PID _pid;
        private PID? _endpoint;

        public Client_RemoteProcess(ActorSystem system, IEndpointManager endpointManager, PID pid) : base(system)
        {
            Logger.LogDebug("[Client_RemoteProcess] created for {pid}", pid);
            //This is identical to RemoteProcess except for the use of IEndpointManager
            //TODO, implement IEndpointmanager in protoactor
            _endpointManager = endpointManager;
            _pid = pid;
        }

        protected override void SendUserMessage(PID _, object message) => Send(message);

        protected override void SendSystemMessage(PID _, object message) => Send(message);


         private void Send(object msg)
        {
            
            object message;
            _endpoint ??= _endpointManager.GetEndpoint(_pid);
            Logger.LogDebug("[Client_RemoteProcess] try to send to {pid} using {Endpoint}", _pid, _endpoint);
            switch (msg)
            {
                case Watch w:
                    if (_endpoint is null)
                    {
                        System.Root.Send(w.Watcher, new Terminated {Why = TerminatedReason.AddressTerminated, Who = _pid});
                        return;
                    }

                    message = new RemoteWatch(w.Watcher, _pid);
                    break;
                case Unwatch uw:
                    if (_endpoint is null) return;
                    message = new RemoteUnwatch(uw.Watcher, _pid);
                    break;
                default:
                    var (m, sender, header) = Proto.MessageEnvelope.Unwrap(msg);
                    if (_endpoint is null)
                    {
                        System.EventStream.Publish(new DeadLetterEvent(_pid, m, sender));
                        return;
                    }

                    message = new RemoteDeliver(header!, m, _pid, sender!, -1);
                    break;
            }
            Logger.LogDebug("[Client_RemoteProcess] Sending remote message to {Endpoint}", _endpoint);
            System.Root.Send(_endpoint, message);
        }
    }

   
}