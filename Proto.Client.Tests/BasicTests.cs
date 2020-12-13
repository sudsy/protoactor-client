using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proto.Client.TestMessages;
using Proto.Remote;
using Xunit;
using Xunit.Abstractions;

namespace Proto.Client.Tests
{
    [Collection("ClientTests")]
    public class BasicTests
    {
        
      
        public BasicTests(ITestOutputHelper testOutputHelper)
        {
            Serialization.RegisterFileDescriptor(Proto.Client.TestMessages.ProtosReflection.Descriptor);
            // _remoteManager = remoteManager;
           
            var logFactory = LoggerFactory.Create(builder => {
                builder.AddXUnit(testOutputHelper);
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            Log.SetLoggerFactory(logFactory);

        }

        [Fact (Timeout = 30000)]
        public async Task CanRespondToPing()
        {
            var tcs = new TaskCompletionSource<Pong>();
            var logger = Log.CreateLogger("CanCreateAndDisposeClientAsync");
            EventStream.Instance.Subscribe(msg => {
                logger.LogInformation(msg.ToString());
            });

            

            // Remote.Remote.Start("localhost", 44000);
            // ClientHost.Start("localhost", 55000);
            
            Client.ConfigureConnection("localhost", 55000, new RemoteConfig(), TimeSpan.FromSeconds(10));
            logger.LogInformation("Getting Client Context");
            var newClientContext = await Client.GetClientContext();

            var clientHostActor = new PID("localhost:44000", "EchoActorInstance");
            logger.LogInformation($"Client address is {ProcessRegistry.Instance.Address}");
            var localActor = newClientContext.Spawn(Props.FromFunc(ctx =>
            {
                switch(ctx.Message){
                    case Started _:
                        var ping = new Ping {
                            Message = "Hello"
                        };
                        ctx.Request(clientHostActor, ping);
                        break;
                    case Pong pongMessage:
                        tcs.SetResult(pongMessage);
                        ctx.Stop(ctx.Self);
                        break;

                }
                
                return Actor.Done;
            }));

               
            await tcs.Task;

            newClientContext.Dispose();

        }
    }
}
