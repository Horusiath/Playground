using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Akka.Eventsourced
{
    /// <summary>
    /// Manages concurrent versions of an eventsourced aggregate.
    /// </summary>
    /// <typeparam name="TState">Type of an aggregate.</typeparam>
    /// <typeparam name="TCommand">Command type.</typeparam>
    /// <typeparam name="TEvent">Event type.</typeparam>
    public sealed class VersionedAggregate<TState, TCommand, TEvent>
    {
        public readonly string Id;
        public readonly Func<TState, TCommand, TEvent> CommandHandler;
        public readonly Func<TState, TEvent, TState> EventHandler;
        public readonly IConcurrentVersions<TState, TEvent> Aggregate;

        /// <summary>
        /// Creates a new instance of the <see cref="VersionedAggregate{TState,TCommand,TEvent}"/>
        /// </summary>
        /// <param name="id">Aggregate id.</param>
        /// <param name="commandHandler">Command handler.</param>
        /// <param name="eventHandler">Event handler.</param>
        /// <param name="aggregate">Aggregate.</param>
        public VersionedAggregate(string id, Func<TState, TCommand, TEvent> commandHandler, Func<TState, TEvent, TState> eventHandler,
            IConcurrentVersions<TState, TEvent> aggregate = null)
        {
            Id = id;
            CommandHandler = commandHandler;
            EventHandler = eventHandler;
            Aggregate = aggregate;
        }

        /// <summary>
        /// Safely returns all versions managed by current object (if any were provided).
        /// </summary>
        public IEnumerable<Versioned<TState>> Versions => Aggregate == null
            ? Enumerable.Empty<Versioned<TState>>()
            : Aggregate.All;
    }

    [Serializable]
    public class AggregateNotLoadedException : Exception
    {
        public AggregateNotLoadedException(string id) : base($"Aggregate '{id}' not loaded") { }
        protected AggregateNotLoadedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class AggregateAlreadyExistsException : Exception
    {
        public AggregateAlreadyExistsException(string id) : base($"Aggregate '{id}' already exists") { }
        protected AggregateAlreadyExistsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class AggregateDoesNotExistException : Exception
    {
        public AggregateDoesNotExistException(string id) : base($"Aggregate '{id}' does not exists") { }
        protected AggregateDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ConflictResolutionRejectedException : Exception
    {
        public ConflictResolutionRejectedException(string id, string origin1, string origin2) 
            : base($"conflict for aggregate '{id}' can only be resolved by '{origin1}' but '{origin2}' has attempted") { }
        protected ConflictResolutionRejectedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ConflictNotDetectedException : Exception
    {
        public ConflictNotDetectedException(string id) : base($"Conflict for aggregate '{id}' not detected") { }
        protected ConflictNotDetectedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ConflictDetectedException<T> : Exception
    {
        public IEnumerable<Versioned<T>> Versions { get; }
        public ConflictDetectedException(string id, IEnumerable<Versioned<T>> versions)
            : base($"Conflict for aggregate '{id}' detected")
        {
            Versions = versions;
        }

        protected ConflictDetectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Versions = (IEnumerable<Versioned<T>>) info.GetValue("Versions", typeof(IEnumerable<Versioned<T>>));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Versions", Versions, typeof(IEnumerable<Versioned<T>>));
            base.GetObjectData(info, context);
        }
    }
}