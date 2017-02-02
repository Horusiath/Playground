using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Akka.Eventsourced
{
    /// <summary>
    /// A versioned value of type <typeparamref name="TValue"/>.
    /// </summary>
    public struct Versioned<TValue> : IEquatable<Versioned<TValue>>
    {
        public readonly TValue Value;

        /// <summary>
        /// Vector timestamp of the event that caused this version.
        /// </summary>
        public readonly VectorTime VectorTimestamp;

        /// <summary>
        /// System timestamp of the event that caused this version.
        /// </summary>
        public readonly long SystemTimestamp;

        /// <summary>
        /// Creator, that caused this version event.
        /// </summary>
        public readonly string Creator;

        /// <summary>
        /// Creates new instance of the <see cref="Versioned{TValue}"/> value.
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="vectorTimestamp">Update vector timestamp of the event that caused this version.</param>
        /// <param name="systemTimestamp">Update system timestamp of the event that caused this version.</param>
        /// <param name="creator">Creator, that caused this version event.</param>
        public Versioned(TValue value, VectorTime vectorTimestamp, long systemTimestamp = 0L, string creator = "")
        {
            Value = value;
            VectorTimestamp = vectorTimestamp;
            SystemTimestamp = systemTimestamp;
            Creator = creator ?? string.Empty;
        }

        public bool Equals(Versioned<TValue> other)
        {
            return Equals(Creator, other.Creator)
                   && Equals(Value, other.Value)
                   && Equals(SystemTimestamp, other.SystemTimestamp)
                   && Equals(VectorTimestamp, other.VectorTimestamp);
        }

        public override bool Equals(object obj)
        {
            if (obj is Versioned<TValue>) return Equals((Versioned<TValue>)obj);
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<TValue>.Default.GetHashCode(Value);
                hashCode = (hashCode * 397) ^ VectorTimestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ SystemTimestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ Creator.GetHashCode();
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Tracks concurrent <typeparamref name="TVersioned"/> values which arise from concurrent updates.
    /// </summary>
    /// <typeparam name="TVersioned">Value type.</typeparam>
    /// <typeparam name="TUpdated">Update type.</typeparam>
    public interface IConcurrentVersions<TVersioned, in TUpdated>
    {
        /// <summary>
        /// Returns all (un-resolved) concurrent versions.
        /// </summary>
        IEnumerable<Versioned<TVersioned>> All { get; }

        /// <summary>
        /// Owner of versioned values.
        /// </summary>
        string Owner { get; }

        /// <summary>
        /// Updates that <typeparamref name="TVersioned"/> value with <paramref name="updated"/> 
        /// that is a predecessor of <paramref name="vectorTimestamp"/>. If there is no such predecessor, 
        /// a new concurrent version is created (optionally derived from an older entry in the version history, 
        /// in case of incremental updates).
        /// </summary>
        [Pure]
        IConcurrentVersions<TVersioned, TUpdated> Update(TUpdated updated, VectorTime vectorTimestamp, long systemTimestamp = 0L, string creator = "");

        /// <summary>
        /// Resolves multiple concurrent versions to a single version. For the resolution to be successful,
        /// one of the concurrent versions must have a <paramref name="vectorTimestamp"/> that is equal 
        /// to <paramref name="selectedTimestamp"/>. Only those concurrent versions with a 
        /// <paramref name="vectorTimestamp"/> less than the given <paramref name="vectorTimestamp"/>
        /// participate in the resolution process (which allows for resolutions to be concurrent to other
        /// updates).
        /// </summary>
        [Pure]
        IConcurrentVersions<TVersioned, TUpdated> Resolve(VectorTime selectedTimestamp, VectorTime vectorTimestamp, long systemTimestamp = 0L);

        /// <summary>
        /// Updates the owner.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        [Pure]
        IConcurrentVersions<TVersioned, TUpdated> WithOwner(string owner);
    }

    public static class ConcurrentVersions
    {
        /// <summary>
        /// Creates a new concurrent versions tree, that uses <paramref name="projection"/> to compute
        /// new (potentially concurrent) versions from parent versions.
        /// </summary>
        /// <typeparam name="TVersioned">Versioned value type.</typeparam>
        /// <typeparam name="TUpdated">Update type.</typeparam>
        /// <param name="init">Value of the initial versions.</param>
        /// <param name="projection">Projection function for updates.</param>
        /// <returns></returns>
        public static IConcurrentVersions<TVersioned, TUpdated> Create<TVersioned, TUpdated>(TVersioned init, Func<TVersioned, TUpdated, TVersioned> projection)
            => new ConcurrentVersionsTree<TVersioned, TUpdated>(init, projection);

        public static IConcurrentVersions<TVersioned, TUpdated> Resolve<TVersioned, TUpdated>(
            this IConcurrentVersions<TVersioned, TUpdated> versions, VectorTime selectedTimestamp)
        {
            var vt = versions.All
                .Select(x => x.VectorTimestamp)
                .Aggregate(VectorTime.Zero, (time, vectorTime) => time.Merge(vectorTime));

            var st = versions.All
                .Select(x => x.SystemTimestamp)
                .Max();

            return versions.Resolve(selectedTimestamp, vt, st);
        }

        public static bool IsConflicting<TVersioned, TUpdated>(this IConcurrentVersions<TVersioned, TUpdated> versions)
            => versions.All.Count() > 1;
    }

    /// <summary>
    /// An immutable implementation of <see cref="IConcurrentVersions{TVersioned,TUpdated}"/> that shall be used 
    /// if updates replace current versioned values.
    /// </summary>
    internal sealed class ConcurrentVersionsList<TVal> : IConcurrentVersions<TVal, TVal>
    {
        private readonly ImmutableList<Versioned<TVal>> versions;

        public ConcurrentVersionsList(ImmutableList<Versioned<TVal>> versions, string owner = "")
        {
            this.versions = versions;
            Owner = owner ?? string.Empty;
        }

        public IEnumerable<Versioned<TVal>> All => versions.Reverse();
        public string Owner { get; }

        [Pure]
        public IConcurrentVersions<TVal, TVal> Update(TVal updated, VectorTime vectorTimestamp, long systemTimestamp = 0L, string creator = "")
        {
            var r = versions.Aggregate(Tuple.Create(ImmutableList<Versioned<TVal>>.Empty, false), (t, versioned) =>
            {
                if (t.Item2) return Tuple.Create(t.Item1.Add(versioned), true);
                else
                {
                    if (vectorTimestamp > versioned.VectorTimestamp)
                        // regular update
                        return Tuple.Create(t.Item1.Add(new Versioned<TVal>(updated, vectorTimestamp, systemTimestamp, creator)), true);
                    else if (vectorTimestamp < versioned.VectorTimestamp)
                        // conflict already resolved
                        return Tuple.Create(t.Item1.Add(versioned), true);
                    else
                        // confliciting update
                        return Tuple.Create(t.Item1.Add(versioned), false);
                }
            });

            return r.Item2
                ? new ConcurrentVersionsList<TVal>(r.Item1, Owner)
                : new ConcurrentVersionsList<TVal>(r.Item1.Add(new Versioned<TVal>(updated, vectorTimestamp, systemTimestamp, creator)), Owner);
        }

        [Pure]
        public IConcurrentVersions<TVal, TVal> Resolve(VectorTime selectedTimestamp, VectorTime vectorTimestamp, long systemTimestamp = 0L)
        {
            var r = versions.Aggregate(ImmutableList<Versioned<TVal>>.Empty,
                (acc, versioned) =>
                {
                    if (versioned.VectorTimestamp == selectedTimestamp)
                        return acc.Add(new Versioned<TVal>(versioned.Value, vectorTimestamp, systemTimestamp, versioned.Creator));
                    else if (versioned.VectorTimestamp.CompareTo(vectorTimestamp) == -1)
                        return acc.Add(versioned);
                    else
                        return acc;
                });

            return new ConcurrentVersionsList<TVal>(r);
        }

        [Pure]
        public IConcurrentVersions<TVal, TVal> WithOwner(string owner) => new ConcurrentVersionsList<TVal>(versions, owner);
    }

    /// <summary>
    /// An inmutable implementation of <see cref="IConcurrentVersions{TVersioned,TUpdated}"/> that 
    /// shall be used if updates are incremental. Therefore, it is recommended not
    /// to share instances of <see cref="ConcurrentVersionsTree{TKey,TVal}"/> directly but rather 
    /// the <see cref="Versioned{TValue}"/> sequence returned by <see cref="All"/>.
    /// </summary>
    internal sealed class ConcurrentVersionsTree<TKey, TVal> : IConcurrentVersions<TKey, TVal>
    {
        #region tree node definition

        [Serializable]
        public sealed class Node
        {
            public Versioned<TKey> Versioned { get; }
            public bool Rejected { get; }
            public ImmutableList<Node> Children { get; }

            public bool IsLeaf => Children.IsEmpty;

            public Node(Versioned<TKey> versioned)
                : this(versioned, ImmutableList<Node>.Empty, false)
            {
            }

            public Node(Versioned<TKey> versioned, ImmutableList<Node> children, bool rejected)
            {
                Versioned = versioned;
                Rejected = rejected;
                Children = children;
            }

            public Node AddChild(Node child) => new Node(Versioned, Children.Add(child), Rejected);

            public Node Reject() =>
                Rejected ? this : new Node(Versioned, Children.Select(x => x.Reject()).ToImmutableList(), true);

            public Node Stamp(VectorTime vectorTimestamp, long systemTimestamp) =>
                new Node(new Versioned<TKey>(Versioned.Value, vectorTimestamp, systemTimestamp, Versioned.Creator), Children, Rejected);
        }

        #endregion

        private readonly Node root;
        private readonly Func<TKey, TVal, TKey> projection;

        public ConcurrentVersionsTree(TKey initial, Func<TKey, TVal, TKey> projection, string owner = "")
            : this(new Node(new Versioned<TKey>(initial, VectorTime.Zero)), projection, owner)
        {
        }

        public ConcurrentVersionsTree(Node root, Func<TKey, TVal, TKey> projection = null, string owner = "")
        {
            this.root = root;
            this.projection = projection ?? ((TKey x, TVal _) => x);
            Owner = owner ?? "";
        }

        public IEnumerable<Versioned<TKey>> All { get; }
        public string Owner { get; }

        [Pure]
        public IConcurrentVersions<TKey, TVal> Update(TVal updated, VectorTime vectorTimestamp, long systemTimestamp = 0L, string creator = "")
        {
            var predecessor = Predecessor(vectorTimestamp);
            return new ConcurrentVersionsTree<TKey, TVal>(predecessor.AddChild(new Node(new Versioned<TKey>(
                value: projection(predecessor.Versioned.Value, updated),
                vectorTimestamp: vectorTimestamp,
                systemTimestamp: systemTimestamp,
                creator: creator))));
        }

        [Pure]
        public IConcurrentVersions<TKey, TVal> Resolve(VectorTime selectedTimestamp, VectorTime updatedTimestamp, long systemTimestamp = 0L)
        {
            var updatedLeaves = Leaves.Select(n =>
            {
                if (n.Rejected || n.Versioned.VectorTimestamp.IsConcurrent(updatedTimestamp)) return n;
                if (n.Versioned.VectorTimestamp == selectedTimestamp) return n.Stamp(updatedTimestamp, systemTimestamp);
                return n.Reject();
            });

        }

        [Pure]
        public IConcurrentVersions<TKey, TVal> WithOwner(string owner)
        {
            return new ConcurrentVersionsTree<TKey, TVal>(root);
        }

        private IEnumerable<Node> Nodes => Aggregate(root, ImmutableList<Node>.Empty, (leaves, n) => leaves.Add(n));

        private IEnumerable<Node> Leaves => Aggregate(root, ImmutableList<Node>.Empty, (leaves, n) =>
            n.IsLeaf ? leaves.Add(n) : leaves);

        private Node Predecessor(VectorTime timestamp) => Aggregate(root, root, (candidate, n) =>
            (timestamp > n.Versioned.VectorTimestamp && n.Versioned.VectorTimestamp > candidate.Versioned.VectorTimestamp)
            ? n
            : candidate);

        private TAcc Aggregate<TAcc>(Node node, TAcc acc, Func<TAcc, Node, TAcc> func)
        {
            var acc1 = func(acc, node);
            return node.Children.IsEmpty
                ? acc1
                : node.Children.Aggregate(acc1, (acc2, n) => Aggregate<TAcc>(n, acc2, func));
        }

    }
}