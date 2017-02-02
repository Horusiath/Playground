using System;
using System.Collections.Immutable;

namespace Akka.Eventsourced
{
    /// <summary>
    /// Records a `persistOnEvent` invocation.
    /// </summary>
    public sealed class PersistOnEventInvocation
    {
        public readonly object Event;
        public readonly ImmutableHashSet<string> CustomDestinationAggregateIds;

        public PersistOnEventInvocation(object @event, ImmutableHashSet<string> customDestinationAggregateIds)
        {
            Event = @event;
            CustomDestinationAggregateIds = customDestinationAggregateIds;
        }
    }

    /// <summary>
    /// A request sent by <see cref="PersistOnEventActor"/> instances to `self` in order to persist events recorded by <see cref="Invocations"/>.
    /// </summary>
    public sealed class PersistOnEventRequest
    {
        public readonly long PersistOnEventSequenceNr;
        public readonly ImmutableArray<PersistOnEventInvocation> Invocations;
        public readonly int InstanceId;

        public PersistOnEventRequest(long persistOnEventSequenceNr, ImmutableArray<PersistOnEventInvocation> invocations, int instanceId)
        {
            PersistOnEventSequenceNr = persistOnEventSequenceNr;
            Invocations = invocations;
            InstanceId = instanceId;
        }
    }

    /// <summary>
    /// Thrown to indicate that an asynchronous `persisOnEvent` operation failed.
    /// </summary>
    public class PersistOnEventException : Exception
    {
        public PersistOnEventException(Exception innerException) : base(string.Empty, innerException)
        {
        }
    }

    /// <summary>
    /// Can be mixed into <see cref="EventsourcedActor"/> for writing new events within the <see cref="EventsourcedView.OnEvent"/> handler. 
    /// New events are written with the asynchronous <see cref="PersistOnEvent{T}"/> method. In contrast to [[EventsourcedActor.persist persist]],
    /// one can '''not''' prevent command processing from running concurrently to [[persistOnEvent]] by setting
    /// [[EventsourcedActor.stateSync stateSync]] to `true`.
    /// 
    /// A `persistOnEvent` operation is reliable and idempotent. Once the event has been successfully written, a repeated
    /// `persistOnEvent` call for that event during event replay has no effect. A failed `persistOnEvent` operation will
    /// restart the actor by throwing a <see cref="PersistOnEventException"/>. After restart, failed `persistOnEvent` operations
    /// are automatically re-tried.
    /// </summary>
    public abstract class PersistOnEventActor : EventsourcedActor
    {
        private ImmutableArray<PersistOnEventInvocation> invocations = ImmutableArray<PersistOnEventInvocation>.Empty;
        private ImmutableSortedDictionary<long, PersistOnEventRequest> requests = ImmutableSortedDictionary<long, PersistOnEventRequest>.Empty;

        public void PersistOnEvent<T>(T evt, ImmutableHashSet<string> customDestinationAggregateIds = null)
        {
            invocations = invocations.Add(new PersistOnEventInvocation(evt, customDestinationAggregateIds ?? ImmutableHashSet<string>.Empty));
        }

        protected override void ReceiveEvent(DurableEvent e)
        {
            base.ReceiveEvent(e);
        }
    }
}