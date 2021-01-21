using Proto.Remote;

namespace Proto.Client
{
    public class RemoteClientHostProcess : Process
    {
        private IEndpointManager _endpointManager;
        private PID _pid;
        private PID? _endpoint;

        public RemoteClientHostProcess(ActorSystem system, IEndpointManager endpointManager, PID pid) : base(system)
        {
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
            _endpoint ??= _endpointManager.GetEndpoint(_pid.Address);
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

            System.Root.Send(_endpoint, message);
        }
    }

   
}