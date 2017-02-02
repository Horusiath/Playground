#region copyright
// -----------------------------------------------------------------------
//  <copyright file="CommittableOffsetBatch.cs" company="Akka.NET project">
//      Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2017 Akka.NET project <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Akka.Streams.Kafka.Internals
{

    internal interface ICommitter
    {
        Task CommitAsync(PartitionOffset offset);
        Task CommitAsync(ICommittableOffsetBatch batch);
    }

    internal class CommittableOffset : ICommittableOffset
    {
        internal readonly ICommitter Committer;

        public CommittableOffset(PartitionOffset offset, ICommitter committer)
        {
            this.Committer = committer;
            PartitionOffset = offset;
        }

        public PartitionOffset PartitionOffset { get; }
        public Task CommitAsync() => Committer.CommitAsync(PartitionOffset);
    }

    internal class CommittableOffsetBatch : ICommittableOffsetBatch
    {
        private readonly IImmutableDictionary<string, ICommitter> stages;

        public CommittableOffsetBatch(IImmutableDictionary<PartitionId, long> offsets, IImmutableDictionary<string, ICommitter> stages)
        {
            this.Offsets = offsets;
            this.stages = stages;
        }

        public IImmutableDictionary<PartitionId, long> Offsets { get; }

        public Task CommitAsync() =>
            Offsets.Count == 0 ? Task.CompletedTask : stages.First().Value.CommitAsync(this);

        public ICommittableOffsetBatch Updated(ICommittableOffset offset)
        {
            var partitionOffset = offset.PartitionOffset;
            var key = partitionOffset.Key;

            var newOffsets = Offsets.SetItem(key, offset.PartitionOffset.Offset);
            var stage = ((CommittableOffset)offset).Committer;

            ICommitter s;
            IImmutableDictionary<string, ICommitter> newStages;
            if (stages.TryGetValue(key.GroupId, out s))
            {
                if (s != stage) throw new ArgumentException();
                newStages = stages;
            }
            else
                newStages = stages.SetItem(key.GroupId, stage);

            return new CommittableOffsetBatch(newOffsets, newStages);
        }
    }
}