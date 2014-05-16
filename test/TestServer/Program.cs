using System;
using System.Net;
using KRPC;

namespace TestServer
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            var server = new KRPCServer (IPAddress.Loopback, ushort.Parse (args [0]));
            var timeSpan = new TimeSpan ();
            server.GetUniversalTime = () => timeSpan.TotalSeconds;
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            server.Start ();
            Console.WriteLine ("Started test server...");
            while (server.Running)
                server.Update ();
            Console.WriteLine ("Test server stopped");
        }
    }
}
