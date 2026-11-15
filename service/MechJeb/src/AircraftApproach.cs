using System;
using System.Collections;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Aircraft Approach and Autoland window, which lands a plane on a runway.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class AircraftApproach : ComputerModule
    {
        static Func<IList> runways;
        static Func<object, string> runwayName;
        static Func<object, global::CelestialBody> runwayBody;
        static Func<object, Vector3d> runwayStart;
        static Field<object, object> runway;
        static Field<object, double> glideslope;
        static Field<object, double> approachSpeed;
        static Field<object, double> touchdownSpeed;
        static Field<object, bool> engageReverseIfAvailable;
        static Field<object, bool> brakeAsSoonAsLanded;
        static Action<object, object> autoland;
        static Action<object> autopilotOff;
        static Func<object, string> state;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleSpaceplaneAutopilot");
            runways = type.StaticGetter<IList> ("runways");
            runway = type.Field<object, object> ("runway");
            glideslope = Bindings.EditableDouble (type, "glideslope");
            approachSpeed = Bindings.EditableDouble (type, "approachSpeed");
            touchdownSpeed = Bindings.EditableDouble (type, "touchdownSpeed");
            engageReverseIfAvailable = type.Field<object, bool> ("bEngageReverseIfAvailable");
            brakeAsSoonAsLanded = type.Field<object, bool> ("bBreakAsSoonAsLanded");
            autoland = type.Method<Action<object, object>> ("Autoland");
            autopilotOff = type.Method<Action<object>> ("AutopilotOff");
            state = type.Method<Func<object, string>> ("AutolandApproachStateToHumanReadableDescription");
            var runwayType = Bindings.Type ("MuMech.Runway");
            runwayName = runwayType.Getter<object, string> ("name");
            runwayBody = runwayType.Getter<object, global::CelestialBody> ("body");
            runwayStart = runwayType.Method<Func<object, Vector3d>> ("Start");
        }

        internal AircraftApproach () : base ("MechJebModuleSpaceplaneAutopilot")
        {
        }

        /// <summary>
        /// The runways of the body the vessel is at, as MechJeb's window lists them.
        /// </summary>
        static List<object> AvailableRunways {
            get {
                var body = FlightGlobals.ActiveVessel.mainBody;
                var result = new List<object> ();
                var all = runways ();
                if (all != null) {
                    foreach (var item in all) {
                        if (runwayBody (item) == body)
                            result.Add (item);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// The runway on the vessel's body that the autopilot lands on. MechJeb's window
        /// selects the first runway when its runway is on another body, which
        /// <see cref="Autoland"/> does too.
        /// </summary>
        object SelectedRunway {
            get {
                var available = AvailableRunways;
                if (available.Count == 0)
                    throw new InvalidOperationException ("There is no runway on " + FlightGlobals.ActiveVessel.mainBody.bodyName);
                var current = runway.Get (Instance);
                foreach (var item in available) {
                    if (runwayName (item) == runwayName (current))
                        return item;
                }
                return available [0];
            }
        }

        /// <summary>
        /// The names of the runways on the body the vessel is at.
        /// </summary>
        [KRPCProperty]
        public IList<string> Runways {
            get { return AvailableRunways.ConvertAll (item => runwayName (item)); }
        }

        /// <summary>
        /// The name of the runway to land on. Throws an exception if there is no runway on the
        /// body the vessel is at, or when set to the name of no such runway.
        /// </summary>
        [KRPCProperty]
        public string Runway {
            get { return runwayName (SelectedRunway); }
            set {
                foreach (var item in AvailableRunways) {
                    if (runwayName (item) == value) {
                        runway.Set (Instance, item);
                        return;
                    }
                }
                throw new ArgumentException ("There is no runway named " + value);
            }
        }

        /// <summary>
        /// The distance to the start of the runway, in meters.
        /// </summary>
        [KRPCProperty]
        public double DistanceToRunway {
            get { return Vector3d.Distance (FlightGlobals.ActiveVessel.CoMD, runwayStart (SelectedRunway)); }
        }

        /// <summary>
        /// Land on the runway. Throws an exception if there is no runway on the body the vessel
        /// is at.
        /// </summary>
        [KRPCMethod]
        public void Autoland ()
        {
            runway.Set (Instance, SelectedRunway);
            autoland (Instance, this);
        }

        /// <summary>
        /// Stop the autoland.
        /// </summary>
        [KRPCMethod]
        public void Abort ()
        {
            autopilotOff (Instance);
        }

        /// <summary>
        /// Whether the autoland is running.
        /// </summary>
        [KRPCProperty]
        public bool Enabled {
            get { return ModuleEnabled; }
        }

        /// <summary>
        /// The stage of the approach, as MechJeb's window describes it.
        /// </summary>
        [KRPCProperty]
        public string State {
            get { return state (Instance); }
        }

        /// <summary>
        /// The glideslope of the approach, in degrees.
        /// </summary>
        [KRPCProperty]
        public double Glideslope {
            get { return glideslope.Get (Instance); }
            set { glideslope.Set (Instance, value); }
        }

        /// <summary>
        /// The speed of the approach, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double ApproachSpeed {
            get { return approachSpeed.Get (Instance); }
            set { approachSpeed.Set (Instance, value); }
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
        /// Whether to reverse the thrust at touchdown, on engines that can.
        /// </summary>
        [KRPCProperty]
        public bool EngageReverseIfAvailable {
            get { return engageReverseIfAvailable.Get (Instance); }
            set { engageReverseIfAvailable.Set (Instance, value); }
        }

        /// <summary>
        /// Whether to brake as soon as the vessel is landed.
        /// </summary>
        [KRPCProperty]
        public bool BrakeAsSoonAsLanded {
            get { return brakeAsSoonAsLanded.Get (Instance); }
            set { brakeAsSoonAsLanded.Set (Instance, value); }
        }
    }
}
