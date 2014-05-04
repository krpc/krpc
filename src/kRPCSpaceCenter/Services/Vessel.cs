using System;
using KRPC.Service.Attributes;
using System.Collections.Generic;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum VesselType
    {
        Ship,
        Station,
        Lander,
        Probe,
        Rover,
        Base,
        Debris
    }

    [KRPCEnum (Service = "SpaceCenter")]
    public enum VesselSituation
    {
        Docked,
        Escaping,
        Flying,
        Landed,
        Orbiting,
        PreLaunch,
        Splashed,
        SubOrbital
    }

    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Vessel
    {
        global::Vessel vessel;
        IDictionary<ReferenceFrame, Flight> flightObjects;

        internal Vessel (global::Vessel vessel)
        {
            this.vessel = vessel;
            flightObjects = new Dictionary<ReferenceFrame, Flight> ();
            Orbit = new Orbit (vessel);
            Control = new Control (vessel);
            AutoPilot = new AutoPilot (vessel);
            Resources = new Resources (vessel);
            Parts = new Parts (vessel);
        }

        [KRPCProperty]
        public string Name {
            get { return vessel.vesselName; }
            set { vessel.vesselName = value; }
        }

        [KRPCProperty]
        public VesselType Type {
            get {
                return vessel.vesselType.ToVesselType ();
            }
            set {
                vessel.vesselType = value.FromVesselType ();
            }
        }

        [KRPCProperty]
        public VesselSituation Situation {
            get { return vessel.situation.ToVesselSituation (); }
        }

        [KRPCProperty]
        public double MET {
            get { return vessel.missionTime; }
        }

        [KRPCMethod]
        public Flight Flight (ReferenceFrame referenceFrame = ReferenceFrame.Surface)
        {
            if (!flightObjects.ContainsKey (referenceFrame))
                flightObjects [referenceFrame] = new Flight (vessel, referenceFrame);
            return flightObjects [referenceFrame];
        }

        [KRPCProperty]
        public Vessel Target {
            get { throw new NotImplementedException (); }
            set { throw new NotImplementedException (); }
        }

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
