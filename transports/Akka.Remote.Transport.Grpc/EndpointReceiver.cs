#region copyright
// -----------------------------------------------------------------------
//  <copyright file="EndpointReceiver.cs" company="Akka.NET project">
//      Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2017 Akka.NET project <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;

namespace Akka.Remote.Transport.Grpc
{
    public static class EndpointReceiver
    {
        private static readonly byte[] EmptyBytes = new byte[0];

        public static async Task<byte[]> Receive(IAsyncStreamReader<byte[]> requestStream, ServerCallContext context)
        {
            await requestStream.ForEachAsync(bytes =>
            {
                throw new NotImplementedException();
            });

            return EmptyBytes;
        }
    }
}