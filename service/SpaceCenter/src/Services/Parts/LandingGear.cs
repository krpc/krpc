using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Landing gear with wheels. Obtained by calling <see cref="Part.LandingGear"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class LandingGear : Equatable<LandingGear>
    {
        readonly ModuleWheels.ModuleWheelDeployment deployment;
        readonly ModuleWheels.ModuleWheelDamage damage;

        internal static bool Is (Part part)
        {
            //TODO: is WheelType.FREE correct? Landing gear are the only stock parts with this wheel type. Rover wheels are WheelType.MOTORIZED
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleWheelBase> () &&
            internalPart.Module<ModuleWheelBase> ().wheelType == WheelType.FREE;
        }

        internal LandingGear (Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not landing gear");
            Part = part;
            var internalPart = part.InternalPart;
            deployment = internalPart.Module<ModuleWheels.ModuleWheelDeployment> ();
            damage = internalPart.Module<ModuleWheels.ModuleWheelDamage> ();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LandingGear other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part == other.Part &&
            (deployment == other.deployment || deployment.Equals (other.deployment)) &&
            (damage == other.damage || damage.Equals (other.damage));
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            var hash = Part.GetHashCode ();
            if (deployment != null)
                hash ^= deployment.GetHashCode ();
            if (damage != null)
                hash ^= damage.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The part object for this landing gear.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the landing gear is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return deployment != null; }
        }

        /// <summary>
        /// Gets the current state of the landing gear.
        /// </summary>
        /// <remarks>
        /// Fixed landing gear are always deployed.
        /// </remarks>
        [KRPCProperty]
        public LandingGearState State {
            get {
                if (damage != null && damage.isDamaged)
                    return LandingGearState.Broken;
                if (deployment != null) {
                    if (deployment.stateString.Contains ("Deployed"))
                        return LandingGearState.Deployed;
                    else if (deployment.stateString.Contains ("Retracted"))
                        return LandingGearState.Retracted;
                    else if (deployment.stateString.Contains ("Deploying"))
                        return LandingGearState.Deploying;
                    else if (deployment.stateString.Contains ("Retracting"))
                        return LandingGearState.Retracting;
                    throw new InvalidOperationException ();
                }
                return LandingGearState.Deployed;
            }
        }

        /// <summary>
        /// Whether the landing gear is deployed.
        /// </summary>
        /// <remarks>
        /// Fixed landing gear are always deployed.
        /// Returns an error if you try to deploy fixed landing gear.
        /// </remarks>
        [KRPCProperty]
        public bool Deployed {
            get { return State == LandingGearState.Deployed; }
            set {
                if (deployment == null)
                    throw new InvalidOperationException ("Landing gear is not deployable");
                if (value) {
                    var extend = deployment.Events.FirstOrDefault (x => x.guiName == "Extend");
                    if (extend != null)
                        extend.Invoke ();
                } else {
                    var retract = deployment.Events.FirstOrDefault (x => x.guiName == "Retract");
                    if (retract != null)
                        retract.Invoke ();
                }
            }
        }
    }
}
