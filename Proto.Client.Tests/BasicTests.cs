using System;
using System.Threading.Tasks;
using Proto.Remote;
using Xunit;

namespace Proto.Client.Tests
{
    public class BasicTests
    {
        [Fact]
        public async Task CanCreateAndDisposeClientAsync()
        {
            Client.ConfigureConnection("127.0.0.1", 12000, new RemoteConfig(), TimeSpan.FromSeconds(10));
            
            var newClientContext = await Client.GetClientContext();

            newClientContext.Dispose();

        }
    }
}
