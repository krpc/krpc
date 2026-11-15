using System;
using System.Collections;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.Services;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Thrown when an operation cannot make its nodes, such as when it needs a target and
    /// there is none.
    /// </summary>
    [KRPCException (Service = "MechJeb")]
    public sealed class OperationException : Exception
    {
        /// <summary>
        /// Create the exception.
        /// </summary>
        public OperationException ()
        {
        }

        /// <summary>
        /// Create the exception with a message.
        /// </summary>
        public OperationException (string message) : base (message)
        {
        }

        /// <summary>
        /// Create the exception with a message and an inner exception.
        /// </summary>
        public OperationException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }

    /// <summary>
    /// An operation of the maneuver planner.
    /// </summary>
    public abstract class Operation
    {
        static Type operationException;
        static Func<object, string> errorMessage;
        static Action<object, string> setErrorMessage;
        static Func<object, List<ManeuverNode>> getManeuverNodes;
        static Func<object, global::Orbit, double, object, IEnumerable> makeNodesImpl;
        static Func<global::Vessel, global::Orbit, Vector3d, double, ManeuverNode> placeManeuverNode;
        static Func<object, Vector3d> dV;
        static Func<object, double> uT;

        internal static void Load ()
        {
            operationException = Bindings.Type ("MuMech.OperationException").Type;
            var type = Bindings.Type ("MuMech.Operation");
            errorMessage = type.Method<Func<object, string>> ("GetErrorMessage");
            setErrorMessage = type.Setter<object, string> ("ErrorMessage");
            getManeuverNodes = Bindings.Type ("MuMech.MechJebModuleManeuverPlanner").Method<Func<object, List<ManeuverNode>>> ("GetManeuverNodes");
            makeNodesImpl = type.Method<Func<object, global::Orbit, double, object, IEnumerable>> ("MakeNodesImpl");
            placeManeuverNode = Bindings.Type ("MuMech.VesselExtensions").Method<Func<global::Vessel, global::Orbit, Vector3d, double, ManeuverNode>> ("PlaceManeuverNode");
            var parameters = Bindings.Type ("MuMech.ManeuverParameters");
            dV = parameters.Getter<object, Vector3d> ("dV");
            uT = parameters.Getter<object, double> ("UT");
            OperationApoapsis.Load ();
            OperationCircularize.Load ();
            OperationCourseCorrection.Load ();
            OperationEllipticize.Load ();
            OperationInclination.Load ();
            OperationInterplanetaryTransfer.Load ();
            OperationKillRelVel.Load ();
            OperationLambert.Load ();
            OperationLan.Load ();
            OperationLongitude.Load ();
            OperationMoonReturn.Load ();
            OperationPeriapsis.Load ();
            OperationPlane.Load ();
            OperationResonantOrbit.Load ();
            OperationSemiMajor.Load ();
            OperationTransfer.Load ();
        }

        readonly string operationName;

        internal Operation (string name)
        {
            operationName = name;
        }

        /// <summary>
        /// The MechJeb operation, which MechJeb shares between vessels.
        /// </summary>
        internal object Instance {
            get { return ManeuverPlanner.Operation (operationName); }
        }

        /// <summary>
        /// The warning or error from the last call to <c>MakeNodes</c>, or an empty string.
        /// </summary>
        [KRPCProperty]
        public string ErrorMessage {
            get { return errorMessage (Instance); }
        }

        /// <summary>
        /// Make the maneuver nodes of the operation. Throws an <see cref="OperationException"/>
        /// if the operation cannot make them.
        /// </summary>
        /// <returns>The maneuver nodes.</returns>
        /// <remarks>
        /// The nodes follow the vessel's last maneuver node, if it has one. A warning may be
        /// left in <c>ErrorMessage</c>.
        /// </remarks>
        [KRPCMethod]
        public IList<Node> MakeNodes ()
        {
            var instance = Instance;
            var targetController = MechJeb.TargetController;
            targetController.Update ();
            var vessel = FlightGlobals.ActiveVessel;
            // Plan from the orbit after the last node, as MechJeb's window does
            var orbit = vessel.orbit;
            var time = Planetarium.GetUniversalTime ();
            var existing = getManeuverNodes (MechJeb.Module ("MechJebModuleManeuverPlanner"));
            if (existing.Count > 0) {
                var last = existing [existing.Count - 1];
                orbit = last.nextPatch;
                time = last.UT;
                if (orbit == null)
                    throw new OperationException ("The last maneuver node has no orbit after it");
            }
            setErrorMessage (instance, string.Empty);
            IEnumerable parameters;
            try {
                parameters = makeNodesImpl (instance, orbit, time, targetController.Instance);
            } catch (Exception exn) {
                if (operationException == null || !operationException.IsInstanceOfType (exn))
                    throw new MJServiceException (exn.Message, exn);
                // Keep the message, as MechJeb's MakeNodes does
                setErrorMessage (instance, exn.Message);
                throw new OperationException (exn.Message);
            }
            var nodes = new List<ManeuverNode> ();
            if (parameters == null)
                return new List<Node> ();
            try {
                foreach (var parameter in parameters)
                    nodes.Add (placeManeuverNode (vessel, orbit, dV (parameter), uT (parameter)));
            } catch (Exception exn) {
                foreach (var node in nodes)
                    node.RemoveSelf ();
                throw new MJServiceException (exn.Message, exn);
            }
            return nodes.ConvertAll (node => new Node (vessel, node));
        }
    }

    /// <summary>
    /// An operation of the maneuver planner that happens at a selected time.
    /// </summary>
    public abstract class TimedOperation : Operation
    {
        internal TimedOperation (string name) : base (name)
        {
            TimeSelector = new TimeSelector (this);
        }

        /// <summary>
        /// A getter for the time selector of an operation type. MechJeb holds it in a static
        /// field for most operations, and in an instance field for the others.
        /// </summary>
        internal static Func<object, object> TimeSelectorField (MemberBinder type)
        {
            var field = type.Type == null ? null : Accessor.Getter<object, object> (type.Type, "_timeSelector");
            if (field != null)
                return field;
            var staticField = type.StaticGetter<object> ("_timeSelector");
            return instance => staticField ();
        }

        /// <summary>
        /// The MechJeb time selector of the operation.
        /// </summary>
        internal abstract object TimeSelectorInstance { get; }

        /// <summary>
        /// The time of the maneuver.
        /// </summary>
        [KRPCProperty]
        public TimeSelector TimeSelector { get; private set; }
    }
}
