using System;
using System.Net;
using System.Diagnostics;
using KRPC;
using KRPC.Utils;

namespace TestServer
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            Logger.Enabled = (args.Length > 1 && args[1] == "log");
            var frameTime = 50;
            var server = new KRPCServer (IPAddress.Loopback, ushort.Parse (args [0]), ushort.Parse (args [1]));
            var timeSpan = new TimeSpan ();
            server.GetUniversalTime = () => timeSpan.TotalSeconds;
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            server.Start ();
            Logger.WriteLine ("Started test server...");
            while (server.Running) {
                Stopwatch timer = Stopwatch.StartNew ();
                server.Update ();
                var elapsed = timer.ElapsedMilliseconds;
                var sleep = frameTime - elapsed;
                if (sleep > 0)
                    System.Threading.Thread.Sleep ((int)sleep);
            }
            Logger.WriteLine ("Test server stopped");
        }
    }
}
