using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Landing Guidance window, which lands at a target or wherever the vessel comes down.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class LandingGuidance : AutopilotModule
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
        static Action<object, global::CelestialBody, double, double> setPositionTarget;
        static Func<object, string> getPositionTargetString;
        static Func<object, Vector3d> getPositionTargetPosition;
        static Action<object> pickPositionTargetOnMap;

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
            var target = Bindings.Type ("MuMech.MechJebModuleTargetController");
            setPositionTarget = target.Method<Action<object, global::CelestialBody, double, double>> ("SetPositionTarget");
            getPositionTargetString = target.Method<Func<object, string>> ("GetPositionTargetString");
            getPositionTargetPosition = target.Method<Func<object, Vector3d>> ("GetPositionTargetPosition");
            pickPositionTargetOnMap = target.Method<Action<object>> ("PickPositionTargetOnMap");
        }

        internal LandingGuidance () : base ("MechJebModuleLandingAutopilot")
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
        public bool RCSAdjustment {
            get { return rcsAdjustment.Get (Instance); }
            set { rcsAdjustment.Set (Instance, value); }
        }

        /// <summary>
        /// Set the landing target to a position on the surface of a body.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        [KRPCMethod]
        public void SetTarget (SpaceCenter.Services.CelestialBody body, double latitude, double longitude)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            setPositionTarget (TargetModule, body.InternalBody, latitude, longitude);
        }

        /// <summary>
        /// The landing target as text.
        /// </summary>
        [KRPCMethod]
        public string GetTargetString ()
        {
            return getPositionTargetString (TargetModule);
        }

        /// <summary>
        /// The world space position of the landing target. Throws an exception if there is no
        /// landing target.
        /// </summary>
        [KRPCMethod]
        public Tuple3 GetTargetPosition ()
        {
            CheckTarget ();
            return getPositionTargetPosition (TargetModule).ToTuple ();
        }

        /// <summary>
        /// Let the player pick the landing target on the map.
        /// </summary>
        [KRPCMethod]
        public void PickTargetOnMap ()
        {
            pickPositionTargetOnMap (TargetModule);
        }

        static object TargetModule {
            get { return MechJeb.TargetController.Instance; }
        }

        /// <summary>
        /// Whether to warp time during the landing.
        /// </summary>
        /// <remarks>The same setting as <see cref="ManeuverPlanner.Autowarp"/>.</remarks>
        [KRPCProperty]
        public bool Autowarp {
            get { return NodeExecutor.Autowarp; }
            set { NodeExecutor.Autowarp = value; }
        }

        /// <summary>
        /// Land at the target. See <see cref="SetTarget"/>.
        /// Throws an exception if there is no target, or the vessel is landed or splashed down.
        /// </summary>
        [KRPCMethod]
        public void LandAtTarget ()
        {
            CheckTarget ();
            CheckNotLanded ();
            landAtPositionTarget (Instance, this);
        }

        /// <summary>
        /// Land wherever the vessel comes down. Throws an exception if the vessel is landed or
        /// splashed down.
        /// </summary>
        [KRPCMethod]
        public void LandSomewhere ()
        {
            CheckNotLanded ();
            landUntargeted (Instance, this);
        }

        static void CheckTarget ()
        {
            var targetController = MechJeb.TargetController;
            targetController.Update ();
            if (!targetController.PositionTargetExists)
                throw new InvalidOperationException ("There is no landing target");
        }

        static void CheckNotLanded ()
        {
            if (FlightGlobals.ActiveVessel.LandedOrSplashed)
                throw new InvalidOperationException ("The vessel is landed");
        }

        /// <summary>
        /// Stop the landing.
        /// </summary>
        [KRPCMethod]
        public void AbortAutoland ()
        {
            stopLanding (Instance);
        }
    }
}
