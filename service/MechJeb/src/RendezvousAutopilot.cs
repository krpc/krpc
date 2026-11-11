using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The rendezvous autopilot, which brings the vessel close to the target vessel.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class RendezvousAutopilot : KRPCComputerModule
    {
        static Value<double> desiredDistance;
        static Value<double> maxPhasingOrbits;
        static Value<string> status;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleRendezvousAutopilot");
            desiredDistance = Reflection.EditableDouble (type, "desiredDistance");
            maxPhasingOrbits = Reflection.EditableDouble (type, "maxPhasingOrbits");
            status = Reflection.Field<string> (type, "status");
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
            get { return status.Get (Instance); }
        }
    }
}
