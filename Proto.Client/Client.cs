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
        private ClientChannelProvider _channelProvider;
        private ClientSendEndpointManager? _clientSendEndpointManager;
        private int _clientHostPort;
        private string _clientActorRoot;
        private string _clientHostAddress;
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
            _clientHostAddress = $"{_clientHost}:{_clientHostPort}";
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
                _channelProvider = new ClientChannelProvider(_config);
                
                //Set up the sending connection using the existing Proto.Remote system
                _clientSendEndpointManager = new ClientSendEndpointManager(System, Config, _channelProvider, _clientHostAddress);
                
                //Set up the receiving connection
                _clientReceiveEndpointReader = new ClientReceiveEndpointReader(System, Config, _channelProvider, _clientSendEndpointManager, _clientHostAddress, _clientActorRoot);
              
                
              
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

        public async Task StopAsync()
        {
           if(_clientSendEndpointManager is null){
               return;
           }
            //Shut down the connections here
            _clientReceiveEndpointReader?.Stop();
            await _clientSendEndpointManager?.StopAsync(); //This will also shutdown the channel
            
            
        }
    }

}