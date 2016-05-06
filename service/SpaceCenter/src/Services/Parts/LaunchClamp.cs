using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.LaunchClamp"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class LaunchClamp : Equatable<LaunchClamp>
    {
        readonly Part part;
        readonly global::LaunchClamp launchClamp;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<global::LaunchClamp> ();
        }

        internal LaunchClamp (Part part)
        {
            this.part = part;
            launchClamp = part.InternalPart.Module<global::LaunchClamp> ();
            if (launchClamp == null)
                throw new ArgumentException ("Part is not a launch clamp");
        }

        /// <summary>
        /// Check if the launch clamps are equal.
        /// </summary>
        public override bool Equals (LaunchClamp obj)
        {
            return part == obj.part && launchClamp == obj.launchClamp;
        }

        /// <summary>
        /// Hash the launch clamp.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ launchClamp.GetHashCode ();
        }

        /// <summary>
        /// The part object for this launch clamp.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Releases the docking clamp. Has no effect if the clamp has already been released.
        /// </summary>
        [KRPCMethod]
        public void Release ()
        {
            launchClamp.Release ();
        }
    }
}
