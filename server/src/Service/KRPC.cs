using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        [SuppressMessage ("Gendarme.Rules.Design", "ConsiderConvertingMethodToPropertyRule")]
        public static Status GetStatus ()
        {
            var core = Core.Instance;
            var version = System.Reflection.Assembly.GetExecutingAssembly ().GetName ().Version;
            var status = new Status (version.Major + "." + version.Minor + "." + version.Build);
            status.BytesRead = core.BytesRead;
            status.BytesWritten = core.BytesWritten;
            status.BytesReadRate = core.BytesReadRate;
            status.BytesWrittenRate = core.BytesWrittenRate;
            status.RpcsExecuted = core.RPCsExecuted;
            status.RpcRate = core.RPCRate;
            status.OneRpcPerUpdate = core.OneRPCPerUpdate;
            status.MaxTimePerUpdate = core.MaxTimePerUpdate;
            status.AdaptiveRateControl = core.AdaptiveRateControl;
            status.BlockingRecv = core.BlockingRecv;
            status.RecvTimeout = core.RecvTimeout;
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
        [SuppressMessage ("Gendarme.Rules.Design", "ConsiderConvertingMethodToPropertyRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public static Messages.Services GetServices ()
        {
            var services = new Messages.Services ();
            foreach (var serviceSignature in Services.Instance.Signatures.Values) {
                var service = new Messages.Service (serviceSignature.Name);
                foreach (var procedureSignature in serviceSignature.Procedures.Values) {
                    var procedure = new Procedure (procedureSignature.Name);
                    if (procedureSignature.HasReturnType)
                        procedure.ReturnType = TypeUtils.GetTypeName (procedureSignature.ReturnType);
                    foreach (var parameterSignature in procedureSignature.Parameters) {
                        var parameter = new Parameter (parameterSignature.Name, TypeUtils.GetTypeName (parameterSignature.Type));
                        if (parameterSignature.HasDefaultValue)
                            parameter.DefaultValue = parameterSignature.DefaultValue;
                        procedure.Parameters.Add (parameter);
                    }
                    foreach (var attribute in procedureSignature.Attributes)
                        procedure.Attributes.Add (attribute);
                    if (procedureSignature.Documentation.Length > 0)
                        procedure.Documentation = procedureSignature.Documentation;
                    service.Procedures.Add (procedure);
                }
                foreach (var clsSignature in serviceSignature.Classes.Values) {
                    var cls = new Class (clsSignature.Name);
                    if (clsSignature.Documentation.Length > 0)
                        cls.Documentation = clsSignature.Documentation;
                    service.Classes.Add (cls);
                }
                foreach (var enmSignature in serviceSignature.Enumerations.Values) {
                    var enm = new Enumeration (enmSignature.Name);
                    if (enmSignature.Documentation.Length > 0)
                        enm.Documentation = enmSignature.Documentation;
                    foreach (var enmValueSignature in enmSignature.Values) {
                        var enmValue = new EnumerationValue (enmValueSignature.Name, enmValueSignature.Value);
                        if (enmValueSignature.Documentation.Length > 0)
                            enmValue.Documentation = enmValueSignature.Documentation;
                        enm.Values.Add (enmValue);
                    }
                    service.Enumerations.Add (enm);
                }
                if (serviceSignature.Documentation.Length > 0)
                    service.Documentation = serviceSignature.Documentation;
                services.ServicesList.Add (service);
            }
            return services;
        }

        /// <summary>
        /// A list of RPC clients that are currently connected to the server.
        /// Each entry in the list is a clients identifier, name and address.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public static IList<Utils.Tuple<byte[], string, string>> Clients {
            get { return Core.Instance.RPCClients.Select (x => new Utils.Tuple<byte[], string, string> (x.Guid.ToByteArray (), x.Name, x.Address)).ToList (); }
        }

        /// <summary>
        /// The game scene. See <see cref="CurrentGameScene"/>.
        /// </summary>
        [KRPCEnum]
        [Serializable]
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Naming", "UsePluralNameInEnumFlagsRule")]
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
                var scene = CallContext.GameScene;
                if ((scene & Service.GameScene.SpaceCenter) != 0)
                    return GameScene.SpaceCenter;
                else if ((scene & Service.GameScene.Flight) != 0)
                    return GameScene.Flight;
                else if ((scene & Service.GameScene.TrackingStation) != 0)
                    return GameScene.TrackingStation;
                else if ((scene & Service.GameScene.Editor) != 0) {
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
            return Core.Instance.AddStream (CallContext.Client, request);
        }

        /// <summary>
        /// Remove a streaming request.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveStream (uint id)
        {
            Core.Instance.RemoveStream (CallContext.Client, id);
        }
    }
}
