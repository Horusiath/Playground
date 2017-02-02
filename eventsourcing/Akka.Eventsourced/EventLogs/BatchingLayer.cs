using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Configuration;
using Akka.Eventsourced.EventsourcedProtocol;
using Akka.Eventsourced.ReplicationProtocol;

namespace Akka.Eventsourced.EventLogs
{
    [Serializable]
    internal sealed class BatchingSettings
    {
        public static BatchingSettings Create(ActorSystem system)
        {
            return Create(system.Settings.Config.GetConfig("akka.eventsourced.log.batching"));
        }

        public static BatchingSettings Create(Config config)
        {
            return new BatchingSettings(
                batchSizeLimit: config.GetInt("batch-size-limit"));
        }

        public readonly int BatchSizeLimit;

        public BatchingSettings(int batchSizeLimit)
        {
            BatchSizeLimit = batchSizeLimit;
        }
    }

    /// <summary>
    /// An event log wrapper that batches write commands. Batched <see cref="EventsourcedProtocol.Write"/> commands are sent as
    /// <see cref="EventsourcedProtocol.WriteMany"/> batch to the wrapped event log. Batched <see cref="ReplicationProtocol.ReplicationWrite"/>
    /// commands are sent as <see cref="ReplicationProtocol.ReplicationWriteMany"/> batch to the wrapped event event log.
    /// 
    /// Batch sizes dynamically increase to a configurable limit under increasing load. The batch size limit can be
    /// configured with `eventuate.log.write-batch-size`. If there is no current write operation in progress, a new
    /// <see cref="EventsourcedProtocol.Write"/> or <see cref="ReplicationProtocol.ReplicationWrite"/> command is served immediately 
    /// (as <see cref="EventsourcedProtocol.WriteMany"/> or <see cref="ReplicationProtocol.ReplicationWriteMany"/> batch of size 1, respectively), 
    /// keeping latency at a minimum.
    /// </summary>
    public class BatchingLayer : ReceiveActor
    {
        /// <summary>
        /// Creates a new batching layer instance.
        /// </summary>
        /// <param name="logProps">
        /// Configuration object of the wrapped event log actor. The wrapped event log actor is created as 
        /// child actor of this wrapper.
        /// </param>
        public BatchingLayer(Props logProps)
        {
            var eventLog = Context.Watch(Context.ActorOf(logProps));
            var defaultBatcher = Context.ActorOf(Props.Create(() => new DefaultBatcher(eventLog)), "default-batcher");
            var replicationBatcher = Context.ActorOf(Props.Create(() => new ReplicationBatcher(eventLog)), "replication-batcher");

            Receive<ReplicationWrite>(write => replicationBatcher.Forward(write.WithInitiator(Sender)));
            Receive<ReplicationRead>(read => replicationBatcher.Forward(read));
            Receive<CircuitBreaker.IServiceEvent>(e => Context.Parent.Tell(e));
            Receive<Terminated>(_ => Context.Stop(Self));
            ReceiveAny(message => defaultBatcher.Forward(message));
        }
    }

    internal abstract class Batcher<TEventBatch> : ActorBase
        where TEventBatch : IDurableEventBatch
    {
        protected readonly BatchingSettings Settings = BatchingSettings.Create(Context.System);
        protected ImmutableArray<TEventBatch> Batch;

        protected Batcher()
        {
            Batch = ImmutableArray<TEventBatch>.Empty;
        }

        protected abstract IActorRef EventLog { get; }
        protected abstract object WriteRequest(IEnumerable<TEventBatch> batches);
        protected abstract bool Idle(object message);

        protected override bool Receive(object message) => Idle(message);

        protected override void Unhandled(object message)
        {
            EventLog.Forward(message);
        }

        protected void WriteAll()
        {
            while (TryWriteBatch()) ;
        }

        protected bool TryWriteBatch()
        {
            if (Batch.IsEmpty) return false;

            int num = 0, i = 0;
            var writes = new List<TEventBatch>();
            for (; i < Batch.Length; i++)
            {
                var write = Batch[i];
                var count = write.Count;
                num += count;
                if (num <= Settings.BatchSizeLimit || num == count)
                {
                    writes.Add(write);
                }
            }

            EventLog.Tell(WriteRequest(writes));

            Batch = Batch.RemoveRange(0, i);
            return !Batch.IsEmpty;
        }
    }

    internal class DefaultBatcher : Batcher<Write>
    {
        public DefaultBatcher(IActorRef eventLog)
        {
            this.EventLog = eventLog;
        }

        protected override IActorRef EventLog { get; }

        protected override object WriteRequest(IEnumerable<Write> batches) => new WriteMany(batches);

        protected override bool Idle(object message)
        {
            if (message is Write)
            {
                Batch = Batch.Add(((Write)message).WithReplyToDefault(Sender));
                TryWriteBatch();
                Context.Become(Writing);

                return true;
            }

            return false;
        }

        protected bool Writing(object message) => message.Match()
            .With<Write>(write => Batch = Batch.Add(write.WithReplyToDefault(Sender)))
            .With<WriteManyComplete>(_ =>
            {
                if (Batch.IsEmpty) Context.Become(Idle);
                else TryWriteBatch();
            })
            .With<Replay>(replay =>
            {
                WriteAll();
                EventLog.Forward(replay);
                Context.Become(Idle);
            })
            .WasHandled;
    }

    internal class ReplicationBatcher : Batcher<ReplicationWrite>
    {
        public ReplicationBatcher(IActorRef eventLog)
        {
            this.EventLog = eventLog;
        }

        protected override IActorRef EventLog { get; }

        protected override object WriteRequest(IEnumerable<ReplicationWrite> batches)
        {
            return new ReplicationWriteMany(batches);
        }

        protected override bool Idle(object message)
        {
            if (message is ReplicationWrite)
            {
                Batch = Batch.Add(((ReplicationWrite)message).WithReplyToDefault(Sender));
                TryWriteBatch();
                Context.Become(Writing);

                return true;
            }
            return false;
        }

        private bool Writing(object message)
        {
            return message.Match()
                .With<ReplicationWrite>(write => Batch = Batch.Add(write.WithInitiator(Sender)))
                .With<ReplicationWriteManyComplete>(_ =>
                {
                    if (Batch.IsEmpty) Context.Become(Idle);
                    else TryWriteBatch();
                })
                .WasHandled;
        }
    }
}