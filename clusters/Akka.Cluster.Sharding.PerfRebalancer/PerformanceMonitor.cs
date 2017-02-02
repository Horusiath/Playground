#region copyright
// -----------------------------------------------------------------------
//  <copyright file="PerformanceMonitor.cs" company="Akka.NET project">
//      Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2017 Akka.NET project <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.DistributedData;

namespace Akka.Cluster.Sharding
{
    internal sealed class PerformanceMonitor : ReceiveActor
    {
        class CollectMetrics
        {
            public static readonly CollectMetrics Instance = new CollectMetrics();
            private CollectMetrics() { }
        }

        public static Actor.Props Props(TimeSpan updateInterval)
            => Actor.Props.Create(() => new PerformanceMonitor(updateInterval)).WithDeploy(Deploy.Local);

        private readonly PerformanceCounter cpuCounter;
        private readonly PerformanceCounter memCounter;
        private readonly ICancelable cancelUpdates;

        private LWWDictionary<UniqueAddress, PerformanceMetrics> lastResult = LWWDictionary<UniqueAddress, PerformanceMetrics>.Empty;

        public PerformanceMonitor(TimeSpan updateInterval)
        {
            // schedule periodical updates
            this.cancelUpdates = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(updateInterval, updateInterval, Self, CollectMetrics.Instance, ActorRefs.NoSender);

            var cluster = Cluster.Get(Context.System);

            // create and start performance counters for a local node
            this.cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            this.memCounter = new PerformanceCounter("Memory", "Available MBytes");
            cpuCounter.NextValue();
            memCounter.NextValue();

            // assign to a distributed store of performance metrics
            var perfCounterKey = new LWWDictionaryKey<UniqueAddress, PerformanceMetrics>("perf-counters");
            var replicator = DistributedData.DistributedData.Get(Context.System).Replicator;
            replicator.Tell(Dsl.Subscribe(perfCounterKey, Self));

            Receive<Replicator.Changed>(changed =>
            {
                lastResult = lastResult.Merge(changed.Get(perfCounterKey));
            });
            Receive<CollectMetrics>(_ =>
            {
                var cpuUsage = cpuCounter.NextValue();
                var memUsage = memCounter.NextValue();
                lastResult = lastResult.SetItem(cluster, cluster.SelfUniqueAddress, new PerformanceMetrics(cpuUsage, memUsage));

                replicator.Tell(Dsl.Update(perfCounterKey, lastResult, WriteLocal.Instance, null, map => lastResult.Merge(map)));
            });
        }

        protected override void PostStop()
        {
            base.PostStop();
            this.cancelUpdates.Cancel();
            this.cpuCounter.Dispose();
            this.memCounter.Dispose();
        }
    }
}