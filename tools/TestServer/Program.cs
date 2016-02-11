using KRPC;
using KRPC.Service;
using KRPC.Utils;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace TestServer
{
    class MainClass
    {
        static KRPCServer server;

        static void Help (OptionSet options)
        {
            Console.WriteLine ("usage: TestServer.exe [-h] [-v] [--rpc_port=VALUE] [--stream_port=VALUE]");
            Console.WriteLine ("                      [--debug] [--quiet] [--server-debug] [--write-ports PATH]");
            Console.WriteLine ("");
            Console.WriteLine ("A kRPC test server for the client library unit tests");
            Console.WriteLine ("");
            options.WriteOptionDescriptions (Console.Out);
        }

        public static void Main (string[] args)
        {
            bool showHelp = false;
            bool showVersion = false;

            Logger.Enabled = true;
            Logger.Level = Logger.Severity.Info;
            RPCException.VerboseErrors = false;
            bool serverDebug = false;
            String writePortsPath = null;
            ushort rpcPort = 0;
            ushort streamPort = 0;

            var options = new OptionSet ()
            {
                { "h|help", "show this help message and exit", v => showHelp = v != null },
                { "v|version", "show program's version number and exit", v => showVersion = v != null },
                { "rpc-port=", "Port number to use for the RPC server. If unspecified, use an ephemeral port.",
                  (ushort v) => rpcPort = v },
                { "stream-port=", "Port number to use for the stream server. If unspecified, use an ephemeral port.",
                  (ushort v) => streamPort = v },
                { "debug", "Set log level to 'debug', defaults to 'info'", v =>
                    {
                        if (v != null)
                        {
                            Logger.Enabled = true;
                            Logger.Level = Logger.Severity.Debug;
                            RPCException.VerboseErrors = true;
                        }
                    }
                },
                { "quiet", "Set log level to 'warning'", v =>
                    {
                        if (v != null)
                        {
                            Logger.Enabled = true;
                            Logger.Level = Logger.Severity.Warning;
                            RPCException.VerboseErrors = false;
                        }
                    }
                },
                { "server-debug", "Output debug information about the server", v => serverDebug = v != null },
                { "write-ports=", "Write the server's port numbers to the given {FILE}", v => writePortsPath = v },
            };
            options.Parse (args);

            if (showHelp) {
                Help (options);
                return;
            }

            if (showVersion) {
                var assembly = Assembly.GetEntryAssembly();
                var info = FileVersionInfo.GetVersionInfo (assembly.Location);
                var version = String.Format("{0}.{1}.{2}", info.FileMajorPart, info.FileMinorPart, info.FileBuildPart);
                Console.WriteLine ("TestServer.exe version " + version);
                return;
            }

            KRPC.Service.Scanner.Scanner.GetServices ();
            server = new KRPCServer (IPAddress.Loopback, rpcPort, streamPort);
            KRPCServer.Context.SetGameScene (GameScene.SpaceCenter);
            var timeSpan = new TimeSpan ();
            server.GetUniversalTime = () => timeSpan.TotalSeconds;
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            server.Start ();

            if (writePortsPath != null)
                System.IO.File.WriteAllText (writePortsPath, server.RPCPort.ToString()+"\n"+server.StreamPort.ToString()+"\n");

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
    }
}
