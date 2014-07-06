using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
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
    public sealed class Vessel : Equatable<Vessel>
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
        }

        public override bool Equals (Vessel other)
        {
            return vessel == other.vessel;
        }

        public override int GetHashCode ()
        {
            return vessel.GetHashCode ();
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
        public Flight Flight (ReferenceFrame referenceFrame = ReferenceFrame.Orbital)
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
        public Part RootPart {
            get { return new Part (vessel.rootPart); }
        }

        [KRPCProperty]
        public Part ControllingPart {
            get { return Parts.FirstOrDefault(x => x.isControlSource); }
            set { throw new NotImplementedException (); }
        }

        [KRPCProperty]
        public IList<Part> Parts {
            get { return vessel.parts.Select (x => new Part (x)).ToList (); }
        }

        //There might be a better solution for this, but for now this works.
        [KRPCProperty]
        public IList<ModuleEngines> Engines
        {
            get
            {
                List<ModuleEngines> engines = new List<ModuleEngines>();
                foreach (var part in vessel.Parts)
                {
                    foreach (global::PartModule module in part.Modules)
                    {
                        var engineModule = module as global::ModuleEngines;
                        if (engineModule != null)
                        {
                            engines.Add(new ModuleEngines(engineModule));
                        }
                    }
                }

                return engines;
            } 
        }
    }
}
