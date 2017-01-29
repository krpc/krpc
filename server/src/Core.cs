using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC
{
    /// <summary>
    /// The kRPC core, which manages the execution of remote procedures,
    /// bridging the gap between servers and services.
    /// This class is a singleton. The instance can be obtained via the <see cref="Instance"/> property.
    /// </summary>
    public sealed class Core
    {
        // TODO: remove servers list, replace with events etc.
        List<Server.Server> servers;
        IDictionary<Guid, IClient<Request, Response>> rpcClients;
        IDictionary<Guid, IClient<NoMessage, StreamMessage>> streamClients;
        RoundRobinScheduler<IClient<Request,Response>> clientScheduler;
        List<RequestContinuation> continuations;
        IDictionary<IClient<NoMessage,StreamMessage>, IList<StreamRequest>> streamRequests;
        IDictionary<uint, object> streamResultCache;
        internal Func<double> GetUniversalTime;

        static Core instance;

        /// <summary>
        /// Get or create an instance of KRPC.Core
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
            servers = new List<Server.Server> ();
            rpcClients = new Dictionary<Guid, IClient<Request, Response>> ();
            streamClients = new Dictionary<Guid, IClient<NoMessage, StreamMessage>> ();
            clientScheduler = new RoundRobinScheduler<IClient<Request, Response>> ();
            continuations = new List<RequestContinuation> ();
            streamRequests = new Dictionary<IClient<NoMessage,StreamMessage>,IList<StreamRequest>> ();
            streamResultCache = new Dictionary<uint, object> ();
            OneRPCPerUpdate = false;
            MaxTimePerUpdate = 5000;
            AdaptiveRateControl = true;
            BlockingRecv = true;
            RecvTimeout = 1000;
        }

        /// <summary>
        /// Event triggered when a RPC client has connected
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> OnRPCClientConnected;

        /// <summary>
        /// Event triggered when a RPC client has disconnected
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> OnRPCClientDisconnected;

        /// <summary>
        /// Event triggered when a stream client has connected
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> OnStreamClientConnected;

        /// <summary>
        /// Event triggered when a stream client has disconnected
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> OnStreamClientDisconnected;

        internal void RPCClientConnected (IClient<Request,Response> client)
        {
            rpcClients [client.Guid] = client;
            clientScheduler.Add (client);
            EventHandlerExtensions.Invoke (OnRPCClientConnected, this, new ClientConnectedEventArgs (client));
        }

        internal void RPCClientDisconnected (IClient<Request,Response> client)
        {
            rpcClients.Remove (client.Guid);
            clientScheduler.Remove (client);
            EventHandlerExtensions.Invoke (OnRPCClientDisconnected, this, new ClientDisconnectedEventArgs (client));
        }

        internal void StreamClientConnected (IClient<NoMessage,StreamMessage> client)
        {
            streamClients [client.Guid] = client;
            streamRequests [client] = new List<StreamRequest> ();
            EventHandlerExtensions.Invoke (OnStreamClientConnected, this, new ClientConnectedEventArgs (client));
        }

        internal void StreamClientDisconnected (IClient<NoMessage,StreamMessage> client)
        {
            streamClients.Remove (client.Guid);
            streamRequests.Remove (client);
            EventHandlerExtensions.Invoke (OnStreamClientDisconnected, this, new ClientDisconnectedEventArgs (client));
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
            servers.Add (server);
        }

        /// <summary>
        /// Only execute one RPC for each client per update.
        /// </summary>
        public bool OneRPCPerUpdate { get; set; }

        /// <summary>
        /// Get/set the maximum number of microseconds to spend in a call to FixedUpdate
        /// </summary>
        public uint MaxTimePerUpdate { get; set; }

        /// <summary>
        /// Get/set whether MaxTimePerUpdate should be adjusted to achieve a target framerate.
        /// </summary>
        public bool AdaptiveRateControl { get; set; }

        /// <summary>
        /// Get/set whether FixedUpdate should block for RecvTimeout microseconds to receive RPCs.
        /// </summary>
        public bool BlockingRecv { get; set; }

        /// <summary>
        /// Get/set the timeout for blocking for RPCs, in microseconds.
        /// </summary>
        public uint RecvTimeout { get; set; }

        ExponentialMovingAverage bytesReadRate = new ExponentialMovingAverage (0.25);
        ExponentialMovingAverage bytesWrittenRate = new ExponentialMovingAverage (0.25);

        /// <summary>
        /// Get the total number of bytes read from the network.
        /// </summary>
        public ulong BytesRead {
            get {
                ulong read = 0;
                for (int i = 0; i < servers.Count; i++)
                    read += servers [i].BytesRead;
                return read;
            }
        }

        /// <summary>
        /// Get the total number of bytes written to the network.
        /// </summary>
        public ulong BytesWritten {
            get {
                ulong written = 0;
                for (int i = 0; i < servers.Count; i++)
                    written += servers [i].BytesWritten;
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
            if (AdaptiveRateControl) {
                var targetTicks = Stopwatch.Frequency / 59;
                if (ticksElapsed > targetTicks) {
                    if (MaxTimePerUpdate > 1000)
                        MaxTimePerUpdate -= 100;
                } else {
                    if (ExecTimePerRPCUpdate < 0.001) {
                        MaxTimePerUpdate = 10000;
                    } else {
                        if (MaxTimePerUpdate < 25000)
                            MaxTimePerUpdate += 100;
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
            long maxTimePerUpdateTicks = StopwatchExtensions.MicrosecondsToTicks (MaxTimePerUpdate);
            long recvTimeoutTicks = StopwatchExtensions.MicrosecondsToTicks (RecvTimeout);
            ulong rpcsExecuted = 0;

            rpcYieldedContinuations.Clear ();
            for (int i = 0; i < servers.Count; i++)
                servers [i].RPCServer.Update ();

            while (true) {

                // Poll for RPCs
                rpcPollTimer.Start ();
                rpcPollTimeout.Reset ();
                rpcPollTimeout.Start ();
                while (true) {
                    PollRequests (rpcYieldedContinuations);
                    if (!BlockingRecv)
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
                if (OneRPCPerUpdate)
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

            for (int i = 0; i < servers.Count; i++)
                servers [i].StreamServer.Update ();

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
                    var streamMessage = new StreamMessage ();
                    foreach (var request in requests) {
                        // Run the RPC
                        Response response;
                        try {
                            response = Service.Services.Instance.HandleRequest (request.Procedure, request.Arguments);
                        } catch (RPCException e) {
                            response = new Response ();
                            response.HasError = true;
                            response.Error = e.ToString ();
                        } catch (YieldException e) {
                            // FIXME: handle yields correctly
                            response = new Response ();
                            response.HasError = true;
                            response.Error = e.ToString ();
                        }
                        rpcsExecuted++;
                        // Don't send an update if it is the previous one
                        // FIXME: does the following comparison work?!? The objects have not been serialized
                        if (response.ReturnValue == streamResultCache [request.Identifier])
                            continue;
                        // Add the update to the response message
                        streamResultCache [request.Identifier] = response.ReturnValue;
                        response.Time = GetUniversalTime ();
                        var streamResponse = request.Response;
                        streamResponse.Response = response;
                        streamMessage.Responses.Add (streamResponse);
                    }
                    if (streamMessage.Responses.Count > 0)
                        streamClient.Stream.Write (streamMessage);
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
        internal uint AddStream (IClient rpcClient, Request request)
        {
            var id = rpcClient.Guid;
            if (!streamClients.ContainsKey (id))
                throw new InvalidOperationException ("No stream client is connected for this RPC client");
            var streamClient = streamClients [id];

            // Check for an existing stream for the request
            var services = Service.Services.Instance;
            var procedure = services.GetProcedureSignature (request.Service, request.Procedure);
            var arguments = services.GetArguments (procedure, request.Arguments);
            foreach (var streamRequest in streamRequests[streamClient]) {
                if (streamRequest.Procedure == procedure && streamRequest.Arguments.SequenceEqual (arguments))
                    return streamRequest.Identifier;
            }

            // Create a new stream
            {
                var streamRequest = new StreamRequest (request);
                streamRequests [streamClient].Add (streamRequest);
                streamResultCache [streamRequest.Identifier] = null;
                return streamRequest.Identifier;
            }
        }

        /// <summary>
        /// Remove a stream from the server
        /// </summary>
        internal void RemoveStream (IClient rpcClient, uint identifier)
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
                            Logger.WriteLine ("Received request from client " + client.Address + " (" + request.Service + "." + request.Procedure + ")", Logger.Severity.Debug);
                        continuations.Add (new RequestContinuation (client, request));
                    }
                } catch (ServerException e) {
                    Logger.WriteLine ("Error receiving request from client " + client.Address + ": " + e.Message, Logger.Severity.Error);
                    client.Stream.Close ();
                    continue;
                } catch (Exception e) {
                    var response = new Response ();
                    response.HasError = true;
                    response.Error = "Error receiving message" + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace;
                    response.Time = GetUniversalTime ();
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
        void ExecuteContinuation (RequestContinuation continuation)
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
                response = new Response ();
                response.HasError = true;
                response.Error = e.Message;
                if (Logger.ShouldLog (Logger.Severity.Debug))
                    Logger.WriteLine (response.Error, Logger.Severity.Debug);
            } catch (Exception e) {
                response = new Response ();
                response.HasError = true;
                response.Error = e.Message + Environment.NewLine + e.StackTrace;
                if (Logger.ShouldLog (Logger.Severity.Debug))
                    Logger.WriteLine (response.Error, Logger.Severity.Debug);
            } finally {
                CallContext.Clear ();
            }

            // Send response to the client
            response.Time = GetUniversalTime ();
            client.Stream.Write (response);
            if (Logger.ShouldLog (Logger.Severity.Debug)) {
                if (response.HasError)
                    Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.Error + ")", Logger.Severity.Debug);
                else
                    Logger.WriteLine ("Sent response to client " + client.Address, Logger.Severity.Debug);
            }
        }
    }
}
