// using System;

// namespace Proto.Client
// {
//     internal class ClientProcessRegistry : IProcessRegistry
//     {
//         private const string NoHost = "nonhost";

//         private IProcessRegistry _originalInstance;

//         public string BaseId {get; set;} = null;

//         public string Address 
//         { 
//             get { return _originalInstance.Address;} 
//             set { _originalInstance.Address = value;} 
//         }

        

//         public ClientProcessRegistry(IProcessRegistry originalInstance)
//         {
//             _originalInstance = originalInstance;
            
//         }

//         public Process Get(PID pid){
//             if ((pid.Address != NoHost && pid.Address != Address) || (BaseId != null && pid.Address == Address &! pid.Id.StartsWith(BaseId)) )
//             {
//                 return new ClientHostProcess(pid);
//             }
//             return _originalInstance.Get(pid);
//         }

//         public Process GetLocal(string id)
//         {
//             return _originalInstance.GetLocal(id);
//         }

//         public string NextId()
//         {
//             return _originalInstance.NextId();
//         }

//         public void RegisterHostResolver(Func<PID, Process> resolver)
//         {
//             _originalInstance.RegisterHostResolver(resolver);
//         }

//         public void Remove(PID pid)
//         {
//             _originalInstance.Remove(pid);
//         }

//         public (PID pid, bool ok) TryAdd(string id, Process process)
//         {
//             return _originalInstance.TryAdd(id, process);
//         }
//     }
// }