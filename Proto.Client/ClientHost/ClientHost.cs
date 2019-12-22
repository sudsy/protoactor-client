using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto.Remote;


namespace Proto.Client
{
    public static class ClientHost 
    {
        private static readonly ILogger _logger = Log.CreateLogger(typeof(ClientHost).FullName);
        private static Server _server;

        static ClientHost()
        {
            Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
        }
        
        public static void Start(string hostname, int port)
        {
            Start(hostname, port, new RemoteConfig());
        }
        public static void Start(string hostname, int port, RemoteConfig config){
                        
            ProcessRegistry.Instance = new ClientHostProcessRegistry();
            var clientEndpointManager = new ClientHostEndpointManager();
            

            _server = new Server(config.ChannelOptions)
            {
                Services = { ClientRemoting.BindService(clientEndpointManager) },
                Ports = { new ServerPort(hostname, port, config.ServerCredentials) }
               
            };

            _logger.LogDebug($"Starting Proto.ClientHost on {hostname}:{port}");
            _server.Start();

            

            
        }

        public static void Shutdown(bool gracefull = true)
        {
            try
            {
                if (gracefull)
                {
                    _server.ShutdownAsync().Wait(10000);
                }
                else
                {
                    _server.KillAsync().Wait(10000);
                }
                
                _logger.LogDebug($"Proto.Actor ClientHost stopped on {ProcessRegistry.Instance.Address}. Graceful:{gracefull}");
            }
            catch(Exception ex)
            {
                _server.KillAsync().Wait(1000);
                _logger.LogError($"Proto.Actor ClientHost stopped on {ProcessRegistry.Instance.Address} with error:\n{ex.Message}");
            }
        }
        
       
        
        
    }
}