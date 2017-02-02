using System;
using System.Threading.Tasks;

namespace Akka.Streams.Kafka
{
    public interface IAsyncDisposable : IDisposable
    {
        Task DisposeAsync();
    }
}