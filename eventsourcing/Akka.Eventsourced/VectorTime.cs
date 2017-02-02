using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Akka.Eventsourced
{
    public struct VectorClock
    {
        public readonly string ProcessId;
        public readonly VectorTime CurrentTime;

        public VectorClock(string processId, VectorTime currentTime) : this()
        {
            ProcessId = processId;
            CurrentTime = currentTime;
        }

        public long CurrentLocalTime => CurrentTime.LocalTime(ProcessId);

        [Pure]
        public VectorClock Update(VectorTime time)
        {
            return Merge(time).Tick();
        }

        [Pure]
        public VectorClock Merge(VectorTime time)
        {
            return new VectorClock(ProcessId, CurrentTime.Merge(time));
        }

        [Pure]
        public VectorClock Tick()
        {
            return new VectorClock(ProcessId, CurrentTime.Increment(ProcessId));
        }

        [Pure]
        public VectorClock Set(string processId, long tick)
        {
            return new VectorClock(ProcessId, CurrentTime.SetLocalTime(processId, tick));
        }

        public bool Covers(VectorTime emittedTimestamp, string emitterProcessId)
        {
            var self = this;
            return emittedTimestamp.Value.Any(entry =>
                entry.Key != self.ProcessId && entry.Key != emitterProcessId && self.CurrentTime.LocalTime(entry.Key) < entry.Value);
        }
    }

    public struct VectorTime : IComparable<VectorTime>, IEquatable<VectorTime>, IComparable
    {
        public static readonly VectorTime Zero = new VectorTime(ImmutableDictionary<string, long>.Empty);

        public readonly IImmutableDictionary<string, long> Value;

        public VectorTime(IImmutableDictionary<string, long> value) : this()
        {
            Value = value;
        }

        [Pure]
        public VectorTime SetLocalTime(string processId, long localTime)
        {
            var newValue = (Value ?? ImmutableDictionary<string, long>.Empty).SetItem(processId, localTime);
            return new VectorTime(newValue);
        }

        [Pure]
        public long LocalTime(string processId)
        {
            if (Value == null) return 0L;
            return Value.GetValueOrDefault(processId, 0L);
        }

        [Pure]
        public VectorTime LocalCopy(string processId)
        {
            long time;
            if (Value != null && Value.TryGetValue(processId, out time))
                return new VectorTime(ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, long>(processId, time) }));
            else
                return new VectorTime(ImmutableDictionary<string, long>.Empty);
        }

        [Pure]
        public VectorTime Increment(string processId)
        {
            long time;
            if (Value != null && Value.TryGetValue(processId, out time))
                return new VectorTime(Value.SetItem(processId, time + 1));
            else
                return new VectorTime((Value ?? ImmutableDictionary<string, long>.Empty).SetItem(processId, 1L));
        }

        [Pure]
        public VectorTime Merge(VectorTime other)
        {
            var x = Value ?? ImmutableDictionary<string, long>.Empty;
            var y = other.Value ?? ImmutableDictionary<string, long>.Empty;
            var dict = x.Union(y)
                .Aggregate(ImmutableDictionary<string, long>.Empty, (map, pair) =>
                    map.SetItem(pair.Key, Math.Max(map.GetValueOrDefault(pair.Key, long.MinValue), pair.Value)));

            return new VectorTime(dict);
        }

        [Pure]
        public bool IsConcurrent(VectorTime other) => 
            VectorTimeComparer.Instance.Compare(this, other) == VectorTimeComparer.Concurrent;

        public bool Equals(VectorTime other) => VectorTimeComparer.Instance.Equals(this, other);

        public override bool Equals(object obj)
        {
            if (obj is VectorTime) return Equals((VectorTime)obj);
            return false;
        }

        public override int GetHashCode() => VectorTimeComparer.Instance.GetHashCode();

        public override string ToString() => $"VectorTime({string.Join(" ; ", Value.Select(p => $"{p.Key}: {p.Value}"))})";

        public int CompareTo(VectorTime other) => VectorTimeComparer.Instance.Compare(this, other);

        public int CompareTo(object obj) => obj is VectorTime ? CompareTo((VectorTime) obj) : -1;

        public static bool operator ==(VectorTime x, VectorTime y) => x.Equals(y);

        public static bool operator !=(VectorTime x, VectorTime y) => !(x == y);

        public static bool operator >(VectorTime x, VectorTime y) => x.CompareTo(y) == 1;

        public static bool operator <(VectorTime x, VectorTime y) => x.CompareTo(y) == -1;

        public static bool operator <=(VectorTime x, VectorTime y) => x.CompareTo(y) != 1;

        public static bool operator >=(VectorTime x, VectorTime y) => x.CompareTo(y) != -1;
    }

    internal class VectorTimeComparer : IComparer<VectorTime>, IEqualityComparer<VectorTime>
    {
        public const int Concurrent = -2;
        public static readonly VectorTimeComparer Instance = new VectorTimeComparer();

        private VectorTimeComparer() { }

        public int Compare(VectorTime x, VectorTime y)
        {
            var xval = x.Value ?? ImmutableDictionary<string, long>.Empty;
            var yval = y.Value ?? ImmutableDictionary<string, long>.Empty;
            const int none = Concurrent;    // how to mark partial ordering
            var keys = xval.Keys.Union(yval.Keys).Distinct();
            var current = 0;
            foreach (var key in keys)
            {
                var x1 = xval.GetValueOrDefault(key, 0L);
                var y2 = yval.GetValueOrDefault(key, 0L);
                var s = Math.Sign(x1 - y2);

                if (current == 0L) current = s;
                else if (current == -1)
                    if (s == 1) return none;
                    else if (s == -1) return none;
            }

            return current;
        }

        public bool Equals(VectorTime x, VectorTime y)
        {
            return x.Value.Keys.Union(y.Value.Keys).All(key =>
                x.Value.GetValueOrDefault(key, 0L) == y.Value.GetValueOrDefault(key, 0L));
        }

        public int GetHashCode(VectorTime obj)
        {
            throw new NotImplementedException();
        }
    }
}
