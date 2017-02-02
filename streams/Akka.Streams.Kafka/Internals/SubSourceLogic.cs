using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using RdKafka;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Akka.Streams.Kafka.Internals
{
    internal abstract class SubSourceLogic<TKey, TValue, TMessage> : 
        GraphStageLogic, 
        IAsyncDisposable,
        IMessageBuilder<TKey, TValue, TMessage>
    {
        private IActorRef consumer;
        private StageActorRef self;
        private Queue<TopicPartition> buffer = new Queue<TopicPartition>();
        private ImmutableDictionary<TopicPartition, IAsyncDisposable> subSources = ImmutableDictionary<TopicPartition, IAsyncDisposable>.Empty;

        protected SubSourceLogic(SourceShape<Tuple<TopicPartition, Source<TMessage, NotUsed>>> shape, 
            SinkSettings<TKey, TValue> settings, ITopicSubscription subscription)
            : base(shape)
        {
        }

        public TMessage CreateMessage(Message record)
        {
            throw new System.NotImplementedException();
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