using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        IDictionary<Guid, IClient<Request, Response>> rpcClients = new Dictionary<Guid, IClient<Request, Response>> ();
        IDictionary<Guid, IClient<NoMessage, StreamUpdate>> streamClients = new Dictionary<Guid, IClient<NoMessage, StreamUpdate>> ();
        RoundRobinScheduler<IClient<Request,Response>> clientScheduler = new RoundRobinScheduler<IClient<Request, Response>> ();
        List<RequestContinuation> rpcContinuations = new List<RequestContinuation> ();
        Dictionary<IClient<NoMessage,StreamUpdate>, Dictionary<ulong, Service.Stream>> streams = new Dictionary<IClient<NoMessage,StreamUpdate>, Dictionary<ulong, Service.Stream>> ();
        Dictionary<IClient<NoMessage,StreamUpdate>, StreamUpdate> cachedStreamUpdates = new Dictionary<IClient<NoMessage,StreamUpdate>, StreamUpdate> ();
        Dictionary<ulong, IClient<NoMessage, StreamUpdate>> removeStreams = new Dictionary<ulong, IClient<NoMessage, StreamUpdate>> ();
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
        /// Event triggered when an RPC client is requesting a connection
        /// </summary>
        public event EventHandler<ClientRequestingConnectionEventArgs> OnClientRequestingConnection;

        /// <summary>
        /// Event triggered when a RPC client has connected
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> OnClientConnected;

        /// <summary>
        /// Event triggered when a RPC client has disconnected
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

        internal void RPCClientConnected (IClient<Request,Response> client)
        {
            rpcClients [client.Guid] = client;
            clientScheduler.Add (client);
            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs (client));
        }

        internal void RPCClientDisconnected (IClient<Request,Response> client)
        {
            rpcClients.Remove (client.Guid);
            // A client that goes away while holding the tick does not get to keep holding it.
            if (tickHoldClient != null && tickHoldClient.Guid == client.Guid)
                ReleaseTickHold ();
            clientScheduler.Remove (client);
            EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs (client));
        }

        internal void StreamClientConnected (IClient<NoMessage,StreamUpdate> client)
        {
            streamClients [client.Guid] = client;
            streams [client] = new Dictionary<ulong, Service.Stream>();
            cachedStreamUpdates [client] = new StreamUpdate ();
        }

        internal void StreamClientDisconnected (IClient<NoMessage,StreamUpdate> client)
        {
            // Note: convert list of streams to remove to array as
            // RemoveStreamInternal modifies the collection
            foreach (var id in streams [client].Keys.ToArray ())
                RemoveStreamInternal (client, id);
            streamClients.Remove (client.Guid);
            streams.Remove (client);
            cachedStreamUpdates.Remove (client);
        }

        /// <summary>
        /// Get a list of all RPC clients connected to the server.
        /// </summary>
        public IEnumerable<IClient> RPCClients {
            get { return rpcClients.Values.Cast<IClient> (); }
        }

        /// <summary>
        /// Get a list of all Stream clients connected to the server.
        /// </summary>
        public IEnumerable<IClient> StreamClients {
            get { return streamClients.Values.Cast<IClient> (); }
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
                    Logger.WriteLine ("Removed server '" + server.Name);
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
                    Logger.WriteLine ("Updated server '" + server.Name + " to '" + newServer.Name);
                    return;
                }
            }
            throw new KeyNotFoundException (newServer.Id.ToString ());
        }

        void Configure (IServer server)
        {
            server.OnStarted += (s, e) => {
                Logger.WriteLine ("Server '" + ((Server.Server)s).Name + "' started");
                AnyRunning = true;
                EventHandlerExtensions.Invoke (OnServerStarted, this, new ServerStartedEventArgs ((Server.Server)s));
            };
            server.OnStopped += (s, e) => {
                Logger.WriteLine ("Server '" + ((Server.Server)s).Name + "' stopped");
                AnyRunning = Servers.Any (x => x.Running);
                // The object store is shared by every server, so it is only emptied
                // once they have all stopped
                if (!AnyRunning)
                    ObjectStore.Clear ();
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
            RPCServerUpdate ();
            EventHandlerExtensions.Invoke (OnAfterCalls, this);
            StreamServerUpdate ();

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
        /// Update the RPC server, called once every FixedUpdate.
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
        void RPCServerUpdate ()
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
                Servers [i].RPCServer.Update ();

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
                    "Ending a hold on the tick left behind by client " + tickHoldClient.Address,
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
        /// Update the Stream server. Executes all streaming RPCs and sends the results to clients (if they have changed).
        /// </summary>
        void StreamServerUpdate ()
        {
            streamTimer.Reset ();
            streamTimer.Start ();
            uint rpcsExecuted = 0;

            // Update stream servers
            for (int i = 0; i < Servers.Count; i++)
                Servers [i].StreamServer.Update ();

            if (removeStreams.Count > 0) {
                foreach (var entry in removeStreams) {
                    Logger.WriteLine("Removing stream " + entry.Key, Logger.Severity.Debug);
                    RemoveStreamInternal(entry.Value, entry.Key);
                }
                removeStreams.Clear();
            }

            // Run stream continuations
            if (streams.Count > 0) {
                foreach (var entry in streams) {
                    var streamClient = entry.Key;
                    var streamClientAddress = streamClient.Address;
                    var id = streamClient.Guid;
                    var clientStreams = entry.Value.Values;
                    if (clientStreams.Count == 0)
                        continue;
                    if (!rpcClients.ContainsKey (id))
                        continue;
                    CallContext.Set (rpcClients [id]);
                    // Update streams
                    bool changed = false;
                    foreach (var stream in clientStreams) {
                        if (stream.Started) {
                            stream.Update();
                            rpcsExecuted++;
                            changed |= stream.Changed;
                        }
                    }
                    // If anything changed, produce an update
                    if (changed) {
                        var streamUpdate = cachedStreamUpdates [streamClient];
                        streamUpdate.Results.Clear ();
                        foreach (var stream in clientStreams) {
                            if (stream.Changed) {
                                var result = stream.StreamResult;
                                streamUpdate.Results.Add (result);
                                if (result.Result.HasError)
                                    removeStreams[stream.Id] = streamClient;
                            }
                        }
                        try {
                            streamClient.Stream.Write (streamUpdate);
                        } catch (ServerException exn) {
                            Logger.WriteLine ("Failed to send stream update to client " + streamClientAddress + Environment.NewLine + exn, Logger.Severity.Error);
                        }
                        Logger.WriteLine ("Sent stream update to client " + streamClientAddress, Logger.Severity.Debug);
                        foreach (var stream in clientStreams)
                            if (stream.Changed)
                                stream.Sent ();
                    }
                }
                CallContext.Clear ();
                if (removeStreams.Count > 0) {
                    foreach (var entry in removeStreams) {
                        Logger.WriteLine("Removing stream as it returned an error", Logger.Severity.Debug);
                        RemoveStreamInternal(entry.Value, entry.Key);
                    }
                    removeStreams.Clear();
                }
            }

            streamTimer.Stop ();
            StreamRPCsExecuted += rpcsExecuted;
            TimePerStreamUpdate = (float)streamTimer.ElapsedSeconds ();
        }

        /// <summary>
        /// Hold the game on the current tick for a client, so that the calls it makes next are
        /// executed in this tick rather than in later ones. Yields to the next tick if this one
        /// has already been held and let go.
        /// </summary>
        internal void HoldTick (IClient rpcClient)
        {
            if (rpcClient == null)
                throw new ArgumentNullException (nameof (rpcClient));
            if (!executingRPCs)
                throw new InvalidOperationException (
                    "The tick can only be held by a client making a call, " +
                    "not by a stream or an event");
            IClient<Request,Response> client;
            if (!rpcClients.TryGetValue (rpcClient.Guid, out client))
                throw new InvalidOperationException (
                    "No RPC client is connected with this identifier");
            // Renewing a hold would let a client hold the game for as long as it liked
            if (TickHeld)
                throw new InvalidOperationException (
                    tickHoldClient.Guid == rpcClient.Guid
                    ? "This client is already holding the tick"
                    : "Another client is holding the tick");
            // One hold per tick. Holding a tick that has already been held and released would
            // defer it a second time, so the call waits for the next tick
            if (heldTick)
                throw new YieldException<Action> (() => HoldTick (rpcClient));
            tickHoldClient = client;
            tickHoldDeadline = Stopwatch.GetTimestamp () +
                StopwatchExtensions.MicrosecondsToTicks (Configuration.Instance.TickHoldTimeout);
            Logger.WriteLine ("Client " + client.Address + " is holding the tick",
                              Logger.Severity.Debug);
        }

        /// <summary>
        /// Let the game move on from the current tick. Does nothing if the client is not
        /// holding it, which is what a client whose hold has already ended sees.
        /// </summary>
        internal void ReleaseTick (IClient rpcClient)
        {
            if (rpcClient == null)
                throw new ArgumentNullException (nameof (rpcClient));
            if (tickHoldClient == null || tickHoldClient.Guid != rpcClient.Guid)
                return;
            Logger.WriteLine ("Client " + tickHoldClient.Address + " released the tick",
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
        internal void NextTick (IClient rpcClient)
        {
            ReleaseTick (rpcClient);
            HoldTick (rpcClient);
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
                        "Client " + tickHoldClient.Address + " held the tick for longer than the " +
                        "timeout allows, so the game has moved on without it",
                        Logger.Severity.Warning);
                    ReleaseTickHold ();
                    return false;
                }
                // Clients are only reconciled between updates, so an update holding a tick
                // checks for itself that the client is still connected
                if (!rpcClients.ContainsKey (tickHoldClient.Guid) || !tickHoldClient.Connected) {
                    Logger.WriteLine (
                        "Client " + tickHoldClient.Address + " disconnected while holding the tick",
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
                "Client " + client.Address + " called a procedure that takes more than one " +
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
        /// Add a stream to the server.
        /// </summary>
        internal ulong AddStream (IClient rpcClient, Service.Stream stream, bool requireNew = true)
        {
            var id = rpcClient.Guid;
            if (!streamClients.ContainsKey (id))
                throw new InvalidOperationException ("No stream client is connected for this RPC client");
            var streamClient = streamClients [id];

            foreach (var entry in streams [streamClient]) {
                if (stream == entry.Value) {
                    // Prevent race condition when removing and re-adding streams by checking if the added stream was marked for removal
                    removeStreams.Remove(entry.Key);
                    if (requireNew)
                        throw new ArgumentException ("Stream already exists", nameof (stream));
                    return entry.Key;
                }
            }

            stream.Id = nextStreamId;
            nextStreamId++;

            var streamId = stream.Id;
            streams [streamClient] [streamId] = stream;
            Logger.WriteLine ("Added stream " + streamId + " for client " + streamClient.Address, Logger.Severity.Debug);
            StreamRPCs++;
            return streamId;
        }

        /// <summary>
        /// Start a stream.
        /// </summary>
        internal void StartStream(IClient rpcClient, ulong streamId)
        {
            var id = rpcClient.Guid;
            if (!streamClients.ContainsKey(id))
                throw new InvalidOperationException("No stream client is connected for this RPC client");
            var streamClient = streamClients[id];
            if (!streams[streamClient].ContainsKey(streamId))
                throw new InvalidOperationException("Stream does not exist with this id");
            streams [streamClient] [streamId].Start ();
            Logger.WriteLine("Started stream " + streamId + " for client " + streamClient.Address, Logger.Severity.Debug);
        }

        /// <summary>
        /// Set the update rate for a stream.
        /// </summary>
        internal void SetStreamRate(IClient rpcClient, ulong streamId, float rate)
        {
            var id = rpcClient.Guid;
            if (!streamClients.ContainsKey(id))
                throw new InvalidOperationException("No stream client is connected for this RPC client");
            var streamClient = streamClients[id];

            streams [streamClient] [streamId].Rate = rate;
            Logger.WriteLine("Set rate for stream for client " + streamClient.Address, Logger.Severity.Debug);
        }

        /// <summary>
        /// Remove a stream from the server, for a given client.
        /// </summary>
        internal void RemoveStream (IClient rpcClient, ulong streamId)
        {
            var id = rpcClient.Guid;
            if (!streamClients.ContainsKey (id))
                throw new InvalidOperationException ("No stream client is connected for this RPC client");
            var streamClient = streamClients [id];
            var clientStreams = streams [streamClient];
            if (!clientStreams.ContainsKey (streamId))
                return;
            removeStreams[streamId] = streamClient;
        }

        /// <summary>
        /// Remove a stream from the server, for all clients.
        /// </summary>
        internal void RemoveStream (ulong streamId)
        {
            foreach (var entry in streams) {
                var streamClient = entry.Key;
                var clientStreams = entry.Value;
                if (clientStreams.ContainsKey (streamId))
                    removeStreams[streamId] = streamClient;
            }
        }

        private void RemoveStreamInternal (IClient<NoMessage,StreamUpdate> client, ulong id)
        {
            if (streams.ContainsKey (client) && streams [client].ContainsKey (id)) {
                streams [client].Remove (id);
                Logger.WriteLine ("Removed stream " + id + " for client " + client.Address, Logger.Severity.Debug);
                StreamRPCs--;
            }
        }

        HashSet<IClient<Request,Response>> pollRequestsCurrentClients = new HashSet<IClient<Request, Response>> ();

        /// <summary>
        /// Send a response carrying nothing but an error, for a request that could not be
        /// turned into a call to make.
        /// </summary>
        static void SendErrorResponse (IClient<Request,Response> client, Error error)
        {
            var response = new Response ();
            response.Error = error;
            try {
                client.Stream.Write (response);
                Logger.WriteLine ("Sent error response to client " + client.Address + " (" + error + ")", Logger.Severity.Debug);
            } catch (ServerException exn) {
                Logger.WriteLine ("Failed to send error response to client " + client.Address + Environment.NewLine + exn, Logger.Severity.Error);
            }
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
                        if (Logger.ShouldLog (Logger.Severity.Debug)) {
                            var calls = string.Join(
                                ", ", request.Calls.Select(call => call.ServiceId > 0 ? call.ServiceId + "." + call.ProcedureId : call.Service + "." + call.Procedure).ToArray());
                            Logger.WriteLine ("Received request from client " + client.Address + " (" + calls + ")", Logger.Severity.Debug);
                        }
                        var requestContinuation = new RequestContinuation (client, request);
                        rpcContinuations.Add (requestContinuation);
                        if (Logger.ShouldLog (Logger.Severity.Debug)) {
                            var calls = string.Join(", ", requestContinuation.Calls.Select(call => call.Procedure.FullyQualifiedName).ToArray());
                            Logger.WriteLine ("Decoded request from client " + client.Address + " (" + calls + ")", Logger.Severity.Debug);
                        }
                    }
                } catch (ClientDisconnectedException) {
                    Logger.WriteLine ("Client " + client.Address + " disconnected");
                    client.Stream.Close ();
                    continue;
                } catch (ServerException e) {
                    Logger.WriteLine ("Error receiving request from client " + client.Address + ": " + e.Message, Logger.Severity.Error);
                    client.Stream.Close ();
                    continue;
                } catch (KRPC.Server.Message.RequestDecodeException e) {
                    // The request arrived intact but names something the server cannot give,
                    // such as an object it has reclaimed. Report a failed call, and leave the
                    // connection open
                    SendErrorResponse (client, KRPC.Service.Services.Instance.HandleException (e.InnerException ?? e));
                } catch (System.Exception e) {
                    if (Logger.ShouldLog (Logger.Severity.Debug))
                        Logger.WriteLine (e.Message + Environment.NewLine + e.StackTrace, Logger.Severity.Error);
                    SendErrorResponse (client, new Error (
                        "Error receiving message" + Environment.NewLine + e.Message, e.StackTrace));
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
                response = new Response { Error = Service.Services.Instance.HandleException (e) };
            } catch (System.Exception e) {
                response = new Response { Error = Service.Services.Instance.HandleException (e) };
            } finally {
                CallContext.Clear ();
            }

            // Send response to the client
            try {
                client.Stream.Write (response);
                if (Logger.ShouldLog (Logger.Severity.Debug)) {
                    if (response.HasError)
                        Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.Error + ")", Logger.Severity.Debug);
                    else
                        Logger.WriteLine ("Sent response to client " + client.Address, Logger.Severity.Debug);
                }
            } catch (ServerException exn) {
                Logger.WriteLine ("Failed to send response to client " + client.Address + Environment.NewLine + exn, Logger.Severity.Error);
            }
        }
    }
}
