using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proto.Client.ClientHost;
using Proto.Client.TestMessages;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Xunit;
using Xunit.Abstractions;

namespace Proto.Client.Tests
{
    [Collection("ClientTests")]
    public class BasicTests
    {
        private Task<IHost> _hostStartTask;
        // private HostedGrpcNetRemote _hostRemote;
        private Client _client;
        private Task<ClientRootContext> _clientRootContextTask;
        private ActorSystem _remoteSystem;
        private PID _echoPID;
        private ILogger _logger;

        public BasicTests(ITestOutputHelper testOutputHelper)
        {
            // Serialization.RegisterFileDescriptor(Proto.Client.TestMessages.ProtosReflection.Descriptor);
            // // _remoteManager = remoteManager;
           
            var logFactory = LoggerFactory.Create(builder => {
                builder.AddXUnit(testOutputHelper);
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            Log.SetLoggerFactory(logFactory);

            _logger = Log.CreateLogger<BasicTests>();
            var serverConfig = GrpcNetRemoteConfig.BindToLocalhost(5000)
                .WithProtoMessages(Proto.Client.TestMessages.ProtosReflection.Descriptor);
                // .WithRemoteKinds(("EchoActor", EchoActorProps));;
            _remoteSystem = new ActorSystem();
            
            
            var hostBuilder = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices(services =>
                    {
                        services.AddGrpc();
                        services.AddSingleton(Log.GetLoggerFactory());
                        services.AddSingleton(sp => _remoteSystem);
                        services.AddRemote(serverConfig);
                    }
                )
                .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(kestrelServerOptions =>
                                {
                                    kestrelServerOptions.Listen(IPAddress.Parse(serverConfig.Host), serverConfig.Port,
                                        listenOption => { listenOption.Protocols = HttpProtocols.Http2; }
                                    );
                                }
                            )
                            .Configure(app =>
                                {
                                    app.UseRouting();
                                    app.UseProtoClient();
                                }
                            );
                    }
                );
            _hostStartTask = hostBuilder.StartAsync();
            
            //Ideally this would be spawned remotely and actors could be spawned on clients too
            _echoPID = _remoteSystem.Root.SpawnNamed(Props.FromFunc(ctx => {
                if(ctx.Message is Ping){
                    _logger.LogDebug("Received Ping from {sender}", ctx.Sender.ToDiagnosticString());
                    
                    // logger.LogDebug("ProcessRegistry responds with {remoteprocess}", ctx.System.ProcessRegistry.Get(ctx.Sender));
                    ctx.Respond(new Pong());
                }
                return Task.CompletedTask;
            }), "echoer");

            var clientConfig = GrpcNetRemoteConfig.BindToLocalhost(5000)
                .WithProtoMessages(Proto.Client.TestMessages.ProtosReflection.Descriptor);
            _client = new Client(new ActorSystem(), clientConfig, "127.0.0.1", 5000);
            _clientRootContextTask = _client.StartAsync();
        }

        [Fact(Timeout= 10000)]
        
        public async Task CanRespondToPing()
        {
            
            var tcs = new TaskCompletionSource<bool>();
            //Wait for server and client to start
            await _hostStartTask;
            var clientContext = await _clientRootContextTask;
            
            // var address = clientContext.System.Address;
            var echoPID = new PID(_echoPID.Address, _echoPID.Id);
            var pinger = clientContext.Spawn(Props.FromFunc(ctx => {
                if(ctx.Message is Started){
                    _logger.LogDebug("Sent Ping to {pid}", echoPID);
                    //Need to convert to a new PID otherwise the logic gets short circuited
                    ctx.Request(echoPID, new Ping());
                    
                }

                if(ctx.Message is Pong){
                    tcs.SetResult(true);
                }
                return Task.CompletedTask;
            }));
            
            // Console.WriteLine(result);
            await tcs.Task;
            
        }

        // [Fact (Timeout = 30000)]
        // public async Task CanRespondToPing()
        // {
        //     var tcs = new TaskCompletionSource<Pong>();
        //     var logger = Log.CreateLogger("CanCreateAndDisposeClientAsync");
        //     EventStream.Instance.Subscribe(msg => {
        //         logger.LogInformation(msg.ToString());
        //     });

            

        //     // Remote.Remote.Start("localhost", 44000);
        //     // ClientHost.Start("localhost", 55000);
            
        //     Client.ConfigureConnection("localhost", 55000, new RemoteConfig(), TimeSpan.FromSeconds(10));
        //     logger.LogInformation("Getting Client Context");
        //     var newClientContext = await Client.GetClientContext();

        //     var clientHostActor = new PID("localhost:44000", "EchoActorInstance");
        //     logger.LogInformation($"Client address is {ProcessRegistry.Instance.Address}");
        //     var localActor = newClientContext.Spawn(Props.FromFunc(ctx =>
        //     {
        //         switch(ctx.Message){
        //             case Started _:
        //                 var ping = new Ping {
        //                     Message = "Hello"
        //                 };
        //                 ctx.Request(clientHostActor, ping);
        //                 break;
        //             case Pong pongMessage:
        //                 tcs.SetResult(pongMessage);
        //                 ctx.Stop(ctx.Self);
        //                 break;

        //         }
                
        //         return Actor.Done;
        //     }));

               
        //     await tcs.Task;

        //     newClientContext.Dispose();

        // }
    }
}
