using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPCSpaceCenter.Services
{
    //FIXME: need to perform memory management for node objects
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Node : Equatable<Node>
    {
        /// Note: Maneuver node delta-v vectors use a special coordinate system.
        /// The z-component is the prograde component.
        /// The y-component is the normal component.
        /// The x-component is the radial component.

        readonly global::Vessel vessel;

        internal Node (global::Vessel vessel, double UT, float prograde, float normal, float radial)
        {
            this.vessel = vessel;
            InternalNode = vessel.patchedConicSolver.AddManeuverNode (UT);
            InternalNode.OnGizmoUpdated (new Vector3d (radial, normal, prograde), UT);
        }

        public Node (ManeuverNode node)
        {
            InternalNode = node;
        }

        public global::Vessel InternalVessel { get; private set; }

        public ManeuverNode InternalNode { get; private set; }

        public override bool Equals (Node obj)
        {
            return InternalNode == obj.InternalNode;
        }

        public override int GetHashCode ()
        {
            //TODO: node should not be null, but Remove could set it as null
            return InternalNode == null ? 0 : InternalNode.GetHashCode ();
        }

        internal Vector3d WorldBurnVector {
            get {
                var prograde = InternalNode.patch.getOrbitalVelocityAtUT (InternalNode.UT).SwapYZ ().normalized;
                var normal = InternalNode.patch.GetOrbitNormal ().SwapYZ ().normalized;
                var radial = Vector3d.Cross (normal, prograde);
                return Prograde * prograde + Normal * normal + Radial * radial;
            }
        }

        [KRPCProperty]
        public float Prograde {
            get { return (float)InternalNode.DeltaV.z; }
            set {
                InternalNode.DeltaV.z = value;
                InternalNode.OnGizmoUpdated (InternalNode.DeltaV, InternalNode.UT);
            }
        }

        [KRPCProperty]
        public float Normal {
            get { return (float)InternalNode.DeltaV.y; }
            set {
                InternalNode.DeltaV.y = value;
                InternalNode.OnGizmoUpdated (InternalNode.DeltaV, InternalNode.UT);
            }
        }

        [KRPCProperty]
        public float Radial {
            get { return (float)InternalNode.DeltaV.x; }
            set {
                InternalNode.DeltaV.x = value;
                InternalNode.OnGizmoUpdated (InternalNode.DeltaV, InternalNode.UT);
            }
        }

        [KRPCProperty]
        public float DeltaV {
            get { return (float)InternalNode.DeltaV.magnitude; }
            set {
                var direction = InternalNode.DeltaV.normalized;
                InternalNode.OnGizmoUpdated (new Vector3d (direction.x * value, direction.y * value, direction.z * value), InternalNode.UT);
            }
        }

        [KRPCProperty]
        public float RemainingDeltaV {
            get { return (float)InternalNode.GetBurnVector (InternalNode.patch).magnitude; }
        }

        [KRPCMethod]
        public Tuple3 BurnVector (ReferenceFrame referenceFrame = null)
        {
            if (referenceFrame == null)
                referenceFrame = ReferenceFrame.Orbital (vessel);
            return referenceFrame.DirectionFromWorldSpace (WorldBurnVector).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 RemainingBurnVector (ReferenceFrame referenceFrame = null)
        {
            if (referenceFrame == null)
                referenceFrame = ReferenceFrame.Orbital (vessel);
            return referenceFrame.DirectionFromWorldSpace (InternalNode.GetBurnVector (InternalNode.patch)).ToTuple ();
        }

        [KRPCProperty]
        public double UT {
            get { return InternalNode.UT; }
            set { InternalNode.UT = value; }
        }

        [KRPCProperty]
        public double TimeTo {
            get { return UT - SpaceCenter.UT; }
        }

        [KRPCProperty]
        public Orbit Orbit {
            get { return new Orbit (InternalNode.nextPatch); }
        }

        [KRPCMethod]
        public void Remove ()
        {
            InternalNode.RemoveSelf ();
            InternalNode = null;
            // TODO: delete this Node object
        }

        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (InternalNode); }
        }

        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (InternalNode); }
        }

        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalNode.patch.getPositionAtUT (InternalNode.UT)).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (WorldBurnVector.normalized).ToTuple ();
        }
    }
}
