namespace Proto.Client
{
    internal class ClientProcessRegistry : ProcessRegistry
    {
        private const string NoHost = "nonhost";

        public string BaseId = null;

        public override Process Get(PID pid){
            if ((pid.Address != NoHost && pid.Address != Address) || (BaseId != null && pid.Address == Address &! pid.Id.StartsWith(BaseId)) )
            {
                return new ClientHostProcess(pid);
            }
            return base.Get(pid);
        }
    }
}