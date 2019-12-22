using System;

namespace Proto.Client
{
    public class ClientContext : RootContext, IDisposable
    {
        private Action onDisposedAction;

        private ClientProcessRegistry clientProcessRegistry;

        public ClientContext(PID channelManager, Action onDisposed)
        {
            this.onDisposedAction = onDisposed;
            this.clientProcessRegistry = (ClientProcessRegistry)ProcessRegistry.Instance;
        }

        public new PID Spawn(Props props)
        {
            var name = ProcessRegistry.Instance.NextId();
            return SpawnNamed(props, name);
        }

        public new PID SpawnNamed(Props props, string name)
        {
            return base.SpawnNamed(props, $"{clientProcessRegistry.BaseId}/{name}");
        }

        public new PID SpawnPrefix(Props props, string prefix)
        {
            var name = prefix + ProcessRegistry.Instance.NextId();
            return SpawnNamed(props, name);
        }

        ~ClientContext(){
            ReleaseResources();
        }

        public void Dispose()
        {
            ReleaseResources();
        }

        private void ReleaseResources()
        {
            onDisposedAction();
        }
    }
}