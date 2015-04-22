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
        ManeuverNode node;

        internal Node (global::Vessel vessel, double UT, float prograde, float normal, float radial)
        {
            this.vessel = vessel;
            node = vessel.patchedConicSolver.AddManeuverNode (UT);
            node.OnGizmoUpdated (new Vector3d (radial, normal, prograde), UT);
        }

        internal Node (ManeuverNode node)
        {
            this.node = node;
        }

        public override bool Equals (Node obj)
        {
            return node == obj.node;
        }

        public override int GetHashCode ()
        {
            //TODO: node should not be null, but Remove could set it as null
            return node == null ? 0 : node.GetHashCode ();
        }

        internal Vector3d WorldBurnVector {
            get {
                var prograde = node.patch.getOrbitalVelocityAtUT (node.UT).SwapYZ ().normalized;
                var normal = node.patch.GetOrbitNormal ().SwapYZ ().normalized;
                var radial = Vector3d.Cross (normal, prograde);
                return Prograde * prograde + Normal * normal + Radial * radial;
            }
        }

        [KRPCProperty]
        public float Prograde {
            get { return (float) node.DeltaV.z; }
            set {
                node.DeltaV.z = value;
                node.OnGizmoUpdated (node.DeltaV, node.UT);
            }
        }

        [KRPCProperty]
        public float Normal {
            get { return (float) node.DeltaV.y; }
            set {
                node.DeltaV.y = value;
                node.OnGizmoUpdated (node.DeltaV, node.UT);
            }
        }

        [KRPCProperty]
        public float Radial {
            get { return (float) node.DeltaV.x; }
            set {
                node.DeltaV.x = value;
                node.OnGizmoUpdated (node.DeltaV, node.UT);
            }
        }

        [KRPCProperty]
        public float DeltaV {
            get { return (float) node.DeltaV.magnitude; }
            set {
                var direction = node.DeltaV.normalized;
                node.OnGizmoUpdated (new Vector3d (direction.x * value, direction.y * value, direction.z * value), node.UT);
            }
        }

        [KRPCProperty]
        public float RemainingDeltaV {
            get { return (float) node.GetBurnVector (node.patch).magnitude; }
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
            return referenceFrame.DirectionFromWorldSpace (node.GetBurnVector (node.patch)).ToTuple ();
        }

        [KRPCProperty]
        public double UT {
            get { return node.UT; }
            set { node.UT = value; }
        }

        [KRPCProperty]
        public double TimeTo {
            get { return UT - SpaceCenter.UT; }
        }

        [KRPCProperty]
        public Orbit Orbit {
            get { return new Orbit (node.nextPatch); }
        }

        [KRPCMethod]
        public void Remove ()
        {
            node.RemoveSelf ();
            node = null;
            // TODO: delete this Node object
        }

        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (node); }
        }

        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (node); }
        }

        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (node.patch.getPositionAtUT (node.UT)).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (WorldBurnVector.normalized).ToTuple ();
        }
    }
}
