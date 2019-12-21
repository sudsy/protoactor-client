using System;
using System.Threading.Tasks;
using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;
using Proto.Remote;
using Xunit;
using Xunit.Abstractions;

namespace Proto.Client.Tests
{
    // [Collection("ClientTests")]
    public class BasicTests
    {
        private readonly RemoteManager _remoteManager;
        

        public BasicTests(ITestOutputHelper testOutputHelper)
        {
            // _remoteManager = remoteManager;
           
            var logFactory = LoggerFactory.Create(builder => {
                builder.AddXunit(testOutputHelper);
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            Log.SetLoggerFactory(logFactory);

        }

        [Fact (Timeout = 30000)]
        public async Task CanCreateAndDisposeClientAsync()
        {
            var logger = Log.CreateLogger("CanCreateAndDisposeClientAsync");
            EventStream.Instance.Subscribe(msg => {
                logger.LogInformation(msg.ToString());
            });

            Remote.Remote.Start("localhost", 44000);
            ClientHost.Start("localhost", 55000);
            
            Client.ConfigureConnection("localhost", 55000, new RemoteConfig(), TimeSpan.FromSeconds(10));
            logger.LogInformation("Getting Client Context");
            var newClientContext = await Client.GetClientContext();

            newClientContext.Dispose();

        }
    }
}
