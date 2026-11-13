using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The target controller, which holds MechJeb's target.
    /// </summary>
    /// <remarks>
    /// Positions and directions are in world space, the coordinate system of the game's
    /// physics.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class TargetController : ComputerModule
    {
        static Action<object, global::CelestialBody, double, double> setPositionTarget;
        static Func<object, string> getPositionTargetString;
        static Func<object, Vector3d> getPositionTargetPosition;
        static Action<object, string> setDirectionTarget;
        static Action<object> pickPositionTargetOnMap;
        static Action<object, Vector3d> updateDirectionTarget;
        static Action<object> onFixedUpdate;
        static Func<object, bool> normalTargetExists;
        static Func<object, bool> positionTargetExists;
        static Func<object, bool> canAlign;
        static Func<object, global::Orbit> targetOrbit;
        static Func<object, Vector3d> position;
        static Func<object, float> distance;
        static Func<object, Vector3d> relativeVelocity;
        static Func<object, Vector3d> relativePosition;
        static Func<object, Vector3d> dockingAxis;
        static Field<object, double> targetLongitude;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleTargetController");
            setPositionTarget = type.Method<Action<object, global::CelestialBody, double, double>> ("SetPositionTarget");
            getPositionTargetString = type.Method<Func<object, string>> ("GetPositionTargetString");
            getPositionTargetPosition = type.Method<Func<object, Vector3d>> ("GetPositionTargetPosition");
            setDirectionTarget = type.Method<Action<object, string>> ("SetDirectionTarget");
            pickPositionTargetOnMap = type.Method<Action<object>> ("PickPositionTargetOnMap");
            updateDirectionTarget = type.Method<Action<object, Vector3d>> ("UpdateDirectionTarget");
            onFixedUpdate = type.Method<Action<object>> ("OnFixedUpdate");
            normalTargetExists = type.Getter<object, bool> ("NormalTargetExists");
            positionTargetExists = type.Getter<object, bool> ("PositionTargetExists");
            canAlign = type.Getter<object, bool> ("CanAlign");
            targetOrbit = type.Getter<object, global::Orbit> ("TargetOrbit");
            position = type.Getter<object, Vector3d> ("Position");
            distance = type.Getter<object, float> ("Distance");
            relativeVelocity = type.Getter<object, Vector3d> ("RelativeVelocity");
            relativePosition = type.Getter<object, Vector3d> ("RelativePosition");
            dockingAxis = type.Getter<object, Vector3d> ("DockingAxis");
            // An angle, which converts to its value in degrees
            targetLongitude = type.Field<object, double> ("targetLongitude");
        }

        internal TargetController () : base ("MechJebModuleTargetController")
        {
        }

        /// <summary>
        /// Update MechJeb's target from the game's. MechJeb does this each physics tick, so a
        /// target set through the SpaceCenter service is not seen until the next one.
        /// </summary>
        internal void Update ()
        {
            onFixedUpdate (Instance);
        }

        internal global::Orbit InternalTargetOrbit {
            get { return targetOrbit (Instance); }
        }

        /// <summary>
        /// The longitude of the position target, in degrees. The maneuver planner's longitude
        /// operations read their new longitude from it.
        /// </summary>
        internal double TargetLongitude {
            get { return targetLongitude.Get (Instance); }
            set { targetLongitude.Set (Instance, value); }
        }

        /// <summary>
        /// Set the target to a position on the surface of a body.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        [KRPCMethod]
        public void SetPositionTarget (SpaceCenter.Services.CelestialBody body, double latitude, double longitude)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            setPositionTarget (Instance, body.InternalBody, latitude, longitude);
        }

        /// <summary>
        /// The position target as text.
        /// </summary>
        [KRPCMethod]
        public string GetPositionTargetString ()
        {
            return getPositionTargetString (Instance);
        }

        /// <summary>
        /// The world space position of the position target.
        /// </summary>
        [KRPCMethod]
        public Tuple3 GetPositionTargetPosition ()
        {
            return getPositionTargetPosition (Instance).ToTuple ();
        }

        /// <summary>
        /// Set the target to a direction.
        /// </summary>
        /// <param name="name">The name of the target.</param>
        /// <remarks>Set the direction with <see cref="UpdateDirectionTarget"/>.</remarks>
        [KRPCMethod]
        public void SetDirectionTarget (string name)
        {
            setDirectionTarget (Instance, name);
        }

        /// <summary>
        /// Let the player pick the position target on the map.
        /// </summary>
        [KRPCMethod]
        public void PickPositionTargetOnMap ()
        {
            pickPositionTargetOnMap (Instance);
        }

        /// <summary>
        /// Set the direction of the direction target.
        /// </summary>
        /// <param name="direction">The world space direction.</param>
        [KRPCMethod]
        public void UpdateDirectionTarget (Tuple3 direction)
        {
            updateDirectionTarget (Instance, direction.ToVector ());
        }

        /// <summary>
        /// Whether there is a target that is a vessel, a body or a direction target.
        /// </summary>
        [KRPCProperty]
        public bool NormalTargetExists {
            get { return normalTargetExists (Instance); }
        }

        /// <summary>
        /// Whether there is a target that is a position or a vessel.
        /// </summary>
        [KRPCProperty]
        public bool PositionTargetExists {
            get { return positionTargetExists (Instance); }
        }

        /// <summary>
        /// Whether the vessel can align with the target, which it can with a docking port.
        /// </summary>
        [KRPCProperty]
        public bool CanAlign {
            get { return canAlign (Instance); }
        }

        /// <summary>
        /// The orbit of the target.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Orbit TargetOrbit {
            get { return new SpaceCenter.Services.Orbit (InternalTargetOrbit); }
        }

        /// <summary>
        /// The world space position of the target.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Position {
            get { return position (Instance).ToTuple (); }
        }

        /// <summary>
        /// The distance to the target, in meters.
        /// </summary>
        [KRPCProperty]
        public float Distance {
            get { return distance (Instance); }
        }

        /// <summary>
        /// The world space velocity of the vessel relative to the target, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public Tuple3 RelativeVelocity {
            get { return relativeVelocity (Instance).ToTuple (); }
        }

        /// <summary>
        /// The world space position of the vessel relative to the target.
        /// </summary>
        [KRPCProperty]
        public Tuple3 RelativePosition {
            get { return relativePosition (Instance).ToTuple (); }
        }

        /// <summary>
        /// The world space docking axis of the target.
        /// </summary>
        [KRPCProperty]
        public Tuple3 DockingAxis {
            get { return dockingAxis (Instance).ToTuple (); }
        }
    }
}
