using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Attributes;
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
        List<RequestContinuation> continuations = new List<RequestContinuation> ();
        IDictionary<IClient<NoMessage,StreamUpdate>, IList<StreamRequest>> streamRequests = new Dictionary<IClient<NoMessage,StreamUpdate>,IList<StreamRequest>> ();
        IDictionary<ulong, object> streamResultCache = new Dictionary<ulong, object> ();

        static Core instance;

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

        internal void RPCClientConnected (IClient<Request,Response> client)
        {
            rpcClients [client.Guid] = client;
            clientScheduler.Add (client);
            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs (client));
        }

        internal void RPCClientDisconnected (IClient<Request,Response> client)
        {
            rpcClients.Remove (client.Guid);
            clientScheduler.Remove (client);
            EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs (client));
        }

        internal void StreamClientConnected (IClient<NoMessage,StreamUpdate> client)
        {
            streamClients [client.Guid] = client;
            streamRequests [client] = new List<StreamRequest> ();
        }

        internal void StreamClientDisconnected (IClient<NoMessage,StreamUpdate> client)
        {
            streamClients.Remove (client.Guid);
            streamRequests.Remove (client);
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
        internal void Add (Server.Server server)
        {
            Servers.Add (server);
            Configure (server);
            Logger.WriteLine ("Added server '" + server.Name);
        }

        /// <summary>
        /// Remove a server from the core.
        /// </summary>
        internal void Remove (Guid id)
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
        internal void Replace (Server.Server newServer)
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
                EventHandlerExtensions.Invoke (OnServerStopped, this, new ServerStoppedEventArgs ((Server.Server)s));
            };
            server.OnClientRequestingConnection += (s, e) => EventHandlerExtensions.Invoke (OnClientRequestingConnection, this, e);
        }

        /// <summary>
        /// Start all servers.
        /// </summary>
        internal void StartAll ()
        {
            foreach (var server in Servers) {
                if (!server.Running)
                    server.Start ();
            }
        }

        /// <summary>
        /// Stop all servers.
        /// </summary>
        internal void StopAll ()
        {
            foreach (var server in Servers) {
                if (server.Running)
                    server.Stop ();
            }
        }

        /// <summary>
        /// Stop all servers.
        /// </summary>
        internal bool AnyRunning { get; private set; }

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
            StreamRPCs = 0;
            StreamRPCsExecuted = 0;
            TimePerStreamUpdate = 0;
        }

        Stopwatch updateTimer = Stopwatch.StartNew ();

        /// <summary>
        /// Update the server
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        internal void Update ()
        {
            ulong startRPCsExecuted = RPCsExecuted;
            ulong startStreamRPCsExecuted = StreamRPCsExecuted;
            ulong startBytesRead = BytesRead;
            ulong startBytesWritten = BytesWritten;

            RPCServerUpdate ();
            StreamServerUpdate ();

            var timeElapsed = updateTimer.ElapsedSeconds ();
            var ticksElapsed = updateTimer.ElapsedTicks;
            updateTimer.Reset ();
            updateTimer.Start ();

            RPCRate = (float)((double)(RPCsExecuted - startRPCsExecuted) / timeElapsed);
            StreamRPCRate = (float)((double)(StreamRPCsExecuted - startStreamRPCsExecuted) / timeElapsed);
            BytesReadRate = (float)((double)(BytesRead - startBytesRead) / timeElapsed);
            BytesWrittenRate = (float)((double)(BytesWritten - startBytesWritten) / timeElapsed);

            // Adjust MaxTimePerUpdate to get a target FixedUpdate rate of 59 FPS. This is slightly smaller
            // than 60 FPS, so that it pushes against the target 60 FPS for FixedUpdate.
            // The minimum MaxTimePerUpdate that will be set is 1ms, and the maximum is 25ms.
            // If very little time is being spent executing RPCs (<1ms), MaxTimePerUpdate is set to 10ms.
            // This prevents MaxTimePerUpdate from being set to a high value when the server is idle, which would
            // cause a drop in framerate if a large burst of RPCs are received.
            var config = Configuration.Instance;
            if (config.AdaptiveRateControl) {
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
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        void RPCServerUpdate ()
        {
            rpcTimer.Reset ();
            rpcTimer.Start ();
            rpcPollTimeout.Reset ();
            rpcPollTimer.Reset ();
            rpcExecTimer.Reset ();
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
                    if (!config.BlockingRecv)
                        break;
                    if (rpcPollTimeout.ElapsedTicks > recvTimeoutTicks)
                        break;
                    if (rpcTimer.ElapsedTicks > maxTimePerUpdateTicks)
                        break;
                    if (continuations.Count > 0)
                        break;
                }
                rpcPollTimer.Stop ();

                if (continuations.Count == 0)
                    break;

                // Execute RPCs
                rpcExecTimer.Start ();
                for (int i = 0; i < continuations.Count; i++) {
                    var continuation = continuations [i];

                    // Ignore the continuation if the client has disconnected
                    if (!continuation.Client.Connected)
                        continue;

                    // Max exec time exceeded, delay to next update
                    if (rpcTimer.ElapsedTicks > maxTimePerUpdateTicks) {
                        rpcYieldedContinuations.Add (continuation);
                        continue;
                    }

                    // Execute the continuation
                    try {
                        ExecuteContinuation (continuation);
                    } catch (YieldException e) {
                        rpcYieldedContinuations.Add ((RequestContinuation)e.Continuation);
                    }
                    rpcsExecuted++;
                }
                continuations.Clear ();
                rpcExecTimer.Stop ();

                // Exit if only execute one RPC per update
                if (config.OneRPCPerUpdate)
                    break;

                // Exit if max exec time exceeded
                if (rpcTimer.ElapsedTicks > maxTimePerUpdateTicks)
                    break;
            }

            // Run yielded continuations on the next update
            var tmp = continuations;
            continuations = rpcYieldedContinuations;
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        void StreamServerUpdate ()
        {
            streamTimer.Reset ();
            streamTimer.Start ();
            uint rpcsExecuted = 0;

            for (int i = 0; i < Servers.Count; i++)
                Servers [i].StreamServer.Update ();

            // Run streaming requests
            if (streamRequests.Count > 0) {
                foreach (var entry in streamRequests) {
                    var streamClient = entry.Key;
                    var id = streamClient.Guid;
                    var requests = entry.Value;
                    if (requests.Count == 0)
                        continue;
                    if (!rpcClients.ContainsKey (id))
                        continue;
                    CallContext.Set (rpcClients [id]);
                    var streamUpdate = new StreamUpdate ();
                    foreach (var request in requests) {
                        // Run the RPC
                        ProcedureResult result;
                        try {
                            result = KRPC.Service.Services.Instance.ExecuteCall (request.Procedure, request.Arguments);
                        } catch (RPCException e) {
                            result = new ProcedureResult ();
                            result.Error = e.ToString ();
                        } catch (YieldException e) {
                            //FIXME: handle yields correctly
                            result = new ProcedureResult ();
                            result.Error = e.ToString ();
                        }
                        rpcsExecuted++;
                        // Don't send an update if it is the previous one
                        //FIXME: does the following comparison work?!? The objects have not been serialized
                        if (result.Value == streamResultCache [request.Identifier])
                            continue;
                        // Add the update to the response message
                        streamResultCache [request.Identifier] = result.Value;
                        var streamResult = request.Result;
                        streamResult.Result = result;
                        streamUpdate.Results.Add (streamResult);
                    }
                    if (streamUpdate.Results.Count > 0)
                        streamClient.Stream.Write (streamUpdate);
                }
            }

            streamTimer.Stop ();
            StreamRPCs = rpcsExecuted;
            StreamRPCsExecuted += rpcsExecuted;
            TimePerStreamUpdate = (float)streamTimer.ElapsedSeconds ();
        }

        /// <summary>
        /// Add a stream to the server
        /// </summary>
        internal ulong AddStream (IClient rpcClient, ProcedureCall call)
        {
            var id = rpcClient.Guid;
            if (!streamClients.ContainsKey (id))
                throw new InvalidOperationException ("No stream client is connected for this RPC client");
            var streamClient = streamClients [id];

            // Check for an existing stream for the request
            var services = Service.Services.Instance;
            var procedure = services.GetProcedureSignature (call.Service, call.Procedure);
            var arguments = services.GetArguments (procedure, call.Arguments);
            foreach (var streamRequest in streamRequests[streamClient]) {
                if (streamRequest.Procedure == procedure && streamRequest.Arguments.SequenceEqual (arguments))
                    return streamRequest.Identifier;
            }

            // Create a new stream
            {
                var streamRequest = new StreamRequest (call);
                streamRequests [streamClient].Add (streamRequest);
                streamResultCache [streamRequest.Identifier] = null;
                return streamRequest.Identifier;
            }
        }

        /// <summary>
        /// Remove a stream from the server
        /// </summary>
        internal void RemoveStream (IClient rpcClient, ulong identifier)
        {
            var id = rpcClient.Guid;
            if (!streamClients.ContainsKey (id))
                throw new InvalidOperationException ("No stream client is connected for this RPC client");
            var streamClient = streamClients [id];
            var requests = streamRequests [streamClient].Where (x => x.Identifier == identifier).ToList ();
            if (!requests.Any ())
                return;
            streamRequests [streamClient].Remove (requests.Single ());
            streamResultCache.Remove (identifier);
        }

        HashSet<IClient<Request,Response>> pollRequestsCurrentClients = new HashSet<IClient<Request, Response>> ();

        /// <summary>
        /// Poll connected clients for new requests.
        /// Adds a continuation to the queue for any client with a new request,
        /// if a continuation is not already being processed for the client.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        void PollRequests (IList<RequestContinuation> yieldedContinuations)
        {
            if (clientScheduler.Empty)
                return;
            pollRequestsCurrentClients.Clear ();
            for (int i = 0; i < continuations.Count; i++)
                pollRequestsCurrentClients.Add (continuations [i].Client);
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
                        if (Logger.ShouldLog (Logger.Severity.Debug))
                            Logger.WriteLine ("Received request from client " + client.Address +
                            " (" + String.Join (", ", request.Calls.Select (call => call.Service + "." + call.Procedure).ToArray ()) + ")",
                                Logger.Severity.Debug);
                        continuations.Add (new RequestContinuation (client, request));
                    }
                } catch (ServerException e) {
                    Logger.WriteLine ("Error receiving request from client " + client.Address + ": " + e.Message, Logger.Severity.Error);
                    client.Stream.Close ();
                    continue;
                } catch (System.Exception e) {
                    var response = new Response ();
                    response.Error = new Error ("Error receiving message" + Environment.NewLine + e.Message, e.StackTrace);
                    if (Logger.ShouldLog (Logger.Severity.Debug))
                        Logger.WriteLine (e.Message + Environment.NewLine + e.StackTrace, Logger.Severity.Error);
                    Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.Error + ")", Logger.Severity.Debug);
                    client.Stream.Write (response);
                }
                item = item.Next;
            }
        }

        /// <summary>
        /// Execute the continuation and send a response to the client,
        /// or throw a YieldException if the continuation is not complete.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
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
                response = HandleException (e);
            } catch (System.Exception e) {
                response = HandleException (e);
            } finally {
                CallContext.Clear ();
            }

            // Send response to the client
            client.Stream.Write (response);
            if (Logger.ShouldLog (Logger.Severity.Debug)) {
                if (response.HasError)
                    Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.Error + ")", Logger.Severity.Debug);
                else
                    Logger.WriteLine ("Sent response to client " + client.Address, Logger.Severity.Debug);
            }
        }

        /// <summary>
        /// Convert an exception thrown by an RPC into a response message.
        /// </summary>
        static Response HandleException(System.Exception exn)
        {
            if (exn is RPCException && exn.InnerException != null)
                exn = exn.InnerException;
            var message = exn.Message;
            var verboseErrors = Configuration.Instance.VerboseErrors;
            var stackTrace = verboseErrors ? exn.StackTrace : string.Empty;
            if (Logger.ShouldLog (Logger.Severity.Debug)) {
                Logger.WriteLine (message, Logger.Severity.Debug);
                if (verboseErrors)
                    Logger.WriteLine (stackTrace, Logger.Severity.Debug);
            }
            var mappedType = Service.Services.Instance.GetMappedExceptionType(exn.GetType());
            var type = mappedType ?? exn.GetType();
            Error error;
            if (Reflection.HasAttribute<KRPCExceptionAttribute>(type))
                error = new Error(TypeUtils.GetExceptionServiceName(type), type.Name, message, stackTrace);
            else
                error = new Error(message, stackTrace);
            return new Response { Error = error };
        }
    }
}
