using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A parachute. Obtained by calling <see cref="Part.Parachute"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class Parachute : Equatable<Parachute>
    {
        readonly ModuleParachute parachute;
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
            parachute = internalPart.Module<ModuleParachute> ();
            var realChuteModule = internalPart.Module ("RealChuteModule");
            if (realChuteModule != null)
                realChute = new Module(part, realChuteModule);
            if (parachute == null && realChute == null)
                throw new ArgumentException ("Part is not a parachute");
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
            if (parachute == null || realChute != null)
                throw new InvalidOperationException ("Operation not defined for a RealChutes parachute");
        }

        void CheckRealChute ()
        {
            if (parachute != null || realChute == null)
                throw new InvalidOperationException ("Operation only defined for a RealChutes parachute");
        }

        /// <summary>
        /// The part object for this parachute.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Deploys the parachute. This has no effect if the parachute has already
        /// been deployed.
        /// </summary>
        [KRPCMethod]
        public void Deploy ()
        {
            if (parachute)
                parachute.Deploy ();
            else if (realChute.HasEvent ("Deploy chute"))
                realChute.TriggerEvent ("Deploy Chute");
        }

        /// <summary>
        /// Whether the parachute has been deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get {
                if (parachute)
                    return parachute.deploymentState != ModuleParachute.deploymentStates.STOWED;
                return realChute.Events.Any (x => x.Contains ("Cut"));
            }
        }

        /// <summary>
        /// Deploys the parachute. This has no effect if the parachute has already
        /// been armed or deployed. Only applicable to RealChutes parachutes.
        /// </summary>
        [KRPCMethod]
        public void Arm ()
        {
            CheckRealChute ();
            if (realChute.HasEvent ("Arm parachute"))
                realChute.TriggerEvent ("Arm parachute");
        }

        /// <summary>
        /// Whether the parachute has been armed or deployed. Only applicable to RealChutes parachutes.
        /// </summary>
        [KRPCProperty]
        public bool Armed {
            get {
                CheckRealChute ();
                return realChute.HasEvent ("Disarm parachute");
            }
        }

        /// <summary>
        /// The current state of the parachute.
        /// </summary>
        [KRPCProperty]
        public ParachuteState State {
            get {
                if (parachute)
                    return parachute.deploymentState.ToParachuteState ();
                if (Deployed)
                    return ParachuteState.Deployed;
                if (Armed)
                    return ParachuteState.Armed;
                if (realChute.Events.Any(x => x.Contains("Deploy")))
                    return ParachuteState.Stowed;
                return ParachuteState.Cut;
            }
        }

        /// <summary>
        /// The altitude at which the parachute will full deploy, in meters.
        /// Only applicable to stock parachutes.
        /// </summary>
        [KRPCProperty]
        public float DeployAltitude {
            get {
                CheckStock();
                return parachute.deployAltitude;
            }
            set {
                CheckStock();
                parachute.deployAltitude = value;
            }
        }

        /// <summary>
        /// The minimum pressure at which the parachute will semi-deploy, in atmospheres.
        /// Only applicable to stock parachutes.
        /// </summary>
        [KRPCProperty]
        public float DeployMinPressure {
            get {
                CheckStock ();
                return parachute.minAirPressureToOpen;
            }
            set {
                CheckStock ();
                parachute.minAirPressureToOpen = value;
            }
        }

        // TODO: add safe deployment information?
    }
}
