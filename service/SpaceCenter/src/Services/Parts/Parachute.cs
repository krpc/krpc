using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A parachute. Obtained by calling <see cref="Part.Parachute"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Parachute : Equatable<Parachute>, IGameObjectState
    {
        ModuleRef parachuteRef;
        readonly Module realChute;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return internalPart.HasModule<ModuleParachute> () ||
                   internalPart.HasModule ("RealChuteModule");
        }

        internal Parachute (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            parachuteRef = ModuleRef.ForType<ModuleParachute> (internalPart);
            var realChuteModule = internalPart.Module ("RealChuteModule");
            if (realChuteModule != null)
                realChute = new Module(part, realChuteModule);
            if (parachuteRef.Find (internalPart) == null && realChute == null)
                throw new ArgumentException ("Part is not a parachute");
        }

        /// <summary>
        /// The stock parachute module, or null where the part carries a RealChutes one
        /// instead, which every member that reads this falls back to. A part that carries
        /// neither has no parachute left for this to stand for, so it raises rather than
        /// handing back a null for the fallback to be tried against.
        /// </summary>
        ModuleParachute InternalParachute {
            get {
                var parachute = (ModuleParachute)parachuteRef.Find (Part.InternalPart);
                if (parachute == null && realChute == null)
                    throw new ObjectDestroyedException (
                        "The parachute no longer exists, as its part no longer has one.");
                return parachute;
            }
        }

        /// <summary>
        /// The state of the parachute. A part carries a stock parachute module, a
        /// RealChutes one, or both, and either is enough, so the parachute takes the more
        /// alive of their states.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = parachuteRef.StateOn (Part);
                return realChute == null ? state : state.MostAlive (realChute.GameObjectState);
            }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Parachute other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode ();
        }

        void CheckStock ()
        {
            if (InternalParachute == null || realChute != null)
                throw new InvalidOperationException ("Operation not defined for a RealChutes parachute");
        }

        /// <summary>
        /// The part object for this parachute.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Deploys the parachute. This has no effect if the parachute has already
        /// been deployed.
        /// </summary>
        [KRPCMethod]
        public void Deploy ()
        {
            var parachute = InternalParachute;
            if (parachute)
                parachute.Deploy ();
            else if (realChute.HasVisibleEvent ("Deploy Chute"))
                realChute.TriggerVisibleEvent ("Deploy Chute");
        }

        /// <summary>
        /// Whether the parachute has been deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get {
                var parachute = InternalParachute;
                if (parachute)
                    return parachute.deploymentState != ModuleParachute.deploymentStates.STOWED;
                return realChute.VisibleEventNames.Any (x => x.Contains ("Cut"));
            }
        }

        /// <summary>
        /// Deploys the parachute. This has no effect if the parachute has already
        /// been armed or deployed.
        /// </summary>
        [KRPCMethod]
        public void Arm ()
        {
            if (realChute != null)
            {
                if (realChute.HasVisibleEvent("Arm parachute"))
                    realChute.TriggerVisibleEvent("Arm parachute");
                return;
            }
            var parachute = InternalParachute;
            if (parachute)
                parachute.Deploy();
        }

        /// <summary>
        /// Whether the parachute has been armed or deployed.
        /// </summary>
        [KRPCProperty]
        public bool Armed {
            get {
                if (realChute != null)
                    return realChute.HasVisibleEvent("Disarm parachute");
                var parachute = InternalParachute;
                return parachute && parachute.deploymentState == ModuleParachute.deploymentStates.ACTIVE;
            }
        }

        /// <summary>
        /// Cuts the parachute.
        /// </summary>
        [KRPCMethod]
        public void Cut()
        {
            if (realChute != null) {
                if (realChute.HasVisibleEvent("Cut main chute"))
                    realChute.TriggerVisibleEvent("Cut main chute");
                return;
            }
            var parachute = InternalParachute;
            if (parachute)
                parachute.CutParachute();
        }

        /// <summary>
        /// The current state of the parachute.
        /// </summary>
        [KRPCProperty]
        public ParachuteState State {
            get {
                var parachute = InternalParachute;
                if (parachute)
                    return parachute.deploymentState.ToParachuteState ();
                if (Armed)
                    return ParachuteState.Armed;
                if (Deployed)
                    return ParachuteState.Deployed;
                if (realChute.VisibleEventNames.Any(x => x.Contains("Deploy")))
                    return ParachuteState.Stowed;
                return ParachuteState.Cut;
            }
        }

        /// <summary>
        /// The altitude at which the parachute will full deploy, in meters.
        /// Only applicable to stock parachutes.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float DeployAltitude {
            get {
                CheckStock();
                return InternalParachute.deployAltitude;
            }
            set {
                CheckStock();
                InternalParachute.deployAltitude = value;
            }
        }

        /// <summary>
        /// The minimum pressure at which the parachute will semi-deploy, in atmospheres.
        /// Only applicable to stock parachutes.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public float DeployMinPressure {
            get {
                CheckStock ();
                return InternalParachute.minAirPressureToOpen;
            }
            set {
                CheckStock ();
                InternalParachute.minAirPressureToOpen = value;
            }
        }

        /// <summary>
        /// Whether it is currently safe to deploy the parachute, given the vessel's
        /// flight conditions. Computed by KSP from the current airspeed, pressure and
        /// temperature. Only applicable to stock parachutes.
        /// Note that KSP reports <see cref="ParachuteSafeState.Unsafe"/> for the first
        /// second after the vessel unpacks, before its thermal data is valid.
        /// </summary>
        [KRPCProperty]
        public ParachuteSafeState SafeState {
            get {
                CheckStock ();
                return InternalParachute.deploymentSafeState.ToParachuteSafeState ();
            }
        }
    }
}
