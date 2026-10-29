using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Provides functionality to interact with Kitten Space Agency.
    /// </summary>
    [KRPCService]
    public static class KSA
    {
        /// <summary>
        /// The vehicle under the player's control, or <c>null</c> if the player controls a
        /// kitten on EVA.
        /// </summary>
        /// <remarks>
        /// Setting it moves the camera to the vehicle, as the switch vehicle key does. See
        /// <see cref="ActiveKitten"/> for a kitten on EVA.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public static Vehicle ActiveVehicle {
            get {
                var vehicle = Program.ControlledVehicle;
                return vehicle == null || vehicle is KittenEva ? null : new Vehicle (vehicle);
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (ActiveVehicle));
                Universe.FollowInHoveredViewport (value.InternalVehicle);
            }
        }

        /// <summary>
        /// The kitten on EVA under the player's control, or <c>null</c> if the player
        /// controls a vehicle.
        /// </summary>
        /// <remarks>
        /// Setting it moves the camera to the kitten. Throws an exception if the kitten is not
        /// on EVA. See <see cref="ActiveVehicle"/> for a vehicle.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public static Kitten ActiveKitten {
            get {
                var vehicle = Program.ControlledVehicle as KittenEva;
                return vehicle == null ? null : new Kitten (vehicle.Id);
            }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException (nameof (ActiveKitten));
                Universe.FollowInHoveredViewport (value.Eva ?? throw new InvalidOperationException ("The kitten is not on EVA"));
            }
        }

        /// <summary>
        /// The game's main camera.
        /// </summary>
        [KRPCProperty]
        public static Camera Camera {
            get { return new Camera (); }
        }

        /// <summary>
        /// The kittens in the agency's roster, including those killed in action, keyed by name.
        /// </summary>
        /// <remarks>
        /// <see cref="Kitten.RosterIndex"/> gives their order in the roster.
        /// </remarks>
        [KRPCProperty]
        public static IDictionary<string, Kitten> Kittens {
            get {
                if (Universe.CurrentSystem == null)
                    throw new InvalidOperationException ("No game is loaded");
                var kittens = new Dictionary<string, Kitten> ();
                foreach (var kitten in Universe.KittenRoster.Kittens)
                    kittens [kitten.Name] = new Kitten (kitten.Name);
                return kittens;
            }
        }

        /// <summary>
        /// The vehicles in the current system, excluding kittens on EVA.
        /// </summary>
        [KRPCProperty]
        public static IList<Vehicle> Vehicles {
            get { return Utils.Vehicles.All.Select (vehicle => new Vehicle (vehicle)).ToList (); }
        }

        /// <summary>
        /// The planets, moons and stars in the current system, keyed by name.
        /// </summary>
        [KRPCProperty]
        public static IDictionary<string, Body> Bodies {
            get {
                var bodies = new Dictionary<string, Body> ();
                foreach (var body in Utils.Vehicles.Bodies)
                    bodies [body.Id] = new Body (body);
                return bodies;
            }
        }

        /// <summary>
        /// The universal time, in seconds.
        /// </summary>
        [KRPCProperty]
        public static double UT {
            get { return Universe.GetElapsedSeconds (); }
        }

        /// <summary>
        /// The simulation speed, as a multiple of real time. Zero when paused.
        /// </summary>
        [KRPCProperty]
        public static double SimulationSpeed {
            get { return Universe.SimulationSpeed; }
            set {
                if (!(value >= 0))
                    throw new ArgumentOutOfRangeException (nameof (SimulationSpeed));
                Universe.SetSimulationSpeed (value, alert: false);
            }
        }

        /// <summary>
        /// Warp to the given universal time, and return once the warp has finished.
        /// The game slows the warp down to normal speed toward the end.
        /// </summary>
        /// <param name="ut">The universal time to warp to, in seconds.</param>
        /// <remarks>
        /// Does nothing when the time is in the past. Returns early if the game stops the
        /// warp, for example when its menu pauses the game.
        /// </remarks>
        [KRPCProcedure]
        public static void WarpTo (double ut)
        {
            var seconds = ut - Universe.GetElapsedSeconds ();
            if (!(seconds > 0))
                return;
            Universe.AutoWarpTo (Universe.GetElapsedTime () + seconds);
            WaitForWarp ();
        }

        static void WaitForWarp ()
        {
            if (Universe.IsAutoWarpActive)
                throw new YieldException<Action> (WaitForWarp);
        }

        /// <summary>
        /// Launch a vehicle from a launch site, and make it the active vehicle. Returns once
        /// the vehicle is on the launch site.
        /// </summary>
        /// <param name="name">The name of the vehicle, one of the game's default vehicles or
        /// a vehicle saved by the player.</param>
        /// <param name="site">The name of the launch site. See <see cref="Body.LaunchSites"/>.</param>
        /// <returns>The vehicle. Its name is the vehicle's name, with a number appended if
        /// another vehicle has that name.</returns>
        /// <remarks>
        /// Throws an exception if there is no such vehicle or launch site, or the name is one
        /// of the game's kittens.
        /// </remarks>
        [KRPCProcedure]
        public static Vehicle LaunchVehicle (string name, string site)
        {
            var save = DefaultVehicleSaves.FindSave (name);
            if (save == null) {
                foreach (var playerSave in VehicleSaves.AsSpan ()) {
                    if (playerSave.Id == name)
                        save = playerSave;
                }
            }
            if (save == null)
                throw new ArgumentException ("No vehicle named " + name);
            if (!string.IsNullOrEmpty (save.VehicleSaveData.Character))
                throw new ArgumentException (name + " is a kitten, not a vehicle");
            Celestial celestial = null;
            LocationReference location = null;
            foreach (var body in Utils.Vehicles.Bodies) {
                if (body is Celestial candidate && candidate.GetLocation (site) is LandmarkReference { IsLaunchPad: true } landmark) {
                    celestial = candidate;
                    location = landmark;
                }
            }
            if (location == null)
                throw new ArgumentException ("No launch site named " + site);
            var tree = save.Load (Program.MainViewport) ?? throw new InvalidOperationException ("Failed to load vehicle " + name);
            tree.ApplyTankAssignments ();
            var id = name;
            for (var i = 1; Universe.CurrentSystem.All.TryGet (id, out Astronomical _); i++)
                id = name + "_" + i;
            CrewAssignmentWindow.ValidateSeatAssignments (tree, id, hasLaunched: false);
            // As the game's launch menu places a vehicle
            var mass = tree.ComputeInertMassPropertiesAsmb () + tree.ComputePropellantMassPropertiesAsmb ();
            var (min, max) = tree.ComputeBoundingBoxAsmb ();
            var state = global::KSA.Vehicle.GetInitialKinematicStateForLocation (
                celestial, Universe.GetNextSimStep ().NextTime, location.Latitude.ToDegrees (), location.Longitude.ToDegrees (),
                min, max, double3.Unpack (in mass.Offset), new Random ().RandomColor ());
            InputEvents.CreateVehicleBuffer.Add (new InputEvents.CreateVehicleData {
                System = Universe.CurrentSystem,
                Parent = state.Orbit.Parent,
                Id = id,
                Body2Cce = state.Body2Cce,
                BodyRates = state.BodyRates,
                Orbit = state.Orbit,
                Root = tree.Root
            });
            return WaitForLaunch (id, 0);
        }

        static Vehicle WaitForLaunch (string id, int updates)
        {
            if (Universe.CurrentSystem.All.TryGet (id, out Astronomical created) && created is global::KSA.Vehicle vehicle) {
                Universe.FollowInHoveredViewport (vehicle);
                return new Vehicle (vehicle);
            }
            if (updates >= 10)
                throw new InvalidOperationException ("The vehicle was not created");
            throw new YieldException<Func<Vehicle>> (() => WaitForLaunch (id, updates + 1));
        }

        // The game's save list, with no public accessor
        static readonly PropertyInfo saves = typeof (GameSaves).GetProperty (
            "Saves", BindingFlags.NonPublic | BindingFlags.Static);

        static GameSave FindSave (string name)
        {
            var collection = (LookupCollection<GameSave>)saves.GetValue (null);
            return collection.TryGet (name, out GameSave save) ? save : null;
        }

        /// <summary>
        /// Whether a save with the given name exists.
        /// </summary>
        /// <param name="name">Name of the save.</param>
        [KRPCProcedure]
        public static bool SaveExists (string name)
        {
            return FindSave (name) != null;
        }

        /// <summary>
        /// Save the game, replacing any save with the same name.
        /// </summary>
        /// <param name="name">Name of the save.</param>
        [KRPCProcedure]
        public static void Save (string name)
        {
            if (UncompressedSave.Make (name) == null)
                throw new InvalidOperationException ("Failed to write save '" + name + "'");
        }

        /// <summary>
        /// Load a save, and return once it has loaded. The game is paused after loading.
        /// </summary>
        /// <param name="name">Name of the save.</param>
        /// <remarks>
        /// Throws an exception if the save does not exist.
        /// </remarks>
        [KRPCProcedure]
        public static void Load (string name)
        {
            var save = FindSave (name);
            if (save == null)
                throw new ArgumentException ("Save '" + name + "' not found");
            var loaded = false;
            Exception error = null;
            // The load replaces every vehicle, so it runs once the vehicle solver step that
            // follows the server update has been queued
            AfterFrame.Enqueue (() => {
                try {
                    save.Load ();
                } catch (Exception e) {
                    error = e;
                }
                loaded = true;
            });
            WaitForLoad (() => loaded, () => error);
        }

        static void WaitForLoad (Func<bool> loaded, Func<Exception> error)
        {
            if (!loaded ())
                throw new YieldException<Action> (() => WaitForLoad (loaded, error));
            if (error () != null)
                throw new InvalidOperationException ("Failed to load save: " + error ().Message);
        }

        /// <summary>
        /// Show a message in the middle of the screen.
        /// </summary>
        /// <param name="text">The message.</param>
        /// <param name="duration">How long to show the message for, in seconds. The game
        /// shows a message for at least 2 seconds.</param>
        /// <param name="warning">Whether to show the message in the warning color.</param>
        [KRPCProcedure]
        public static void ShowMessage (string text, double duration = 5, bool warning = false)
        {
            if (text == null)
                throw new ArgumentNullException (nameof (text));
            if (warning)
                TimedAlert.CreateWarning (text, duration);
            else
                TimedAlert.Create (text, Color.White.AsByte4, duration);
        }

        // The game's screenshot capture and folder, with no public accessors
        static readonly FieldInfo screenshotCapture = typeof (Program).GetField (
            "_screenshotCapture", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly PropertyInfo screenshotsFolder = typeof (ScreenshotCapture).GetProperty (
            "ScreenshotsFolderPath", BindingFlags.NonPublic | BindingFlags.Static);

        // The last chunk of a PNG file, written once the file is complete
        static readonly byte [] pngEnd = { 0, 0, 0, 0, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };

        /// <summary>
        /// Save a screenshot, as the screenshot key does, and return once the file is written.
        /// </summary>
        /// <param name="scale">The size of the image, as a multiple of the window size,
        /// from 1 to 8. The game reduces it to fit the graphics memory.</param>
        /// <param name="ui">Whether to include the game's windows. The image is then the size
        /// of the window.</param>
        /// <returns>The path of the PNG file, in the game's screenshots folder.</returns>
        /// <remarks>
        /// Throws an exception in the vehicle editor, or if a screenshot is already being taken.
        /// </remarks>
        [KRPCProcedure]
        public static string TakeScreenshot (int scale = 1, bool ui = false)
        {
            if (!(scale >= 1 && scale <= 8))
                throw new ArgumentOutOfRangeException (nameof (scale));
            var capture = (ScreenshotCapture)screenshotCapture.GetValue (Program.Instance)
                ?? throw new InvalidOperationException ("The game is not ready to take a screenshot");
            if (capture.IsActive)
                throw new InvalidOperationException ("A screenshot is already being taken");
            if (Program.EditorFlag)
                throw new InvalidOperationException ("Screenshots are not available in the vehicle editor");
            var folder = (string)screenshotsFolder.GetValue (null);
            // The file name has the time to the second, so a second screenshot within the
            // second replaces the first
            var start = DateTime.Now.AddSeconds (-1);
            capture.Request (scale, ui ? "ui" : string.Empty);
            var timer = Stopwatch.StartNew ();
            return WaitForScreenshot (capture, folder, start, timer);
        }

        static string WaitForScreenshot (ScreenshotCapture capture, string folder, DateTime start, Stopwatch timer)
        {
            // The game writes the file on a worker thread once the capture is done
            if (!capture.IsActive && Directory.Exists (folder)) {
                foreach (var path in Directory.EnumerateFiles (folder, "ksa_*.png")) {
                    if (File.GetLastWriteTime (path) >= start && IsCompletePng (path))
                        return path;
                }
            }
            if (timer.Elapsed.TotalSeconds > 60)
                throw new InvalidOperationException ("The screenshot was not saved");
            throw new YieldException<Func<string>> (() => WaitForScreenshot (capture, folder, start, timer));
        }

        static bool IsCompletePng (string path)
        {
            try {
                using (var file = File.OpenRead (path)) {
                    if (file.Length < pngEnd.Length)
                        return false;
                    file.Seek (-pngEnd.Length, SeekOrigin.End);
                    var end = new byte [pngEnd.Length];
                    return file.Read (end, 0, end.Length) == end.Length && end.SequenceEqual (pngEnd);
                }
            } catch (IOException) {
                return false;
            }
        }

        /// <summary>
        /// Convert a position from one reference frame to another.
        /// </summary>
        /// <param name="position">Position, in meters, in the frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame the position is in.</param>
        /// <param name="to">The reference frame to convert the position to.</param>
        /// <returns>The position in the frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple<double, double, double> TransformPosition (
            Tuple<double, double, double> position, ReferenceFrame from, ReferenceFrame to)
        {
            return Vectors.ToTuple (to.PositionFromEcl (from.PositionToEcl (Vectors.ToVector (position))));
        }

        /// <summary>
        /// Convert a direction from one reference frame to another.
        /// </summary>
        /// <param name="direction">Direction in the frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame the direction is in.</param>
        /// <param name="to">The reference frame to convert the direction to.</param>
        /// <returns>The direction in the frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple<double, double, double> TransformDirection (
            Tuple<double, double, double> direction, ReferenceFrame from, ReferenceFrame to)
        {
            return Vectors.ToTuple (to.DirectionFromEcl (from.DirectionToEcl (Vectors.ToVector (direction))));
        }

        /// <summary>
        /// Convert a rotation from one reference frame to another.
        /// </summary>
        /// <param name="rotation">Rotation in the frame <paramref name="from"/>.</param>
        /// <param name="from">The reference frame the rotation is in.</param>
        /// <param name="to">The reference frame to convert the rotation to.</param>
        /// <returns>The rotation in the frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple<double, double, double, double> TransformRotation (
            Tuple<double, double, double, double> rotation, ReferenceFrame from, ReferenceFrame to)
        {
            return Vectors.ToTuple (to.RotationFromEcl (from.RotationToEcl (Vectors.ToRotation (rotation))));
        }

        /// <summary>
        /// Convert a velocity from one reference frame to another. The velocity of a point
        /// depends on its position in a rotating frame, so the position is also needed.
        /// </summary>
        /// <param name="position">Position, in meters, in the frame <paramref name="from"/>.</param>
        /// <param name="velocity">Velocity, in meters per second, in the frame
        /// <paramref name="from"/>.</param>
        /// <param name="from">The reference frame the position and velocity are in.</param>
        /// <param name="to">The reference frame to convert the velocity to.</param>
        /// <returns>The velocity in the frame <paramref name="to"/>.</returns>
        [KRPCProcedure]
        public static Tuple<double, double, double> TransformVelocity (
            Tuple<double, double, double> position, Tuple<double, double, double> velocity,
            ReferenceFrame from, ReferenceFrame to)
        {
            var positionEcl = from.PositionToEcl (Vectors.ToVector (position));
            var velocityEcl = from.VelocityToEcl (positionEcl, Vectors.ToVector (velocity));
            return Vectors.ToTuple (to.VelocityFromEcl (positionEcl, velocityEcl));
        }
    }
}
