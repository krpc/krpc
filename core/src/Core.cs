using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC
{
    /// <summary>
    /// The kRPC core, which manages the execution of remote procedures,
    /// bridging the gap between servers and services. Also stores the configuration.
    /// This class is a singleton. The instance can be obtained via the <see cref="Instance"/> property.
    /// </summary>
    public sealed class Core
    {
        /// <summary>
        /// The servers.
        /// </summary>
        public IList<Server.Server> Servers { get; private set; }

        IDictionary<Guid, IClient<Request, Response>> clients = new Dictionary<Guid, IClient<Request, Response>> ();
        // The connected clients, polled for requests in the order they connected
        RoundRobinScheduler<IClient<Request,Response>> clientScheduler = new RoundRobinScheduler<IClient<Request, Response>> ();
        // The calls to execute in this update: new requests, and calls the previous update yielded or deferred
        List<RequestContinuation> rpcContinuations = new List<RequestContinuation> ();
        Dictionary<IClient<Request,Response>, Dictionary<ulong, Service.Stream>> streams = new Dictionary<IClient<Request,Response>, Dictionary<ulong, Service.Stream>> ();
        // Each client's stream update message, reused on every update
        Dictionary<IClient<Request,Response>, StreamUpdate> cachedStreamUpdates = new Dictionary<IClient<Request,Response>, StreamUpdate> ();
        // The streams to remove, with their client, removed outside the loop over the streams
        Dictionary<ulong, IClient<Request,Response>> removeStreams = new Dictionary<ulong, IClient<Request,Response>> ();
        // The id of the next stream. Ids are shared by every client and never reused, so an id at or
        // above this one was never issued
        ulong nextStreamId = 0;

        /// <summary>
        /// The client holding the game on the current tick, and the timestamp its hold runs
        /// out at. One client at a time: what a hold offers is that nothing else happens,
        /// which two clients cannot both be given.
        /// </summary>
        IClient<Request,Response> tickHoldClient;
        long tickHoldDeadline;

        /// <summary>
        /// Whether the update is executing calls a client made. A hold only means anything
        /// while it is: a call made by a stream or an event runs after the update has finished
        /// with the calls, so a hold taken there would be found in force by the next update
        /// with no client waiting to release it, and be taken again before that one ended too.
        /// </summary>
        bool executingRPCs;

        static Core instance;

        /// <summary>
        /// The server version string, set by the server plugin on startup.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Get the core instance
        /// </summary>
        public static Core Instance {
            get {
                if (instance == null)
                    instance = new Core ();
                return instance;
            }
        }

        Core ()
        {
            Servers = new List<Server.Server> ();
            Service.Services.Init();
        }

        /// <summary>
        /// Event triggered when a server starts
        /// </summary>
        public event EventHandler<ServerStartedEventArgs> OnServerStarted;

        /// <summary>
        /// Event triggered when a server stops
        /// </summary>
        public event EventHandler<ServerStoppedEventArgs> OnServerStopped;

        /// <summary>
        /// Event triggered when a client is requesting a connection
        /// </summary>
        public event EventHandler<ClientRequestingConnectionEventArgs> OnClientRequestingConnection;

        /// <summary>
        /// Event triggered when a client has connected
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> OnClientConnected;

        /// <summary>
        /// Event triggered when a client has disconnected
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> OnClientDisconnected;

        /// <summary>
        /// Event triggered at the start of an update, before it executes any calls.
        /// </summary>
        public event EventHandler OnBeforeCalls;

        /// <summary>
        /// Event triggered once an update has executed the calls it received, before it
        /// produces stream updates.
        /// </summary>
        public event EventHandler OnAfterCalls;

        internal void ClientConnected (IClient<Request,Response> client)
        {
            clients [client.Guid] = client;
            clientScheduler.Add (client);
            streams [client] = new Dictionary<ulong, Service.Stream> ();
            cachedStreamUpdates [client] = new StreamUpdate ();
            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs (client));
        }

        internal void ClientDisconnected (IClient<Request,Response> client)
        {
            Dictionary<ulong, Service.Stream> clientStreams;
            if (streams.TryGetValue (client, out clientStreams)) {
                // Note: convert list of streams to remove to array as
                // RemoveStreamInternal modifies the collection
                foreach (var id in clientStreams.Keys.ToArray ())
                    RemoveStreamInternal (client, id);
                streams.Remove (client);
                cachedStreamUpdates.Remove (client);
            }
            clients.Remove (client.Guid);
            // A client that goes away while holding the tick does not get to keep holding it.
            if (tickHoldClient != null && tickHoldClient.Guid == client.Guid)
                ReleaseTickHold ();
            clientScheduler.Remove (client);
            EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs (client));
        }

        /// <summary>
        /// Get a list of all clients connected to the server.
        /// </summary>
        public IEnumerable<IClient> Clients {
            get { return clients.Values.Cast<IClient> (); }
        }

        /// <summary>
        /// Event triggered when a client performs some activity
        /// </summary>
        public event EventHandler<ClientActivityEventArgs> OnClientActivity;

        /// <summary>
        /// Add a server to the core.
        /// </summary>
        public void Add (Server.Server server)
        {
            Servers.Add (server);
            Configure (server);
            Logger.WriteLine ("Added server '" + server.Name + "'");
        }

        /// <summary>
        /// Remove a server from the core.
        /// </summary>
        public void Remove (Guid id)
        {
            for (var i = 0; i < Servers.Count; i++) {
                var server = Servers [i];
                if (server.Id == id) {
                    if (server.Running)
                        server.Stop ();
                    Servers.RemoveAt (i);
                    Logger.WriteLine ("Removed server '" + server.Name + "'");
                    return;
                }
            }
            throw new KeyNotFoundException (id.ToString ());
        }

        /// <summary>
        /// Replace a server object. The object is matched using the id of the replacement.
        /// </summary>
        public void Replace (Server.Server newServer)
        {
            for (var i = 0; i < Servers.Count; i++) {
                var server = Servers [i];
                if (server.Id == newServer.Id) {
                    if (server.Running)
                        server.Stop ();
                    Servers [i] = newServer;
                    Configure (newServer);
                    Logger.WriteLine ("Updated server '" + server.Name + "' to '" + newServer.Name + "'");
                    return;
                }
            }
            throw new KeyNotFoundException (newServer.Id.ToString ());
        }

        /// <summary>
        /// The settings a server runs with, its own and those every server shares, for a log
        /// line. The shared settings are named as in the settings file.
        /// </summary>
        static string SettingsText (Server.Server server)
        {
            var config = Configuration.Instance;
            var text = new StringBuilder ();
            text.Append ("protocol = ").Append (server.Protocol);
            text.Append (", address = ").Append (server.Address);
            // The serial line parameters, which the address leaves out
            var messageServer = server.InnerServer as Server.Message.Server;
            var serial = messageServer != null ? messageServer.InnerServer as Server.SerialIO.ByteServer : null;
            if (serial != null) {
                text.Append (", serialio_baud_rate = ").Append (serial.BaudRate.ToString (CultureInfo.InvariantCulture));
                text.Append (", serialio_data_bits = ").Append (serial.DataBits.ToString (CultureInfo.InvariantCulture));
                text.Append (", serialio_parity = ").Append (serial.Parity);
                text.Append (", serialio_stop_bits = ").Append (serial.StopBits);
            }
            text.Append (", logLevel = ").Append (Logger.Level);
            text.Append (", autoStartServers = ").Append (config.AutoStartServers);
            text.Append (", autoAcceptConnections = ").Append (config.AutoAcceptConnections);
            text.Append (", pauseServerWithGame = ").Append (config.PauseServerWithGame);
            text.Append (", verboseErrors = ").Append (config.VerboseErrors);
            text.Append (", oneRPCPerUpdate = ").Append (config.OneRPCPerUpdate);
            text.Append (", maxTimePerUpdate = ").Append (config.MaxTimePerUpdate.ToString (CultureInfo.InvariantCulture));
            text.Append (", adaptiveRateControl = ").Append (config.AdaptiveRateControl);
            text.Append (", blockingRecv = ").Append (config.BlockingRecv);
            text.Append (", recvTimeout = ").Append (config.RecvTimeout.ToString (CultureInfo.InvariantCulture));
            text.Append (", tickHoldTimeout = ").Append (config.TickHoldTimeout.ToString (CultureInfo.InvariantCulture));
            return text.ToString ();
        }

        void Configure (IServer server)
        {
            server.OnStarted += (s, e) => {
                Logger.WriteLine ("Server '" + ((Server.Server)s).Name + "' started on " + ((Server.Server)s).Address);
                Logger.WriteLine ("Server '" + ((Server.Server)s).Name + "' settings: " + SettingsText ((Server.Server)s));
                AnyRunning = true;
                EventHandlerExtensions.Invoke (OnServerStarted, this, new ServerStartedEventArgs ((Server.Server)s));
            };
            server.OnStopped += (s, e) => {
                Logger.WriteLine ("Server '" + ((Server.Server)s).Name + "' stopped");
                AnyRunning = Servers.Any (x => x.Running);
                // The object store and the expression constants are shared by every
                // server, so they are only emptied once they have all stopped
                if (!AnyRunning) {
                    ObjectStore.Clear ();
                    Service.KRPC.Expression.ClearConstants ();
                }
                EventHandlerExtensions.Invoke (OnServerStopped, this, new ServerStoppedEventArgs ((Server.Server)s));
            };
            server.OnClientRequestingConnection += (s, e) => EventHandlerExtensions.Invoke (OnClientRequestingConnection, this, e);
        }

        /// <summary>
        /// Start all servers.
        /// </summary>
        public void StartAll ()
        {
            foreach (var server in Servers) {
                if (!server.Running)
                    server.Start ();
            }
        }

        /// <summary>
        /// Stop all servers.
        /// </summary>
        public void StopAll ()
        {
            foreach (var server in Servers) {
                if (server.Running)
                    server.Stop ();
            }
        }

        /// <summary>
        /// Stop all servers.
        /// </summary>
        public bool AnyRunning { get; private set; }

        ExponentialMovingAverage bytesReadRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage bytesWrittenRate = new ExponentialMovingAverage (0.25);

        /// <summary>
        /// Get the total number of bytes read from the network.
        /// </summary>
        public ulong BytesRead {
            get {
                ulong read = 0;
                for (int i = 0; i < Servers.Count; i++)
                    read += Servers [i].BytesRead;
                return read;
            }
        }

        /// <summary>
        /// Get the total number of bytes written to the network.
        /// </summary>
        public ulong BytesWritten {
            get {
                ulong written = 0;
                for (int i = 0; i < Servers.Count; i++)
                    written += Servers [i].BytesWritten;
                return written;
            }
        }

        /// <summary>
        /// Get the total number of bytes read from the network.
        /// </summary>
        public float BytesReadRate {
            get { return bytesReadRate.Value; }
            set { bytesReadRate.Update (value); }
        }

        /// <summary>
        /// Get the total number of bytes written to the network.
        /// </summary>
        public float BytesWrittenRate {
            get { return bytesWrittenRate.Value; }
            set { bytesWrittenRate.Update (value); }
        }

        ExponentialMovingAverage rpcRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage timePerRPCUpdate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage pollTimePerRPCUpdate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage execTimePerRPCUpdate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage streamRPCRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage timePerStreamUpdate = new ExponentialMovingAverage (0.25);

        /// <summary>
        /// Total number of RPCs executed.
        /// </summary>
        public ulong RPCsExecuted { get; private set; }

        /// <summary>
        /// Number of RPCs processed per second.
        /// </summary>
        public float RPCRate {
            get { return rpcRate.Value; }
            set { rpcRate.Update (value); }
        }

        /// <summary>
        /// Time taken by the update loop per update, in seconds.
        /// </summary>
        public float TimePerRPCUpdate {
            get { return timePerRPCUpdate.Value; }
            set { timePerRPCUpdate.Update (value); }
        }

        /// <summary>
        /// Time taken polling for new RPCs per update, in seconds.
        /// </summary>
        public float PollTimePerRPCUpdate {
            get { return pollTimePerRPCUpdate.Value; }
            set { pollTimePerRPCUpdate.Update (value); }
        }

        /// <summary>
        /// Time taken polling executing RPCs per update, in seconds.
        /// </summary>
        public float ExecTimePerRPCUpdate {
            get { return execTimePerRPCUpdate.Value; }
            set { execTimePerRPCUpdate.Update (value); }
        }

        /// <summary>
        /// Number of currently active streaming RPCs.
        /// </summary>
        public uint StreamRPCs { get; private set; }

        /// <summary>
        /// Total number of streaming RPCs executed.
        /// </summary>
        public ulong StreamRPCsExecuted { get; private set; }

        /// <summary>
        /// Number of streaming RPCs processed per second.
        /// </summary>
        public float StreamRPCRate {
            get { return streamRPCRate.Value; }
            set { streamRPCRate.Update (value); }
        }

        /// <summary>
        /// Time taken by the stream update loop, in seconds.
        /// </summary>
        public float TimePerStreamUpdate {
            get { return timePerStreamUpdate.Value; }
            set { timePerStreamUpdate.Update (value); }
        }

        /// <summary>
        /// Clear the server statistics.
        /// </summary>
        public void ClearStats ()
        {
            RPCsExecuted = 0;
            RPCRate = 0;
            TimePerRPCUpdate = 0;
            ExecTimePerRPCUpdate = 0;
            PollTimePerRPCUpdate = 0;
            StreamRPCsExecuted = 0;
            TimePerStreamUpdate = 0;
        }

        Stopwatch updateTimer = Stopwatch.StartNew ();

        /// <summary>
        /// Update the server
        /// </summary>
        public void Update ()
        {
            ulong startRPCsExecuted = RPCsExecuted;
            ulong startStreamRPCsExecuted = StreamRPCsExecuted;
            ulong startBytesRead = BytesRead;
            ulong startBytesWritten = BytesWritten;

            // Bracket the call phase, so that the events are raised once per update however
            // many times the poll loop runs. OnAfterCalls is raised before the stream update,
            // so that streams observe what the handlers did.
            EventHandlerExtensions.Invoke (OnBeforeCalls, this);
            UpdateRPCs ();
            EventHandlerExtensions.Invoke (OnAfterCalls, this);
            UpdateStreams ();

            var timeElapsed = updateTimer.ElapsedSeconds ();
            var ticksElapsed = updateTimer.ElapsedTicks;
            updateTimer.Reset ();
            updateTimer.Start ();

            RPCRate = (float)((RPCsExecuted - startRPCsExecuted) / timeElapsed);
            StreamRPCRate = (float)((StreamRPCsExecuted - startStreamRPCsExecuted) / timeElapsed);
            BytesReadRate = (float)((BytesRead - startBytesRead) / timeElapsed);
            BytesWrittenRate = (float)((BytesWritten - startBytesWritten) / timeElapsed);

            // Adjust MaxTimePerUpdate to get a target FixedUpdate rate of 59 FPS. This is slightly smaller
            // than 60 FPS, so that it pushes against the target 60 FPS for FixedUpdate.
            // The minimum MaxTimePerUpdate that will be set is 1ms, and the maximum is 25ms.
            // If very little time is being spent executing RPCs (<1ms), MaxTimePerUpdate is set to 10ms.
            // This prevents MaxTimePerUpdate from being set to a high value when the server is idle, which would
            // cause a drop in framerate if a large burst of RPCs are received.
            var config = Configuration.Instance;
            // An update that held the tick ran for as long as the client asked. Adapting to it
            // would wind the limit down to its floor for every other client.
            if (config.AdaptiveRateControl && !heldTick) {
                var targetTicks = Stopwatch.Frequency / 59;
                if (ticksElapsed > targetTicks) {
                    if (config.MaxTimePerUpdate > 1000)
                        config.MaxTimePerUpdate -= 100;
                } else {
                    if (ExecTimePerRPCUpdate < 0.001) {
                        config.MaxTimePerUpdate = 10000;
                    } else {
                        if (config.MaxTimePerUpdate < 25000)
                            config.MaxTimePerUpdate += 100;
                    }
                }
            }
        }

        /// <summary>
        /// Whether a client held the tick during the update that just ran. Two things read it:
        /// the tick gets held once per update and no more, and an update that was held took as
        /// long as the client it was waiting for, which is not a measurement the frame rate
        /// should be adapted to.
        /// </summary>
        bool heldTick;

        Stopwatch rpcTimer = new Stopwatch ();
        Stopwatch rpcPollTimeout = new Stopwatch ();
        Stopwatch rpcPollTimer = new Stopwatch ();
        Stopwatch rpcExecTimer = new Stopwatch ();
        List<RequestContinuation> rpcYieldedContinuations = new List<RequestContinuation> ();

        /// <summary>
        /// Update the servers, called once every FixedUpdate.
        /// This method receives and executes RPCs, for up to MaxTimePerUpdate microseconds.
        /// RPCs are delayed to the next update if this time expires. If AdaptiveRateControl
        /// is true, MaxTimePerUpdate will be automatically adjusted to achieve a target framerate.
        /// If NonBlockingUpdate is false, this call will block waiting for new RPCs for up to
        /// MaxPollTimePerUpdate microseconds. If NonBlockingUpdate is true, a single non-blocking call
        /// will be made to check for new RPCs.
        /// While a client holds the tick, none of those limits apply: its RPCs are waited for and
        /// executed until it releases the tick or its hold runs out of time, so that a program can
        /// read the game state, compute with it and write the result back within one tick.
        /// </summary>
        void UpdateRPCs ()
        {
            rpcTimer.Reset ();
            rpcTimer.Start ();
            rpcPollTimeout.Reset ();
            rpcPollTimer.Reset ();
            rpcExecTimer.Reset ();
            heldTick = false;
            var config = Configuration.Instance;
            long maxTimePerUpdateTicks = StopwatchExtensions.MicrosecondsToTicks (config.MaxTimePerUpdate);
            long recvTimeoutTicks = StopwatchExtensions.MicrosecondsToTicks (config.RecvTimeout);
            ulong rpcsExecuted = 0;

            rpcYieldedContinuations.Clear ();
            for (int i = 0; i < Servers.Count; i++)
                Servers [i].InnerServer.Update ();

            while (true) {

                // Poll for RPCs
                rpcPollTimer.Start ();
                rpcPollTimeout.Reset ();
                rpcPollTimeout.Start ();
                while (true) {
                    PollRequests (rpcYieldedContinuations);
                    if (rpcContinuations.Count > 0)
                        break;
                    // Wait for the client to release the tick, so that a call it makes after
                    // seeing an earlier result is executed in this update
                    if (TickHeld) {
                        heldTick = true;
                        continue;
                    }
                    if (!config.BlockingRecv)
                        break;
                    if (rpcPollTimeout.ElapsedTicks > recvTimeoutTicks)
                        break;
                    if (rpcTimer.ElapsedTicks > maxTimePerUpdateTicks)
                        break;
                }
                rpcPollTimer.Stop ();

                if (rpcContinuations.Count == 0)
                    break;

                // Execute RPCs
                rpcExecTimer.Start ();
                executingRPCs = true;
                for (int i = 0; i < rpcContinuations.Count; i++) {
                    var continuation = rpcContinuations [i];

                    // Ignore the continuation if the client has disconnected
                    if (!continuation.Client.Connected)
                        continue;

                    // Max exec time exceeded, delay to next update. An update holding a tick
                    // runs for as long as the hold lasts instead.
                    if (!TickHeld && rpcTimer.ElapsedTicks > maxTimePerUpdateTicks) {
                        rpcYieldedContinuations.Add (continuation);
                        continue;
                    }

                    // Execute the continuation
                    try {
                        ExecuteContinuation (continuation);
                    } catch (YieldException<RequestContinuation> e) {
                        rpcYieldedContinuations.Add (e.Value);
                        ReleaseTickToYield (e.Value.Client);
                    }
                    rpcsExecuted++;
                }
                executingRPCs = false;
                rpcContinuations.Clear ();
                rpcExecTimer.Stop ();

                if (TickHeld) {
                    heldTick = true;
                    continue;
                }

                // Exit if a hold ended during this update, so that the game takes the tick
                // before another hold can defer it
                if (heldTick)
                    break;

                // Exit if only execute one RPC per update
                if (config.OneRPCPerUpdate)
                    break;

                // Exit if max exec time exceeded
                if (rpcTimer.ElapsedTicks > maxTimePerUpdateTicks)
                    break;
            }

            // Nothing leaves the loop while the tick is held, so a hold still in force here
            // belongs to an update that failed part way through
            executingRPCs = false;
            if (tickHoldClient != null) {
                Logger.WriteLine (
                    "Ending a hold on the tick left behind by client " + tickHoldClient.Label (),
                    Logger.Severity.Error);
                ReleaseTickHold ();
            }

            // Run yielded continuations on the next update
            var tmp = rpcContinuations;
            rpcContinuations = rpcYieldedContinuations;
            rpcYieldedContinuations = tmp;

            rpcTimer.Stop ();

            RPCsExecuted += rpcsExecuted;
            TimePerRPCUpdate = (float)rpcTimer.ElapsedSeconds ();
            PollTimePerRPCUpdate = (float)rpcPollTimer.ElapsedSeconds ();
            ExecTimePerRPCUpdate = (float)rpcExecTimer.ElapsedSeconds ();
        }

        Stopwatch streamTimer = new Stopwatch ();

        /// <summary>
        /// Remove the streams queued for removal, update every stream, and write each client
        /// a stream update holding the results that changed.
        /// </summary>
        void UpdateStreams ()
        {
            streamTimer.Reset ();
            streamTimer.Start ();
            uint rpcsExecuted = 0;

            if (removeStreams.Count > 0) {
                foreach (var entry in removeStreams) {
                    if (RemoveStreamInternal (entry.Value, entry.Key) && Logger.ShouldLog (Logger.Severity.Debug))
                        Logger.WriteLine ("Removed stream " + entry.Key + " for client " + entry.Value.Label (), Logger.Severity.Debug);
                }
                removeStreams.Clear();
            }

            // Run stream continuations
            if (streams.Count > 0) {
                foreach (var entry in streams) {
                    var client = entry.Key;
                    var clientStreams = entry.Value.Values;
                    if (clientStreams.Count == 0)
                        continue;
                    CallContext.Set (client);
                    // Update streams
                    bool changed = false;
                    foreach (var stream in clientStreams) {
                        // An exception fails only this stream
                        try {
                            stream.Update ();
                        } catch (System.Exception exn) {
                            stream.SetError (exn);
                        }
                        rpcsExecuted++;
                        changed |= stream.Changed;
                    }
                    // If anything changed, produce an update
                    if (changed) {
                        var streamUpdate = cachedStreamUpdates [client];
                        streamUpdate.Results.Clear ();
                        foreach (var stream in clientStreams) {
                            if (stream.Changed) {
                                var result = stream.StreamResult;
                                streamUpdate.Results.Add (result);
                                if (result.Result.HasError) {
                                    removeStreams[stream.Id] = client;
                                    if (Logger.ShouldLog (Logger.Severity.Debug))
                                        LogStreamEnded (client, stream.Id, result.Result.Error);
                                }
                            }
                        }
                        var messageStream = client.Stream as IMessageStream;
                        if (messageStream != null) {
                            messageStream.Write (streamUpdate);
                        }
                        foreach (var stream in clientStreams)
                            if (stream.Changed)
                                stream.Sent ();
                    }
                }
                CallContext.Clear ();
                if (removeStreams.Count > 0) {
                    foreach (var entry in removeStreams)
                        RemoveStreamInternal(entry.Value, entry.Key);
                    removeStreams.Clear();
                }
            }

            streamTimer.Stop ();
            StreamRPCsExecuted += rpcsExecuted;
            TimePerStreamUpdate = (float)streamTimer.ElapsedSeconds ();
        }

        /// <summary>
        /// Log the end of a stream whose last update carries an error. An event that has
        /// finished is the normal end, so only its description is given.
        /// </summary>
        static void LogStreamEnded (IClient<Request,Response> client, ulong id, Error error)
        {
            var finished = error.Service == "KRPC" && error.Name == "StreamRemovedException";
            var text = new StringBuilder ("Removed stream " + id + " for client " + client.Label () + ": ");
            if (finished) {
                text.Append (error.Description);
            } else {
                text.Append ("failed: ");
                text.Append (Describe (error));
                AppendStackTrace (text, error);
            }
            Logger.WriteLine (text.ToString (), Logger.Severity.Debug);
        }

        /// <summary>
        /// Hold the game on the current tick for a client, so that the calls it makes next are
        /// executed in this tick rather than in later ones. Yields to the next tick if this one
        /// has already been held and let go.
        /// </summary>
        internal void HoldTick (IClient caller)
        {
            if (caller == null)
                throw new ArgumentNullException (nameof (caller));
            if (!executingRPCs)
                throw new InvalidOperationException (
                    "The tick can only be held by a client making a call, " +
                    "not by a stream or an event");
            IClient<Request,Response> client;
            if (!clients.TryGetValue (caller.Guid, out client))
                throw new InvalidOperationException (
                    "No client is connected with this identifier");
            // Renewing a hold would let a client hold the game for as long as it liked
            if (TickHeld)
                throw new InvalidOperationException (
                    tickHoldClient.Guid == caller.Guid
                    ? "This client is already holding the tick"
                    : "Another client is holding the tick");
            // One hold per tick. Holding a tick that has already been held and released would
            // defer it a second time, so the call waits for the next tick
            if (heldTick)
                throw new YieldException<Action> (() => HoldTick (caller));
            tickHoldClient = client;
            tickHoldDeadline = Stopwatch.GetTimestamp () +
                StopwatchExtensions.MicrosecondsToTicks (Configuration.Instance.TickHoldTimeout);
            Logger.WriteLine ("Client " + client.Label () + " is holding the tick",
                              Logger.Severity.Debug);
        }

        /// <summary>
        /// Let the game move on from the current tick. Does nothing if the client is not
        /// holding it, which is what a client whose hold has already ended sees.
        /// </summary>
        internal void ReleaseTick (IClient caller)
        {
            if (caller == null)
                throw new ArgumentNullException (nameof (caller));
            if (tickHoldClient == null || tickHoldClient.Guid != caller.Guid)
                return;
            Logger.WriteLine ("Client " + tickHoldClient.Label () + " released the tick",
                              Logger.Severity.Debug);
            ReleaseTickHold ();
        }

        /// <summary>
        /// Let the game move on from the current tick and hold the one after it, so that a
        /// client works on consecutive ticks rather than on whichever it manages to ask for in
        /// time. The hold on the next tick is taken by the yield the second call makes: the
        /// caller's continuation waits in the server and is carried on at the start of the next
        /// update, ahead of anything the server has to be polled for, so there is no window for
        /// the game to advance through.
        /// </summary>
        internal void NextTick (IClient caller)
        {
            ReleaseTick (caller);
            HoldTick (caller);
        }

        /// <summary>
        /// Whether a client is holding the game on this tick. A hold that has run out of time,
        /// or whose client has gone away, ends here: the client cannot end it itself, and an
        /// update must not be left waiting for one that will never end.
        /// </summary>
        bool TickHeld {
            get {
                if (tickHoldClient == null)
                    return false;
                if (Stopwatch.GetTimestamp () >= tickHoldDeadline) {
                    Logger.WriteLine (
                        "Client " + tickHoldClient.Label () + " held the tick for longer than the " +
                        "timeout allows, so the game has moved on without it",
                        Logger.Severity.Warning);
                    ReleaseTickHold ();
                    return false;
                }
                // Clients are only reconciled between updates, so an update holding a tick
                // checks for itself that the client is still connected
                if (!clients.ContainsKey (tickHoldClient.Guid) || !tickHoldClient.Connected) {
                    Logger.WriteLine (
                        "Client " + tickHoldClient.Label () + " disconnected while holding the tick",
                        Logger.Severity.Warning);
                    ReleaseTickHold ();
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// End a client's hold on the tick because a call it made needs another update to
        /// finish. Only a later update can carry on such a call, which is exactly what the hold
        /// prevents, so waiting the hold out would leave the client and the game waiting for
        /// each other until the timeout.
        /// </summary>
        void ReleaseTickToYield (IClient<Request,Response> client)
        {
            if (tickHoldClient == null || tickHoldClient.Guid != client.Guid)
                return;
            Logger.WriteLine (
                "Client " + client.Label () + " called a procedure that takes more than one " +
                "update to finish while holding the tick, so the hold has ended",
                Logger.Severity.Warning);
            ReleaseTickHold ();
        }

        void ReleaseTickHold ()
        {
            tickHoldClient = null;
            tickHoldDeadline = 0;
        }

        /// <summary>
        /// Add a stream to the server, with a new id.
        /// </summary>
        internal ulong AddStream (IClient caller, Service.Stream stream)
        {
            IClient<Request,Response> client;
            if (!clients.TryGetValue (caller.Guid, out client))
                throw new InvalidOperationException ("No client is connected with this identifier");

            stream.Id = nextStreamId;
            nextStreamId++;

            var streamId = stream.Id;
            streams [client] [streamId] = stream;
            Logger.WriteLine ("Added stream " + streamId + " for client " + client.Label (), Logger.Severity.Debug);
            StreamRPCs++;
            return streamId;
        }

        /// <summary>
        /// Set the update rate for a stream.
        /// </summary>
        internal void SetStreamRate(IClient caller, ulong streamId, float rate)
        {
            var stream = GetStream (caller, streamId);
            if (stream == null)
                return;
            stream.Rate = rate;
            if (Logger.ShouldLog (Logger.Severity.Debug))
                Logger.WriteLine (
                    "Set the rate of stream " + streamId + " for client " + caller.Label () + " to " +
                    rate.ToString (CultureInfo.InvariantCulture) + " Hz", Logger.Severity.Debug);
        }

        /// <summary>
        /// Remove a stream from the server, for a given client.
        /// </summary>
        internal void RemoveStream (IClient caller, ulong streamId)
        {
            if (GetStream (caller, streamId) == null)
                return;
            removeStreams[streamId] = clients [caller.Guid];
        }

        /// <summary>
        /// The client's stream with the given id, or null if the stream has ended.
        /// Throws an exception if the id has never been issued.
        /// </summary>
        Service.Stream GetStream (IClient caller, ulong streamId)
        {
            IClient<Request,Response> client;
            if (!clients.TryGetValue (caller.Guid, out client))
                throw new InvalidOperationException ("No client is connected with this identifier");
            if (streamId >= nextStreamId)
                throw new ArgumentException ("No stream exists with id " + streamId, nameof (streamId));
            Service.Stream stream;
            streams [client].TryGetValue (streamId, out stream);
            return stream;
        }

        /// <summary>
        /// Remove a client's stream. Returns false if the client has no stream with the id.
        /// </summary>
        bool RemoveStreamInternal (IClient<Request,Response> client, ulong id)
        {
            if (!streams.ContainsKey (client) || !streams [client].ContainsKey (id))
                return false;
            streams [client].Remove (id);
            var stream = client.Stream as IMessageStream;
            if (stream != null)
                stream.StreamRemoved (id);
            StreamRPCs--;
            return true;
        }

        HashSet<IClient<Request,Response>> pollRequestsCurrentClients = new HashSet<IClient<Request, Response>> ();

        /// <summary>
        /// Send a response carrying nothing but an error, for a request that could not be
        /// turned into a call to make.
        /// </summary>
        static void SendErrorResponse (IClient<Request,Response> client, ulong id, Error error)
        {
            var response = new Response ();
            response.Id = id;
            response.Error = error;
            client.Stream.Write (response);
        }

        /// <summary>
        /// Poll connected clients for new requests.
        /// Adds a continuation to the queue for any client with a new request,
        /// if a continuation is not already being processed for the client.
        /// </summary>
        void PollRequests (IList<RequestContinuation> yieldedContinuations)
        {
            if (clientScheduler.Empty)
                return;
            pollRequestsCurrentClients.Clear ();
            for (int i = 0; i < rpcContinuations.Count; i++)
                pollRequestsCurrentClients.Add (rpcContinuations [i].Client);
            for (int i = 0; i < yieldedContinuations.Count; i++)
                pollRequestsCurrentClients.Add (yieldedContinuations [i].Client);
            var item = clientScheduler.Items.First;
            while (item != null) {
                var client = item.Value;
                var stream = client.Stream;
                try {
                    if (!pollRequestsCurrentClients.Contains (client) && stream.DataAvailable) {
                        Request request = stream.Read ();
                        EventHandlerExtensions.Invoke (OnClientActivity, this, new ClientActivityEventArgs (client));
                        rpcContinuations.Add (new RequestContinuation (client, request));
                    }
                } catch (KRPC.Server.Message.RequestDecodeException e) {
                    // The request arrived intact but names something the server cannot give,
                    // such as an object it has reclaimed. Report a failed call, and leave the
                    // connection open
                    var error = KRPC.Service.Services.Instance.HandleException (e.InnerException ?? e);
                    if (Logger.ShouldLog (Logger.Severity.Debug)) {
                        var text = new StringBuilder ("Client " + client.Label () + " sent a request that failed: " + Describe (error));
                        AppendStackTrace (text, error);
                        Logger.WriteLine (text.ToString (), Logger.Severity.Debug);
                    }
                    SendErrorResponse (client, e.Id, error);
                } catch (System.Exception e) {
                    // No request exists to tie a response to, so the connection ends. The
                    // connection logs the end of the session
                    if (Logger.ShouldLog (Logger.Severity.Debug))
                        Logger.WriteLine ("Failed to read a request from client " + client.Label () + ": " + e, Logger.Severity.Debug);
                    var messageStream = client.Stream as IMessageStream;
                    if (messageStream != null) {
                        messageStream.Disconnect (DisconnectReason.MalformedMessage, "Failed to read a request: " + e.Message);
                    } else {
                        Logger.WriteLine ("Failed to read a request from client " + client.Label () + ": " + e.Message, Logger.Severity.Warning);
                        client.Stream.Close ();
                    }
                }
                item = item.Next;
            }
        }

        /// <summary>
        /// Execute the continuation and send a response to the client,
        /// or throw a YieldException if the continuation is not complete.
        /// </summary>
        static void ExecuteContinuation (RequestContinuation continuation)
        {
            var client = continuation.Client;

            // Run the continuation, and either return a result, an error,
            // or throw a YieldException if the continuation has not completed
            Response response;
            try {
                CallContext.Set (client);
                response = continuation.Run ();
            } catch (YieldException) {
                throw;
            } catch (RPCException e) {
                response = new Response { Id = continuation.Id, Error = Service.Services.Instance.HandleException (e) };
            } catch (System.Exception e) {
                response = new Response { Id = continuation.Id, Error = Service.Services.Instance.HandleException (e) };
            } finally {
                CallContext.Clear ();
            }

            // Send response to the client
            client.Stream.Write (response);
            if (Logger.ShouldLog (Logger.Severity.Debug))
                LogCalls (continuation, response);
        }

        /// <summary>
        /// Log the calls in a request and the error of each one that failed, as one line.
        /// The stack traces of the errors follow it.
        /// </summary>
        static void LogCalls (RequestContinuation continuation, Response response)
        {
            var text = new StringBuilder ("Client ");
            text.Append (continuation.Client.Label ());
            if (response.HasError) {
                text.Append (" sent a request that failed: ");
                text.Append (Describe (response.Error));
                AppendStackTrace (text, response.Error);
            } else {
                text.Append (" called ");
                var calls = continuation.Calls;
                for (int i = 0; i < calls.Count; i++) {
                    if (i > 0)
                        text.Append (", ");
                    var procedure = calls [i].Procedure;
                    text.Append (procedure != null ? procedure.FullyQualifiedName : "an unknown procedure");
                    var result = i < response.Results.Count ? response.Results [i] : null;
                    if (result != null && result.HasError) {
                        text.Append (" (failed: ");
                        text.Append (Describe (result.Error));
                        text.Append (')');
                    }
                }
                foreach (var result in response.Results)
                    if (result.HasError)
                        AppendStackTrace (text, result.Error);
            }
            Logger.WriteLine (text.ToString (), Logger.Severity.Debug);
        }

        /// <summary>
        /// An error's name and description, for a log line.
        /// </summary>
        static string Describe (Error error)
        {
            return string.IsNullOrEmpty (error.Name) ? error.Description : error.Name + ": " + error.Description;
        }

        /// <summary>
        /// Append an error's stack trace on the lines after a log line, if it has one.
        /// </summary>
        static void AppendStackTrace (StringBuilder text, Error error)
        {
            if (!string.IsNullOrEmpty (error.StackTrace)) {
                text.Append (Environment.NewLine);
                text.Append (error.StackTrace.TrimEnd ());
            }
        }
    }
}
