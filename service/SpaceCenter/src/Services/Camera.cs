using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Controls the game's camera.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Camera : Equatable<Camera>
    {
        /// <summary>
        /// Create a camera object.
        /// </summary>
        internal Camera ()
        {
        }

        /// <summary>
        /// Check if cameras are equal.
        /// Note: there is only one camera object.
        /// </summary>
        public override bool Equals (Camera obj)
        {
            return true;
        }

        /// <summary>
        /// Hash the camera.
        /// Note: there is only one camera object.
        /// </summary>
        public override int GetHashCode ()
        {
            return 0;
        }

        /// <summary>
        /// The current mode of the camera.
        /// </summary>
        [KRPCProperty]
        public CameraMode Mode {
            get {
                if (MapView.MapIsEnabled)
                    return CameraMode.Map;
                var mode = CameraManager.Instance.currentCameraMode;
                if (mode == CameraManager.CameraMode.Flight) {
                    switch (FlightCamera.fetch.mode) {
                    case FlightCamera.Modes.AUTO:
                        return CameraMode.Automatic;
                    case FlightCamera.Modes.FREE:
                        return CameraMode.Free;
                    case FlightCamera.Modes.CHASE:
                        return CameraMode.Chase;
                    case FlightCamera.Modes.LOCKED:
                        return CameraMode.Locked;
                    case FlightCamera.Modes.ORBITAL:
                        return CameraMode.Orbital;
                    default:
                        throw new InvalidOperationException ("Unknown camera mode " + FlightCamera.fetch.mode);
                    }
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
        /// The distance from the camera to the subject.
        /// A value between <see cref="MinDistance"/> and <see cref="MaxDistance"/>.
        /// </summary>
        [KRPCProperty]
        public float Distance {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.Distance;
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
                        camera.SetDistance (value.Clamp (camera.minDistance, camera.maxDistance));
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
        /// Minimum distance from the camera to the subject.
        /// </summary>
        [KRPCProperty]
        public float MinDistance {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.minDistance;
                case CameraMode.IVA:
                    return InternalCamera.Instance.maxZoom;
                default:
                    return FlightCamera.fetch.minDistance;
                }
            }
        }

        /// <summary>
        /// Maximum distance from the camera to the subject.
        /// </summary>
        [KRPCProperty]
        public float MaxDistance {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.maxDistance;
                case CameraMode.IVA:
                    return InternalCamera.Instance.minZoom;
                default:
                    return FlightCamera.fetch.maxDistance;
                }
            }
        }

        /// <summary>
        /// Default distance from the camera to the subject.
        /// </summary>
        [KRPCProperty]
        public float DefaultDistance {
            get {
                switch (Mode) {
                case CameraMode.Map:
                    return PlanetariumCamera.fetch.startDistance;
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
        public CelestialBody FocussedBody {
            get {
                CheckCameraFocus ();
                var body = PlanetariumCamera.fetch.target.celestialBody;
                return body == null ? null : new CelestialBody (body);
            }
            set {
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

