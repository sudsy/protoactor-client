// using System;

// namespace Proto.Client
// {
    
//     public record ClientContext : RootContext, IDisposable
//     {
//         private Action onDisposedAction;

//         private ClientProcessRegistry clientProcessRegistry;

//         private ActorSystem _actorSystem;

        

//         public ClientContext(ActorSystem actorSystem, PID channelManager, Action onDisposed) : base(actorSystem)
//         {
//             this._actorSystem = actorSystem;
//             this.onDisposedAction = onDisposed;
//             // this.clientProcessRegistry = (ClientProcessRegistry)ProcessRegistry.Instance;
            
//         }

//         public new PID Spawn(Props props)
//         {
//             var name = _actorSystem.ProcessRegistry.NextId();
//             return SpawnNamed(props, name);
//         }

//         public new PID SpawnNamed(Props props, string name)
//         {
//             return base.SpawnNamed(props, $"{clientProcessRegistry.BaseId}/{name}");
//         }

//         public new PID SpawnPrefix(Props props, string prefix)
//         {
//             var name = prefix + _actorSystem.ProcessRegistry.NextId();
//             return SpawnNamed(props, name);
//         }

//         ~ClientContext(){
//             ReleaseResources();
//         }

//         public void Dispose()
//         {
//             ReleaseResources();
//         }

//         private void ReleaseResources()
//         {
//             onDisposedAction();
//         }
//     }
// }