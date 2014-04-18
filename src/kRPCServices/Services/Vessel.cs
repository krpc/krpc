using System;
using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class Vessel
    {
        internal Vessel (global::Vessel vessel)
        {
            OrbitalFlight = new Flight (vessel, Flight.ReferenceFrame.Orbital);
            SurfaceFlight = new Flight (vessel, Flight.ReferenceFrame.Surface);
            TargetFlight = new Flight (vessel, Flight.ReferenceFrame.Target);
            Orbit = new Orbit (vessel);
            Control = new Control (vessel);
            Resources = new Resources (vessel);
            Parts = new Parts (vessel);
        }

        [KRPCProperty]
        public string Name {
            get { throw new NotImplementedException (); }
            set { throw new NotImplementedException (); }
        }

        [KRPCProperty]
        public Flight OrbitalFlight { get; private set; }

        [KRPCProperty]
        public Flight SurfaceFlight { get; private set; }

        [KRPCProperty]
        public Flight TargetFlight { get; private set; }

        [KRPCProperty]
        public Vessel Target { get; set; }

        [KRPCProperty]
        public Orbit Orbit { get; private set; }

        [KRPCProperty]
        public Control Control { get; private set; }

        [KRPCProperty]
        public Resources Resources { get; private set; }

        [KRPCProperty]
        public Parts Parts { get; private set; }
    }
}
