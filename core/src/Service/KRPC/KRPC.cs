using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// Main kRPC service, used by clients to interact with basic server functionality.
    /// </summary>
    [KRPCService (Id = 1)]
    public static class KRPC
    {
        /// <summary>
        /// Returns the identifier for the current client.
        /// </summary>
        [KRPCProcedure]
        public static byte[] GetClientID() {
          return CallContext.Client.Guid.ToByteArray ();
        }

        /// <summary>
        /// Returns the name of the current client.
        /// This is an empty string if the client has no name.
        /// </summary>
        [KRPCProcedure]
        public static string GetClientName() {
          return CallContext.Client.Name;
        }

        /// <summary>
        /// Returns some information about the server, such as the version.
        /// </summary>
        [KRPCProcedure]
        public static Status GetStatus ()
        {
            var core = Core.Instance;
            var config = Configuration.Instance;
            var status = new Status (core.Version ?? "unknown");
            status.BytesRead = core.BytesRead;
            status.BytesWritten = core.BytesWritten;
            status.BytesReadRate = core.BytesReadRate;
            status.BytesWrittenRate = core.BytesWrittenRate;
            status.RpcsExecuted = core.RPCsExecuted;
            status.RpcRate = core.RPCRate;
            status.OneRpcPerUpdate = config.OneRPCPerUpdate;
            status.MaxTimePerUpdate = config.MaxTimePerUpdate;
            status.AdaptiveRateControl = config.AdaptiveRateControl;
            status.BlockingRecv = config.BlockingRecv;
            status.RecvTimeout = config.RecvTimeout;
            status.TimePerRpcUpdate = core.TimePerRPCUpdate;
            status.PollTimePerRpcUpdate = core.PollTimePerRPCUpdate;
            status.ExecTimePerRpcUpdate = core.ExecTimePerRPCUpdate;
            status.StreamRpcs = core.StreamRPCs;
            status.StreamRpcsExecuted = core.StreamRPCsExecuted;
            status.StreamRpcRate = core.StreamRPCRate;
            status.TimePerStreamUpdate = core.TimePerStreamUpdate;
            return status;
        }

        /// <summary>
        /// Returns information on all services, procedures, classes, properties etc. provided by the server.
        /// Can be used by client libraries to automatically create functionality such as stubs.
        /// </summary>
        [KRPCProcedure]
        public static Messages.Services GetServices ()
        {
            var services = new Messages.Services ();
            foreach (var serviceSignature in Services.Instance.Signatures.Values) {
                var service = new Messages.Service (serviceSignature.Name);
                foreach (var procedureSignature in serviceSignature.Procedures.Values) {
                    var procedure = new Procedure (procedureSignature.Name);
                    if (procedureSignature.HasReturnType)
                        procedure.ReturnType = procedureSignature.ReturnSpec;
                    foreach (var parameterSignature in procedureSignature.Parameters) {
                        var parameter = new Parameter (
                            parameterSignature.Name, parameterSignature.Spec);
                        if (parameterSignature.HasDefaultValue)
                            parameter.DefaultValue = parameterSignature.DefaultValue;
                        procedure.Parameters.Add (parameter);
                    }
                    procedure.GameScene = procedureSignature.GameScene;
                    if (procedureSignature.Documentation.Length > 0)
                        procedure.Documentation = procedureSignature.Documentation;
                    procedure.Deprecated = procedureSignature.Deprecated;
                    procedure.DeprecatedReason = procedureSignature.DeprecatedReason;
                    service.Procedures.Add (procedure);
                }
                foreach (var clsSignature in serviceSignature.Classes.Values) {
                    var cls = new Class (clsSignature.Name);
                    if (clsSignature.Documentation.Length > 0)
                        cls.Documentation = clsSignature.Documentation;
                    cls.Deprecated = clsSignature.Deprecated;
                    cls.DeprecatedReason = clsSignature.DeprecatedReason;
                    service.Classes.Add (cls);
                }
                foreach (var enmSignature in serviceSignature.Enumerations.Values) {
                    var enm = new Enumeration (enmSignature.Name);
                    if (enmSignature.Documentation.Length > 0)
                        enm.Documentation = enmSignature.Documentation;
                    enm.Deprecated = enmSignature.Deprecated;
                    enm.DeprecatedReason = enmSignature.DeprecatedReason;
                    foreach (var enmValueSignature in enmSignature.Values) {
                        var enmValue = new EnumerationValue (enmValueSignature.Name, enmValueSignature.Value);
                        if (enmValueSignature.Documentation.Length > 0)
                            enmValue.Documentation = enmValueSignature.Documentation;
                        enmValue.Deprecated = enmValueSignature.Deprecated;
                        enmValue.DeprecatedReason = enmValueSignature.DeprecatedReason;
                        enm.Values.Add (enmValue);
                    }
                    service.Enumerations.Add (enm);
                }
                foreach (var structSignature in serviceSignature.Structs.Values) {
                    var str = new Messages.Struct (structSignature.Name);
                    if (structSignature.Documentation.Length > 0)
                        str.Documentation = structSignature.Documentation;
                    str.Deprecated = structSignature.Deprecated;
                    str.DeprecatedReason = structSignature.DeprecatedReason;
                    foreach (var fieldSignature in structSignature.Fields) {
                        var field = new StructField (fieldSignature.Name, fieldSignature.Spec);
                        if (fieldSignature.Documentation.Length > 0)
                            field.Documentation = fieldSignature.Documentation;
                        field.Deprecated = fieldSignature.Deprecated;
                        field.DeprecatedReason = fieldSignature.DeprecatedReason;
                        str.Fields.Add (field);
                    }
                    service.Structs.Add (str);
                }
                foreach (var exnSignature in serviceSignature.Exceptions.Values) {
                    var exn = new Messages.Exception (exnSignature.Name);
                    if (exnSignature.Documentation.Length > 0)
                        exn.Documentation = exnSignature.Documentation;
                    exn.Deprecated = exnSignature.Deprecated;
                    exn.DeprecatedReason = exnSignature.DeprecatedReason;
                    service.Exceptions.Add (exn);
                }
                if (serviceSignature.Documentation.Length > 0)
                    service.Documentation = serviceSignature.Documentation;
                service.Deprecated = serviceSignature.Deprecated;
                service.DeprecatedReason = serviceSignature.DeprecatedReason;
                services.ServicesList.Add (service);
            }
            return services;
        }

        /// <summary>
        /// A list of RPC clients that are currently connected to the server.
        /// Each entry in the list is a clients identifier, name and address.
        /// </summary>
        [KRPCProperty]
        public static IList<Tuple<byte[], string, string>> Clients {
            get { return Core.Instance.RPCClients.Select (x => new Tuple<byte[], string, string> (x.Guid.ToByteArray (), x.Name, x.Address)).ToList (); }
        }

        /// <summary>
        /// The current game scene. Setting this switches the game to the given
        /// scene, or opens/closes the corresponding facility for the pseudo-scenes.
        /// Scene changes happen asynchronously: setting this property returns
        /// immediately, and clients should poll it until it reports the requested
        /// scene. Setting it to <see cref="GameScene.Flight"/> resumes the save's
        /// active vessel, and fails if there is none. Moving between
        /// <see cref="GameScene.EditorVAB"/> and <see cref="GameScene.EditorSPH"/>
        /// keeps the vessel that is being constructed, as the game's own switch
        /// editor button does. Leaving flight saves the game first, unless the game
        /// cannot be saved, e.g. while under acceleration.
        /// </summary>
        [KRPCProperty]
        public static GameScene GameScene {
            get {
                var scene = CallContext.GameScene;
                if ((scene & Service.GameScene.AstronautComplex) != 0)
                    return GameScene.AstronautComplex;
                if ((scene & Service.GameScene.MissionControl) != 0)
                    return GameScene.MissionControl;
                if ((scene & Service.GameScene.ResearchAndDevelopment) != 0)
                    return GameScene.ResearchAndDevelopment;
                if ((scene & Service.GameScene.Administration) != 0)
                    return GameScene.Administration;
                if ((scene & Service.GameScene.SpaceCenter) != 0)
                    return GameScene.SpaceCenter;
                if ((scene & Service.GameScene.Flight) != 0)
                    return GameScene.Flight;
                if ((scene & Service.GameScene.TrackingStation) != 0)
                    return GameScene.TrackingStation;
                if ((scene & Service.GameScene.EditorVAB) != 0)
                    return GameScene.EditorVAB;
                if ((scene & Service.GameScene.EditorSPH) != 0)
                    return GameScene.EditorSPH;
                if ((scene & Service.GameScene.MissionBuilder) != 0)
                    return GameScene.MissionBuilder;
                throw new System.InvalidOperationException ("Unknown game scene");
            }
            set {
                var loadScene = CallContext.LoadScene;
                if (loadScene == null)
                    throw new System.InvalidOperationException (
                        "Changing the game scene is not supported by this server");
                loadScene (value);
            }
        }

        /// <summary>
        /// Whether the game is paused.
        /// </summary>
        [KRPCProperty]
        public static bool Paused {
            get {
                return CallContext.IsPaused();
            }
            set {
                if (value)
                    CallContext.Pause();
                else
                    CallContext.Unpause();
            }
        }

        /// <summary>
        /// The number of game states that have finished loading.
        /// </summary>
        /// <remarks>
        /// Increases once a game load, quickload, revert or scene change has finished.
        /// </remarks>
        [KRPCProperty]
        public static uint GameStateLoads {
            get { return GameState.Loads; }
        }

        /// <summary>
        /// Add a streaming request and return its identifier.
        /// </summary>
        /// <remarks>
        /// The stream starts sending results from the next update.
        /// </remarks>
        [KRPCProcedure]
        public static Messages.Stream AddStream (ProcedureCall call)
        {
            var callStream = new ProcedureCallStream (call);
            return new Messages.Stream (Core.Instance.AddStream (CallContext.Client, callStream));
        }

        /// <summary>
        /// Set the update rate for a stream in Hz.
        /// </summary>
        /// <remarks>
        /// Does nothing for a stream that has ended. Throws an exception if the id has never
        /// been issued.
        /// </remarks>
        [KRPCProcedure]
        public static void SetStreamRate (ulong id, float rate)
        {
            Core.Instance.SetStreamRate (CallContext.Client, id, rate);
        }

        /// <summary>
        /// Remove a streaming request.
        /// </summary>
        /// <remarks>
        /// Does nothing for a stream that has ended. Throws an exception if the id has never
        /// been issued.
        /// </remarks>
        [KRPCProcedure]
        // FIXME: should be a Stream not a ulong
        public static void RemoveStream (ulong id)
        {
            Core.Instance.RemoveStream (CallContext.Client, id);
        }

        /// <summary>
        /// Add a stream of the value a server side function computes on each update.
        /// </summary>
        /// <remarks>
        /// Each call adds a new stream, with values of the type given by
        /// <see cref="Expression.ReturnType"/>. Any exception from the function ends the
        /// stream, including one for an object that is not loaded. A procedure that pauses
        /// execution to resume on a later tick produces an error on the stream.
        /// </remarks>
        [KRPCProcedure]
        public static Messages.Stream AddFunctionStream (Expression function)
        {
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            var functionStream = new FunctionStream (function);
            return new Messages.Stream (Core.Instance.AddStream (CallContext.Client, functionStream));
        }

        /// <summary>
        /// Compile a server side function, so that running, streaming or adding it as an
        /// event does not compile it.
        /// </summary>
        /// <remarks>
        /// Compiling a large function takes time on the game's main thread. Compiling it
        /// in advance keeps that time out of its first run.
        /// </remarks>
        [KRPCProcedure]
        public static void CompileFunction (Expression function)
        {
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            function.Check ();
            function.Compile ();
        }

        /// <summary>
        /// Run a server side function once, within a single physics tick, and return
        /// the value it produces.
        /// </summary>
        /// <remarks>
        /// The arguments are encoded at the types given by <see cref="Expression.ParameterTypes"/>.
        /// Throws an exception if a procedure the function calls pauses execution, and the
        /// effects up to the pause remain.
        /// </remarks>
        /// <returns>The encoded value of the function, or no bytes for a function with no
        /// value.</returns>
        /// <param name="function">The function to run.</param>
        /// <param name="arguments">The encoded arguments, one per parameter.</param>
        [KRPCProcedure (Nullable = true)]
        public static byte[] RunFunction (
            Expression function,
            [KRPCDefaultValue (typeof (NoArguments))]
            [KRPCNullable (Position.Element)] IList<byte[]> arguments = null)
        {
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            function.Check ();
            // Check the value can be sent and the arguments decode before evaluating, so
            // a call that fails has no effects
            if (function.HasReturnType)
                function.GetValidReturnType ();
            var values = function.DecodeArguments (arguments);
            var value = function.Evaluate (values);
            if (!function.HasReturnType)
                return new byte [0];
            // A null is sent in the result's is_null flag
            if (ReferenceEquals (value, null))
                return null;
            return Server.ProtocolBuffers.Encoder.Encode (value, function.FunctionSpec).ToByteArray ();
        }

        static class NoArguments
        {
            public static object Create ()
            {
                return new List<byte[]> ();
            }
        }

        /// <summary>
        /// Create an event from a server side function that evaluates to a boolean value.
        /// </summary>
        /// <remarks>
        /// The function is evaluated on each update, and each call adds a new event. Any
        /// exception from the function ends the event, including one for an object that is not
        /// loaded. A procedure that pauses execution to resume on a later tick produces an
        /// error on the event's stream.
        /// </remarks>
        [KRPCProcedure]
        public static Messages.Event AddEvent(Expression function)
        {
            var stream = new EventStream (EventCondition (function), false);
            return new Messages.Event (new Messages.Stream (Core.Instance.AddStream (CallContext.Client, stream)));
        }

        /// <summary>
        /// The condition an event created by <see cref="AddEvent"/> checks on each update.
        /// </summary>
        internal static Func<bool> EventCondition (Expression function)
        {
            if (ReferenceEquals (function, null))
                throw new ArgumentNullException (nameof (function));
            function.CheckNoParameters ();
            var type = function.HasReturnType ? function.FunctionSpec.Type : typeof (void);
            if (type != typeof (bool))
                throw new ArgumentException ("The function must evaluate to a boolean value");
            function.Check ();
            function.Compile ();
            return () => {
                var value = function.Evaluate ();
                if (ReferenceEquals (value, null))
                    throw new NullReferenceException (Expression.NullResultMessage);
                return (bool)value;
            };
        }

        /// <summary>
        /// Hold the game on its current physics tick until <see cref="ReleaseTick" /> is called
        /// or the hold times out. Every call made in between is executed before the game
        /// advances, so a read, compute and write all happen in the state of one tick.
        /// </summary>
        /// <remarks>
        /// The game renders no frame, takes no input and runs no physics while the tick is
        /// held. Keep a hold as short as possible, and release it even when the code between
        /// the two calls fails.
        ///
        /// A tick is held once, so this waits for the next tick when the current one has
        /// already been released. One client holds the tick at a time, and only a client
        /// making a call can hold it: a stream or an event cannot.
        ///
        /// A hold ends by itself when it times out, when the client disconnects, and when the
        /// client calls a procedure that takes more than one tick to finish, such as staging
        /// or warping. Stream updates are sent once the tick is released, so waiting for a
        /// stream or an event inside a hold waits out the timeout.
        /// </remarks>
        [KRPCProcedure]
        public static void HoldTick ()
        {
            Core.Instance.HoldTick (CallContext.Client);
        }

        /// <summary>
        /// Let the game move on from the tick that <see cref="HoldTick" /> held it on. Does
        /// nothing when no tick is held, which is the case once a hold has timed out.
        /// </summary>
        [KRPCProcedure]
        public static void ReleaseTick ()
        {
            Core.Instance.ReleaseTick (CallContext.Client);
        }

        /// <summary>
        /// Let the game move on from the tick being held and hold the one after it, so that a
        /// program acts on consecutive physics ticks. Holds the soonest tick it can if no tick
        /// is currently being held.
        /// </summary>
        /// <remarks>
        /// A release and a hold made as two separate calls let the game advance between them.
        /// This takes the next tick without that gap. Call it at the top of a loop that has to
        /// act on every tick, work, and come back to it.
        ///
        /// The game advances one tick per call, so it runs at the rate of the loop for as long
        /// as the loop runs. End the loop with <see cref="ReleaseTick" />.
        ///
        /// Every limit of <see cref="HoldTick" /> applies, the timeout included. A hold lost to
        /// the timeout is not reported, and the next call takes whatever tick it lands on.
        /// </remarks>
        [KRPCProcedure]
        public static void NextTick ()
        {
            Core.Instance.NextTick (CallContext.Client);
        }
    }
}
