﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace Akka.Eventsourced
{
    [Serializable]
    public sealed class DurableEvent
    {
        public const string UndefinedLogId = "";
        public const long UndefinedSequenceNr = 0L;

        public readonly object Payload;
        public readonly string EmitterId;
        public readonly string EmitterAggregateId;
        public readonly IImmutableSet<string> CustomDestinationAggregateIds;
        public readonly long SystemTimestamp;
        public readonly VectorTime VectorTimestamp;
        public readonly string ProcessId;
        public readonly string LocalLogId;
        public readonly long LocalSequenceNr;

        public DurableEvent(object payload,
            string emitterId,
            string emitterAggregateId = null,
            IImmutableSet<string> customDestinationAggregateIds = null,
            long systemTimestamp = 0L,
            VectorTime vectorTimestamp = default(VectorTime),
            string processId = null,
            string localLogId = null,
            long localSequenceNr = 0L)
        {
            Contract.Assert(payload != null);
            Contract.Assert(emitterId != null);

            Payload = payload;
            EmitterId = emitterId;
            EmitterAggregateId = emitterAggregateId;
            CustomDestinationAggregateIds = customDestinationAggregateIds ?? ImmutableHashSet<string>.Empty;
            SystemTimestamp = systemTimestamp;
            VectorTimestamp = vectorTimestamp == default(VectorTime) ? VectorTime.Zero : vectorTimestamp;
            ProcessId = processId;
            LocalLogId = localLogId;
            LocalSequenceNr = localSequenceNr;
        }

        public VectorTime Id { get { return VectorTimestamp; } }

        public string DefaultDestinationAggregateId { get { return EmitterAggregateId; } }

        public IImmutableSet<string> DestinationAggregateId
        {
            get
            {
                return DefaultDestinationAggregateId != null
                    ? CustomDestinationAggregateIds.Add(DefaultDestinationAggregateId)
                    : CustomDestinationAggregateIds;
            }
        }

        [Pure]
        public bool Replicate(VectorTime time)
        {
            return !(VectorTimestamp <= time);
        }

        [Pure]
        public bool Replicate(VectorTime time, IReplicationFilter filter)
        {
            return Replicate(time) && filter.Apply(this);
        }

        [Pure]
        internal DurableEvent PrepareWrite(string logId, long sequenceNr, long timestamp)
        {
            var st = ProcessId == null ? timestamp : SystemTimestamp;
            var vt = ProcessId == null ? VectorTimestamp.SetLocalTime(logId, sequenceNr) : VectorTimestamp;
            var id = ProcessId ?? logId;

            return new DurableEvent(
                payload: Payload,
                emitterId: EmitterId,
                emitterAggregateId: EmitterAggregateId,
                customDestinationAggregateIds: CustomDestinationAggregateIds,
                systemTimestamp: st,
                vectorTimestamp: vt,
                processId: id,
                localLogId: logId,
                localSequenceNr: sequenceNr);
        }

        [Pure]
        internal DurableEvent PrepareReplicate(string logId, long sequenceNr)
        {
            return new DurableEvent(
                payload: Payload,
                emitterId: EmitterId,
                emitterAggregateId: EmitterAggregateId,
                customDestinationAggregateIds: CustomDestinationAggregateIds,
                systemTimestamp: SystemTimestamp,
                vectorTimestamp: VectorTimestamp,
                processId: ProcessId,
                localLogId: logId,
                localSequenceNr: sequenceNr);
        }

        /// <summary>
        /// Returns true if this event did not happen before or at a given <paramref name="vectorTime"/>
        /// and passes the given replication <paramref name="filter"/>.
        /// </summary>
        [Pure]
        public bool Replicable(VectorTime vectorTime, IReplicationFilter filter)
        {
            return !Before(vectorTime) && filter.Apply(this);
        }

        /// <summary>
        /// Returns true if current event happened before provided <paramref name="vectorTime"/>.
        /// </summary>
        [Pure]
        private bool Before(VectorTime vectorTime)
        {
            return VectorTimestamp <= vectorTime;
        }
    }

    /// <summary>
    /// Implemented by protocol messages that contain a <see cref="DurableEvent"/> sequences.
    /// </summary>
    public interface IDurableEventBatch
    {
        int Count { get; }
        IEnumerable<DurableEvent> Events { get; }
    }
}