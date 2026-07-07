using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;
using LinqExpression = System.Linq.Expressions.Expression;

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
                    if (procedureSignature.HasReturnType) {
                        procedure.ReturnType = procedureSignature.ReturnType;
                        procedure.ReturnIsNullable = procedureSignature.ReturnIsNullable;
                    }
                    foreach (var parameterSignature in procedureSignature.Parameters) {
                        var parameter = new Parameter (
                            parameterSignature.Name, parameterSignature.Type, parameterSignature.Nullable);
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
        /// active vessel, and fails if there is none.
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
        /// Get the current game scene.
        /// </summary>
        [KRPCProperty]
        [Obsolete ("Use <see cref='GameScene'/> instead.")]
        public static GameScene CurrentGameScene {
            get { return GameScene; }
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
        /// Add a streaming request and return its identifier.
        /// </summary>
        [KRPCProcedure]
        public static Messages.Stream AddStream (ProcedureCall call, bool start = true)
        {
            var callStream = new ProcedureCallStream (call);
            var core = Core.Instance;
            var stream = new Messages.Stream (core.AddStream (CallContext.Client, callStream, false));
            if (start)
                core.StartStream (CallContext.Client, stream.Id);
            return stream;
        }

        /// <summary>
        /// Start a previously added streaming request.
        /// </summary>
        [KRPCProcedure]
        public static void StartStream (ulong id)
        {
            Core.Instance.StartStream (CallContext.Client, id);
        }

        /// <summary>
        /// Set the update rate for a stream in Hz.
        /// </summary>
        [KRPCProcedure]
        public static void SetStreamRate (ulong id, float rate)
        {
            Core.Instance.SetStreamRate (CallContext.Client, id, rate);
        }

        /// <summary>
        /// Remove a streaming request.
        /// </summary>
        [KRPCProcedure]
        // FIXME: should be a Stream not a ulong
        public static void RemoveStream (ulong id)
        {
            Core.Instance.RemoveStream (CallContext.Client, id);
        }

        /// <summary>
        /// Create an event from a server side expression.
        /// </summary>
        [KRPCProcedure]
        public static Messages.Event AddEvent(Expression expression)
        {
            var func = LinqExpression.Lambda<Func<bool>>(expression).Compile();
            return new Event((evnt) => func()).Message;
        }
    }
}
