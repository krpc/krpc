using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A launch clamp. Obtained by calling <see cref="Part.LaunchClamp"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class LaunchClamp : Equatable<LaunchClamp>, IGameObjectState
    {
        ModuleRef launchClampRef;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<global::LaunchClamp> ();
        }

        internal LaunchClamp (Part part)
        {
            Part = part;
            launchClampRef = ModuleRef.ForType<global::LaunchClamp> (part.InternalPart);
            if (launchClampRef.Find (part.InternalPart) == null)
                throw new ArgumentException ("Part is not a launch clamp");
        }

        global::LaunchClamp InternalLaunchClamp {
            get { return (global::LaunchClamp)launchClampRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the launch clamp: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return launchClampRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LaunchClamp other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && launchClampRef == other.launchClampRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ launchClampRef.GetHashCode ();
        }

        /// <summary>
        /// The part object for this launch clamp.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Releases the docking clamp. Has no effect if the clamp has already been released.
        /// </summary>
        [KRPCMethod]
        public void Release ()
        {
            InternalLaunchClamp.Release ();
        }
    }
}
