using System;
using System.Threading.Tasks;
using Proto.Remote;

namespace Proto.Client
{
    public class Client
    {
        private static ClientContext _clientContext;
        private static PID _channelmanager;
        private static string _hostname;
        private static RemoteConfig _config;
        private static TimeSpan _connectionTimeout;
        private static int _port;

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

            if (_channelmanager is null)
            {
                _channelmanager = RootContext.Empty.SpawnNamed(Props.FromProducer(() => new ClientChannelManager(_config, _connectionTimeout)), "client_channel_manager");
            }
            await RootContext.Empty.RequestAsync<EndpointConnectedEvent>(_channelmanager, $"{_hostname}:{_port}");
            var clientContext = new ClientContext(_channelmanager, OnContextDispose);

            _clientContext = clientContext;

            return clientContext;
        }

        private static void OnContextDispose()
        {
            _clientContext = null;
        }
    }
}
