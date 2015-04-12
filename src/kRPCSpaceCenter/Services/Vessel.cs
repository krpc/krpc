using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using KRPCSpaceCenter.ExternalAPI;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;
using System.Collections;
using System.Collections.Generic;

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
        Comms comms;

        internal Vessel (global::Vessel vessel)
        {
            InternalVessel = vessel;
            Orbit = new Orbit (vessel);
            Control = new Control (vessel);
            AutoPilot = new AutoPilot (vessel);
            Parts = new Parts.Parts (vessel);
            Resources = new VesselResources (vessel);
            if (RemoteTech.IsAvailable)
                comms = new Comms (vessel);
        }

        internal global::Vessel InternalVessel { get; private set; }

        public override bool Equals (Vessel obj)
        {
            return InternalVessel == obj.InternalVessel;
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
                referenceFrame = ReferenceFrame.Surface (InternalVessel);
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
        public VesselResources Resources { get; private set; }

        [KRPCProperty]
        public Parts.Parts Parts { get; private set; }

        [KRPCProperty]
        public Comms Comms {
            get {
                if (!RemoteTech.IsAvailable)
                    throw new InvalidOperationException ("RemoteTech is not installed");
                return comms;
            }
        }

        [KRPCProperty]
        public float Mass {
            get {
                return InternalVessel.parts.Where (p => p.IsPhysicallySignificant ()).Sum (p => p.TotalMass ());
            }
        }

        [KRPCProperty]
        public float DryMass {
            get {
                return InternalVessel.parts.Where (p => p.IsPhysicallySignificant ()).Sum (p => p.DryMass ());
            }
        }

        [KRPCProperty]
        public float CrossSectionalArea {
            get {
                if (FAR.IsAvailable)
                    return (float)FAR.GetActiveControlSys_RefArea ();
                else
                    return FlightGlobals.DragMultiplier * Mass;
            }
        }

        [KRPCProperty]
        public float Thrust {
            get { return Parts.Engines.Where (e => e.Active).Sum (e => e.Thrust); }
        }

        [KRPCProperty]
        public float AvailableThrust {
            get { return Parts.Engines.Where (e => e.Active).Sum (e => e.AvailableThrust); }
        }

        [KRPCProperty]
        public float MaxThrust {
            get { return Parts.Engines.Where (e => e.Active).Sum (e => e.MaxThrust); }
        }

        [KRPCProperty]
        public float SpecificImpulse {
            get {
                var thrust = Parts.Engines.Where (e => e.Active).Sum (e => e.Thrust);
                var fuelConsumption = Parts.Engines.Where (e => e.Active).Sum (e => e.Thrust / e.SpecificImpulse);
                if (fuelConsumption > 0f)
                    return thrust / fuelConsumption;
                return 0f;
            }
        }

        [KRPCProperty]
        public float VacuumSpecificImpulse {
            get {
                var thrust = Parts.Engines.Where (e => e.Active).Sum (e => e.Thrust);
                var fuelConsumption = Parts.Engines.Where (e => e.Active).Sum (e => e.Thrust / e.VacuumSpecificImpulse);
                if (fuelConsumption > 0f)
                    return thrust / fuelConsumption;
                return 0f;
            }
        }

        [KRPCProperty]
        public float KerbinSeaLevelSpecificImpulse {
            get {
                var thrust = Parts.Engines.Where (e => e.Active).Sum (e => e.Thrust);
                var fuelConsumption = Parts.Engines.Where (e => e.Active).Sum (e => e.Thrust / e.KerbinSeaLevelSpecificImpulse);
                if (fuelConsumption > 0f)
                    return thrust / fuelConsumption;
                return 0f;
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

        [KRPCProperty]
        public ReferenceFrame SurfaceVelocityReferenceFrame {
            get { return ReferenceFrame.SurfaceVelocity (InternalVessel); }
        }

        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalVessel.GetWorldPos3D ()).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.VelocityFromWorldSpace (InternalVessel.CoM, InternalVessel.GetOrbit ().GetVel ()).ToTuple ();
        }

        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            return referenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (InternalVessel.ReferenceTransform.up).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 AngularVelocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.AngularVelocityFromWorldSpace (InternalVessel.angularVelocity).ToTuple ();
        }
    }
}