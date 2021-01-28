using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Proto.Client
{

    public class Client 
    {
        private ActorSystem System;
        private GrpcNetRemoteConfig _config;
        private string _clientHost;
        private ClientReceiveEndpointReader? _clientReceiveEndpointReader;
        private IEndpointManager? _clientSendEndpointManager;
        private int _clientHostPort;
        private string _clientActorRoot;
        private ClientRootContext _clientRootContext;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1,1);

        public bool Started { get; private set; }
        private readonly ILogger _logger = Log.CreateLogger<Client>();

        public RemoteConfigBase Config => _config;

        public Client(ActorSystem system, GrpcNetRemoteConfig config, string clientHost, int clientHostPort)
        {
            System = system;
            _config = config;
            _clientHost = clientHost;
            _clientHostPort = clientHostPort;
            var clientGUID = Guid.NewGuid();
            _clientActorRoot = $"$clients/{clientGUID}";
            _clientRootContext = new ClientRootContext(system.Root, _clientActorRoot);
            System.Extensions.Register(config.Serialization);
        }


        public async Task<ClientRootContext> StartAsync()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (Started)
                    return _clientRootContext;
                var channelProvider = new GrpcNetChannelProvider(_config);
                
                //Set up the sending connection using the existing Proto.Remote system
                _clientSendEndpointManager = new ClientSendEndpointManager(System, Config, channelProvider, $"{_clientHost}:{_clientHostPort}");
                
                //Set up the receiving connection
                _clientReceiveEndpointReader = new ClientReceiveEndpointReader(System, Config, channelProvider, _clientSendEndpointManager, $"{_clientHost}:{_clientHostPort}", _clientActorRoot);
              
                
              
                // _healthCheck = new HealthServiceImpl(); //we really want a health check to shut down the connection on the server in case the client disappears

                await _clientReceiveEndpointReader.ConnectAsync();
           
                
               
                System.SetAddress(_clientHost, _clientHostPort);
                
                _clientSendEndpointManager.Start();
                // _logger.LogInformation("Starting Proto.Client server on {Host}:{Port} ({Address})", Config.Host,
                //     Config.Port, System.Address
                // );
                Started = true;
                return _clientRootContext;
                
            }finally{
                semaphoreSlim.Release();
            }

            
            
                
        }

        public Task StopAsync()
        {
            //Shut down the connections here
            return Task.CompletedTask;
        }
    }

}