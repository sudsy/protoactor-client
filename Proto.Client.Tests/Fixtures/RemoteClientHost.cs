using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proto.Client.ClientHost;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Xunit;

namespace Proto.Client.Tests.Fixtures
{
    public class RemoteClientHost : IDisposable
    {
        public ActorSystem remoteSystem;
        public Task<IHost> hostStartTask;

        public RemoteClientHost()
        {
            var serverConfig = GrpcNetRemoteConfig.BindToLocalhost(5000)
                .WithProtoMessages(Proto.Client.TestMessages.ProtosReflection.Descriptor);
                // .WithRemoteKinds(("EchoActor", EchoActorProps));;
            remoteSystem = new ActorSystem();
            
            
            var hostBuilder = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices(services =>
                    {
                        services.AddGrpc();
                        services.AddSingleton(Log.GetLoggerFactory());
                        services.AddSingleton(sp => remoteSystem);
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
            hostStartTask = hostBuilder.StartAsync();
        }

        public void Dispose()
        {
            var host = hostStartTask.GetAwaiter().GetResult();
            host.StopAsync().GetAwaiter().GetResult();
        }
    }

    [CollectionDefinition("Remote Client Host collection")]
    public class RemoteClientHostCollection : ICollectionFixture<RemoteClientHost>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}

