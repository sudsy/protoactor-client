using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proto.Client
{

    public class ClientRootContext : RootContextDecorator
    {
        private Guid _clientGUID;

        public ClientRootContext(IRootContext context, Guid clientGUID) : base(context)
        {
            _clientGUID = clientGUID;
        }


        public override PID SpawnNamed(Props props, string name)
        {
            return base.SpawnNamed(props, $"$clients/{_clientGUID}/{name}");
        }
      
       
    }

}