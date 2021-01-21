using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proto.Remote.GrpcNet;

namespace Proto.Client
{

    public class Client 
    {
        private ActorSystem System;
        private GrpcNetRemoteConfig _config;
        public bool Started { get; private set; }
        private readonly ILogger _logger = Log.CreateLogger<Client>();

        public Client(ActorSystem system, GrpcNetRemoteConfig config)
        {
            System = system;
            _config = config;
            
            System.Extensions.Register(config.Serialization);
        }


        public Task StartAsync()
        {
            lock (this)
            {
                if (Started)
                    return Task.CompletedTask;
                var channelProvider = new GrpcNetChannelProvider(_config);
                // _endpointManager = new EndpointManager(System, Config, channelProvider);
                // _endpointReader = new EndpointReader(System, _endpointManager, Config.Serialization);
                // _healthCheck = new HealthServiceImpl();

               
                
               
                // System.SetAddress(Config.AdvertisedHost ?? Config.Host,
                //     Config.AdvertisedPort ?? boundPort
                // );
                // _endpointManager.Start();
                // _logger.LogInformation("Starting Proto.Client server on {Host}:{Port} ({Address})", Config.Host,
                //     Config.Port, System.Address
                // );
                Started = true;
                return Task.CompletedTask;
            }
        }
    }

}