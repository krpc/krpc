using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A launch clamp. Obtained by calling <see cref="Part.LaunchClamp"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class LaunchClamp : Equatable<LaunchClamp>
    {
        global::LaunchClamp launchClamp {
            get { return Part.InternalPart.Module<global::LaunchClamp> (); }
        }

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<global::LaunchClamp> ();
        }

        internal LaunchClamp (Part part)
        {
            Part = part;
            if (part.InternalPart.Module<global::LaunchClamp> () == null)
                throw new ArgumentException ("Part is not a launch clamp");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LaunchClamp other)
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

        /// <summary>
        /// The part object for this launch clamp.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

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
