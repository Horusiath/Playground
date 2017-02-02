#region copyright
// -----------------------------------------------------------------------
//  <copyright file="PerformanceBasedAllocationStrategy.cs" company="Akka.NET project">
//      Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2017 Akka.NET project <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Cluster.Sharding
{
    public class PerformanceBasedAllocationStrategy : IShardAllocationStrategy
    {
        private readonly IActorRef performanceMonitor;

        private readonly double maxCpuUtilization;
        private readonly double maxMemoryUtilization;

        public PerformanceBasedAllocationStrategy(ActorSystem system)
        {
            var updateInterval = TimeSpan.FromSeconds(3);
            performanceMonitor = system.ActorOf(PerformanceMonitor.Props(updateInterval));
        }

        /// <summary>
        /// Invoked when the location of a new shard is to be decided.
        /// </summary>
        /// <param name="requester">
        /// Actor reference to the <see cref="ShardRegion"/> that requested the location of the shard, can be returned 
        /// if preference should be given to the node where the shard was first accessed.
        /// </param>
        /// <param name="shardId">The id of the shard to allocate.</param>
        /// <param name="currentShardAllocations">
        /// All actor refs to <see cref="ShardRegion"/> and their current allocated shards, in the order they were allocated
        /// </param>
        /// <returns>
        /// <see cref="Task"/> of the actor ref of the <see cref="ShardRegion"/> that is to be responsible for the shard,
        /// must be one of the references included in the <paramref name="currentShardAllocations"/> parameter.
        /// </returns>
        public Task<IActorRef> AllocateShard(IActorRef requester, string shardId, IImmutableDictionary<IActorRef, IImmutableList<string>> currentShardAllocations)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Invoked periodically to decide which shards to rebalance to another location.
        /// </summary>
        /// <param name="currentShardAllocations">
        /// All actor refs to <see cref="ShardRegion"/> and their current allocated shards, in the order they were allocated.
        /// </param>
        /// <param name="rebalanceInProgress">
        /// Set of shards that are currently being rebalanced, i.e. you should not include these in the returned set.
        /// </param>
        /// <returns><see cref="Task"/> of the shards to be migrated, may be empty to skip rebalance in this round. </returns>
        public Task<IImmutableSet<string>> Rebalance(IImmutableDictionary<IActorRef, IImmutableList<string>> currentShardAllocations, IImmutableSet<string> rebalanceInProgress)
        {
            throw new System.NotImplementedException();
        }
    }
}