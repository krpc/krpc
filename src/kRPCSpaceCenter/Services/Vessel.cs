using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using KRPCSpaceCenter.ExternalAPI;
using Tuple3 = KRPC.Utils.Tuple<double,double,double>;
using Tuple4 = KRPC.Utils.Tuple<double,double,double,double>;

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
            Resources = new Resources (vessel);
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
        public Comms Comms {
            get {
                if (!RemoteTech.IsAvailable)
                    throw new RPCException ("RemoteTech is not installed");
                return comms;
            }
        }

        [KRPCProperty]
        public double Mass {
            get {
                return InternalVessel.parts.Where (p => p.IsPhysicallySignificant ()).Sum (p => p.TotalMass ());
            }
        }

        [KRPCProperty]
        public double DryMass {
            get {
                return InternalVessel.parts.Where (p => p.IsPhysicallySignificant ()).Sum (p => p.DryMass ());
            }
        }

        [KRPCProperty]
        public double CrossSectionalArea {
            get {
                if (FAR.IsAvailable)
                    return FAR.GetActiveControlSys_RefArea ();
                else
                    return FlightGlobals.DragMultiplier * Mass;
            }
        }

        /// <summary>
        /// The maximum thrust (in Newtons) of all active engines combined when throttled up to 100%
        /// </summary>
        //FIXME: just sums the max thrust of every engine, i.e. assumes all engines are pointing the same direction
        [KRPCProperty]
        public double Thrust {
            get {
                double thrust = 0;
                foreach (var part in InternalVessel.parts) {
                    foreach (PartModule module in part.Modules) {
                        if (!module.isEnabled)
                            continue;
                        var engine = module as ModuleEngines;
                        if (engine != null) {
                            if (!engine.EngineIgnited || engine.getFlameoutState)
                                continue;
                            thrust += engine.maxThrust;
                        }
                        var engineFx = module as ModuleEnginesFX;
                        if (engineFx != null) {
                            if (!engine.EngineIgnited || engine.getFlameoutState)
                                continue;
                            thrust += engineFx.maxThrust;
                        }
                    }
                }
                return thrust * 1000d;
            }
        }

        /// <summary>
        /// The combined specific impulse (in seconds) of all active engines
        /// </summary>
        [KRPCProperty]
        public double SpecificImpulse {
            get {
                double totalThrust = 0;
                double totalFlowRate = 0;
                foreach (var part in InternalVessel.parts) {
                    foreach (PartModule module in part.Modules) {
                        if (!module.isEnabled)
                            continue;
                        var engine = module as ModuleEngines;
                        if (engine != null) {
                            if (!engine.EngineIgnited || engine.getFlameoutState)
                                continue;
                            totalThrust += engine.maxThrust;
                            totalFlowRate += (engine.maxThrust / engine.realIsp);
                        }
                        var engineFx = module as ModuleEnginesFX;
                        if (engineFx != null) {
                            if (!engine.EngineIgnited || engine.getFlameoutState)
                                continue;
                            totalThrust += engine.maxThrust;
                            totalFlowRate += (engine.maxThrust / engine.realIsp);
                        }
                    }
                }
                return totalThrust / totalFlowRate;
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

        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.VelocityFromWorldSpace (InternalVessel.CoM, InternalVessel.GetOrbit ().GetVel ()).ToTuple ();
        }

        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            return referenceFrame.RotationFromWorldSpace (InternalVessel.transform.rotation).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (InternalVessel.transform.up).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 AngularVelocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.AngularVelocityFromWorldSpace (InternalVessel.angularVelocity).ToTuple ();
        }
    }
}