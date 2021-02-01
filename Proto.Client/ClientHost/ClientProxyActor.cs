using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Client.ClientHost
{
    //This is very similar to the Endpoitn Actor but doesn't use the connected events
    public class ClientProxyActor : IActor
    {
        private readonly ILogger Logger = Log.CreateLogger<ClientProxyActor>();
        private IServerStreamWriter<MessageBatch> _responseStream;
        private RemoteConfigBase _remoteConfig;
        private int _serializerId;
        private string _address;
        private readonly Dictionary<string, HashSet<PID>> _watchedActors = new();

        public ClientProxyActor(ClientDetails request, IServerStreamWriter<MessageBatch> responseStream, RemoteConfigBase remoteConfig)
        {
            this._responseStream = responseStream;
            this._remoteConfig = remoteConfig;
            this._serializerId = _remoteConfig.Serialization.DefaultSerializerId;
            this._address = request.ClientActorRoot;
        }

        public Task ReceiveAsync(IContext context) => 
            context.Message switch
            {
                RemoteTerminate msg          => RemoteTerminate(context, msg),
                EndpointErrorEvent msg       => EndpointError(msg),
                RemoteUnwatch msg            => RemoteUnwatch(context, msg),
                RemoteWatch msg              => RemoteWatch(context, msg),
                Restarting _                 => EndpointTerminated(context),
                Stopped _                    => EndpointTerminated(context),
                IEnumerable<RemoteDeliver> m => RemoteDeliver(m, context),
                _                            => Ignore
            };
        
        private Task EndpointError(EndpointErrorEvent evt) => throw evt.Exception;

        private Task EndpointTerminated(IContext context)
        {
            Logger.LogDebug("[ClientProxyActor] Handle terminated address {Address}", _address);
            foreach (var (id, pidSet) in _watchedActors)
            {
                var watcherPid = PID.FromAddress(context.System.Address, id);
                var watcherRef = context.System.ProcessRegistry.Get(watcherPid);

                if (watcherRef == context.System.DeadLetter) continue;

                foreach (var t in pidSet.Select(
                    pid => new Terminated
                    {
                        Who = pid,
                        Why = TerminatedReason.AddressTerminated
                    }
                ))
                {
                    //send the address Terminated event to the Watcher
                    watcherPid.SendSystemMessage(context.System, t);
                }
            }

            _watchedActors.Clear();
            return Task.CompletedTask;
            //TODO: Check the terminated behaviour - would be good to shut down the channel here
            //return ShutDownChannel();
        }

        private Task RemoteTerminate(IContext context, RemoteTerminate msg)
        {
            if (_watchedActors.TryGetValue(msg.Watcher.Id, out var pidSet))
            {
                pidSet.Remove(msg.Watchee);

                if (pidSet.Count == 0) _watchedActors.Remove(msg.Watcher.Id);
            }

            //create a terminated event for the Watched actor
            var t = new Terminated {Who = msg.Watchee};

            //send the address Terminated event to the Watcher
            msg.Watcher.SendSystemMessage(context.System, t);
            return Task.CompletedTask;
        }

        private Task RemoteUnwatch(IContext context, RemoteUnwatch msg)
        {
            if (_watchedActors.TryGetValue(msg.Watcher.Id, out var pidSet))
            {
                pidSet.Remove(msg.Watchee);

                if (pidSet.Count == 0) _watchedActors.Remove(msg.Watcher.Id);
            }

            var w = new Unwatch(msg.Watcher);
            RemoteDeliver(context, msg.Watchee, w);
            return Task.CompletedTask;
        }

        private Task RemoteWatch(IContext context, RemoteWatch msg)
        {
            if (_watchedActors.TryGetValue(msg.Watcher.Id, out var pidSet))
                pidSet.Add(msg.Watchee);
            else
                _watchedActors[msg.Watcher.Id] = new HashSet<PID> {msg.Watchee};

            var w = new Watch(msg.Watcher);
            RemoteDeliver(context, msg.Watchee, w);
            return Task.CompletedTask;
        }

        public void RemoteDeliver(IContext context, PID pid, object msg)
        {
            var (message, sender, header) = Proto.MessageEnvelope.Unwrap(msg);
            var env = new RemoteDeliver(header!, message, pid, sender!, -1);
            context.Send(context.Self!, env);
        }

        private Task RemoteDeliver(IEnumerable<RemoteDeliver> m, IContext context)
        {
            var envelopes = new List<Remote.MessageEnvelope>();
            var typeNames = new Dictionary<string, int>();
            var targetNames = new Dictionary<string, int>();
            var typeNameList = new List<string>();
            var targetNameList = new List<string>();

            foreach (var rd in m)
            {
                var targetName = rd.Target.Id;
                var serializerId = rd.SerializerId == -1 ? _serializerId : rd.SerializerId;

                if (!targetNames.TryGetValue(targetName, out var targetId))
                {
                    targetId = targetNames[targetName] = targetNames.Count;
                    targetNameList.Add(targetName);
                }

                var typeName = _remoteConfig.Serialization.GetTypeName(rd.Message, serializerId);

                if (!typeNames.TryGetValue(typeName, out var typeId))
                {
                    typeId = typeNames[typeName] = typeNames.Count;
                    typeNameList.Add(typeName);
                }

                Remote.MessageHeader? header = null;

                if (rd.Header != null && rd.Header.Count > 0)
                {
                    header = new Remote.MessageHeader();
                    header.HeaderData.Add(rd.Header.ToDictionary());
                }

                var bytes = _remoteConfig.Serialization.Serialize(rd.Message, serializerId);

                var envelope = new Remote.MessageEnvelope
                {
                    MessageData = bytes,
                    Sender = rd.Sender,
                    Target = targetId,
                    TypeId = typeId,
                    SerializerId = serializerId,
                    MessageHeader = header
                };

                envelopes.Add(envelope);
            }

            var batch = new MessageBatch();
            batch.TargetNames.AddRange(targetNameList);
            batch.TypeNames.AddRange(typeNameList);
            batch.Envelopes.AddRange(envelopes);

            // Logger.LogDebug("[ClientProxyActor] Sending {Count} envelopes for {Address}", envelopes.Count, _address);

            return SendEnvelopesAsync(batch, context);
        }

        private async Task SendEnvelopesAsync(MessageBatch batch, IContext context)
        {
            if (_responseStream == null)
            {
                Logger.LogError(
                    "[ClientProxyActor] gRPC Failed to send to address {Address}, reason No Connection available"
                    , _address
                );
                return;
            }

            try
            {
                Logger.LogDebug("[ClientProxyActor] Writing batch to {Address}", _address);
                
                await _responseStream.WriteAsync(batch).ConfigureAwait(false);
            }
            catch (Exception x)
            {
                Logger.LogError(x, "[ClientProxyActor] gRPC Failed to send to address {Address}, reason {Message}",
                    _address,
                    x.Message
                );
                context.Stash();
                throw;
            }
        }
        
        private static Task Ignore => Task.CompletedTask;
    }
}