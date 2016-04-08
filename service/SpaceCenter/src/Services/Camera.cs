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
        /// The distance from the camera to the subject.
        /// A value between <see cref="MinDistance"/> and <see cref="MaxDistance"/>.
        /// </summary>
        [KRPCProperty]
        public float Distance {
            get {
                if (MapView.MapIsEnabled)
                    return PlanetariumCamera.fetch.Distance;
                var mode = CameraManager.Instance.currentCameraMode;
                if (mode == CameraManager.CameraMode.Flight)
                    return FlightCamera.fetch.Distance;
                else if (mode == CameraManager.CameraMode.IVA)
                    throw new NotImplementedException ();
                throw new InvalidOperationException ("Unknown camera mode " + mode);
            }
            set {
                if (MapView.MapIsEnabled)
                    PlanetariumCamera.fetch.SetDistance (value.Clamp (MinDistance, MaxDistance));
                var mode = CameraManager.Instance.currentCameraMode;
                if (mode == CameraManager.CameraMode.Flight)
                    FlightCamera.fetch.SetDistance (value.Clamp (MinDistance, MaxDistance));
                else if (mode == CameraManager.CameraMode.IVA)
                    throw new NotImplementedException ();
                else
                    throw new InvalidOperationException ("Unknown camera mode " + mode);
            }
        }

        /// <summary>
        /// Maximum distance from the camera to the subject.
        /// </summary>
        [KRPCProperty]
        public float MaxDistance {
            get {
                if (MapView.MapIsEnabled)
                    return PlanetariumCamera.fetch.maxDistance;
                var mode = CameraManager.Instance.currentCameraMode;
                if (mode == CameraManager.CameraMode.Flight)
                    return FlightCamera.fetch.maxDistance;
                else if (mode == CameraManager.CameraMode.IVA)
                    throw new NotImplementedException ();
                throw new InvalidOperationException ("Unknown camera mode " + mode);
            }
        }

        /// <summary>
        /// Minimum distance from the camera to the subject.
        /// </summary>
        [KRPCProperty]
        public float MinDistance {
            get {
                if (MapView.MapIsEnabled)
                    return PlanetariumCamera.fetch.minDistance;
                var mode = CameraManager.Instance.currentCameraMode;
                if (mode == CameraManager.CameraMode.Flight)
                    return FlightCamera.fetch.minDistance;
                else if (mode == CameraManager.CameraMode.IVA)
                    throw new NotImplementedException ();
                throw new InvalidOperationException ("Unknown camera mode " + mode);
            }
        }

        /// <summary>
        /// Default distance from the camera to the subject.
        /// </summary>
        [KRPCProperty]
        public float DefaultDistance {
            get {
                if (MapView.MapIsEnabled)
                    return PlanetariumCamera.fetch.startDistance;
                var mode = CameraManager.Instance.currentCameraMode;
                if (mode == CameraManager.CameraMode.Flight)
                    return FlightCamera.fetch.startDistance;
                else if (mode == CameraManager.CameraMode.IVA)
                    throw new NotImplementedException ();
                throw new InvalidOperationException ("Unknown camera mode " + mode);
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
                if (body == null)
                    return null;
                return new CelestialBody (body);
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
                if (vessel == null)
                    return null;
                return new Vessel (vessel);
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
                if (vessel == null || node == null)
                    return null;
                return new Node (vessel, node);
            }
            set {
                CheckCameraFocus ();
                var mapObject = PlanetariumCamera.fetch.targets.Single (x => x.maneuverNode == value.InternalNode);
                PlanetariumCamera.fetch.SetTarget (mapObject);
            }
        }

        void CheckCameraFocus ()
        {
            if (!MapView.MapIsEnabled)
                throw new InvalidOperationException ("There is no camera focus when the camera is not in map mode.");
        }
    }
}

