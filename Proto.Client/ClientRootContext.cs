using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proto.Client
{

    public class ClientRootContext : RootContextDecorator
    {
        private String _clientActorRoot;

        public ClientRootContext(IRootContext context, string clientActorRoot) : base(context)
        {
            _clientActorRoot = clientActorRoot;
        }

        public override PID Spawn(Props props)
        {
            var name = System.ProcessRegistry.NextId();
            return SpawnNamed(props, name);
        }

        public override PID SpawnNamed(Props props, string name)
        {
            return base.SpawnNamed(props, $"{_clientActorRoot}/{name}");
        }

        public override PID SpawnPrefix(Props props, string prefix)
        {
            var name = prefix + System.ProcessRegistry.NextId();
            return SpawnNamed(props, name);
        }
      
       
    }

}