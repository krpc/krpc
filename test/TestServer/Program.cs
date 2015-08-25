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
        static KRPCServer server;

        public static void Main (string[] args)
        {
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
            bool serverDebug = cmdargs.Contains ("--server-debug");
            cmdargs.Remove ("--server-debug");
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

            KRPC.Service.Scanner.Scanner.GetServices ();
            server = new KRPCServer (IPAddress.Loopback, rpcPort, streamPort);
            KRPCServer.Context.SetGameScene (GameScene.SpaceCenter);
            var timeSpan = new TimeSpan ();
            server.GetUniversalTime = () => timeSpan.TotalSeconds;
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            server.Start ();

            const long targetFPS = 60;
            long update = 0;
            long ticksPerUpdate = Stopwatch.Frequency / targetFPS;
            var timer = new Stopwatch ();
            while (server.Running) {
                timer.Reset ();
                timer.Start ();

                server.Update ();

                if (serverDebug && update % targetFPS == 0) {
                    // Output details about whether server.Update() took too little or too long to execute
                    var elapsed = timer.ElapsedTicks;
                    var diffTicks = Math.Abs (ticksPerUpdate - elapsed);
                    var diffTime = Math.Round ((double)diffTicks / (double)Stopwatch.Frequency * 1000d, 2);
                    if (elapsed > ticksPerUpdate)
                        Console.WriteLine ("Slow by " + diffTime + " ms (" + diffTicks + " ticks)");
                    else
                        Console.WriteLine ("Fast by " + diffTime + " ms (" + diffTicks + " ticks)");
                }

                // Wait, to force 60 FPS
                while (timer.ElapsedTicks < ticksPerUpdate) {
                }
                update++;
            }
        }

        static void Help ()
        {
            Console.WriteLine ("TestServer.exe RPCPORT STREAMPORT [--debug]");
        }
    }
}
