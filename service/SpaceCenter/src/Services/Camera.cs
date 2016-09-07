using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Controls the game's camera.
    /// Obtained by calling <see cref="SpaceCenter.Camera"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Camera : Equatable<Camera>
    {
        /// <summary>
        /// Create a camera object.
        /// </summary>
        internal Camera ()
        {
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Camera other)
        {
            return !ReferenceEquals (other, null);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return 0;
        }

        /// <summary>
        /// The current mode of the camera.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public CameraMode Mode {
            get {
                if (MapView.MapIsEnabled)
                    return CameraMode.Map;
                var mode = CameraManager.Instance.currentCameraMode;
                if (mode == CameraManager.CameraMode.Flight) {
                    return FlightCamera.fetch.mode.ToCameraMode ();
                } else if (mode == CameraManager.CameraMode.IVA)
                    return CameraMode.IVA;
                throw new InvalidOperationException ("Unknown camera mode " + CameraManager.Instance.currentCameraMode);
            }
            set {
                if (value == CameraMode.Map && !MapView.MapIsEnabled)
                    MapView.EnterMapView ();
                else if (value != CameraMode.Map && MapView.MapIsEnabled)
                    MapView.ExitMapView ();
                else {
                    switch (value) {
                    case CameraMode.Automatic:
                        CameraManager.Instance.SetCameraFlight ();
                        FlightCamera.SetMode (FlightCamera.Modes.AUTO);
                        break;
                    case CameraMode.Free:
                        CameraManager.Instance.SetCameraFlight ();
                        FlightCamera.SetMode (FlightCamera.Modes.FREE);
                        break;
                    case CameraMode.Chase:
                        CameraManager.Instance.SetCameraFlight ();
                        FlightCamera.SetMode (FlightCamera.Modes.CHASE);
                        break;
                    case CameraMode.Locked:
                        CameraManager.Instance.SetCameraFlight ();
                        FlightCamera.SetMode (FlightCamera.Modes.LOCKED);
                        break;
                    case CameraMode.Orbital:
                        CameraManager.Instance.SetCameraFlight ();
                        FlightCamera.SetMode (FlightCamera.Modes.ORBITAL);
                        break;
                    case CameraMode.IVA:
                        CameraManager.Instance.SetCameraIVA ();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The pitch of the camera, in degrees.
        /// A value between <see cref="MinPitch"/> and <see cref="MaxPitch"/>
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.BadPractice", "DoNotForgetNotImplementedMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float Pitch {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.getPitch ();
                case CameraMode.IVA:
                    throw new NotImplementedException ();
                default:
                    return FlightCamera.fetch.getPitch ();
                }
            }
            set {
                switch (Mode) {
                case CameraMode.Map:
                    {
                        var camera = PlanetariumCamera.fetch;
                        camera.camPitch = GeometryExtensions.ToRadians (value).Clamp (camera.minPitch, camera.maxPitch);
                        break;
                    }
                case CameraMode.IVA:
                    throw new NotImplementedException ();
                default:
                    {
                        var camera = FlightCamera.fetch;
                        camera.camPitch = GeometryExtensions.ToRadians (value).Clamp (camera.minPitch, camera.maxPitch);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The heading of the camera, in degrees.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.BadPractice", "DoNotForgetNotImplementedMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float Heading {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.getYaw ();
                case CameraMode.IVA:
                    throw new NotImplementedException ();
                default:
                    return FlightCamera.fetch.getYaw ();
                }
            }
            set {
                switch (Mode) {
                case CameraMode.Map:
                    PlanetariumCamera.fetch.camHdg = GeometryExtensions.ToRadians (value);
                    break;
                case CameraMode.IVA:
                    throw new NotImplementedException ();
                default:
                    FlightCamera.fetch.camHdg = GeometryExtensions.ToRadians (value);
                    break;
                }
            }
        }

        /// <summary>
        /// The distance from the camera to the subject, in meters.
        /// A value between <see cref="MinDistance"/> and <see cref="MaxDistance"/>.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.BadPractice", "DoNotForgetNotImplementedMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float Distance {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.Distance * ScaledSpace.ScaleFactor;
                case CameraMode.IVA:
                    throw new NotImplementedException ();
                default:
                    return FlightCamera.fetch.Distance;
                }
            }
            set {
                switch (Mode) {
                case CameraMode.Map:
                    {
                        var camera = PlanetariumCamera.fetch;
                        camera.SetDistance (value.Clamp (camera.minDistance, camera.maxDistance) / ScaledSpace.ScaleFactor);
                        break;
                    }
                case CameraMode.IVA:
                    throw new NotImplementedException ();
                default:
                    {
                        var camera = FlightCamera.fetch;
                        camera.SetDistance (value.Clamp (camera.minDistance, camera.maxDistance));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The minimum pitch of the camera.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float MinPitch {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return GeometryExtensions.ToDegrees (PlanetariumCamera.fetch.minPitch);
                case CameraMode.IVA:
                    return InternalCamera.Instance.minPitch;
                default:
                    return GeometryExtensions.ToDegrees (FlightCamera.fetch.minPitch);
                }
            }
        }

        /// <summary>
        /// The maximum pitch of the camera.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float MaxPitch {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return GeometryExtensions.ToDegrees (PlanetariumCamera.fetch.maxPitch);
                case CameraMode.IVA:
                    return InternalCamera.Instance.maxPitch;
                default:
                    return GeometryExtensions.ToDegrees (FlightCamera.fetch.maxPitch);
                }
            }
        }

        /// <summary>
        /// Minimum distance from the camera to the subject, in meters.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float MinDistance {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.minDistance * ScaledSpace.ScaleFactor;
                case CameraMode.IVA:
                    return InternalCamera.Instance.maxZoom;
                default:
                    return FlightCamera.fetch.minDistance;
                }
            }
        }

        /// <summary>
        /// Maximum distance from the camera to the subject, in meters.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public float MaxDistance {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.maxDistance * ScaledSpace.ScaleFactor;
                case CameraMode.IVA:
                    return InternalCamera.Instance.minZoom;
                default:
                    return FlightCamera.fetch.maxDistance;
                }
            }
        }

        /// <summary>
        /// Default distance from the camera to the subject, in meters.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.BadPractice", "DoNotForgetNotImplementedMethodsRule")]
        public float DefaultDistance {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.startDistance * ScaledSpace.ScaleFactor;
                case CameraMode.IVA:
                    throw new NotImplementedException ();
                default:
                    return FlightCamera.fetch.startDistance;
                }
            }
        }

        /// <summary>
        /// In map mode, the celestial body that the camera is focussed on.
        /// Returns <c>null</c> if the camera is not focussed on a celestial body.
        /// Returns an error is the camera is not in map mode.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public CelestialBody FocussedBody {
            get {
                CheckCameraFocus ();
                var body = PlanetariumCamera.fetch.target.celestialBody;
                return body == null ? null : new CelestialBody (body);
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException ("FocussedBody");
                CheckCameraFocus ();
                PlanetariumCamera.fetch.SetTarget (value.InternalBody);
            }
        }

        /// <summary>
        /// In map mode, the vessel that the camera is focussed on.
        /// Returns <c>null</c> if the camera is not focussed on a vessel.
        /// Returns an error is the camera is not in map mode.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public Vessel FocussedVessel {
            get {
                CheckCameraFocus ();
                var vessel = PlanetariumCamera.fetch.target.vessel;
                return vessel == null ? null : new Vessel (vessel);
            }
            set {
                CheckCameraFocus ();
                var mapObject = PlanetariumCamera.fetch.targets.Single (x => x.vessel == value.InternalVessel);
                PlanetariumCamera.fetch.SetTarget (mapObject);
            }
        }

        /// <summary>
        /// In map mode, the maneuver node that the camera is focussed on.
        /// Returns <c>null</c> if the camera is not focussed on a maneuver node.
        /// Returns an error is the camera is not in map mode.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public Node FocussedNode {
            get {
                CheckCameraFocus ();
                var vessel = PlanetariumCamera.fetch.target.vessel;
                var node = PlanetariumCamera.fetch.target.maneuverNode;
                return (vessel == null || node == null) ? null : new Node (vessel, node);
            }
            set {
                CheckCameraFocus ();
                var mapObject = PlanetariumCamera.fetch.targets.Single (x => x.maneuverNode == value.InternalNode);
                PlanetariumCamera.fetch.SetTarget (mapObject);
            }
        }

        static void CheckCameraFocus ()
        {
            if (!MapView.MapIsEnabled)
                throw new InvalidOperationException ("There is no camera focus when the camera is not in map mode.");
        }
    }
}
