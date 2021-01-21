using Microsoft.AspNetCore.Builder;

namespace Proto.Client 
{
    public static class Extensions 
    {
        public static void UseProtoClient(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<ClientRemoting.ClientRemotingBase>();
            });
            
        }
    }
}