using System;

namespace Proto.Client
{
    internal class ClientHostProcessRegistry : IProcessRegistry
    {
        private IProcessRegistry _originalInstance;

        public ClientHostProcessRegistry(IProcessRegistry originalInstance)
        {
            _originalInstance = originalInstance;
            
        }

        public string Address 
        { 
            get { return _originalInstance.Address;} 
            set { _originalInstance.Address = value;} 
        }

        public Process Get(PID pid){
            if (pid.Address == Address && pid.Id.StartsWith("$client/") & pid.Id.Length > 44) // 44 is the length with the GUID
            {
                return new ClientProcess(pid);
            }
            return _originalInstance.Get(pid);
        }

        public Process GetLocal(string id)
        {
            return _originalInstance.GetLocal(id);
        }

        public string NextId()
        {
            return _originalInstance.NextId();
        }

        public void RegisterHostResolver(Func<PID, Process> resolver)
        {
            _originalInstance.RegisterHostResolver(resolver);
        }

        public void Remove(PID pid)
        {
            _originalInstance.Remove(pid);
        }

        public (PID pid, bool ok) TryAdd(string id, Process process)
        {
            return _originalInstance.TryAdd(id, process);
        }
    }
}