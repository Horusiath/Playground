using System;
using Akka.Streams.Stage;
using RdKafka;

namespace Akka.Streams.Kafka.Internals
{

    internal class KafkaSourceStage<TKey, TVal, TMsg> : GraphStageWithMaterializedValue<SourceShape<TMsg>, IAsyncDisposable>
    {
        protected readonly Outlet<TMsg> Out = new Outlet<TMsg>("out");
        public override SourceShape<TMsg> Shape { get; }

        public KafkaSourceStage(Func<SourceShape<TMsg>, GraphStageLogic> logic)
        {
            Logic = logic;
            Shape = new SourceShape<TMsg>(Out);
        }

        protected Func<SourceShape<TMsg>, GraphStageLogic> Logic { get; }

        public override ILogicAndMaterializedValue<IAsyncDisposable> CreateLogicAndMaterializedValue(Attributes inheritedAttributes)
        {
            var result = Logic(Shape);
            return new LogicAndMaterializedValue<IAsyncDisposable>(result, (IAsyncDisposable)result);
        }
    }

    internal class ConsumerStage
    {
        
    }
}