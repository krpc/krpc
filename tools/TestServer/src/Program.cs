using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Reflection;
using KRPC;
using KRPC.Server;
using KRPC.Server.TCP;
using KRPC.Service;
using KRPC.Utils;
using NDesk.Options;
#if NET
using SerialPorts = System.IO.Ports;
#else
using SerialPorts = KRPC.IO.Ports;
#endif

namespace TestServer
{
    static class MainClass
    {
        static void Help (OptionSet options)
        {
            Console.WriteLine ("usage: TestServer.exe [-h] [-v] [--type=TYPE]");
            Console.WriteLine ("                      [--bind=ADDRESS] [--rpc_port=VALUE] [--stream_port=VALUE]");
            Console.WriteLine ("                      [--port=PATH]");
            Console.WriteLine ("                      [--no-frame-pacing]");
            Console.WriteLine ("                      [--debug] [--quiet] [--server-debug]");
            Console.WriteLine ();
            Console.WriteLine ("A kRPC test server for the client library unit tests");
            Console.WriteLine ();
            options.WriteOptionDescriptions (Console.Out);
        }

        public static void Main (string[] args)
        {
            bool showHelp = false;
            bool showVersion = false;

            Logger.Format = "{0:G} - {1} - {2}";
            Logger.Enabled = true;
            Logger.Level = Logger.Severity.Info;
            bool serverDebug = false;
            bool framePacing = true;
            string type = "protobuf";
            string bind = "127.0.0.1";
            ushort rpcPort = 0;
            ushort streamPort = 0;
            string portName = string.Empty;
            string rpcPath = string.Empty;
            string streamPath = string.Empty;
            uint baudRate = 9600;
            ushort dataBits = 8;
            SerialPorts.Parity parity = SerialPorts.Parity.None;
            SerialPorts.StopBits stopBits = SerialPorts.StopBits.One;

            var options = new OptionSet { {
                    "h|help", "show this help message and exit",
                    v => showHelp = v != null
                }, {
                    "v|version", "show program's version number and exit",
                    v => showVersion = v != null
                }, {
                    "type=", "Type of server to run. Either protobuf, websockets, websockets-echo, or serialio.",
                    v => type = v
                }, {
                    "bind=", "For TCP based protocols, the address to bind the server to. If unspecified, the loopback address is used (127.0.0.1).",
                    v => bind = v
                }, {
                    "rpc-port=", "For TCP based protocols, the port number to use for the RPC server. If unspecified, use an ephemeral port.",
                    (ushort v) => rpcPort = v
                }, {
                    "stream-port=", "For TCP based protocols, the port number to use for the stream server. If unspecified, use an ephemeral port.",
                    (ushort v) => streamPort = v
                }, {
                    "rpc-path=", "For local socket protocols, the path of the socket to use for the RPC server.",
                    v => rpcPath = v
                }, {
                    "stream-path=", "For local socket protocols, the path of the socket to use for the stream server.",
                    v => streamPath = v
                }, {
                    "port=", "For SerialIO based protocols, the port name to communicate on.",
                    v => portName = v
                }, {
                    "baud-rate=", "For SerialIO based protocols, the baud rate.",
                    (uint v) => baudRate = v
                }, {
                    "data-bits=", "For SerialIO based protocols, the number of data bits.",
                    (ushort v) => dataBits = v
                }, {
                    "parity=", "For SerialIO based protocols, the parity.",
                    (SerialPorts.Parity v) => parity = v
                }, {
                    "stop-bits=", "For SerialIO based protocols, the number of stop bits.",
                    (SerialPorts.StopBits v) => stopBits = v
                }, {
                    "debug", "Set log level to 'debug', defaults to 'info'",
                    v => {
                        if (v != null) {
                            Logger.Enabled = true;
                            Logger.Level = Logger.Severity.Debug;
                        }
                    }
                }, { "quiet", "Set log level to 'warning'",
                    v => {
                        if (v != null) {
                            Logger.Enabled = true;
                            Logger.Level = Logger.Severity.Warning;
                        }
                    }
                }, {
                    "server-debug", "Output debug information about the server",
                    v => serverDebug = v != null
                }, {
                    "no-frame-pacing", "Run the update loop as fast as it will go, rather than pacing it to 60 updates a second, and turn adaptive rate control off with it. For timing a client: paced, a call whose client takes longer than the receive timeout to send the next one is served once per update, so the round trip measures the update rate rather than the client.",
                    v => framePacing = v == null
                }
            };
            options.Parse (args);

            if (showHelp) {
                Help (options);
                return;
            }

            if (showVersion) {
                var assembly = Assembly.GetEntryAssembly ();
                var info = FileVersionInfo.GetVersionInfo (assembly.Location);
                var version = string.Format ("{0}.{1}.{2}", info.FileMajorPart, info.FileMinorPart, info.FileBuildPart);
                Console.WriteLine ("TestServer.exe version " + version);
                return;
            }

            // Services are found by scanning the assemblies that are loaded, and the runtime
            // does not load one until something in it is used. Nothing in this program calls
            // into the benchmark service, so name a type from it to get it loaded before the
            // scan. KSP loads every assembly it installs, so the game has no such problem.
            Logger.WriteLine (
                "Loaded " + typeof (KRPC.Benchmarks.Benchmark).Assembly.GetName ().Name);

            var core = Core.Instance;
            // Adaptive rate control keys off how long an update took against a target update
            // rate. Unpaced there is no such rate, so all it does is wander MaxTimePerUpdate
            // around, which is run to run variation in a measurement for nothing.
            if (!framePacing)
                Configuration.Instance.AdaptiveRateControl = false;
            var serverVersion = Assembly.GetEntryAssembly ().GetName ().Version;
            core.Version = serverVersion.Major + "." + serverVersion.Minor + "." + serverVersion.Build;
            CallContext.GameScene = GameScene.SpaceCenter;
            core.OnClientRequestingConnection += (s, e) => e.Request.Allow ();

            IPAddress bindAddress;
            if (!IPAddress.TryParse(bind, out bindAddress)) {
                Console.WriteLine("Failed to parse bind address.");
                return;
            }

            TCPServer rpcTcpServer = null;
            TCPServer streamTcpServer = null;
            KRPC.Server.SerialIO.ByteServer serialServer = null;
            KRPC.Server.LocalSocket.LocalSocketServer rpcSocketServer = null;
            KRPC.Server.LocalSocket.LocalSocketServer streamSocketServer = null;
            if (type == "protobuf" || type == "websockets" || type == "websockets-echo") {
                rpcTcpServer = new TCPServer (bindAddress, rpcPort);
                streamTcpServer = new TCPServer (bindAddress, streamPort);
            }
            if (type == "serialio")
                serialServer = new KRPC.Server.SerialIO.ByteServer (
                    portName, baudRate, dataBits, parity, stopBits);
            if (type == "localsocket") {
                rpcSocketServer = new KRPC.Server.LocalSocket.LocalSocketServer (rpcPath);
                streamSocketServer = new KRPC.Server.LocalSocket.LocalSocketServer (streamPath);
            }

            Server server;
            if (type == "protobuf") {
                var rpcServer = new KRPC.Server.ProtocolBuffers.RPCServer (rpcTcpServer);
                var streamServer = new KRPC.Server.ProtocolBuffers.StreamServer (streamTcpServer);
                server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverTCP, "TestServer", rpcServer, streamServer);
            } else if (type == "websockets") {
                var rpcServer = new KRPC.Server.WebSockets.RPCServer (rpcTcpServer);
                var streamServer = new KRPC.Server.WebSockets.StreamServer (streamTcpServer);
                server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverWebsockets, "TestServer", rpcServer, streamServer);
            } else if (type == "websockets-echo") {
                var rpcServer = new KRPC.Server.WebSockets.RPCServer (rpcTcpServer, true);
                var streamServer = new KRPC.Server.WebSockets.StreamServer (streamTcpServer);
                server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverWebsockets, "TestServer", rpcServer, streamServer);
            } else if (type == "serialio") {
                var rpcServer = new KRPC.Server.SerialIO.RPCServer (serialServer);
                var streamServer = new KRPC.Server.SerialIO.StreamServer ();
                server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverSerialIO, "TestServer", rpcServer, streamServer);
            } else if (type == "localsocket") {
                var rpcServer = new KRPC.Server.ProtocolBuffers.RPCServer (rpcSocketServer);
                var streamServer = new KRPC.Server.ProtocolBuffers.StreamServer (streamSocketServer);
                server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverLocalSocket, "TestServer", rpcServer, streamServer);
            } else {
                Logger.WriteLine ("Server type '" + type + "' not supported", Logger.Severity.Error);
                return;
            }
            core.Add (server);

            Logger.WriteLine ("Starting server...");
            core.StartAll ();
            Logger.WriteLine ("type = " + type);
            Logger.WriteLine ("frame pacing = " + (framePacing ? "60 updates/s" : "off"));
            if (rpcTcpServer != null) {
                Logger.WriteLine ("bind = " + bindAddress);
                Logger.WriteLine ("rpc_port = " + rpcTcpServer.ActualPort);
                if (streamTcpServer != null)
                    Logger.WriteLine ("stream_port = " + streamTcpServer.ActualPort);
            }
            if (serialServer != null) {
                Logger.WriteLine ("port = " + serialServer.Address);
            }
            if (rpcSocketServer != null) {
                Logger.WriteLine ("rpc_path = " + rpcSocketServer.Address);
                Logger.WriteLine ("stream_path = " + streamSocketServer.Address);
            }
            Logger.WriteLine ("Server started successfully");

            const long targetFPS = 60;
            long update = 0;
            long ticksPerUpdate = Stopwatch.Frequency / targetFPS;
            var timer = new Stopwatch ();
            while (server.Running) {
                timer.Reset ();
                timer.Start ();

                core.Update ();

                if (serverDebug && update % targetFPS == 0) {
                    // Output details about whether server.Update() took too little or too long to execute
                    var elapsed = timer.ElapsedTicks;
                    var diffTicks = Math.Abs (ticksPerUpdate - elapsed);
                    var diffTime = Math.Round (diffTicks / (double)Stopwatch.Frequency * 1000d, 2);
                    if (elapsed > ticksPerUpdate)
                        Console.WriteLine ("Slow by " + diffTime + " ms (" + diffTicks + " ticks)");
                    else
                        Console.WriteLine ("Fast by " + diffTime + " ms (" + diffTicks + " ticks)");
                }

                // Wait, to force 60 FPS. Sleep most of the remaining time, so the process
                // does not burn a core during profiling and benchmarks. Unpaced, the next
                // update follows immediately, which costs a core and makes a round trip
                // measure the client and not the update it landed in.
                if (framePacing) {
                    var remainingTicks = ticksPerUpdate - timer.ElapsedTicks;
                    if (remainingTicks > Stopwatch.Frequency / 1000) {
                        var sleepMs = (int)(remainingTicks * 1000L / Stopwatch.Frequency) - 1;
                        if (sleepMs > 0)
                            Thread.Sleep (sleepMs);
                    }
                    while (timer.ElapsedTicks < ticksPerUpdate) {
                    }
                }
                update++;
            }
        }
    }
}
