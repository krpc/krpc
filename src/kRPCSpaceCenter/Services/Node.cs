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

        ManeuverNode node;

        internal Node (global::Vessel vessel, double UT, double prograde, double normal, double radial)
        {
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

        Vector3d WorldBurnVector {
            get {
                var prograde = node.patch.getOrbitalVelocityAtUT (node.UT).normalized;
                var normal = node.patch.GetOrbitNormal ().normalized;
                var radial = Vector3d.Cross (normal, prograde);
                return Prograde * prograde + Normal * normal + Radial * radial;
            }
        }

        [KRPCProperty]
        public double Prograde {
            get { return node.DeltaV.z; }
            set {
                node.DeltaV.z = value;
                node.OnGizmoUpdated (node.DeltaV, node.UT);
            }
        }

        [KRPCProperty]
        public double Normal {
            get { return node.DeltaV.y; }
            set {
                node.DeltaV.y = value;
                node.OnGizmoUpdated (node.DeltaV, node.UT);
            }
        }

        [KRPCProperty]
        public double Radial {
            get { return node.DeltaV.x; }
            set {
                node.DeltaV.x = value;
                node.OnGizmoUpdated (node.DeltaV, node.UT);
            }
        }

        [KRPCProperty]
        public double DeltaV {
            get { return node.DeltaV.magnitude; }
            set {
                var direction = node.DeltaV.normalized;
                node.OnGizmoUpdated (new Vector3d (direction.x * value, direction.y * value, direction.z * value), node.UT);
            }
        }

        [KRPCMethod]
        public Tuple3 BurnVector (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (WorldBurnVector).ToTuple ();
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
