using System;
using System.Threading.Tasks;
using Akka.Event;
using Akka.Streams.Stage;
using Akka.Util.Internal;
using RdKafka;

namespace Akka.Streams.Kafka.Internals
{

    internal sealed class ProducerStage<TKey, TValue, TPass> : GraphStage<FlowShape<KafkaMessage<TKey, TValue, TPass>, Task<KafkaResult<TKey, TValue, TPass>>>>
    {
        public readonly Inlet<KafkaMessage<TKey, TValue, TPass>> In = new Inlet<KafkaMessage<TKey, TValue, TPass>>("messages");
        public readonly Outlet<Task<KafkaResult<TKey, TValue, TPass>>> Out = new Outlet<Task<KafkaResult<TKey, TValue, TPass>>>("result");

        private readonly TimeSpan closeTimeout;
        private readonly bool closeProducerOnStop;
        private readonly Func<Producer> producerProvider;

        public override FlowShape<KafkaMessage<TKey, TValue, TPass>, Task<KafkaResult<TKey, TValue, TPass>>> Shape { get; }

        public ProducerStage(TimeSpan closeTimeout, bool closeProducerOnStop, Func<RdKafka.Producer> producerProvider)
        {
            this.closeTimeout = closeTimeout;
            this.closeProducerOnStop = closeProducerOnStop;
            this.producerProvider = producerProvider;
            Shape = new FlowShape<KafkaMessage<TKey, TValue, TPass>, Task<KafkaResult<TKey, TValue, TPass>>>(In, Out);
        }

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        #region logic 

        sealed class Logic : GraphStageLogic
        {
            private readonly ProducerStage<TKey, TValue, TPass> self;
            private readonly RdKafka.Producer producer;
            private readonly AtomicCounter awaitingConfirmation = new AtomicCounter(0);
            private readonly TaskCompletionSource<NotUsed> completionState = new TaskCompletionSource<NotUsed>();

            private volatile bool inputIsClosed = false;

            public Logic(ProducerStage<TKey, TValue, TPass> self) : base(self.Shape)
            {
                this.self = self;
                this.producer = self.producerProvider();

                SetHandler(self.Out, onPull: () => TryPull(self.In));
                SetHandler(self.In, onPush: () =>
                {
                    var msg = Grab(self.In);
                    var result = SendToProducer(msg);
                    awaitingConfirmation.IncrementAndGet();
                    Push(self.Out, result);
                },
                onUpstreamFinish: () =>
                {
                    inputIsClosed = true;
                    completionState.SetResult(NotUsed.Instance);
                    CheckForCompletion();
                },
                onUpstreamFailure: cause =>
                {
                    inputIsClosed = true;
                    completionState.SetException(cause);
                    CheckForCompletion();
                });
            }

            public override void PostStop()
            {
                if (self.closeProducerOnStop)
                {
                    try
                    {
                        producer.Dispose();
                    }
                    catch (Exception cause)
                    {

                    }
                }

                base.PostStop();
            }

            private async Task<KafkaResult<TKey, TValue, TPass>> SendToProducer(KafkaMessage<TKey, TValue, TPass> msg)
            {
                using (var topic = producer.Topic(msg.Record.Topic))
                {
                    throw new NotImplementedException();
                }
            }

            private void CheckForCompletion()
            {
                if (IsClosed(self.In) && awaitingConfirmation.Current == 0)
                {
                    var completionTask = completionState.Task;
                    if (completionTask.IsFaulted) FailStage(completionTask.Exception);
                    else if (completionTask.IsCompleted) CompleteStage();
                }
            }
        }

        #endregion
    }
}