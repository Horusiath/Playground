using System;
using System.Collections.Immutable;

namespace Akka.Eventsourced
{
    public struct SnapshotMetadata
    {
        /// <summary>
        /// Id of the <see cref="EventsourcedActor"/>, <see cref="EventsourcedView"/>, 
        /// stateful <see cref="EventsourcedWriter"/> or <see cref="EventsourcedProcessor"/> that saves the snapshot.
        /// </summary>
        public readonly string EmitterId;

        /// <summary>
        /// The highest event sequence number covered by the snapshot.
        /// </summary>
        public readonly long SequenceNr;

        public SnapshotMetadata(string emitterId, long sequenceNr)
        {
            EmitterId = emitterId;
            SequenceNr = sequenceNr;
        }
    }

    /// <summary>
    /// Provider API.
    /// 
    /// Snapshot storage format. <see cref="EventsourcedActor"/>s, <see cref="EventsourcedView"/>s, stateful <see cref="EventsourcedWriter"/>s
    /// and <see cref="EventsourcedProcessor"/>s can save snapshots of internal state by calling the (inherited)
    /// <see cref="EventsourcedView.Save"/> method.
    /// </summary>
    /// @param payload 
    /// @param emitterId 
    /// @param lastEvent 
    /// @param currentTime 
    /// @param sequenceNr 
    /// @param deliveryAttempts 
    /// @param persistOnEventRequests Unconfirmed [[PersistOnEvent.PersistOnEventRequest PersistOnEventRequest]]s when the
    ///                               snapshot was saved (can only be non-empty if the actor implements [[PersistOnEvent]]).
    /// 
    [Serializable]
    public sealed class Snapshot
    {
        /// <summary>
        /// Application-specific snapshot.
        /// </summary>
        public readonly object Payload;

        /// <summary>
        /// Id of the event-sourced actor, view, stateful writer or processor that saved the snapshot.
        /// </summary>
        public readonly string EmitterId;

        /// <summary>
        /// Last handled event before the snapshot was saved.
        /// </summary>
        public readonly DurableEvent LastEvent;

        /// <summary>
        /// Current vector time when the snapshot was saved.
        /// </summary>
        public readonly VectorTime CurrentTime;

        /// <summary>
        /// Sequence number of the last *received* event when the snapshot was saved.
        /// </summary>
        public readonly long SequenceNr;

        /// <summary>
        /// Unconfirmed <see cref="DeliveryAttempt"/>s when the snapshot was saved 
        /// (can only be non-empty if the actor implements <see cref="ConfirmedDelivery"/>).
        /// </summary>
        public readonly ImmutableArray<DeliveryAttempt> DeliveryAttempts;

        public readonly ImmutableArray<PersistOnEventRequest> PersistOnEventRequests;

        public readonly SnapshotMetadata Metadata;

        public Snapshot(object payload, string emitterId, DurableEvent lastEvent, VectorTime currentTime, long sequenceNr,
            ImmutableArray<DeliveryAttempt> deliveryAttempts = default(ImmutableArray<DeliveryAttempt>), 
            ImmutableArray<PersistOnEventRequest> persistOnEventRequests = default(ImmutableArray<PersistOnEventRequest>))
        {
            Payload = payload;
            EmitterId = emitterId;
            LastEvent = lastEvent;
            CurrentTime = currentTime;
            SequenceNr = sequenceNr;
            DeliveryAttempts = deliveryAttempts;
            PersistOnEventRequests = persistOnEventRequests;

            Metadata = new SnapshotMetadata(EmitterId, LastEvent.LocalSequenceNr);
        }

        public Snapshot Add(DeliveryAttempt deliveryAttempt)
        {
            return new Snapshot(
                payload: Payload,
                emitterId: EmitterId,
                lastEvent: LastEvent,
                currentTime: CurrentTime,
                sequenceNr: SequenceNr,
                deliveryAttempts: DeliveryAttempts.Add(deliveryAttempt),
                persistOnEventRequests: PersistOnEventRequests);
        }

        public Snapshot Add(PersistOnEventRequest eventRequest)
        {
            return new Snapshot(
                payload: Payload,
                emitterId: EmitterId,
                lastEvent: LastEvent,
                currentTime: CurrentTime,
                sequenceNr: SequenceNr,
                deliveryAttempts: DeliveryAttempts,
                persistOnEventRequests: PersistOnEventRequests.Add(eventRequest));
        }
    }
}