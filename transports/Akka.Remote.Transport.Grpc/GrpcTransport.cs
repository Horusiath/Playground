#region copyright
// -----------------------------------------------------------------------
//  <copyright file="GrpcTransport.cs" company="Akka.NET project">
//      Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2017 Akka.NET project <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Grpc.Core;
using Grpc.Core.Utils;

namespace Akka.Remote.Transport.Grpc
{
    public sealed class GrpcTransport : Transport
    {
        private readonly GrpcSettings settings;
        private readonly ActorSystem system;
        private readonly Method<byte[], byte[]> inboundMethod;
        private readonly Method<byte[], byte[]> outboundMethod;
        private readonly Server server;
        private readonly ConcurrentDictionary<Address, AsyncClientStreamingCall<byte[], byte[]>> connectedCalls;
        private bool disposing = false;

        public GrpcTransport(ActorSystem system, Config config)
        {
            this.system = system;
            this.settings = GrpcSettings.Create(config);
            this.connectedCalls = new ConcurrentDictionary<Address, AsyncClientStreamingCall<byte[], byte[]>>();

            this.inboundMethod = new Method<byte[], byte[]>(
                type: MethodType.ClientStreaming,
                serviceName: "AkkaGrpcTransport",
                name: "Receive",
                requestMarshaller: Marshallers.Create(
                    serializer: SerializeRequest,
                    deserializer: DeserializeRequest),
                responseMarshaller: Marshallers.Create(
                    serializer: SerializeResponse,
                    deserializer: DeserializeResponse));

            this.outboundMethod = new Method<byte[], byte[]>(
                type: MethodType.ClientStreaming,
                serviceName: "AkkaGrpcTransport",
                name: "Send",
                requestMarshaller: Marshallers.Create(
                    serializer: bytes => SerializeRequest(bytes),
                    deserializer: bytes => DeserializeRequest(bytes)),
                responseMarshaller: Marshallers.Create(
                    serializer: bytes => SerializeResponse(bytes),
                    deserializer: bytes => DeserializeResponse(bytes)));

            var serviceDefinition = ServerServiceDefinition.CreateBuilder()
                .AddMethod(inboundMethod, EndpointReceiver.Receive)
                .Build();
            this.server = new Server
            {
                Ports = { { settings.Host, settings.Port, ServerCredentials.Insecure } },
                Services = { serviceDefinition }
            };
        }

        public override async Task<Tuple<Address, TaskCompletionSource<IAssociationEventListener>>> Listen()
        {
            var promise = new TaskCompletionSource<IAssociationEventListener>();

            server.Start();

            var address = new Address("tcp", system.Name, settings.Host, settings.Port);
            return Tuple.Create(address, promise);
        }

        public override bool IsResponsibleFor(Address remote) => true;

        public override async Task<AssociationHandle> Associate(Address remoteAddress)
        {
            var channel = new Channel(remoteAddress.Host, remoteAddress.Port.Value, ChannelCredentials.Insecure);
            var invoker = new DefaultCallInvoker(channel);
            
        }

        public override async Task<bool> Shutdown()
        {
            if (!disposing)
            {
                disposing = true;

                var tasks = connectedCalls.Values.Select(call => call.RequestStream.CompleteAsync()).ToArray();
                await Task.WhenAll(tasks);

                connectedCalls.Clear();

                await server.ShutdownAsync();

                return true;
            }
            return true;
        }

        private static byte[] SerializeRequest(byte[] bytes) => bytes;
        private static byte[] DeserializeRequest(byte[] bytes) => bytes;
        private static byte[] SerializeResponse(byte[] bytes) => bytes;
        private static byte[] DeserializeResponse(byte[] bytes) => bytes;
    }
}