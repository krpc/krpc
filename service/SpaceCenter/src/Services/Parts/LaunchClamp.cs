using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

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

        internal LaunchClamp (Part part)
        {
            this.part = part;
            launchClamp = part.InternalPart.Module<global::LaunchClamp> ();
            if (launchClamp == null)
                throw new ArgumentException ("Part does not have a LaunchClamp PartModule");
        }

        /// <summary>
        /// Check if the launch clamps are equal.
        /// </summary>
        public override bool Equals (LaunchClamp obj)
        {
            return part == obj.part;
        }

        /// <summary>
        /// Hash the launch clamp.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
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
