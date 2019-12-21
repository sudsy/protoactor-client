using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Client
{
    internal class ClientChannelManager : IActor, ISupervisorStrategy
    {
        private static readonly ILogger _logger = Log.CreateLogger<ClientChannelManager>();
        private RemoteConfig config;
        private TimeSpan connectionTimeout;
        private string _clientId;
        private int _backoff;
        private Random _random;
        private string _address;
        private PID _requestor;
        private Channel _channel;
        private CancellationTokenSource _cancelFutureRetries = new CancellationTokenSource();

        public ClientChannelManager(RemoteConfig config, TimeSpan connectionTimeout)
        {
            this.config = config;
            this.connectionTimeout = connectionTimeout;
            this._clientId = Guid.NewGuid().ToString();
            _backoff = config.EndpointWriterOptions.RetryBackOffms;
            _random = new Random();
        }

        public void HandleFailure(ISupervisor supervisor, PID child, RestartStatistics rs, Exception cause, object message)
        {
            if (ShouldStop(rs))
            {
                _logger.LogWarning($"Stopping {child.ToShortString()} connection to address {_address} after retries expired Reason {cause}");
                _cancelFutureRetries.Cancel();
                supervisor.StopChildren(child);
                ProcessRegistry.Instance.Remove(child); //TODO: work out why this hangs around in the process registry
                
                var terminated = new EndpointTerminatedEvent
                {
                    Address = _address
                };
                Actor.EventStream.Publish(terminated);
                
                //Reset everything to original values
                _backoff = config.EndpointWriterOptions.RetryBackOffms;
                _cancelFutureRetries = new CancellationTokenSource();
            }
            else
            {
                _backoff = _backoff * 2;
                var noise = _random.Next(_backoff);
                var duration = TimeSpan.FromMilliseconds(_backoff + noise);
                Task.Delay(duration).ContinueWith(t =>
                {
                    _logger.LogWarning($"Restarting {child.ToShortString()} for {_address} after {duration} Reason {cause}");
                    supervisor.RestartChildren(cause, child);
                }, _cancelFutureRetries.Token);
            }
        }

        private bool ShouldStop(RestartStatistics rs)
        {
            if (config.EndpointWriterOptions.MaxRetries == 0)
            {
                return true;
            }
            rs.Fail();
            
            if (rs.NumberOfFailures(config.EndpointWriterOptions.RetryTimeSpan) > config.EndpointWriterOptions.MaxRetries)
            {
                rs.Reset();
                return true;
            }
            
            return false;
        }

        public Task ReceiveAsync(IContext context)
        {
            switch(context.Message){
                case String address:
                    _address = address;
                    _requestor = context.Sender;
                    _channel = new Channel(address, config.ChannelCredentials, config.ChannelOptions);
                    // _logger.LogDebug("Creating Remoting Client");
                    var clientChannel = context.Spawn(Props.FromProducer(() => new ClientStreamManager(_channel, _clientId, connectionTimeout)));
                    
                    
                    
                    break;
                case EndpointConnectedEvent connectedEvent:
                    context.Send(_requestor, connectedEvent);
                    break;
            }
            return Actor.Done;
        }
    }
}