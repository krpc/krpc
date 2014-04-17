using KRPC.Service.Attributes;
using KRPC.Schema.Geometry;

namespace KRPCServices.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class Node
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
        public KRPC.Schema.Geometry.Vector3 Vector {
            get { 
                return Vector3.CreateBuilder ()
                    .SetX (Prograde)
                    .SetY (Normal)
                    .SetZ (Radial)
                    .Build ();
            }
            set {
                node.DeltaV.x = value.Z;
                node.DeltaV.y = value.Y;
                node.DeltaV.z = value.X;
                node.OnGizmoUpdated (node.DeltaV, node.UT);
            }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Direction {
            get {
                var direction = node.DeltaV.normalized;
                return Utils.ToVector3 (new Vector3d (direction.z, direction.y, direction.x));
            }
            set {
                var magnitude = node.DeltaV.magnitude;
                node.OnGizmoUpdated (new Vector3d (value.Z * magnitude, value.Y * magnitude, value.X * magnitude), node.UT);
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

        [KRPCProperty]
        public double UT {
            get { return node.UT; }
            set { node.UT = value; }
        }

        [KRPCProperty]
        public double TimeTo {
            get { return UT - SpaceCenter.UT; }
        }

        [KRPCMethod]
        public void Remove ()
        {
            node.RemoveSelf ();
            node = null;
            // TODO: delete this Node object
        }
    }
}
