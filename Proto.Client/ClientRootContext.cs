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


        public override PID SpawnNamed(Props props, string name)
        {
            return base.SpawnNamed(props, $"{_clientActorRoot}/{name}");
        }
      
       
    }

}