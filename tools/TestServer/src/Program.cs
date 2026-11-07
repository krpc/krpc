using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Reflection;
using KRPC;
using KRPC.Server;
using KRPC.Server.TCP;
using KRPC.Service;
using KRPC.Tools;
using KRPC.Utils;
using SerialPorts = System.IO.Ports;

namespace TestServer
{
    static class MainClass
    {
        static readonly Option<string> typeOption = new Option<string> ("--type") {
            Description = "Type of server to run. Either protobuf, websockets, websockets-echo, serialio or localsocket, or several separated by commas, each of a different transport.",
            DefaultValueFactory = _ => "protobuf"
        };
        static readonly Option<string> bindOption = new Option<string> ("--bind") {
            Description = "For TCP based protocols, the address to bind the server to. If unspecified, the loopback address is used (127.0.0.1).",
            DefaultValueFactory = _ => "127.0.0.1"
        };
        static readonly Option<ushort> tcpipPortOption = new Option<ushort> ("--tcpip-port") {
            Description = "For the protobuf protocol, the port number to use. If unspecified, use an ephemeral port."
        };
        static readonly Option<ushort> websocketsPortOption = new Option<ushort> ("--websockets-port") {
            Description = "For the websockets protocols, the port number to use. If unspecified, use an ephemeral port."
        };
        static readonly Option<string> localsocketPathOption = new Option<string> ("--localsocket-path") {
            Description = "For local socket protocols, the path of the socket to use.",
            DefaultValueFactory = _ => string.Empty
        };
        static readonly Option<string> serialioPortOption = new Option<string> ("--serialio-port") {
            Description = "For SerialIO based protocols, the port name to communicate on.",
            DefaultValueFactory = _ => string.Empty
        };
        static readonly Option<uint> baudRateOption = new Option<uint> ("--baud-rate") {
            Description = "For SerialIO based protocols, the baud rate.",
            DefaultValueFactory = _ => 9600
        };
        static readonly Option<ushort> dataBitsOption = new Option<ushort> ("--data-bits") {
            Description = "For SerialIO based protocols, the number of data bits.",
            DefaultValueFactory = _ => 8
        };
        static readonly Option<SerialPorts.Parity> parityOption = new Option<SerialPorts.Parity> ("--parity") {
            Description = "For SerialIO based protocols, the parity.",
            DefaultValueFactory = _ => SerialPorts.Parity.None
        };
        static readonly Option<SerialPorts.StopBits> stopBitsOption = new Option<SerialPorts.StopBits> ("--stop-bits") {
            Description = "For SerialIO based protocols, the number of stop bits.",
            DefaultValueFactory = _ => SerialPorts.StopBits.One
        };
        static readonly Option<bool> debugOption = new Option<bool> ("--debug") {
            Description = "Set log level to 'debug', defaults to 'info'"
        };
        static readonly Option<bool> quietOption = new Option<bool> ("--quiet") {
            Description = "Set log level to 'warning'"
        };
        static readonly Option<bool> serverDebugOption = new Option<bool> ("--server-debug") {
            Description = "Output debug information about the server"
        };
        static readonly Option<bool> noFramePacingOption = new Option<bool> ("--no-frame-pacing") {
            Description = "Run the update loop as fast as it will go, rather than pacing it to 60 updates a second, and turn adaptive rate control off with it. For timing a client: paced, a call whose client takes longer than the receive timeout to send the next one is served once per update, so the round trip measures the update rate rather than the client."
        };

        public static int Main (string[] args)
        {
            var root = new RootCommand ("A kRPC test server for the client library unit tests") {
                typeOption,
                bindOption,
                tcpipPortOption,
                websocketsPortOption,
                localsocketPathOption,
                serialioPortOption,
                baudRateOption,
                dataBitsOption,
                parityOption,
                stopBitsOption,
                debugOption,
                quietOption,
                serverDebugOption,
                noFramePacingOption
            };
            root.SetAction (Run);
            return ToolCommandLine.Invoke (root, args);
        }

