using System.Threading.Tasks;
using Grpc.Core;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Proto.Client
{
    public class ClientChannelProvider : IChannelProvider
    {
        private GrpcNetRemoteConfig _remoteConfig;
        private GrpcNetChannelProvider _grpcNetChannelProvider;
        private ChannelBase? _channel;

        public ClientChannelProvider(GrpcNetRemoteConfig remoteConfig)
        {
            _remoteConfig = remoteConfig;

            _grpcNetChannelProvider = new GrpcNetChannelProvider(remoteConfig);
        }

        public ChannelBase GetChannel(string address)
        {
            if(_channel != null){
                return _channel;
            }
            _channel = _grpcNetChannelProvider.GetChannel(address);
            return _channel;
        }

        public async Task ShutdownChannelAsync(){
            if(_channel is null){
                return;
            }
            await _channel.ShutdownAsync();
            _channel = null;
            
        }
    }
}