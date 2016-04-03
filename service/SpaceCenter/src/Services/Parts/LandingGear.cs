using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.LandingGear"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class LandingGear : Equatable<LandingGear>
    {
        readonly Part part;
        readonly ModuleWheels.ModuleWheelDeployment deployment;
        readonly ModuleWheels.ModuleWheelDamage damage;

        internal static bool Is (Part part)
        {
            return
                part.InternalPart.HasModule<ModuleWheels.ModuleWheelDeployment> () ||
                part.InternalPart.HasModule<ModuleWheels.ModuleWheelDamage> ();
        }

        internal LandingGear (Part part)
        {
            this.part = part;
            deployment = part.InternalPart.Module<ModuleWheels.ModuleWheelDeployment> ();
            damage = part.InternalPart.Module<ModuleWheels.ModuleWheelDamage> ();
            if (deployment == null || damage == null)
                throw new ArgumentException ("Part does not have a ModuleWheelDeployment and ModuleWheelDamage PartModules");
        }

        /// <summary>
        /// Check the landing gear are equal.
        /// </summary>
        public override bool Equals (LandingGear obj)
        {
            return part == obj.part && deployment == obj.deployment && damage == obj.damage;
        }

        /// <summary>
        /// Hash the landing gear.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ deployment.GetHashCode () ^ damage.GetHashCode ();
        }

        /// <summary>
        /// The part object for this landing gear.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Whether the landing gear is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return deployment == null; }
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
                throw new NotImplementedException ();
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
                throw new NotImplementedException ();
            }
        }
    }
}
