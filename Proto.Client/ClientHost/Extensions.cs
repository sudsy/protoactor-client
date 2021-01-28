using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Proto.Client.ClientHost
{
    public static class Extensions 
    {
        public static void UseProtoClient(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseProtoRemote();
            applicationBuilder.UseEndpoints(endpoints =>
            {
                var system = applicationBuilder.ApplicationServices.GetRequiredService<ActorSystem>();
                var remoteConfig = applicationBuilder.ApplicationServices.GetRequiredService<GrpcNetRemoteConfig>();
                var clientMessageSenderService = new ClientMessageSenderService(system, remoteConfig);
                ClientRemoting.BindService(clientMessageSenderService);

                endpoints.MapGrpcService<ClientRemoting.ClientRemotingBase>();
            });
            
        }
    }
}