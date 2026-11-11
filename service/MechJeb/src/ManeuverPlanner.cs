using System;
using KRPC.MechJeb.Maneuver;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The maneuver planner, which makes maneuver nodes for its operations.
    /// </summary>
    /// <remarks>
    /// MechJeb shares the operations, and most of their settings, between vessels.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class ManeuverPlanner
    {
        static Func<Array> operations;

        readonly OperationApoapsis operationApoapsis = new OperationApoapsis ();
        readonly OperationCircularize operationCircularize = new OperationCircularize ();
        readonly OperationCourseCorrection operationCourseCorrection = new OperationCourseCorrection ();
        readonly OperationEllipticize operationEllipticize = new OperationEllipticize ();
        readonly OperationInclination operationInclination = new OperationInclination ();
        readonly OperationInterplanetaryTransfer operationInterplanetaryTransfer = new OperationInterplanetaryTransfer ();
        readonly OperationKillRelVel operationKillRelVel = new OperationKillRelVel ();
        readonly OperationLambert operationLambert = new OperationLambert ();
        readonly OperationLan operationLan = new OperationLan ();
        readonly OperationLongitude operationLongitude = new OperationLongitude ();
        readonly OperationMoonReturn operationMoonReturn = new OperationMoonReturn ();
        readonly OperationPeriapsis operationPeriapsis = new OperationPeriapsis ();
        readonly OperationPlane operationPlane = new OperationPlane ();
        readonly OperationResonantOrbit operationResonantOrbit = new OperationResonantOrbit ();
        readonly OperationSemiMajor operationSemiMajor = new OperationSemiMajor ();
        readonly OperationTransfer operationTransfer = new OperationTransfer ();

        internal static void Load ()
        {
            operations = Reflection.Static<Array> (Reflection.Type ("MuMech.MechJebModuleManeuverPlanner"), "_operation");
        }

        internal ManeuverPlanner ()
        {
        }

        /// <summary>
        /// The MechJeb operation of the given type name, such as <c>OperationApoapsis</c>.
        /// </summary>
        internal static object Operation (string name)
        {
            foreach (var operation in operations ()) {
                if (operation.GetType ().Name == name)
                    return operation;
            }
            throw new MJServiceException ("MechJeb operation " + name + " not found");
        }

        /// <summary>
        /// The operation that changes the apoapsis.
        /// </summary>
        [KRPCProperty]
        public OperationApoapsis OperationApoapsis {
            get { return operationApoapsis; }
        }

        /// <summary>
        /// The operation that circularizes the orbit.
        /// </summary>
        [KRPCProperty]
        public OperationCircularize OperationCircularize {
            get { return operationCircularize; }
        }

        /// <summary>
        /// The operation that fine tunes the closest approach to the target.
        /// </summary>
        [KRPCProperty]
        public OperationCourseCorrection OperationCourseCorrection {
            get { return operationCourseCorrection; }
        }

        /// <summary>
        /// The operation that changes both the periapsis and the apoapsis.
        /// </summary>
        [KRPCProperty]
        public OperationEllipticize OperationEllipticize {
            get { return operationEllipticize; }
        }

        /// <summary>
        /// The operation that changes the inclination.
        /// </summary>
        [KRPCProperty]
        public OperationInclination OperationInclination {
            get { return operationInclination; }
        }

        /// <summary>
        /// The operation that transfers to another planet.
        /// </summary>
        [KRPCProperty]
        public OperationInterplanetaryTransfer OperationInterplanetaryTransfer {
            get { return operationInterplanetaryTransfer; }
        }

        /// <summary>
        /// The operation that matches velocities with the target.
        /// </summary>
        [KRPCProperty]
        public OperationKillRelVel OperationKillRelVel {
            get { return operationKillRelVel; }
        }

        /// <summary>
        /// The operation that intercepts the target at a chosen time.
        /// </summary>
        [KRPCProperty]
        public OperationLambert OperationLambert {
            get { return operationLambert; }
        }

        /// <summary>
        /// The operation that changes the longitude of the ascending node.
        /// </summary>
        [KRPCProperty]
        public OperationLan OperationLan {
            get { return operationLan; }
        }

        /// <summary>
        /// The operation that changes the surface longitude of the apsis.
        /// </summary>
        [KRPCProperty]
        public OperationLongitude OperationLongitude {
            get { return operationLongitude; }
        }

        /// <summary>
        /// The operation that returns from a moon.
        /// </summary>
        [KRPCProperty]
        public OperationMoonReturn OperationMoonReturn {
            get { return operationMoonReturn; }
        }

        /// <summary>
        /// The operation that changes the periapsis.
        /// </summary>
        [KRPCProperty]
        public OperationPeriapsis OperationPeriapsis {
            get { return operationPeriapsis; }
        }

        /// <summary>
        /// The operation that matches planes with the target.
        /// </summary>
        [KRPCProperty]
        public OperationPlane OperationPlane {
            get { return operationPlane; }
        }

        /// <summary>
        /// The operation that changes to a resonant orbit.
        /// </summary>
        [KRPCProperty]
        public OperationResonantOrbit OperationResonantOrbit {
            get { return operationResonantOrbit; }
        }

        /// <summary>
        /// The operation that changes the semi-major axis.
        /// </summary>
        [KRPCProperty]
        public OperationSemiMajor OperationSemiMajor {
            get { return operationSemiMajor; }
        }

        /// <summary>
        /// The operation that transfers to the target.
        /// </summary>
        [KRPCProperty]
        public OperationTransfer OperationTransfer {
            get { return operationTransfer; }
        }
    }
}
