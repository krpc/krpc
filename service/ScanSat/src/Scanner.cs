using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.ScanSat
{
    /// <summary>
    /// A SCANsat scanner on a part. Obtained by calling <see cref="ScanSat.Scanner" /> or
    /// <see cref="ScanSat.Scanners" />.
    /// </summary>
    [KRPCClass (Service = "ScanSat", GameScene = GameScene.Flight)]
    public class Scanner : Equatable<Scanner>, IGameObjectState
    {
        internal static bool Is (SpaceCenter.Services.Parts.Part part)
        {
            return API.GetModule (part.InternalPart) != null;
        }

        internal Scanner (SpaceCenter.Services.Parts.Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not a SCANsat scanner");
            Part = part;
        }

        /// <summary>
        /// The state of the scanner. It takes the state of its part, and is destroyed once a
        /// live part no longer carries the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = Part.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return API.GetModule (Part.InternalPart) != null
                    ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        PartModule Module {
            get {
                var module = API.GetModule (Part.InternalPart);
                if (module == null)
                    throw new ObjectDestroyedException (
                        "The scanner no longer exists, as its part no longer has one.");
                return module;
            }
        }

        /// <summary>
        /// Check that the scanners are the same.
        /// </summary>
        public override bool Equals (Scanner other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part;
        }

        /// <summary>
        /// Hash the scanner.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode ();
        }

        /// <summary>
        /// The part containing this scanner.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part { get; private set; }

        /// <summary>
        /// The scan types of the scanner.
        /// </summary>
        [KRPCProperty]
        public ScanType ScanTypes {
            get { return (ScanType)API.Module.SensorType (Module) & ScanType.All; }
        }

        /// <summary>
        /// The field of view of the scanner at its best altitude, in degrees.
        /// </summary>
        [KRPCProperty]
        public float FieldOfView {
            get { return API.Module.FieldOfView (Module); }
        }

        /// <summary>
        /// The lowest altitude the scanner scans at, in meters.
        /// </summary>
        [KRPCProperty]
        public float MinAltitude {
            get { return API.Module.MinAltitude (Module); }
        }

        /// <summary>
        /// The highest altitude the scanner scans at, in meters.
        /// </summary>
        [KRPCProperty]
        public float MaxAltitude {
            get { return API.Module.MaxAltitude (Module); }
        }

        /// <summary>
        /// The lowest altitude for the full field of view, in meters.
        /// </summary>
        [KRPCProperty]
        public float BestAltitude {
            get { return API.Module.BestAltitude (Module); }
        }

        /// <summary>
        /// Whether the scanner only scans in daylight, when <see cref="ScanSat.DaylightCheck" />
        /// is on.
        /// </summary>
        [KRPCProperty]
        public bool RequiresLight {
            get { return API.Module.RequireLight (Module); }
        }

        /// <summary>
        /// Returns whether the scanner is switched on, and switches it on or off, as the buttons
        /// of the part do.
        /// </summary>
        /// <remarks>
        /// A resource scanner only switches on when <see cref="ScanSat.InstantResourceScan" />
        /// is off, or <see cref="ScanSat.StockResourceScan" /> is off.
        /// </remarks>
        [KRPCProperty]
        public bool Active {
            get { return API.Module.Scanning (Module); }
            set {
                var module = Module;
                if (value == API.Module.Scanning (module))
                    return;
                if (!value)
                    API.Module.StopScan (module);
                else if (API.Module.ResourceScanner.IsInstanceOfType (module))
                    API.Module.StartResourceScan (module, new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
                else
                    API.Module.StartScan (module);
            }
        }

        /// <summary>
        /// The state of the scanner.
        /// </summary>
        [KRPCProperty]
        public ScannerState State {
            get {
                var module = Module;
                if (!API.Module.Scanning (module))
                    return ScannerState.Off;
                // SCANsat updates the status of each sensor while it scans. A sensor in darkness
                // keeps its range flags from before.
                var controller = API.Sensor.Controller ();
                var status = controller == null ? null :
                    API.Sensor.Status (controller, Part.InternalPart.vessel, API.Module.SensorType (module));
                if (status == null)
                    return ScannerState.OutOfRange;
                if (API.Sensor.InDarkness (status))
                    return ScannerState.InDarkness;
                if (API.Sensor.BestRange (status))
                    return ScannerState.InBestRange;
                return API.Sensor.InRange (status) ? ScannerState.InRange : ScannerState.OutOfRange;
            }
        }
    }
}
