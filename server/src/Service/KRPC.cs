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
        [SuppressMessage ("Gendarme.Rules.Design", "ConsiderConvertingMethodToPropertyRule")]
        public static string GetClientName() {
          return CallContext.Client.Name;
        }

        /// <summary>
        /// Returns some information about the server, such as the version.
        /// </summary>
        [KRPCProcedure]
        [SuppressMessage ("Gendarme.Rules.Design", "ConsiderConvertingMethodToPropertyRule")]
        public static Status GetStatus ()
        {
            var core = Core.Instance;
            var config = Configuration.Instance;
            var version = System.Reflection.Assembly.GetExecutingAssembly ().GetName ().Version;
            var status = new Status (version.Major + "." + version.Minor + "." + version.Build);
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
                        procedure.ReturnType = procedureSignature.ReturnType;
                    foreach (var parameterSignature in procedureSignature.Parameters) {
                        var parameter = new Parameter (parameterSignature.Name, parameterSignature.Type);
                        if (parameterSignature.HasDefaultValue)
                            parameter.DefaultValue = parameterSignature.DefaultValue;
                        procedure.Parameters.Add (parameter);
                    }
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
                foreach (var exnSignature in serviceSignature.Exceptions.Values) {
                    var exn = new Messages.Exception (exnSignature.Name);
                    if (exnSignature.Documentation.Length > 0)
                        exn.Documentation = exnSignature.Documentation;
                    service.Exceptions.Add (exn);
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
                if ((scene & Service.GameScene.Flight) != 0)
                    return GameScene.Flight;
                if ((scene & Service.GameScene.TrackingStation) != 0)
                    return GameScene.TrackingStation;
                if ((scene & Service.GameScene.Editor) != 0) {
                    if (EditorDriver.editorFacility == EditorFacility.VAB)
                        return GameScene.EditorVAB;
                    if (EditorDriver.editorFacility == EditorFacility.SPH)
                        return GameScene.EditorSPH;
                }
                throw new System.InvalidOperationException ("Unknown game scene");
            }
        }

        /// <summary>
        /// Add a streaming request and return its identifier.
        /// </summary>
        [KRPCProcedure]
        public static Stream AddStream (ProcedureCall call)
        {
            return new Stream (Core.Instance.AddStream (CallContext.Client, call));
        }

        /// <summary>
        /// Remove a streaming request.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveStream (ulong id)
        {
            Core.Instance.RemoveStream (CallContext.Client, id);
        }

        /// <summary>
        /// A method call was made to a method that is invalid
        /// given the current state of the object.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [KRPCException (MappedException = typeof (System.InvalidOperationException))]
        public sealed class InvalidOperationException : System.Exception
        {
            /// <summary>
            /// Construct the exception.
            /// </summary>
            public InvalidOperationException ()
            {
            }

            /// <summary>
            /// Construct the exception.
            /// </summary>
            public InvalidOperationException (string message) :
                base (message)
            {
            }

            /// <summary>
            /// Construct the exception.
            /// </summary>
            public InvalidOperationException (string message, System.Exception innerException) :
                base (message, innerException)
            {
            }
        }

        /// <summary>
        /// A method was invoked where at least one of the passed arguments does not
        /// meet the parameter specification of the method.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [KRPCException (MappedException = typeof (System.ArgumentException))]
        public sealed class ArgumentException : System.Exception
        {
            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentException ()
            {
            }

            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentException (string message) :
                base (message)
            {
            }

            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentException (string message, System.Exception innerException) :
                base (message, innerException)
            {
            }
        }

        /// <summary>
        /// A null reference was passed to a method that does not accept it as a valid argument.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [KRPCException (MappedException = typeof (System.ArgumentNullException))]
        public sealed class ArgumentNullException : System.Exception
        {
            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentNullException ()
            {
            }

            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentNullException (string message) :
                base (message)
            {
            }

            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentNullException (string message, System.Exception innerException) :
                base (message, innerException)
            {
            }
        }

        /// <summary>
        /// The value of an argument is outside the allowable range of values as defined by the invoked method.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [KRPCException (MappedException = typeof (System.ArgumentOutOfRangeException))]
        public sealed class ArgumentOutOfRangeException : System.Exception
        {
            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentOutOfRangeException ()
            {
            }

            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentOutOfRangeException (string message) :
                base (message)
            {
            }

            /// <summary>
            /// Construct the exception.
            /// </summary>
            public ArgumentOutOfRangeException (string message, System.Exception innerException) :
                base (message, innerException)
            {
            }
        }
    }
}
