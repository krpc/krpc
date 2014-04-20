using System;
using KRPC.Service.Attributes;
using System.Collections.Generic;

namespace KRPCServices.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class Vessel
    {
        global::Vessel vessel;
        IDictionary<ReferenceFrame, Flight> flightObjects;

        internal Vessel (global::Vessel vessel)
        {
            this.vessel = vessel;
            flightObjects = new Dictionary<ReferenceFrame, KRPCServices.Services.Flight> ();
            Orbit = new Orbit (vessel);
            Control = new Control (vessel);
            AutoPilot = new AutoPilot (vessel);
            Resources = new Resources (vessel);
            Parts = new Parts (vessel);
        }

        [KRPCProperty]
        public string Name {
            get { throw new NotImplementedException (); }
            set { throw new NotImplementedException (); }
        }

        [KRPCMethod]
        public Flight Flight (ReferenceFrame referenceFrame = ReferenceFrame.Surface)
        {
            if (!flightObjects.ContainsKey (referenceFrame))
                flightObjects [referenceFrame] = new Flight (vessel, referenceFrame);
            return flightObjects [referenceFrame];
        }

        [KRPCProperty]
        public Vessel Target { get; set; }

        [KRPCProperty]
        public Orbit Orbit { get; private set; }

        [KRPCProperty]
        public Control Control { get; private set; }

        [KRPCProperty]
        public AutoPilot AutoPilot { get; private set; }

        [KRPCProperty]
        public Resources Resources { get; private set; }

        [KRPCProperty]
        public Parts Parts { get; private set; }
    }
}
