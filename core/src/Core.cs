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

        IDictionary<Guid, IClient<IMessageStream>> clients = new Dictionary<Guid, IClient<IMessageStream>> ();
        // The connected clients, polled for requests in the order they connected
        List<IClient<IMessageStream>> clientOrder = new List<IClient<IMessageStream>> ();
        // Each client's wait policy, by client identifier
        Dictionary<Guid, CallWait> callWaits = new Dictionary<Guid, CallWait> ();
        // The calls to execute in this update: new requests, and calls the previous update yielded or deferred
        List<RequestContinuation> rpcContinuations = new List<RequestContinuation> ();
        Dictionary<IClient<IMessageStream>, Dictionary<ulong, Service.Stream>> streams = new Dictionary<IClient<IMessageStream>, Dictionary<ulong, Service.Stream>> ();
        // Each client's stream update message, reused on every update
        Dictionary<IClient<IMessageStream>, StreamUpdate> cachedStreamUpdates = new Dictionary<IClient<IMessageStream>, StreamUpdate> ();
        // The streams to remove, with their client, removed outside the loop over the streams
        Dictionary<ulong, IClient<IMessageStream>> removeStreams = new Dictionary<ulong, IClient<IMessageStream>> ();
        // The id of the next stream. Ids are shared by every client and never reused, so an id at or
        // above this one was never issued
        ulong nextStreamId = 0;

        /// <summary>
        /// The client holding the game on the current tick, and the timestamp its hold runs
        /// out at. One client at a time: what a hold offers is that nothing else happens,
        /// which two clients cannot both be given.
        /// </summary>
        IClient<IMessageStream> tickHoldClient;
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

        internal void ClientConnected (IClient<IMessageStream> client)
        {
            clients [client.Guid] = client;
            clientOrder.Add (client);
            callWaits [client.Guid] = new CallWait ();
            streams [client] = new Dictionary<ulong, Service.Stream> ();
            cachedStreamUpdates [client] = new StreamUpdate ();
            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs (client));
        }

        internal void ClientDisconnected (IClient<IMessageStream> client)
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
            clientOrder.Remove (client);
            callWaits.Remove (client.Guid);
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
            text.Append (", adaptiveRateControl = ").Append (config.AdaptiveRateControl);
            text.Append (", targetFrameRate = ").Append (config.TargetFrameRate.ToString (CultureInfo.InvariantCulture));
            text.Append (", maxTimePerFrame = ").Append (config.MaxTimePerFrame.ToString (CultureInfo.InvariantCulture));
            text.Append (", maxCallWait = ").Append (config.MaxCallWait.ToString (CultureInfo.InvariantCulture));
            text.Append (", tickHoldTimeout = ").Append (config.TickHoldTimeout.ToString (CultureInfo.InvariantCulture));
            return text.ToString ();
        }

        void Configure (Server.Server server)
        {
            server.OnStarted += (s, e) => {
                Logger.WriteLine ("Server '" + server.Name + "' started on " + server.Address);
                Logger.WriteLine ("Server '" + server.Name + "' settings: " + SettingsText (server));
                AnyRunning = true;
                EventHandlerExtensions.Invoke (OnServerStarted, this, new ServerStartedEventArgs (server));
            };
            server.OnStopped += (s, e) => {
                Logger.WriteLine ("Server '" + server.Name + "' stopped");
                AnyRunning = Servers.Any (x => x.Running);
                // The object store and the expression constants are shared by every
                // server, so they are only emptied once they have all stopped
                if (!AnyRunning) {
                    ObjectStore.Clear ();
                    Service.KRPC.Expression.ClearConstants ();
                }
                EventHandlerExtensions.Invoke (OnServerStopped, this, new ServerStoppedEventArgs (server));
            };
            server.OnClientRequestingConnection += (s, e) => EventHandlerExtensions.Invoke (OnClientRequestingConnection, this, e);
        }

        /// <summary>
        /// Start every server that is not running.
        /// </summary>
        /// <remarks>
        /// A server that fails to start leaves the others to start. Throws a
        /// <see cref="ServerException"/> naming each failed server, once every server has been tried.
        /// </remarks>
        public void StartAll ()
        {
            var failures = new List<KeyValuePair<Server.Server, ServerException>> ();
            foreach (var server in Servers) {
                if (server.Running)
                    continue;
                try {
                    server.Start ();
                } catch (ServerException exn) {
                    failures.Add (new KeyValuePair<Server.Server, ServerException> (server, exn));
                }
            }
            if (failures.Count == 0)
                return;
            var message = string.Join (
                "; ", failures.Select (x => "Failed to start server '" + x.Key.Name + "': " + x.Value.Message).ToArray ());
            throw new ServerException (message, failures [0].Value);
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
            private set { bytesReadRate.Update (value); }
        }

        /// <summary>
        /// Get the total number of bytes written to the network.
        /// </summary>
        public float BytesWrittenRate {
            get { return bytesWrittenRate.Value; }
            private set { bytesWrittenRate.Update (value); }
        }

        ExponentialMovingAverage rpcRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage streamEvaluationRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage frameRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage timePerFrame = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage execTimePerFrame = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage waitTimePerFrame = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage streamTimePerFrame = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage budgetBoundShare = new ExponentialMovingAverage (0.25);

        /// <summary>
        /// Total number of RPCs executed.
        /// </summary>
        public ulong RPCsExecuted { get; private set; }

        /// <summary>
        /// Number of RPCs processed per second.
        /// </summary>
        public float RPCRate {
            get { return rpcRate.Value; }
            private set { rpcRate.Update (value); }
        }

        /// <summary>
        /// Frames per second.
        /// </summary>
        public float FrameRate {
            get { return frameRate.Value; }
        }

        /// <summary>
        /// The server's time per frame, in seconds: executing calls, waiting for them and
        /// updating streams.
        /// </summary>
        public float TimePerFrame {
            get { return timePerFrame.Value; }
        }

        /// <summary>
        /// Time spent executing calls per frame, in seconds.
        /// </summary>
        public float ExecTimePerFrame {
            get { return execTimePerFrame.Value; }
        }

        /// <summary>
        /// Time spent waiting for calls per frame, in seconds.
        /// </summary>
        public float WaitTimePerFrame {
            get { return waitTimePerFrame.Value; }
        }

        /// <summary>
        /// Time spent updating streams per frame, in seconds.
        /// </summary>
        public float StreamTimePerFrame {
            get { return streamTimePerFrame.Value; }
        }

        /// <summary>
        /// The share of recent frames in which the budget stopped the server, from 0 to 1.
        /// </summary>
        public float BudgetBoundShare {
            get { return budgetBoundShare.Value; }
        }

        /// <summary>
        /// The median of the clients' recent turnarounds, the time from a response being sent
        /// to the client's next request arriving, in seconds. Zero with none measured.
        /// </summary>
        public float MedianTurnaround {
            get {
                turnarounds.Clear ();
                foreach (var callWait in callWaits.Values)
                    callWait.CopyTurnarounds (turnarounds);
                if (turnarounds.Count == 0)
                    return 0;
                turnarounds.Sort ();
                return (float)turnarounds [turnarounds.Count / 2] / Stopwatch.Frequency;
            }
        }

        readonly List<long> turnarounds = new List<long> ();

        /// <summary>
        /// Number of calls left for the next update, because the update ran out of time or
        /// the call yielded.
        /// </summary>
        public uint DeferredCalls { get; private set; }

        /// <summary>
        /// Number of open streams, events included.
        /// </summary>
        public uint Streams { get; private set; }

        /// <summary>
        /// Total number of stream evaluations, one per stream per update.
        /// </summary>
        public ulong StreamEvaluations { get; private set; }

        /// <summary>
        /// Total number of stream update messages sent to clients.
        /// </summary>
        public ulong StreamUpdatesSent { get; private set; }

        /// <summary>
        /// Number of stream evaluations per second.
        /// </summary>
        public float StreamEvaluationRate {
            get { return streamEvaluationRate.Value; }
            private set { streamEvaluationRate.Update (value); }
        }

        /// <summary>
        /// Clear the server statistics: the totals, the averages and each server's bytes read
        /// and written.
        /// </summary>
        public void ClearStats ()
        {
            RPCsExecuted = 0;
            StreamEvaluations = 0;
            StreamUpdatesSent = 0;
            foreach (var average in new [] {
                bytesReadRate, bytesWrittenRate, rpcRate, streamEvaluationRate, frameRate,
                timePerFrame, execTimePerFrame, waitTimePerFrame, streamTimePerFrame,
                budgetBoundShare })
                average.Reset ();
            for (int i = 0; i < Servers.Count; i++)
                Servers [i].ClearStats ();
        }

        Stopwatch updateTimer = Stopwatch.StartNew ();

        // Running totals for the benchmarks: the number of updates, and the stopwatch ticks
        // spent in them, in polling for calls, in executing calls and in updating streams
        internal ulong Updates;
        internal long UpdateTicks;
        internal long PollTicks;
        internal long ExecTicks;
        internal long StreamTicks;

        /// <summary>
        /// Update the server
        /// </summary>
        public void Update ()
        {
            long updateStart = Stopwatch.GetTimestamp ();
            ulong startRPCsExecuted = RPCsExecuted;
            ulong startStreamEvaluations = StreamEvaluations;
            ulong startBytesRead = BytesRead;
            ulong startBytesWritten = BytesWritten;

            // Bracket the call phase, so that the events are raised once per update however
            // many times the poll loop runs. OnAfterCalls is raised before the stream update,
            // so that streams observe what the handlers did.
            ConfigureBudget ();
            EventHandlerExtensions.Invoke (OnBeforeCalls, this);
            UpdateRPCs (frameBudget.Allowance (frameSpent));
            EventHandlerExtensions.Invoke (OnAfterCalls, this);
            UpdateStreams ();
            Updates++;
            UpdateTicks += Stopwatch.GetTimestamp () - updateStart;

            var timeElapsed = updateTimer.ElapsedSeconds ();
            updateTimer.Reset ();
            updateTimer.Start ();

            RPCRate = (float)((RPCsExecuted - startRPCsExecuted) / timeElapsed);
            StreamEvaluationRate = (float)((StreamEvaluations - startStreamEvaluations) / timeElapsed);
            BytesReadRate = (float)((BytesRead - startBytesRead) / timeElapsed);
            BytesWrittenRate = (float)((BytesWritten - startBytesWritten) / timeElapsed);

            frameSpent += Stopwatch.GetTimestamp () - updateStart;
            if (heldTick)
                frameHeld = true;
            if (!hostEndsFrames)
                FrameEnded (Stopwatch.GetTimestamp (), 0);
        }

        readonly FrameBudget frameBudget = new FrameBudget ();

        // When the current frame began, or zero before the first frame; the server's time in
        // it, in all and executing, waiting and updating streams; whether the budget stopped
        // the server in it; and whether a client held a tick
        long frameStart;
        long frameSpent;
        long frameExec;
        long frameWait;
        long frameStreams;
        bool frameBound;
        bool frameHeld;

        // Whether the host marks the end of each frame. Until it does, each update is a frame
        bool hostEndsFrames;

        /// <summary>
        /// Mark the end of a frame. Called by the host once a frame, on the game thread. A host
        /// that never calls it has each update taken as a frame.
        /// </summary>
        public void EndFrame ()
        {
            hostEndsFrames = true;
            FrameEnded (Stopwatch.GetTimestamp (), 0);
        }

        /// <summary>
        /// Mark the end of a frame, given its length in seconds as the game measured it.
        /// Called by the host once a frame, on the game thread.
        /// </summary>
        /// <remarks>
        /// For a host whose calls come at a point in the frame that moves, such as after the
        /// frame's fixed updates, whose work varies.
        /// </remarks>
        public void EndFrame (double period)
        {
            hostEndsFrames = true;
            FrameEnded (Stopwatch.GetTimestamp (), (long)(period * Stopwatch.Frequency));
        }

        void FrameEnded (long now, long period)
        {
            if (period <= 0 && frameStart != 0)
                period = now - frameStart;
            if (frameStart != 0 && period > 0) {
                frameBudget.FrameEnded (period, frameSpent, frameBound, frameHeld);
                double frequency = Stopwatch.Frequency;
                frameRate.Update ((float)(frequency / period));
                timePerFrame.Update ((float)(frameSpent / frequency));
                execTimePerFrame.Update ((float)(frameExec / frequency));
                waitTimePerFrame.Update ((float)(frameWait / frequency));
                streamTimePerFrame.Update ((float)(frameStreams / frequency));
                budgetBoundShare.Update (frameBound ? 1f : 0f);
            }
            frameStart = now;
            frameSpent = 0;
            frameExec = 0;
            frameWait = 0;
            frameStreams = 0;
            frameBound = false;
            frameHeld = false;
        }

        /// <summary>
        /// Apply the rate settings to the budget, which a change restarts from the maximum.
        /// </summary>
        void ConfigureBudget ()
        {
            var config = Configuration.Instance;
            frameBudget.Configure (config.AdaptiveRateControl, config.TargetFrameRate, config.MaxTimePerFrame);
        }

        /// <summary>
        /// The time the server may spend per frame, in microseconds. Adaptive rate control
        /// adjusts it, up to <see cref="Configuration.MaxTimePerFrame"/>.
        /// </summary>
        public uint BudgetPerFrame {
            get {
                ConfigureBudget ();
                return (uint)(frameBudget.Budget * 1000000 / Stopwatch.Frequency);
            }
        }

        /// <summary>
        /// Whether the budget per frame is at its floor, 1 ms or the max time per frame if lower.
        /// </summary>
        public bool BudgetAtFloor {
            get {
                ConfigureBudget ();
                return frameBudget.AtFloor;
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
        Stopwatch rpcPollTimer = new Stopwatch ();
        Stopwatch rpcExecTimer = new Stopwatch ();
        List<RequestContinuation> rpcYieldedContinuations = new List<RequestContinuation> ();

        /// <summary>
        /// Update the servers, called once every FixedUpdate.
        /// This method receives and executes RPCs, for up to <paramref name="allowance"/>
        /// stopwatch ticks, its share of the frame's budget. RPCs are delayed to the next
        /// update if this time expires.
        /// With no request waiting, it waits for a client's next call as <see cref="CallWait"/>
        /// decides, up to <see cref="Configuration.MaxCallWait"/>.
        /// While a client holds the tick, none of those limits apply: its RPCs are waited for and
        /// executed until it releases the tick or its hold runs out of time, so that a program can
        /// read the game state, compute with it and write the result back within one tick.
        /// </summary>
        void UpdateRPCs (long allowance)
        {
            rpcTimer.Reset ();
            rpcTimer.Start ();
            long rpcStart = Stopwatch.GetTimestamp ();
            rpcPollTimer.Reset ();
            rpcExecTimer.Reset ();
            heldTick = false;
            var config = Configuration.Instance;
            long maxCallWaitTicks = StopwatchExtensions.MicrosecondsToTicks (config.MaxCallWait);
            ulong rpcsExecuted = 0;

            rpcYieldedContinuations.Clear ();
            for (int i = 0; i < Servers.Count; i++)
                Servers [i].InnerServer.Update ();

            while (true) {

                // Poll for RPCs, waiting for bytes to arrive between polls
                rpcPollTimer.Start ();
                while (true) {
                    // Cleared before the poll, so that bytes arriving during it end the wait
                    Arrivals.Reset ();
                    PollRequests (rpcYieldedContinuations);
                    if (rpcContinuations.Count > 0)
                        break;
                    // Wait for the client to release the tick, so that a call it makes after
                    // seeing an earlier result is executed in this update. The client's
                    // disconnect also sets the signal.
                    if (TickHeld) {
                        heldTick = true;
                        Arrivals.WaitUntil (tickHoldDeadline);
                        continue;
                    }
                    var waitDeadline = CallWaitDeadline (rpcStart + allowance, maxCallWaitTicks);
                    if (waitDeadline == 0)
                        break;
                    Arrivals.WaitUntil (waitDeadline);
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
                    if (!TickHeld && rpcTimer.ElapsedTicks > allowance) {
                        rpcYieldedContinuations.Add (continuation);
                        continue;
                    }

                    // Execute the continuation
                    try {
                        ExecuteContinuation (continuation);
                        CallWait callWait;
                        if (callWaits.TryGetValue (continuation.Client.Guid, out callWait))
                            callWait.ResponseSent (Stopwatch.GetTimestamp ());
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

                // Exit if max exec time exceeded
                if (rpcTimer.ElapsedTicks > allowance)
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

            // Send the responses held back for clients with more requests queued, as a
            // request left for the next update holds back the responses before it
            for (int i = 0; i < Servers.Count; i++)
                Servers [i].InnerServer.Flush ();

            // Run yielded continuations on the next update
            var tmp = rpcContinuations;
            rpcContinuations = rpcYieldedContinuations;
            rpcYieldedContinuations = tmp;

            rpcTimer.Stop ();
            if (rpcTimer.ElapsedTicks >= allowance)
                frameBound = true;
            PollTicks += rpcPollTimer.ElapsedTicks;
            ExecTicks += rpcExecTimer.ElapsedTicks;

            RPCsExecuted += rpcsExecuted;
            DeferredCalls = (uint)rpcContinuations.Count;
            frameExec += rpcExecTimer.ElapsedTicks;
            frameWait += rpcPollTimer.ElapsedTicks;
        }

        /// <summary>
        /// The earliest timestamp, before the end of the update's budget, to wait until for a
        /// client's next call, or zero to wait for none.
        /// </summary>
        long CallWaitDeadline (long budgetEnd, long maxCallWait)
        {
            long now = Stopwatch.GetTimestamp ();
            if (now >= budgetEnd)
                return 0;
            long earliest = 0;
            for (int i = 0; i < clientOrder.Count; i++) {
                CallWait callWait;
                if (!callWaits.TryGetValue (clientOrder [i].Guid, out callWait))
                    continue;
                bool budgetLimited;
                var deadline = callWait.Deadline (now, budgetEnd - now, maxCallWait, out budgetLimited);
                if (budgetLimited)
                    frameBound = true;
                if (deadline != 0 && (earliest == 0 || deadline < earliest))
                    earliest = deadline;
            }
            if (earliest > budgetEnd)
                frameBound = true;
            return earliest == 0 ? 0 : Math.Min (earliest, budgetEnd);
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
            uint evaluations = 0;

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
                        evaluations++;
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
                        client.Stream.Write (streamUpdate);
                        StreamUpdatesSent++;
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
            StreamTicks += streamTimer.ElapsedTicks;
            StreamEvaluations += evaluations;
            frameStreams += streamTimer.ElapsedTicks;
        }

        /// <summary>
        /// Log the end of a stream whose last update carries an error. An event that has
        /// finished is the normal end, so only its description is given.
        /// </summary>
        static void LogStreamEnded (IClient<IMessageStream> client, ulong id, Error error)
        {
            var finished = error.Service == "KRPC" && error.Exception == "StreamRemovedException";
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
            IClient<IMessageStream> client;
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
        void ReleaseTickToYield (IClient<IMessageStream> client)
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
            IClient<IMessageStream> client;
            if (!clients.TryGetValue (caller.Guid, out client))
                throw new InvalidOperationException ("No client is connected with this identifier");

            stream.Id = nextStreamId;
            nextStreamId++;

            var streamId = stream.Id;
            streams [client] [streamId] = stream;
            Logger.WriteLine ("Added stream " + streamId + " for client " + client.Label (), Logger.Severity.Debug);
            Streams++;
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
            IClient<IMessageStream> client;
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
        bool RemoveStreamInternal (IClient<IMessageStream> client, ulong id)
        {
            if (!streams.ContainsKey (client) || !streams [client].ContainsKey (id))
                return false;
            streams [client].Remove (id);
            client.Stream.StreamRemoved (id);
            Streams--;
            return true;
        }

        HashSet<IClient<IMessageStream>> pollRequestsCurrentClients = new HashSet<IClient<IMessageStream>> ();

        /// <summary>
        /// Poll connected clients for new requests.
        /// Adds a continuation to the queue for any client with a new request,
        /// if a continuation is not already being processed for the client.
        /// </summary>
        void PollRequests (IList<RequestContinuation> yieldedContinuations)
        {
            if (clientOrder.Count == 0)
                return;
            pollRequestsCurrentClients.Clear ();
            for (int i = 0; i < rpcContinuations.Count; i++)
                pollRequestsCurrentClients.Add (rpcContinuations [i].Client);
            for (int i = 0; i < yieldedContinuations.Count; i++)
                pollRequestsCurrentClients.Add (yieldedContinuations [i].Client);
            for (int i = 0; i < clientOrder.Count; i++) {
                var client = clientOrder [i];
                var stream = client.Stream;
                try {
                    if (!pollRequestsCurrentClients.Contains (client) && stream.DataAvailable) {
                        Request request = stream.Read ();
                        CallWait callWait;
                        if (callWaits.TryGetValue (client.Guid, out callWait))
                            callWait.RequestArrived (request.Arrived != 0 ? request.Arrived : Stopwatch.GetTimestamp ());
                        EventHandlerExtensions.Invoke (OnClientActivity, this, new ClientActivityEventArgs (client));
                        rpcContinuations.Add (new RequestContinuation (client, request));
                    }
                } catch (System.Exception e) {
                    // No request exists to tie a response to, so the connection ends. The
                    // connection logs the end of the session
                    if (Logger.ShouldLog (Logger.Severity.Debug))
                        Logger.WriteLine ("Failed to read a request from client " + client.Label () + ": " + e, Logger.Severity.Debug);
                    client.Stream.Disconnect (DisconnectReason.MalformedMessage, "Failed to read a request: " + e.Message);
                }
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
            return string.IsNullOrEmpty (error.Exception) ? error.Description : error.Exception + ": " + error.Description;
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
