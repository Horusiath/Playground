#region copyright
// -----------------------------------------------------------------------
//  <copyright file="PerformanceMetrics.cs" company="Akka.NET project">
//      Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2017 Akka.NET project <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;

namespace Akka.Cluster.Sharding
{
    public class PerformanceMetrics : IEquatable<PerformanceMetrics>, IComparable<PerformanceMetrics>
    {
        public readonly float CpuUsage;
        public readonly float FreeMemory;

        public PerformanceMetrics(float cpuUsage, float freeMemory)
        {
            CpuUsage = cpuUsage;
            FreeMemory = freeMemory;
        }
        

        public int CompareTo(PerformanceMetrics other) => GetNormalized().CompareTo(other.GetNormalized());

        public bool Equals(PerformanceMetrics other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CpuUsage.Equals(other.CpuUsage) && FreeMemory.Equals(other.FreeMemory);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PerformanceMetrics) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CpuUsage.GetHashCode() * 397) ^ FreeMemory.GetHashCode();
            }
        }

        public override string ToString() => $"[cpu: {CpuUsage}%, free memory: {FreeMemory}B]";

        private float GetNormalized()
        {
            return CpuUsage * FreeMemory;
        }
    }
}