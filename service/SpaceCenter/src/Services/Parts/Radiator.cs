using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A radiator. Obtained by calling <see cref="Part.Radiator"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Radiator : Equatable<Radiator>
    {
        ModuleActiveRadiator activeRadiator {
            get { return Part.InternalPart.Module<ModuleActiveRadiator> (); }
        }

        ModuleDeployableRadiator deployableRadiator {
            get { return Part.InternalPart.Module<ModuleDeployableRadiator> (); }
        }

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleActiveRadiator> () ||
            internalPart.HasModule<ModuleDeployableRadiator> ();
        }

        internal Radiator (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            if (internalPart.Module<ModuleActiveRadiator> () == null && internalPart.Module<ModuleDeployableRadiator> () == null)
                throw new ArgumentException ("Part is not a radiator");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Radiator other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part == other.Part;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode ();
        }

        /// <summary>
        /// The part object for this radiator.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the radiator is deployable.
        /// </summary>
        [KRPCProperty]
        public bool Deployable {
            get { return deployableRadiator != null; }
        }

        /// <summary>
        /// For a deployable radiator, <c>true</c> if the radiator is extended.
        /// If the radiator is not deployable, this is always <c>true</c>.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get {
                return
                !Deployable ||
                deployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED ||
                deployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDING;
            }
            set {
                if (!Deployable)
                    throw new InvalidOperationException ("Radiator is not deployable");
                if (value)
                    deployableRadiator.Extend ();
                else
                    deployableRadiator.Retract ();
            }
        }

        /// <summary>
        /// The current state of the radiator.
        /// </summary>
        /// <remarks>
        /// A fixed radiator is always <see cref="RadiatorState.Extended" />.
        /// </remarks>
        [KRPCProperty]
        public RadiatorState State {
            get {
                if (!Deployable)
                    return RadiatorState.Extended;
                return deployableRadiator.deployState.ToRadiatorState ();
            }
        }
    }
}
