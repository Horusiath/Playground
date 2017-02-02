using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Akka.Streams.Dsl;

namespace Akka.Streams.Kafka
{
    #region messages

    /// <summary>
    /// Output element of <see cref="KafkaSource.Committable{TKey,TValue}"/>.
    /// The offset can be committed via the included <see cref="ICommittableOffset"/>.
    /// </summary>
    public sealed class CommittableMessage<TKey, TValue>
    {
        public readonly ICommittableOffset Offset;
        public RdKafka.Message Record;

        public CommittableMessage(ICommittableOffset offset, RdKafka.Message record)
        {
            Offset = offset;
            Record = record;
        }
    }

    /// <summary>
    /// Commit an offset that is included in a <see cref="CommittableMessage{TKey,TValue}"/>.
    /// If you need to store offsets in anything other than Kafka, this API
    /// should not be used.
    /// 
    /// This interface might move into `akka.stream`
    /// </summary>
    public interface ICommittable
    {
        Task CommitAsync();
    }

    /// <summary>
    /// Included in <see cref="CommittableMessage{TKey,TValue}"/>. Makes it possible to
    /// commit an offset with the <see cref="ICommittable.CommitAsync"/> method
    /// or aggregate several offsets in a <see cref="ICommittableOffsetBatch"/>
    /// before committing.
    /// 
    /// Note that the offset position that is committed to Kafka will automatically
    /// be one more than the `offset` of the message, because the committed offset
    /// should be the next message your application will consume,
    /// i.e. lastProcessedMessageOffset + 1.
    /// </summary>
    public interface ICommittableOffset : ICommittable
    {
        /// <summary>
        /// Information about the offset position for a groupId, topic, partition.
        /// </summary>
        PartitionOffset PartitionOffset { get; }
    }

    /// <summary>
    /// For improved efficiency it is good to aggregate several <see cref="ICommittableOffset"/>,
    /// using this class, before <see cref="ICommittable.CommitAsync"/> them. 
    /// </summary>
    public interface ICommittableOffsetBatch : ICommittable
    {
        /// <summary>
        /// Add/overwrite an offset position for the given groupId, topic, partition.
        /// </summary>
        ICommittableOffsetBatch Updated(ICommittableOffset offset);

        /// <summary>
        /// Get current offset positions.
        /// </summary>
        IImmutableDictionary<PartitionId, long> Offsets { get; }
    }

    /// <summary>
    /// GroupId, topic, partition key for an offset position.
    /// </summary>
    public struct PartitionId : IEquatable<PartitionId>, IComparable<PartitionId>, IComparable
    {
        public readonly string GroupId;
        public readonly string Topic;
        public readonly int Partition;

        public PartitionId(string groupId, string topic, int partition)
        {
            GroupId = groupId;
            Topic = topic;
            Partition = partition;
        }

        public bool Equals(PartitionId other) => 
            Equals(GroupId, other.GroupId) && Equals(Topic, other.Topic) && Equals(Partition, other.Partition);

        public override bool Equals(object obj) => obj is PartitionId && Equals((PartitionId)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GroupId?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Topic?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Partition;
                return hashCode;
            }
        }

        public int CompareTo(PartitionId other)
        {
            var cmp = string.Compare(GroupId, other.GroupId, StringComparison.InvariantCulture);
            if (cmp != 0) return cmp;

            cmp = string.Compare(Topic, other.Topic, StringComparison.InvariantCulture);
            if (cmp != 0) return cmp;

            cmp = Partition.CompareTo(other.Partition);
            return cmp;
        }

        public int CompareTo(object obj)
        {
            if (obj is PartitionId) return CompareTo((PartitionId) obj);
            throw new ArgumentException($"Cannot compare object of type {obj?.GetType()} to {nameof(PartitionId)}");
        }

        public override string ToString() => $"PartitionId({GroupId}, {Topic}, {Partition})";
    }

    /// <summary>
    /// Offset position for a groupId, topic, partition.
    /// </summary>
    public struct PartitionOffset
    {
        public readonly PartitionId Key;
        public readonly long Offset;

        public PartitionOffset(PartitionId key, long offset)
        {
            Key = key;
            Offset = offset;
        }
    }

    #endregion

    public sealed class SourceSettings<TKey, TValue>
    {

    }

    public interface ISubscription { }
    public interface ITopicSubscription : ISubscription{ }
    public interface IManualSubscription : ISubscription { }

    public sealed class TopicSubscription : ITopicSubscription { }
    public sealed class TopicSubscriptionPattern : ITopicSubscription { }
    public sealed class Assignment : IManualSubscription { }
    public sealed class AssignmentWithOffset : IManualSubscription { }

    public static class KafkaSource
    {
        public static Source<RdKafka.Message, IAsyncDisposable> Uncommitable<TKey, TValue>(
            SourceSettings<TKey, TValue> settings, ISubscription subscription)
        {
            throw new NotImplementedException();
        }

        public static Source<CommittableMessage<TKey, TValue>, IAsyncDisposable> Committable<TKey, TValue>(
            SourceSettings<TKey, TValue> settings, ISubscription subscription)
        {
            throw new NotImplementedException();
        }

        public static Source<KeyValuePair<RdKafka.TopicPartition, Source<RdKafka.Message, NotUsed>>, IAsyncDisposable> PartitionedUncommitable<TKey, TValue>(
            SourceSettings<TKey, TValue> settings, ITopicSubscription subscription)
        {
            throw new NotImplementedException();
        }

        public static Source<KeyValuePair<RdKafka.TopicPartition, Source<CommittableMessage<TKey, TValue>, NotUsed>>, IAsyncDisposable> PartitionedCommittable<TKey, TValue>(
            SourceSettings<TKey, TValue> settings, ITopicSubscription subscription)
        {
            throw new NotImplementedException();
        }
    }
}