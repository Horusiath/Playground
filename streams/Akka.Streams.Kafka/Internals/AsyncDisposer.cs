#region copyright
// -----------------------------------------------------------------------
//  <copyright file="AsyncDisposer.cs" company="Akka.NET project">
//      Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2017 Akka.NET project <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Threading.Tasks;
using Akka.Streams.Stage;

namespace Akka.Streams.Kafka.Internals
{
    internal abstract class AsyncDisposer : GraphStageLogic, IAsyncDisposable
    {
        public AsyncDisposer(int inCount, int outCount) : base(inCount, outCount)
        {
        }

        public AsyncDisposer(Shape shape) : base(shape)
        {
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public Task DisposeAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}