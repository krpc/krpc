using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Smart RCS window, which holds a velocity with RCS.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class SmartRCS : DisplayModule
    {
        static Field<object, int> target;
        static Field<object, bool> autoDisableSmartRCS;
        static Action<object> engage;
        static Field<object, bool> rcsThrottle;
        static Field<object, bool> rcsForRotation;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleSmartRcs");
            target = type.Field<object, int> ("target");
            autoDisableSmartRCS = type.Field<object, bool> ("autoDisableSmartRCS");
            engage = type.Method<Action<object>> ("Engage");
            var controller = Bindings.Type ("MuMech.MechJebModuleRCSController");
            rcsThrottle = controller.Field<object, bool> ("rcsThrottle");
            rcsForRotation = controller.Field<object, bool> ("rcsForRotation");
        }

        internal SmartRCS () : base ("MechJebModuleSmartRcs")
        {
        }

        /// <summary>
        /// Whether Smart RCS turns off when an autopilot takes control.
        /// </summary>
        [KRPCProperty]
        public bool AutoDisableSmartRCS {
            get { return autoDisableSmartRCS.Get (Instance); }
            set { autoDisableSmartRCS.Set (Instance, value); }
        }

        /// <summary>
        /// The mode. Throws an exception if set to <see cref="SmartRCSMode.ZeroVelocity"/>,
        /// which MechJeb 2.15 removed.
        /// </summary>
        [KRPCProperty]
        public SmartRCSMode Mode {
            get { return (SmartRCSMode)target.Get (Instance); }
            set {
                if (value == SmartRCSMode.ZeroVelocity)
                    throw new MJServiceException ("MechJeb has no ZeroVelocity mode");
                target.Set (Instance, (int)value);
                engage (Instance);
            }
        }

        /// <summary>
        /// Whether to throttle the RCS when the engines are off.
        /// </summary>
        [KRPCProperty]
        public bool RCSThrottle {
            get { return rcsThrottle.Get (RCSModule); }
            set { rcsThrottle.Set (RCSModule, value); }
        }

        /// <summary>
        /// Whether to use the RCS for rotation.
        /// </summary>
        [KRPCProperty]
        public bool RCSForRotation {
            get { return rcsForRotation.Get (RCSModule); }
            set { rcsForRotation.Set (RCSModule, value); }
        }

        static object RCSModule {
            get { return MechJeb.Module ("MechJebModuleRCSController"); }
        }
    }

    /// <summary>
    /// The mode of Smart RCS. See <see cref="SmartRCS.Mode"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum SmartRCSMode
    {
        /// <summary>
        /// Off.
        /// </summary>
        Off,

        /// <summary>
        /// Hold zero velocity relative to the target.
        /// </summary>
        ZeroRelativeVelocity,

        /// <summary>
        /// Hold zero velocity. MechJeb 2.15 removed this mode.
        /// </summary>
        ZeroVelocity
    }
}
