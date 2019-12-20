using System;
using System.Threading.Tasks;
using Proto.Remote;
using Xunit;

namespace Proto.Client.Tests
{
    [Collection("ClientTests")]
    public class BasicTests
    {
        private readonly RemoteManager _remoteManager;

        public BasicTests(RemoteManager remoteManager)
        {
            _remoteManager = remoteManager;
        }

        [Fact (Timeout = 30)]
        public async Task CanCreateAndDisposeClientAsync()
        {
            Client.ConfigureConnection("127.0.0.1", 12000, new RemoteConfig(), TimeSpan.FromSeconds(10));
            
            var newClientContext = await Client.GetClientContext();

            newClientContext.Dispose();

        }
    }
}
