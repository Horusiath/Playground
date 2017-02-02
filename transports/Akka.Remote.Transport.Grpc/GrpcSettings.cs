#region copyright
// -----------------------------------------------------------------------
//  <copyright file="GrpcSettings.cs" company="Akka.NET project">
//      Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2017 Akka.NET project <https://github.com/akkadotnet>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Net;
using Akka.Configuration;

namespace Akka.Remote.Transport.Grpc
{
    public class GrpcSettings
    {
        public static GrpcSettings Create(Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config), $"No HOCON config was provided for {nameof(GrpcTransport)}");
            
            return new GrpcSettings(
                host: config.GetString("hostname", "localhost"), 
                port: config.GetInt("port"));
        }

        public readonly string Host;
        public readonly int Port;

        public GrpcSettings(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}