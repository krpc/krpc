using System;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;

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
            status.BytesRead = KRPCCore.Instance.BytesRead;
            status.BytesWritten = KRPCCore.Instance.BytesWritten;
            status.BytesReadRate = KRPCCore.Instance.BytesReadRate;
            status.BytesWrittenRate = KRPCCore.Instance.BytesWrittenRate;
            status.RpcsExecuted = KRPCCore.Instance.RPCsExecuted;
            status.RpcRate = KRPCCore.Instance.RPCRate;
            status.OneRpcPerUpdate = KRPCCore.Instance.OneRPCPerUpdate;
            status.MaxTimePerUpdate = KRPCCore.Instance.MaxTimePerUpdate;
            status.AdaptiveRateControl = KRPCCore.Instance.AdaptiveRateControl;
            status.BlockingRecv = KRPCCore.Instance.BlockingRecv;
            status.RecvTimeout = KRPCCore.Instance.RecvTimeout;
            status.TimePerRpcUpdate = KRPCCore.Instance.TimePerRPCUpdate;
            status.PollTimePerRpcUpdate = KRPCCore.Instance.PollTimePerRPCUpdate;
            status.ExecTimePerRpcUpdate = KRPCCore.Instance.ExecTimePerRPCUpdate;
            status.StreamRpcs = KRPCCore.Instance.StreamRPCs;
            status.StreamRpcsExecuted = KRPCCore.Instance.StreamRPCsExecuted;
            status.StreamRpcRate = KRPCCore.Instance.StreamRPCRate;
            status.TimePerStreamUpdate = KRPCCore.Instance.TimePerStreamUpdate;
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
                        if (parameterSignature.HasDefaultValue) {
                            parameter.HasDefaultValue = true;
                            parameter.DefaultValue = parameterSignature.DefaultValue;
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
                var scene = KRPCCore.Context.GameScene;
                if ((scene & global::KRPC.Service.GameScene.SpaceCenter) != 0)
                    return GameScene.SpaceCenter;
                else if ((scene & global::KRPC.Service.GameScene.Flight) != 0)
                    return GameScene.Flight;
                else if ((scene & global::KRPC.Service.GameScene.TrackingStation) != 0)
                    return GameScene.TrackingStation;
                else if ((scene & global::KRPC.Service.GameScene.Editor) != 0) {
                    if (EditorDriver.editorFacility == EditorFacility.VAB)
                        return GameScene.EditorVAB;
                    else if (EditorDriver.editorFacility == EditorFacility.SPH)
                        return GameScene.EditorSPH;
                }
                throw new InvalidOperationException ("Unknown game scene");
            }
        }

        /// <summary>
        /// Add a streaming request and return its identifier.
        /// </summary>
        [KRPCProcedure]
        public static uint AddStream (Request request)
        {
            return KRPCCore.Instance.AddStream (KRPCCore.Context.RPCClient, request);
        }

        /// <summary>
        /// Remove a streaming request.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveStream (uint id)
        {
            KRPCCore.Instance.RemoveStream (KRPCCore.Context.RPCClient, id);
        }
    }
}
