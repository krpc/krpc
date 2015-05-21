using System;
using System.Net;
using System.Diagnostics;
using KRPC;
using KRPC.Service;
using KRPC.Utils;
using System.Linq;

namespace TestServer
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            KRPC.Service.Scanner.Scanner.GetServices ();
            var cmdargs = args.ToList ();
            if (cmdargs.Contains ("--debug")) {
                cmdargs.Remove ("--debug");
                Logger.Enabled = true;
                Logger.Level = Logger.Severity.Debug;
            } else if (cmdargs.Contains ("--quiet")) {
                cmdargs.Remove ("--quiet");
                Logger.Enabled = true;
                Logger.Level = Logger.Severity.Warning;
            } else {
                Logger.Enabled = true;
                Logger.Level = Logger.Severity.Info;
            }
            if (args.Contains ("--help") || args.Contains ("-h")) {
                Help ();
                return;
            }
            ushort rpcPort, streamPort;
            try {
                rpcPort = ushort.Parse (cmdargs [0]);
                streamPort = ushort.Parse (cmdargs [1]);
            } catch (Exception) {
                Help ();
                return;
            }
            const int frameTime = 50;
            var server = new KRPCServer (IPAddress.Loopback, rpcPort, streamPort);
            KRPCServer.Context.SetGameScene (GameScene.SpaceCenter);
            var timeSpan = new TimeSpan ();
            server.GetUniversalTime = () => timeSpan.TotalSeconds;
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            server.Start ();
            while (server.Running) {
                Stopwatch timer = Stopwatch.StartNew ();
                server.Update ();
                var elapsed = timer.ElapsedMilliseconds;
                var sleep = frameTime - elapsed;
                if (sleep > 0)
                    System.Threading.Thread.Sleep ((int)sleep);
            }
        }

        static void Help ()
        {
            Console.WriteLine ("TestServer.exe RPCPORT STREAMPORT [--debug]");
        }
    }
}
