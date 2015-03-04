using System;
using System.Net;
using System.Diagnostics;
using KRPC;

namespace TestServer
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            var frameTime = 50;
            // TODO: make the stream server port configurable
            var server = new KRPCServer (IPAddress.Loopback, ushort.Parse (args [0]), (ushort)(ushort.Parse (args [0])+1));
            var timeSpan = new TimeSpan ();
            server.GetUniversalTime = () => timeSpan.TotalSeconds;
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            server.Start ();
            Console.WriteLine ("Started test server...");
            while (server.Running) {
                Stopwatch timer = Stopwatch.StartNew ();
                server.Update ();
                var elapsed = timer.ElapsedMilliseconds;
                var sleep = frameTime - elapsed;
                if (sleep > 0)
                    System.Threading.Thread.Sleep ((int)sleep);
            }
            Console.WriteLine ("Test server stopped");
        }
    }
}
