#region copyright
// -----------------------------------------------------------------------
//  <copyright file="MessageBuilder.cs" company="Akka.NET Team">
//      Copyright (C) 2015-2016 Lightbend <http://www.lightbend.com/>
//      Copyright (C) 2016-2016 Akka.NET Team <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Threading.Tasks;
using Akka.Streams.Kafka.Internals;
using RdKafka;

namespace Akka.Streams.Kafka
{
    public interface IMessageBuilder<TKey, TVal, out TMsg>
    {
        TMsg CreateMessage(RdKafka.Message record);
    }

    public sealed class DefaultMessageBuilder<TKey, TVal> : IMessageBuilder<TKey, TVal, RdKafka.Message>
    {
        public RdKafka.Message CreateMessage(RdKafka.Message record) => record;
    }

    internal abstract class CommittableMessageBuilder<TKey, TVal> :
        IMessageBuilder<TKey, TVal, CommittableMessage<TKey, TVal>>
    {
        public abstract string GroupId { get; }
        public abstract ICommitter Committer { get; }

        public CommittableMessage<TKey, TVal> CreateMessage(Message record)
        {
            var offset = new PartitionOffset(new PartitionId(
                groupId: GroupId,
                topic: record.Topic,
                partition: record.Partition), record.Offset);
            return new CommittableMessage<TKey, TVal>(new CommittableOffset(offset, Committer), record);
        }
    }
}