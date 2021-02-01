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
using Proto.Client.Tests.Fixtures;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Xunit;
using Xunit.Abstractions;

namespace Proto.Client.Tests
{
    [Collection("Remote Client Host collection")]
    public class BasicTests
    {
        RemoteClientHost _remoteClientHost;
        private ILogger _logger;

        public BasicTests(RemoteClientHost remoteClientHost, ITestOutputHelper testOutputHelper)
        {
            _remoteClientHost = remoteClientHost;
           
            var logFactory = LoggerFactory.Create(builder => {
                builder.AddXUnit(testOutputHelper);
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            Log.SetLoggerFactory(logFactory);

            _logger = Log.CreateLogger<BasicTests>();
            
            
           

           
        }

        [Fact(Timeout= 10000)]
        
        public async Task CanRespondToPing()
        {
            
            var tcs = new TaskCompletionSource<bool>();

            //Wait for the server to start
            await _remoteClientHost.hostStartTask;
            
            var clientConfig = GrpcNetRemoteConfig.BindToLocalhost(5001)
                .WithProtoMessages(Proto.Client.TestMessages.ProtosReflection.Descriptor);
            var client = new Client(new ActorSystem(), clientConfig, "127.0.0.1", 5000);
            var clientContext = await client.StartAsync();
   

             //Ideally this would be spawned remotely and actors could be spawned on clients too
            var echoPIDServer = _remoteClientHost.remoteSystem.Root.SpawnNamed(Props.FromFunc(ctx => {
                if(ctx.Message is Ping)
                {
                    _logger.LogDebug("Received Ping from {sender}", ctx.Sender.ToDiagnosticString());
                    
                    // logger.LogDebug("ProcessRegistry responds with {remoteprocess}", ctx.System.ProcessRegistry.Get(ctx.Sender));
                    ctx.Respond(new Pong());
                }
                return Task.CompletedTask;
            }), "echoer");

            var echoPID = new PID(echoPIDServer); //Must copy the pid otherwise it will send directly to the server process shortcircuiting the remote connection

            //Wait for server and client to start
            
            
            
            // var address = clientContext.System.Address;
            
            
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

        [Fact(Timeout= 10000)]
        public async Task CanStopActorOnClienthost()
        {
            var tcs = new TaskCompletionSource<bool>();

            await _remoteClientHost.hostStartTask;
            
            var clientConfig = GrpcNetRemoteConfig.BindToLocalhost(5001)
                .WithProtoMessages(Proto.Client.TestMessages.ProtosReflection.Descriptor);
            var client = new Client(new ActorSystem(), clientConfig, "127.0.0.1", 5000);
            var clientContext = await client.StartAsync();

            var stopperPIDServer = _remoteClientHost.remoteSystem.Root.SpawnNamed(Props.FromFunc(ctx => {
                if(ctx.Message is Stopped)
                {
                    tcs.SetResult(true);
                }
                return Task.CompletedTask;
            }), "stopper");
            var stopperPID = new PID(stopperPIDServer);

             //Wait for server and client to start
            
            
            clientContext.Stop(stopperPID);
            await tcs.Task;
            // throw new ApplicationException();

        }

        [Fact(Timeout= 10000)]
        public async Task CanWatchActorOnClienthost()
        {

            var tcs = new TaskCompletionSource<bool>();

            await _remoteClientHost.hostStartTask;
            
            var clientConfig = GrpcNetRemoteConfig.BindToLocalhost(5001)
                .WithProtoMessages(Proto.Client.TestMessages.ProtosReflection.Descriptor);
            var client = new Client(new ActorSystem(), clientConfig, "127.0.0.1", 5000);
            var clientContext = await client.StartAsync();

            var watchedPIDServer = _remoteClientHost.remoteSystem.Root.SpawnNamed(Props.FromFunc(ctx => {
         
                return Task.CompletedTask;
            }), "watched");
            var watchedPID = new PID(watchedPIDServer);

            var watcherPID = clientContext.Spawn(Props.FromFunc(ctx => {
                if(ctx.Message is Started){
                    ctx.Watch(watchedPID);
                    ctx.Stop(watchedPID);
                }
                if(ctx.Message is Terminated){
                    tcs.TrySetResult(true);
                }

                return Task.CompletedTask;
            }));
            
            await tcs.Task;
        }


        [Fact(Timeout= 10000)]
        public async Task CanStopActorOnClient()
        {
            var tcs = new TaskCompletionSource<bool>();

            await _remoteClientHost.hostStartTask;
            
            var clientConfig = GrpcNetRemoteConfig.BindToLocalhost(5001)
                .WithProtoMessages(Proto.Client.TestMessages.ProtosReflection.Descriptor);
            var client = new Client(new ActorSystem(), clientConfig, "127.0.0.1", 5000);
            var clientContext = await client.StartAsync();

            var clientStopper = clientContext.SpawnNamed(Props.FromFunc(ctx => {
                if(ctx.Message is Stopped)
                {
                    tcs.SetResult(true);
                }
                return Task.CompletedTask;
            }), "stopper");
            

             //Wait for server and client to start
            
            _remoteClientHost.remoteSystem.Root.Stop(clientStopper);
            
            await tcs.Task;


        }

        [Fact(Timeout= 10000)]
        public async Task CanWatchActorOnClient()
        {
              var tcs = new TaskCompletionSource<bool>();

            await _remoteClientHost.hostStartTask;
            
            var clientConfig = GrpcNetRemoteConfig.BindToLocalhost(5001)
                .WithProtoMessages(Proto.Client.TestMessages.ProtosReflection.Descriptor);
            var client = new Client(new ActorSystem(), clientConfig, "127.0.0.1", 5000);
            var clientContext = await client.StartAsync();

            var watchedClient = clientContext.SpawnNamed(Props.FromFunc(ctx => {
         
                return Task.CompletedTask;
            }), "watched");
            
            var watchedClientPID = new PID(watchedClient);

            var watcherPID = _remoteClientHost.remoteSystem.Root.Spawn(Props.FromFunc(ctx => {
                if(ctx.Message is Started){
                    ctx.Watch(watchedClientPID);
                    ctx.Stop(watchedClientPID);
                }
                if(ctx.Message is Terminated){
                    tcs.TrySetResult(true);
                }

                return Task.CompletedTask;
            }));
            
            await tcs.Task;
        }

        
    }
}
