namespace Proto.Client
{
    internal class ClientHostProcessRegistry : ProcessRegistry
    {
        
        public ClientHostProcessRegistry(ProcessRegistry originalInstance)
        {
            this.Address = originalInstance.Address; 
        }

        public override Process Get(PID pid){
            if (pid.Address == Address && pid.Id.StartsWith("$client/") & pid.Id.Length > 44) // 44 is the length with the GUID
            {
                return new ClientProcess(pid);
            }
            return base.Get(pid);
        }

    }
}