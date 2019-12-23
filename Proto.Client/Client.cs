using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Client
{
    public class Client
    {
        private static readonly ILogger _logger = Log.CreateLogger<Client>();
        private static ClientContext _clientContext;
        private static PID _channelmanager;
        private static string _hostname;
        private static RemoteConfig _config;
        private static TimeSpan _connectionTimeout;
        private static int _port;
        
        static Client(){
            Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
        }
        
        public static void ConfigureConnection(string hostname, int port, RemoteConfig config, TimeSpan connectionTimeout)
        {
            _port = port;
            _hostname = hostname;
            _config = config;
            _connectionTimeout = connectionTimeout;
            
        }

      

        public static async Task<ClientContext> GetClientContext()
        {
            if (_clientContext != null)
            {
                return _clientContext;
            }

            ProcessRegistry.Instance = new ClientProcessRegistry(ProcessRegistry.Instance);

            if (_channelmanager is null)
            {
                _channelmanager = RootContext.Empty.SpawnNamed(Props.FromProducer(() => new ClientChannelManager(_config, _connectionTimeout)), "client_channel_manager");
            }
            await RootContext.Empty.RequestAsync<EndpointConnectedEvent>(_channelmanager, $"{_hostname}:{_port}");
            var clientContext = new ClientContext(_channelmanager, OnContextDispose);

            _clientContext = clientContext;

            

            return clientContext;
        }

        internal static void SendMessage(PID target, object envelope, int serializerId)
        {

            
            var (message, sender, header) = MessageEnvelope.Unwrap(envelope);
            
            
            var env = new RemoteDeliver(header, message, target, sender, serializerId);

            if (_channelmanager == null)
            {
                _logger.LogWarning("Tried to deliver message when clientEndpoint manager was unavailable");
                EventStream.Instance.Publish(new DeadLetterEvent(target, message, sender));
                // throw new ApplicationException("Could not send message, no connection available.");
            }
            else
            {
                RootContext.Empty.Send(_channelmanager, env);    
            }
            

        }

        private static void OnContextDispose()
        {
            _clientContext = null;
        }
    }
}
