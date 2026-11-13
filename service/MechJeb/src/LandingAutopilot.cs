using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The landing autopilot, which lands at a target or wherever the vessel comes down.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class LandingAutopilot : AutopilotModule
    {
        static Field<object, double> touchdownSpeed;
        static Field<object, bool> deployGears;
        static Field<object, int> limitGearsStage;
        static Field<object, bool> deployChutes;
        static Field<object, int> limitChutesStage;
        static Field<object, bool> rcsAdjustment;
        static Action<object, object> landAtPositionTarget;
        static Action<object, object> landUntargeted;
        static Action<object> stopLanding;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleLandingAutopilot");
            touchdownSpeed = Bindings.EditableDouble (type, "TouchdownSpeed");
            deployGears = type.Field<object, bool> ("DeployGears");
            limitGearsStage = Bindings.EditableInt (type, "LimitGearsStage");
            deployChutes = type.Field<object, bool> ("DeployChutes");
            limitChutesStage = Bindings.EditableInt (type, "LimitChutesStage");
            rcsAdjustment = type.Field<object, bool> ("RCSAdjustment");
            landAtPositionTarget = type.Method<Action<object, object>> ("LandAtPositionTarget");
            landUntargeted = type.Method<Action<object, object>> ("LandUntargeted");
            stopLanding = type.Method<Action<object>> ("StopLanding");
        }

        internal LandingAutopilot () : base ("MechJebModuleLandingAutopilot")
        {
        }

        /// <summary>
        /// The speed at touchdown, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double TouchdownSpeed {
            get { return touchdownSpeed.Get (Instance); }
            set { touchdownSpeed.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot deploys the landing gear.
        /// </summary>
        [KRPCProperty]
        public bool DeployGears {
            get { return deployGears.Get (Instance); }
            set { deployGears.Set (Instance, value); }
        }

        /// <summary>
        /// The last stage in which the autopilot deploys the landing gear.
        /// </summary>
        [KRPCProperty]
        public int LimitGearsStage {
            get { return limitGearsStage.Get (Instance); }
            set { limitGearsStage.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot deploys the parachutes.
        /// </summary>
        [KRPCProperty]
        public bool DeployChutes {
            get { return deployChutes.Get (Instance); }
            set { deployChutes.Set (Instance, value); }
        }

        /// <summary>
        /// The last stage in which the autopilot deploys the parachutes.
        /// </summary>
        [KRPCProperty]
        public int LimitChutesStage {
            get { return limitChutesStage.Get (Instance); }
            set { limitChutesStage.Set (Instance, value); }
        }

        /// <summary>
        /// Whether the autopilot corrects its course with RCS.
        /// </summary>
        [KRPCProperty]
        public bool RcsAdjustment {
            get { return rcsAdjustment.Get (Instance); }
            set { rcsAdjustment.Set (Instance, value); }
        }

        /// <summary>
        /// Land at the position target. See <see cref="TargetController.SetPositionTarget"/>.
        /// </summary>
        [KRPCMethod]
        public void LandAtPositionTarget ()
        {
            landAtPositionTarget (Instance, this);
        }

        /// <summary>
        /// Land wherever the vessel comes down.
        /// </summary>
        [KRPCMethod]
        public void LandUntargeted ()
        {
            landUntargeted (Instance, this);
        }

        /// <summary>
        /// Stop the landing.
        /// </summary>
        [KRPCMethod]
        public void StopLanding ()
        {
            stopLanding (Instance);
        }
    }
}
