namespace Proto.Client
{
    internal class ClientHostProcessRegistry : ProcessRegistry
    {

         public override Process Get(PID pid){
            if (pid.Address == Address && pid.Id.StartsWith("$client/"))
            {
                return new ClientProcess(pid);
            }
            return base.Get(pid);
        }

    }
}