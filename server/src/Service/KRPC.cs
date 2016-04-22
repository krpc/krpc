using System;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// Main kRPC service, used by clients to interact with basic server functionality.
    /// </summary>
    [KRPCService]
    public static class KRPC
    {
        /// <summary>
        /// Returns some information about the server, such as the version.
        /// </summary>
        [KRPCProcedure]
        public static Status GetStatus ()
        {
            var status = new Status ();
            var version = System.Reflection.Assembly.GetExecutingAssembly ().GetName ().Version;
            status.Version = version.Major + "." + version.Minor + "." + version.Build;
            var server = KRPCServer.Context.Server;
            status.BytesRead = server != null ? server.BytesRead : 0;
            status.BytesWritten = server != null ? server.BytesWritten : 0;
            status.BytesReadRate = server != null ? server.BytesReadRate : 0;
            status.BytesWrittenRate = server != null ? server.BytesWrittenRate : 0;
            status.RpcsExecuted = server != null ? server.RPCsExecuted : 0;
            status.RpcRate = server != null ? server.RPCRate : 0;
            status.OneRpcPerUpdate = server != null ? server.OneRPCPerUpdate : false;
            status.MaxTimePerUpdate = server != null ? server.MaxTimePerUpdate : 0;
            status.AdaptiveRateControl = server != null ? server.AdaptiveRateControl : false;
            status.BlockingRecv = server != null ? server.BlockingRecv : false;
            status.RecvTimeout = server != null ? server.RecvTimeout : 0;
            status.TimePerRpcUpdate = server != null ? server.TimePerRPCUpdate : 0;
            status.PollTimePerRpcUpdate = server != null ? server.PollTimePerRPCUpdate : 0;
            status.ExecTimePerRpcUpdate = server != null ? server.ExecTimePerRPCUpdate : 0;
            status.StreamRpcs = server != null ? server.StreamRPCs : 0;
            status.StreamRpcsExecuted = server != null ? server.StreamRPCsExecuted : 0;
            status.StreamRpcRate = server != null ? server.StreamRPCRate : 0;
            status.TimePerStreamUpdate = server != null ? server.TimePerStreamUpdate : 0;
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
                var service = new Messages.Service ();
                service.Name = serviceSignature.Name;
                foreach (var procedureSignature in serviceSignature.Procedures.Values) {
                    var procedure = new Procedure ();
                    procedure.Name = procedureSignature.Name;
                    if (procedureSignature.HasReturnType) {
                        procedure.HasReturnType = true;
                        procedure.ReturnType = TypeUtils.GetTypeName (procedureSignature.ReturnType);
                    }
                    foreach (var parameterSignature in procedureSignature.Parameters) {
                        var parameter = new Parameter ();
                        parameter.Name = parameterSignature.Name;
                        parameter.Type = TypeUtils.GetTypeName (parameterSignature.Type);
                        if (parameterSignature.HasDefaultArgument) {
                            parameter.HasDefaultArgument = true;
                            parameter.DefaultArgument = ProtocolBuffers.Encode (parameterSignature.DefaultArgument, parameterSignature.Type);
                        }
                        procedure.Parameters.Add (parameter);
                    }
                    foreach (var attribute in procedureSignature.Attributes) {
                        procedure.Attributes.Add (attribute);
                    }
                    if (procedureSignature.Documentation != "")
                        procedure.Documentation = procedureSignature.Documentation;
                    service.Procedures.Add (procedure);
                }
                foreach (var clsSignature in serviceSignature.Classes.Values) {
                    var cls = new Class ();
                    cls.Name = clsSignature.Name;
                    if (clsSignature.Documentation != "")
                        cls.Documentation = clsSignature.Documentation;
                    service.Classes.Add (cls);
                }
                foreach (var enmSignature in serviceSignature.Enumerations.Values) {
                    var enm = new Enumeration ();
                    enm.Name = enmSignature.Name;
                    if (enmSignature.Documentation != "")
                        enm.Documentation = enmSignature.Documentation;
                    foreach (var enmValueSignature in enmSignature.Values) {
                        var enmValue = new EnumerationValue ();
                        enmValue.Name = enmValueSignature.Name;
                        enmValue.Value = enmValueSignature.Value;
                        if (enmValueSignature.Documentation != "")
                            enmValue.Documentation = enmValueSignature.Documentation;
                        enm.Values.Add (enmValue);
                    }
                    service.Enumerations.Add (enm);
                }
                if (serviceSignature.Documentation != "")
                    service.Documentation = serviceSignature.Documentation;
                services.Services_.Add (service);
            }
            return services;
        }

        /// <summary>
        /// The game scene. See <see cref="CurrentGameScene"/>.
        /// </summary>
        [KRPCEnum]
        public enum GameScene
        {
            /// <summary>
            /// The game scene showing the Kerbal Space Center buildings.
            /// </summary>
            SpaceCenter,
            /// <summary>
            /// The game scene showing a vessel in flight (or on the launchpad/runway).
            /// </summary>
            Flight,
            /// <summary>
            /// The tracking station.
            /// </summary>
            TrackingStation,
            /// <summary>
            /// The Vehicle Assembly Building.
            /// </summary>
            EditorVAB,
            /// <summary>
            /// The Space Plane Hangar.
            /// </summary>
            EditorSPH
        }

        /// <summary>
        /// Get the current game scene.
        /// </summary>
        [KRPCProperty]
        public static GameScene CurrentGameScene {
            get {
                switch (KRPCServer.Context.GameScene) {
                case global::KRPC.Service.GameScene.SpaceCenter:
                    return GameScene.SpaceCenter;
                case global::KRPC.Service.GameScene.Flight:
                    return GameScene.Flight;
                case global::KRPC.Service.GameScene.TrackingStation:
                    return GameScene.TrackingStation;
                case global::KRPC.Service.GameScene.EditorVAB:
                    return GameScene.EditorVAB;
                case global::KRPC.Service.GameScene.EditorSPH:
                    return GameScene.EditorSPH;
                default:
                    throw new InvalidOperationException ("Unknown game scene");
                }
            }
        }

        /// <summary>
        /// Add a streaming request and return its identifier.
        /// </summary>
        [KRPCProcedure]
        public static uint AddStream (Request request)
        {
            return KRPCServer.Context.Server.AddStream (KRPCServer.Context.RPCClient, request);
        }

        /// <summary>
        /// Remove a streaming request.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveStream (uint id)
        {
            KRPCServer.Context.Server.RemoveStream (KRPCServer.Context.RPCClient, id);
        }
    }
}
