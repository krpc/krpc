using System;
using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class Vessel
    {
        VesselData vesselData;

        internal Vessel (global::Vessel vessel)
        {
            vesselData = new VesselData (vessel);
            OrbitalFlight = new Flight (vesselData);
            //TODO: SurfaceFlight
            //TODO: TargetFlight
            Orbit = new Orbit (vesselData);
            Control = new Control (vessel);
            Resources = new Resources (vessel);
            Parts = new Parts (vessel);
        }

        [KRPCProperty]
        public string Name {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        [KRPCProperty]
        public Flight OrbitalFlight { get; private set; }

        [KRPCProperty]
        public Flight SurfaceFlight { get { throw new NotImplementedException(); } }

        [KRPCProperty]
        public Flight TargetFlight { get { throw new NotImplementedException(); } }

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
