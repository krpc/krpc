using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Attitude Adjustment window, which selects the attitude controller that the
    /// autopilots steer with.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AttitudeAdjustment : ComputerModule
    {
        static Field<object, bool> rcsAuto;
        static Field<object, int> activeController;
        static Action<object, int> setActiveController;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleAttitudeController");
            rcsAuto = type.Field<object, bool> ("RCS_auto");
            activeController = type.Field<object, int> ("activeController");
            setActiveController = type.Method<Action<object, int>> ("SetActiveController");
        }

        internal AttitudeAdjustment () : base ("MechJebModuleAttitudeController")
        {
        }

        /// <summary>
        /// Whether the attitude controller turns RCS on for a large attitude error, and off once
        /// the vessel settles.
        /// </summary>
        [KRPCProperty]
        public bool RCSAuto {
            get { return rcsAuto.Get (Instance); }
            set { rcsAuto.Set (Instance, value); }
        }

        /// <summary>
        /// The attitude controller.
        /// </summary>
        [KRPCProperty]
        public AttitudeController Controller {
            get { return (AttitudeController)activeController.Get (Instance); }
            set { setActiveController (Instance, (int)value); }
        }
    }

    /// <summary>
    /// An attitude controller of MechJeb. See <see cref="AttitudeAdjustment.Controller"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum AttitudeController
    {
        /// <summary>
        /// MechJeb's original attitude controller.
        /// </summary>
        MJ,

        /// <summary>
        /// The attitude controller of kOS.
        /// </summary>
        Kos,

        /// <summary>
        /// A hybrid of the MechJeb and kOS controllers.
        /// </summary>
        Hybrid,

        /// <summary>
        /// MechJeb's default controller.
        /// </summary>
        Better,

        /// <summary>
        /// A linear-quadratic regulator.
        /// </summary>
        LQR
    }
}
