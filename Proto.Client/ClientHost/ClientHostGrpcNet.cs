using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proto.Remote;


namespace Proto.Client
{
    public class ClientHostGrpcNet : IRemote
    {

        private static readonly ILogger _logger = Log.CreateLogger<ClientHostGrpcNet>();
        private readonly ClientHostGrpcNetRemoteConfig _config;
        
        public bool Started { get; private set; }
        public RemoteConfigBase Config => _config;
        public ActorSystem System { get; }

        //Don't know about this being static, should see if we can match the way remote works
        public ClientHostGrpcNet(ActorSystem system, ClientHostGrpcNetRemoteConfig config)
        {
            System = system;
            _config = config;
            System.Extensions.Register(this);
            System.Extensions.Register(config.Serialization);
        }
        

    
       

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

         public Task ShutdownAsync(bool graceful = true)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(PID pid, object msg, int serializerId)
        {
            //As far as I can see this is retired
            throw new NotImplementedException();
        }
    }
}