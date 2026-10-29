using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The game's main camera. Obtained from <see cref="KSA.Camera"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Camera : Equatable<Camera>, IGameObjectState
    {
        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Camera other)
        {
            return !ReferenceEquals (other, null);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return GameObjectState.Live; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return 0;
        }

        static IGameViewport Viewport {
            get { return Program.MainViewport ?? throw new InvalidOperationException ("The game has no camera"); }
        }

        /// <summary>
        /// The mode of the camera.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="CameraMode.IVA"/> while the camera is not focused on a vehicle
        /// with a crew seat sets <see cref="CameraMode.Orbit"/> instead, as the game does. A
        /// change of mode releases the active vehicle's held rotation and translation inputs.
        /// </remarks>
        [KRPCProperty]
        public CameraMode Mode {
            // The service enum lists the game's modes in the game's order
            get { return (CameraMode)Viewport.Mode; }
            set { Viewport.SetCameraMode ((global::KSA.CameraMode)value); }
        }

        /// <summary>
        /// The view of the orbit or map camera. Throws an exception in any other mode.
        /// </summary>
        static OrbitView View {
            get {
                var viewport = Viewport;
                switch (viewport.Mode) {
                case global::KSA.CameraMode.Orbit:
                    return viewport.OrbitController.View ?? throw new InvalidOperationException ("The camera has no focus");
                case global::KSA.CameraMode.Map:
                    return viewport.MapController.OrbitView;
                default:
                    throw new InvalidOperationException ("The camera is not in orbit or map mode");
                }
            }
        }

        /// <summary>
        /// The angle of the camera around the focused object, in degrees. Throws an exception
        /// in any mode other than orbit and map.
        /// </summary>
        [KRPCProperty]
        public float Azimuth {
            get { return (float)(View.Azimuth * (180 / Math.PI)); }
            set { View.Azimuth = value * (Math.PI / 180); }
        }

        /// <summary>
        /// The angle of the camera above the plane it orbits in, in degrees, between -90 and
        /// 90. Throws an exception in any mode other than orbit and map.
        /// </summary>
        [KRPCProperty]
        public float Elevation {
            get { return (float)(View.Elevation * (180 / Math.PI)); }
            set {
                if (!(value >= -90 && value <= 90))
                    throw new ArgumentOutOfRangeException (nameof (Elevation));
                View.Elevation = value * (Math.PI / 180);
            }
        }

        /// <summary>
        /// The distance from the camera to the focused object, in meters. Throws an exception
        /// in any mode other than orbit and map.
        /// </summary>
        /// <remarks>
        /// The camera moves smoothly to a new distance, and stays at least the focused object's
        /// radius from it in map mode, and half its radius in orbit mode.
        /// </remarks>
        [KRPCProperty]
        public float Distance {
            get {
                var view = View;
                var viewport = Viewport;
                if (viewport.Mode == global::KSA.CameraMode.Map)
                    return (float)viewport.MapController.Scope;
                // The orbit camera's distance is a multiple of the focused object's radius
                return (float)(view.DistancePower * Following.MeanRadius);
            }
            set {
                var view = View;
                if (!(value > 0))
                    throw new ArgumentOutOfRangeException (nameof (Distance));
                var viewport = Viewport;
                if (viewport.Mode == global::KSA.CameraMode.Map)
                    viewport.MapController.Scope = value;
                else
                    view.DistancePower = Math.Max (value / Following.MeanRadius, OrbitController.MIN_DISTANCE_POWER);
            }
        }

        /// <summary>
        /// The camera's vertical field of view, in degrees, between 15 and 120.
        /// </summary>
        [KRPCProperty]
        public float FieldOfView {
            get { return (float)(Viewport.GetCamera ().GetFieldOfView () * (180 / Math.PI)); }
            set {
                if (!(value >= global::KSA.Camera.MINIMUM_FOV && value <= global::KSA.Camera.MAXIMUM_FOV))
                    throw new ArgumentOutOfRangeException (nameof (FieldOfView));
                Viewport.GetCamera ().SetFieldOfView ((float)value);
            }
        }

        static IFollowable Following {
            get { return Viewport.GetCamera ().Following ?? throw new InvalidOperationException ("The camera has no focus"); }
        }

        static void Follow (IFollowable target)
        {
            Viewport.GetCamera ().SetFollow (target, tidalLocking: false, changeControl: false, alert: false);
        }

        /// <summary>
        /// Returns the vehicle the camera is focused on, or <c>null</c> if it is focused on
        /// something else, and sets the focus to a vehicle.
        /// </summary>
        /// <remarks>
        /// Setting the focus leaves the active vehicle as it is.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public Vehicle FocusedVehicle {
            get {
                var following = Viewport.GetCamera ().Following;
                return following is global::KSA.Vehicle vehicle && following is not KittenEva ? new Vehicle (vehicle) : null;
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (FocusedVehicle));
                Follow (value.InternalVehicle);
            }
        }

        /// <summary>
        /// Returns the body the camera is focused on, or <c>null</c> if it is focused on
        /// something else, and sets the focus to a body.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Body FocusedBody {
            get { return Viewport.GetCamera ().Following is IParentBody body ? new Body (body) : null; }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (FocusedBody));
                Follow (value.InternalBody as IFollowable ?? throw new InvalidOperationException ("The camera cannot focus on the body"));
            }
        }

        /// <summary>
        /// Returns the kitten on EVA the camera is focused on, or <c>null</c> if it is focused
        /// on something else, and sets the focus to a kitten on EVA.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the kitten is not on EVA.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public Kitten FocusedKitten {
            get { return Viewport.GetCamera ().Following is KittenEva eva ? new Kitten (eva.Id) : null; }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (FocusedKitten));
                Follow (value.Eva ?? throw new InvalidOperationException ("The kitten is not on EVA"));
            }
        }
    }
}
