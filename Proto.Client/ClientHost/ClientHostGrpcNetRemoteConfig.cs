
using Proto.Remote;
// using Grpc.Net.Client;
using System;
// using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Proto.Client
{
    public record ClientHostGrpcNetRemoteConfig : RemoteConfigBase
    {
        protected ClientHostGrpcNetRemoteConfig(string host, int port) : base(host, port)
        {
        }

        public bool UseHttps { get; init; }
        // public GrpcChannelOptions ChannelOptions { get; init; } = new();
        // public Action<ListenOptions>? ConfigureKestrel { get; init; }

        public static ClientHostGrpcNetRemoteConfig BindToAllInterfaces(string advertisedHost, int port = 0) =>
            new ClientHostGrpcNetRemoteConfig(AllInterfaces, port).WithAdvertisedHost(advertisedHost);

        public static ClientHostGrpcNetRemoteConfig BindToLocalhost(int port = 0) => new(Localhost, port);

        public static ClientHostGrpcNetRemoteConfig BindTo(string host, int port = 0) => new(host, port);
    }
}