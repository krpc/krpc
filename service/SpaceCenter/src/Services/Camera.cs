using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Controls the game's camera.
    /// Obtained by calling <see cref="SpaceCenter.Camera"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
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
        public CameraMode Mode {
            get {
                return CurrentMode;
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
                        CameraManager.Instance.SetCameraIVA();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The current mode of the camera, as reported by the game's camera manager.
        /// Shared with <see cref="CameraAddon"/>, which uses it to tell when a mode
        /// switch has completed before re-applying a deferred property write.
        /// </summary>
        internal static CameraMode CurrentMode {
            get {
                if (MapView.MapIsEnabled)
                    return CameraMode.Map;
                var mode = CameraManager.Instance.currentCameraMode;
                if (mode == CameraManager.CameraMode.Flight)
                    return FlightCamera.fetch.mode.ToCameraMode ();
                if (mode == CameraManager.CameraMode.IVA)
                    return CameraMode.IVA;
                throw new InvalidOperationException ("Unknown camera mode " + CameraManager.Instance.currentCameraMode);
            }
        }

        /// <summary>
        /// Switch to the next available camera.
        /// In flight, this moves to the next flight camera mode. In IVA, it moves the
        /// view to the next crew member in the active vessel.
        /// </summary>
        [KRPCMethod]
        public void NextCamera()
        {
            switch (CameraManager.Instance.currentCameraMode) {
            case CameraManager.CameraMode.Flight:
                SetFlightMode (1);
                break;
            case CameraManager.CameraMode.IVA:
                CycleIVACrewMember (1);
                break;
            }
        }

        /// <summary>
        /// Switch to the previous available camera.
        /// In flight, this moves to the previous flight camera mode. In IVA, it moves
        /// the view to the previous crew member in the active vessel.
        /// </summary>
        [KRPCMethod]
        public void PreviousCamera()
        {
            switch (CameraManager.Instance.currentCameraMode) {
            case CameraManager.CameraMode.Flight:
                SetFlightMode (-1);
                break;
            case CameraManager.CameraMode.IVA:
                CycleIVACrewMember (-1);
                break;
            }
        }

        /// <summary>
        /// The number of modes the flight camera cycles through.
        /// </summary>
        const int NumFlightCameraModes = 5;

        /// <summary>
        /// Move the flight camera the given number of places along its list of modes,
        /// wrapping around the ends of the list. The camera is re-targeted at the active
        /// vessel first, and nothing happens while camera mode switching is locked, as
        /// the game does when cycling the modes itself.
        /// </summary>
        static void SetFlightMode (int step)
        {
            if (!InputLockManager.IsUnlocked (ControlTypes.CAMERAMODES))
                return;
            var camera = FlightCamera.fetch;
            if (camera.targetMode != FlightCamera.TargetMode.Vessel || camera.vesselTarget != FlightGlobals.ActiveVessel)
                camera.SetTargetVessel (FlightGlobals.ActiveVessel);
            var mode = ((int)camera.mode + step + NumFlightCameraModes) % NumFlightCameraModes;
            camera.setMode ((FlightCamera.Modes)mode);
        }

        /// <summary>
        /// Move the IVA view the given number of places along the active vessel's crew
        /// list from the crew member currently in view.
        /// </summary>
        static void CycleIVACrewMember (int step)
        {
            var crew = FlightGlobals.ActiveVessel.GetVesselCrew ();
            var index = FindIVACrewMember (crew, step);
            if (index != -1)
                FocusIVACrewMember (crew [index].KerbalRef);
        }

        /// <summary>
        /// The index in the crew list of the crew member the given number of places from
        /// the one currently in view, wrapping around the ends of the list. Crew that the
        /// game has not placed in an interior are skipped, as there is no view to move to.
        /// Returns -1 if there is no crew member to move to.
        /// </summary>
        static int FindIVACrewMember (IList<ProtoCrewMember> crew, int step)
        {
            var count = crew.Count;
            if (count == 0)
                return -1;
            // The camera manager reports an index of -1 when no crew member is in view.
            // Starting from just off either end of the list then gives its first or last
            // crew member, depending on the direction being stepped in.
            var current = CameraManager.Instance.IVACameraActiveKerbalIndex;
            if (current < 0 || current >= count)
                current = step > 0 ? -1 : count;
            for (var offset = 1; offset <= count; offset++) {
                var index = (current + (offset * step)) % count;
                if (index < 0)
                    index += count;
                if (crew [index].KerbalRef != null)
                    return index;
            }
            return -1;
        }

        /// <summary>
        /// Move the IVA view to a crew member, switching the camera to IVA mode if it is
        /// not already. Selecting the crew member already in view does nothing, as the
        /// game's camera manager takes that as a request to leave IVA.
        /// </summary>
        static void FocusIVACrewMember (Kerbal kerbal)
        {
            var cameraManager = CameraManager.Instance;
            if (cameraManager.currentCameraMode == CameraManager.CameraMode.IVA &&
                cameraManager.IVACameraActiveKerbal == kerbal)
                return;
            cameraManager.SetCameraIVA (kerbal, true);
            GameEvents.OnIVACameraKerbalChange.Fire (kerbal);
        }

        /// <summary>
        /// The pitch of the camera, in degrees.
        /// A value between <see cref="MinPitch"/> and <see cref="MaxPitch"/>
        /// </summary>
        [KRPCProperty]
        public float Pitch {
            get {
                return ReadPitch ();
            }
            set {
                CameraAddon.Request (CameraProperty.Pitch, CurrentMode, value);
                ApplyPitch (value);
            }
        }

        static float ReadPitch ()
        {
            switch (CurrentMode) {
            case CameraMode.Map:
                return PlanetariumCamera.fetch.getPitch ();
            case CameraMode.IVA:
                var camera = InternalCamera.Instance;
                return (float) InternalCameraExtensions.GetPitch(camera);
            default:
                return FlightCamera.fetch.getPitch ();
            }
        }

        static void ApplyPitch (float value)
        {
            switch (CurrentMode) {
            case CameraMode.Map:
                {
                    var camera = PlanetariumCamera.fetch;
                    camera.camPitch = GeometryExtensions.ToRadians (value).Clamp (camera.minPitch, camera.maxPitch);
                    break;
                }
            case CameraMode.IVA:
                {
                    var camera = InternalCamera.Instance;
                    InternalCameraExtensions.SetPitch(camera, value.Clamp(camera.minPitch, camera.maxPitch));
                    break;
                }
            default:
                {
                    var camera = FlightCamera.fetch;
                    camera.camPitch = GeometryExtensions.ToRadians (value).Clamp (camera.minPitch, camera.maxPitch);
                    break;
                }
            }
        }

        /// <summary>
        /// The heading of the camera, in degrees.
        /// </summary>
        [KRPCProperty]
        public float Heading {
            get {
                return ReadHeading ();
            }
            set {
                CameraAddon.Request (CameraProperty.Heading, CurrentMode, value);
                ApplyHeading (value);
            }
        }

        static float ReadHeading ()
        {
            switch (CurrentMode) {
            case CameraMode.Map:
                return PlanetariumCamera.fetch.getYaw ();
            case CameraMode.IVA:
                var camera = InternalCamera.Instance;
                return (float) InternalCameraExtensions.GetRot(camera);
            default:
                return FlightCamera.fetch.getYaw ();
            }
        }

        static void ApplyHeading (float value)
        {
            switch (CurrentMode) {
            case CameraMode.Map:
                PlanetariumCamera.fetch.camHdg = GeometryExtensions.ToRadians (value);
                break;
            case CameraMode.IVA:
                var camera = InternalCamera.Instance;
                InternalCameraExtensions.SetRot(camera, value.Clamp(-camera.maxRot, camera.maxRot));
                break;
            default:
                FlightCamera.fetch.camHdg = GeometryExtensions.ToRadians (value);
                break;
            }
        }

        /// <summary>
        /// The distance from the camera to the subject, in meters.
        /// A value between <see cref="MinDistance"/> and <see cref="MaxDistance"/>.
        /// </summary>
        [KRPCProperty]
        public float Distance {
            get {
                return ReadDistance ();
            }
            set {
                CameraAddon.Request (CameraProperty.Distance, CurrentMode, value);
                ApplyDistance (value);
            }
        }

        static float ReadDistance ()
        {
            switch (CurrentMode) {
            case CameraMode.Map:
                return PlanetariumCamera.fetch.Distance * ScaledSpace.ScaleFactor;
            case CameraMode.IVA:
                throw new NotImplementedException ();
            default:
                return FlightCamera.fetch.Distance;
            }
        }

        static void ApplyDistance (float value)
        {
            switch (CurrentMode) {
            case CameraMode.Map:
                {
                    var camera = PlanetariumCamera.fetch;
                    camera.SetDistance ((value / ScaledSpace.ScaleFactor).Clamp (camera.minDistance, camera.maxDistance));
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

        /// <summary>
        /// The Field of View of the camera, in degrees.
        /// A value between <see cref="MinFoV"/> and <see cref="MaxFoV"/>.
        /// </summary>
        [KRPCProperty]
        public float FoV {
            get {
                return ReadFoV ();
            }
            set {
                CameraAddon.Request (CameraProperty.FoV, CurrentMode, value);
                ApplyFoV (value);
            }
        }

        static float ReadFoV ()
        {
            switch (CurrentMode) {
            case CameraMode.Map:
                throw new NotImplementedException ();
            case CameraMode.IVA:
                var camera = InternalCamera.Instance;
                return (float) InternalCameraExtensions.GetFoV(camera);
            default:
                return FlightCamera.fetch.FieldOfView;
            }
        }

        static void ApplyFoV (float value)
        {
            switch (CurrentMode) {
            case CameraMode.Map:
                throw new NotImplementedException ();
            case CameraMode.IVA:
                {
                    var camera = InternalCamera.Instance;
                    InternalCameraExtensions.SetZoom(camera, (value / (float) InternalCameraExtensions.GetDefaultFoV(camera)).Clamp(camera.maxZoom, camera.minZoom));
                    break;
                }
            default:
                {
                    var camera = FlightCamera.fetch;
                    camera.SetFoV(value.Clamp (camera.fovMin, camera.fovMax));
                    break;
                }
            }
        }

        /// <summary>
        /// Re-apply a deferred property write. Called by <see cref="CameraAddon"/> once
        /// the camera has reached the mode the write was requested for. Mirrors the
        /// concrete write each setter performs.
        /// </summary>
        internal static void ApplyRaw (CameraProperty property, float value)
        {
            switch (property) {
            case CameraProperty.Distance:
                ApplyDistance (value);
                break;
            case CameraProperty.Pitch:
                ApplyPitch (value);
                break;
            case CameraProperty.Heading:
                ApplyHeading (value);
                break;
            case CameraProperty.FoV:
                ApplyFoV (value);
                break;
            }
        }

        /// <summary>
        /// Whether the live camera has reached a requested property value, within a
        /// per-property tolerance. Distance is compared relatively because the flight
        /// camera lerps toward its target over several frames; pitch, heading and field
        /// of view are compared as angles in degrees. Heading wraps at 360 degrees.
        /// </summary>
        internal static bool Converged (CameraProperty property, float value)
        {
            switch (property) {
            case CameraProperty.Distance:
                return Math.Abs (ReadDistance () - value) <= Math.Max (0.1f, 0.01f * Math.Abs (value));
            case CameraProperty.Pitch:
                return Math.Abs (ReadPitch () - value) <= 0.1f;
            case CameraProperty.Heading:
                return AngleDifference (ReadHeading (), value) <= 0.1f;
            case CameraProperty.FoV:
                return Math.Abs (ReadFoV () - value) <= 0.1f;
            default:
                return true;
            }
        }

        static float AngleDifference (float a, float b)
        {
            var difference = Math.Abs (a - b) % 360f;
            return difference > 180f ? 360f - difference : difference;
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
        /// Minimum distance from the camera to the subject, in meters.
        /// </summary>
        [KRPCProperty]
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
        /// The maximum field of view the camera in degrees.
        /// </summary>
        [KRPCProperty]
        public float MaxFoV {
            get {
                switch (Mode) {
                    case CameraMode.Map:
                        throw new NotImplementedException();
                    case CameraMode.IVA:
                        var camera = InternalCamera.Instance;
                        return (float) InternalCameraExtensions.GetDefaultFoV(camera) * camera.minZoom;
                    default:
                        return FlightCamera.fetch.fovMax;
                }
            }
        }

        /// <summary>
        /// The minimum field of view the camera in degrees.
        /// </summary>
        [KRPCProperty]
        public float MinFoV {
            get {
                switch (Mode) {
                    case CameraMode.Map:
                        throw new NotImplementedException();
                    case CameraMode.IVA:
                        var camera = InternalCamera.Instance;
                        return (float) InternalCameraExtensions.GetDefaultFoV(camera) * camera.maxZoom;
                    default:
                        return FlightCamera.fetch.fovMin;
                }
            }
        }

        /// <summary>
        /// The default field of view the camera in degrees.
        /// </summary>
        [KRPCProperty]
        public float DefaultFoV {
            get {
                switch (Mode) {
                    case CameraMode.Map:
                        throw new NotImplementedException();
                    case CameraMode.IVA:
                        var camera = InternalCamera.Instance;
                        return (float) InternalCameraExtensions.GetDefaultFoV(camera);
                    default:
                        return FlightCamera.fetch.fovDefault;
                }
            }
        }

        /// <summary>
        /// In map mode, the celestial body that the camera is focussed on.
        /// Returns <c>null</c> if the camera is not focussed on a celestial body.
        /// Returns an error is the camera is not in map mode.
        /// </summary>
        [KRPCProperty (Nullable = true)]
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
        [KRPCProperty (Nullable = true)]
        public Vessel FocussedVessel {
            get {
                CheckCameraFocus ();
                var vessel = PlanetariumCamera.fetch.target.vessel;
                return vessel == null ? null : new Vessel (vessel);
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException ("FocussedVessel");
                CheckCameraFocus ();
                PlanetariumCamera.fetch.SetTarget (value.InternalVessel.mapObject);
            }
        }

        /// <summary>
        /// In map mode, the maneuver node that the camera is focussed on.
        /// Returns <c>null</c> if the camera is not focussed on a maneuver node.
        /// Returns an error is the camera is not in map mode.
        /// </summary>
        [KRPCProperty (Nullable = true)]
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

        /// <summary>
        /// In IVA mode, the crew member whose view the camera is showing.
        /// Returns <c>null</c> if no crew member is in view.
        /// Returns an error if the camera is not in IVA mode.
        /// Setting this moves the view to the given crew member, switching the camera to
        /// IVA mode if it is not already. The crew member must be in the active vessel,
        /// and in a part whose interior the game has placed them in.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public CrewMember FocussedCrewMember
        {
            get
            {
                if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA)
                    throw new InvalidOperationException ("There is no focussed kerbal when the camera is not in IVA mode.");
                var kerbal = CameraManager.Instance.IVACameraActiveKerbal;
                return kerbal == null ? null : new CrewMember (kerbal.protoCrewMember);
            }
            set
            {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException ("FocussedCrewMember");
                var crewMember = value.InternalCrewMember;
                if (!FlightGlobals.ActiveVessel.GetVesselCrew ().Contains (crewMember))
                    throw new InvalidOperationException ("The crew member is not in the active vessel.");
                if (crewMember.KerbalRef == null)
                    throw new InvalidOperationException ("The crew member is not in the interior of a part.");
                FocusIVACrewMember (crewMember.KerbalRef);
            }
        }

        static void CheckCameraFocus ()
        {
            if (!MapView.MapIsEnabled)
                throw new InvalidOperationException ("There is no camera focus when the camera is not in map mode.");
        }
    }
}
