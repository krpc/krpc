using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double,double,double>;

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
        internal Vessel (global::Vessel vessel)
        {
            InternalVessel = vessel;
            Orbit = new Orbit (vessel);
            Control = new Control (vessel);
            AutoPilot = new AutoPilot (vessel);
            Resources = new Resources (vessel);
        }

        internal global::Vessel InternalVessel { get; private set; }

        public override bool Equals (Vessel other)
        {
            return InternalVessel == other.InternalVessel;
        }

        public override int GetHashCode ()
        {
            return InternalVessel.GetHashCode ();
        }

        [KRPCProperty]
        public string Name {
            get { return InternalVessel.vesselName; }
            set { InternalVessel.vesselName = value; }
        }

        [KRPCProperty]
        public VesselType Type {
            get {
                return InternalVessel.vesselType.ToVesselType ();
            }
            set {
                InternalVessel.vesselType = value.FromVesselType ();
            }
        }

        [KRPCProperty]
        public VesselSituation Situation {
            get { return InternalVessel.situation.ToVesselSituation (); }
        }

        [KRPCProperty]
        public double MET {
            get { return InternalVessel.missionTime; }
        }

        [KRPCMethod]
        public Flight Flight (ReferenceFrame referenceFrame = null)
        {
            if (referenceFrame == null)
                referenceFrame = ReferenceFrame.Orbital (InternalVessel);
            return new Flight (InternalVessel, referenceFrame);
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
        public double Mass {
            get {
                return InternalVessel.parts.Where (p => p.IsPhysicallySignificant ()).Select (p => p.TotalMass ()).Sum ();
            }
        }

        [KRPCProperty]
        public double DryMass {
            get {
                return InternalVessel.parts.Where (p => p.IsPhysicallySignificant ()).Select (p => p.DryMass ()).Sum ();
            }
        }

        [KRPCProperty]
        public double CrossSectionalArea {
            get { return FlightGlobals.DragMultiplier * Mass; }
        }

        [KRPCProperty]
        public double DragCoefficient {
            get {
                // Mass-weighted average of max_drag for each part
                // Note: Uses Part.mass, so does not include the mass of resources
                return InternalVessel.Parts.Select (p => p.maximum_drag * p.mass).Sum () /
                InternalVessel.Parts.Select (p => p.mass).Sum ();
            }
        }

        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (InternalVessel); }
        }

        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (InternalVessel); }
        }

        [KRPCProperty]
        public ReferenceFrame SurfaceReferenceFrame {
            get { return ReferenceFrame.Surface (InternalVessel); }
        }

        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalVessel.GetWorldPos3D ()).ToTuple ();
        }
    }
}