using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proto.Remote.GrpcNet;

namespace Proto.Client.TestNode
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            GrpcNetRemoteConfig config = GrpcNetRemoteConfig.BindToLocalhost();
            services.AddGrpc();
            // services.AddSingleton(Log.GetLoggerFactory());
            services.AddSingleton(sp => new ActorSystem());
            services.AddRemote(config);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseProtoRemote(); //This is a requirement of ProtoClient - bundle it in
            app.UseProtoClient(); 

            
        }
    }
}
