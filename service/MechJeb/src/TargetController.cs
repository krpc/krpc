using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
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
        static Value<bool> normalTargetExists;
        static Value<bool> positionTargetExists;
        static Value<bool> canAlign;
        static Value<global::Orbit> targetOrbit;
        static Value<Vector3d> position;
        static Value<float> distance;
        static Value<Vector3d> relativeVelocity;
        static Value<Vector3d> relativePosition;
        static Value<Vector3d> dockingAxis;
        static Value<double> targetLongitude;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleTargetController");
            setPositionTarget = Reflection.Method<Action<object, global::CelestialBody, double, double>> (type, "SetPositionTarget");
            getPositionTargetString = Reflection.Method<Func<object, string>> (type, "GetPositionTargetString");
            getPositionTargetPosition = Reflection.Method<Func<object, Vector3d>> (type, "GetPositionTargetPosition");
            setDirectionTarget = Reflection.Method<Action<object, string>> (type, "SetDirectionTarget");
            pickPositionTargetOnMap = Reflection.Method<Action<object>> (type, "PickPositionTargetOnMap");
            updateDirectionTarget = Reflection.Method<Action<object, Vector3d>> (type, "UpdateDirectionTarget");
            onFixedUpdate = Reflection.Method<Action<object>> (type, "OnFixedUpdate");
            normalTargetExists = Reflection.Field<bool> (type, "NormalTargetExists");
            positionTargetExists = Reflection.Field<bool> (type, "PositionTargetExists");
            canAlign = Reflection.Field<bool> (type, "CanAlign");
            targetOrbit = Reflection.Field<global::Orbit> (type, "TargetOrbit");
            position = Reflection.Field<Vector3d> (type, "Position");
            distance = Reflection.Field<float> (type, "Distance");
            relativeVelocity = Reflection.Field<Vector3d> (type, "RelativeVelocity");
            relativePosition = Reflection.Field<Vector3d> (type, "RelativePosition");
            dockingAxis = Reflection.Field<Vector3d> (type, "DockingAxis");
            // An angle, which converts to its value in degrees
            targetLongitude = Reflection.Field<double> (type, "targetLongitude");
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
            get { return targetOrbit.Get (Instance); }
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
            get { return normalTargetExists.Get (Instance); }
        }

        /// <summary>
        /// Whether there is a target that is a position or a vessel.
        /// </summary>
        [KRPCProperty]
        public bool PositionTargetExists {
            get { return positionTargetExists.Get (Instance); }
        }

        /// <summary>
        /// Whether the vessel can align with the target, which it can with a docking port.
        /// </summary>
        [KRPCProperty]
        public bool CanAlign {
            get { return canAlign.Get (Instance); }
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
            get { return position.Get (Instance).ToTuple (); }
        }

        /// <summary>
        /// The distance to the target, in meters.
        /// </summary>
        [KRPCProperty]
        public float Distance {
            get { return distance.Get (Instance); }
        }

        /// <summary>
        /// The world space velocity of the vessel relative to the target, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public Tuple3 RelativeVelocity {
            get { return relativeVelocity.Get (Instance).ToTuple (); }
        }

        /// <summary>
        /// The world space position of the vessel relative to the target.
        /// </summary>
        [KRPCProperty]
        public Tuple3 RelativePosition {
            get { return relativePosition.Get (Instance).ToTuple (); }
        }

        /// <summary>
        /// The world space docking axis of the target.
        /// </summary>
        [KRPCProperty]
        public Tuple3 DockingAxis {
            get { return dockingAxis.Get (Instance).ToTuple (); }
        }
    }
}
