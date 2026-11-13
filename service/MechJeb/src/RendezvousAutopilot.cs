using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The rendezvous autopilot, which brings the vessel close to the target vessel.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class RendezvousAutopilot : KRPCComputerModule
    {
        static Field<object, double> desiredDistance;
        static Field<object, double> maxPhasingOrbits;
        static Func<object, string> status;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleRendezvousAutopilot");
            desiredDistance = Bindings.EditableDouble (type, "desiredDistance");
            maxPhasingOrbits = Bindings.EditableDouble (type, "maxPhasingOrbits");
            status = type.Getter<object, string> ("status");
        }

        internal RendezvousAutopilot () : base ("MechJebModuleRendezvousAutopilot")
        {
        }

        /// <summary>
        /// The final distance from the target, in meters.
        /// </summary>
        [KRPCProperty]
        public double DesiredDistance {
            get { return desiredDistance.Get (Instance); }
            set { desiredDistance.Set (Instance, value); }
        }

        /// <summary>
        /// The largest number of orbits to wait for the phase angle.
        /// </summary>
        [KRPCProperty]
        public double MaxPhasingOrbits {
            get { return maxPhasingOrbits.Get (Instance); }
            set { maxPhasingOrbits.Set (Instance, value); }
        }

        /// <summary>
        /// The status of the autopilot.
        /// </summary>
        [KRPCProperty]
        public string Status {
            get { return status (Instance); }
        }
    }
}
