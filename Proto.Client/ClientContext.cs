using System;

namespace Proto.Client
{
    public class ClientContext : RootContext, IDisposable
    {
        private Action onDisposedAction;

        public ClientContext(PID channelManager, Action onDisposed)
        {
            this.onDisposedAction = onDisposed;
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