        static void Run (ParseResult result)
        {
            Logger.Format = "{0:G} - {1} - {2}";
            Logger.Enabled = true;
            Logger.Level = Logger.Severity.Info;
            if (result.GetValue (debugOption))
                Logger.Level = Logger.Severity.Debug;
            if (result.GetValue (quietOption))
                Logger.Level = Logger.Severity.Warning;
            bool serverDebug = result.GetValue (serverDebugOption);
            bool framePacing = !result.GetValue (noFramePacingOption);
            string type = result.GetValue (typeOption);
            string bind = result.GetValue (bindOption);
            ushort tcpipPort = result.GetValue (tcpipPortOption);
            ushort websocketsPort = result.GetValue (websocketsPortOption);
            string serialioPort = result.GetValue (serialioPortOption);
            string localsocketPath = result.GetValue (localsocketPathOption);
            uint baudRate = result.GetValue (baudRateOption);
            ushort dataBits = result.GetValue (dataBitsOption);
            SerialPorts.Parity parity = result.GetValue (parityOption);
            SerialPorts.StopBits stopBits = result.GetValue (stopBitsOption);

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

            var types = type.Split (',');
            TCPServer tcpServer = null;
            TCPServer websocketsServer = null;
            KRPC.Server.SerialIO.ByteServer serialServer = null;
            KRPC.Server.LocalSocket.LocalSocketServer socketServer = null;
            var servers = new List<Server> ();
            foreach (var serverType in types) {
                Server server;
                if (serverType == "protobuf") {
                    tcpServer = new TCPServer (bindAddress, tcpipPort);
                    var messageServer = new KRPC.Server.ProtocolBuffers.Server (tcpServer);
                    server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverTCP, "TestServer", messageServer);
                } else if (serverType == "websockets") {
                    websocketsServer = new TCPServer (bindAddress, websocketsPort);
                    var messageServer = new KRPC.Server.WebSockets.Server (websocketsServer);
                    server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverWebsockets, "TestServer", messageServer);
                } else if (serverType == "websockets-echo") {
                    websocketsServer = new TCPServer (bindAddress, websocketsPort);
                    var messageServer = new KRPC.Server.WebSockets.Server (websocketsServer, true);
                    server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverWebsockets, "TestServer", messageServer);
                } else if (serverType == "serialio") {
                    serialServer = new KRPC.Server.SerialIO.ByteServer (
                        serialioPort, baudRate, dataBits, parity, stopBits);
                    var messageServer = new KRPC.Server.SerialIO.Server (serialServer);
                    server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverSerialIO, "TestServer", messageServer);
                } else if (serverType == "localsocket") {
                    socketServer = new KRPC.Server.LocalSocket.LocalSocketServer (localsocketPath);
                    var messageServer = new KRPC.Server.ProtocolBuffers.Server (socketServer);
                    server = new Server (Guid.NewGuid (), Protocol.ProtocolBuffersOverLocalSocket, "TestServer", messageServer);
                } else {
                    Logger.WriteLine ("Server type '" + serverType + "' not supported", Logger.Severity.Error);
                    return;
                }
                core.Add (server);
                servers.Add (server);
            }

            Logger.WriteLine ("Starting server...");
            core.StartAll ();
            Logger.WriteLine ("type = " + type);
            Logger.WriteLine ("frame pacing = " + (framePacing ? "60 updates/s" : "off"));
            if (tcpServer != null || websocketsServer != null)
                Logger.WriteLine ("bind = " + bindAddress);
            if (tcpServer != null)
                Logger.WriteLine ("tcpip_port = " + tcpServer.ActualPort);
            if (websocketsServer != null)
                Logger.WriteLine ("websockets_port = " + websocketsServer.ActualPort);
            if (serialServer != null) {
                Logger.WriteLine ("serialio_port = " + serialServer.Address);
            }
            if (socketServer != null)
                Logger.WriteLine ("localsocket_path = " + socketServer.Address);
            Logger.WriteLine ("Server started successfully");

            const long targetFPS = 60;
            long update = 0;
            long ticksPerUpdate = Stopwatch.Frequency / targetFPS;
            var timer = new Stopwatch ();
            while (servers.Any (x => x.Running)) {
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
