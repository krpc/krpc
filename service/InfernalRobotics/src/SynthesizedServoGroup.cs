using System;
using System.Collections.Generic;
using System.Linq;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// A servo group built by kRPC from a vessel's servo modules, for a vessel whose groups IR's
    /// controller does not hold. Movement goes to the member servos. Group state that lives only
    /// in IR's controller has no store here, and throws.
    /// </summary>
    sealed class SynthesizedServoGroup : IServoGroup
    {
        readonly global::Vessel vessel;
        readonly IList<object> modules;
        string name;

        SynthesizedServoGroup (global::Vessel groupVessel, string groupName, IList<object> groupModules)
        {
            vessel = groupVessel;
            name = groupName;
            modules = groupModules;
        }

        /// <summary>
        /// The groups of the given servo modules. A module's groupName encodes every group it
        /// belongs to, as IR's Controller.RebuildServoGroupsFlight parses it: memberships are
        /// separated by '|', and each is "&lt;group name&gt;;&lt;ordering index&gt;".
        /// </summary>
        internal static IList<IServoGroup> Build (global::Vessel vessel, IList<object> servos)
        {
            var order = new List<string> ();
            var byGroup = new Dictionary<string, List<KeyValuePair<int, object>>> ();
            foreach (var servo in servos) {
                var raw = API.GroupName.Get (servo);
                if (string.IsNullOrEmpty (raw))
                    continue;
                // IR appends another token to groupName on every vessel activation, without
                // removing the previous one. We keep the first token per group, as the fix in
                // https://github.com/meirumeiru/InfernalRobotics/pull/5 does.
                var servoGroups = new HashSet<string> ();
                foreach (var membership in raw.Split ('|')) {
                    var parts = membership.Split (';');
                    var groupName = parts [0];
                    if (!servoGroups.Add (groupName))
                        continue;
                    int index;
                    if (parts.Length < 2 || !int.TryParse (parts [1], out index))
                        index = int.MaxValue;
                    List<KeyValuePair<int, object>> members;
                    if (!byGroup.TryGetValue (groupName, out members)) {
                        members = new List<KeyValuePair<int, object>> ();
                        byGroup [groupName] = members;
                        order.Add (groupName);
                    }
                    members.Add (new KeyValuePair<int, object> (index, servo));
                }
            }
            // OrderBy is a stable sort, so servos with an equal or absent index keep their order
            return order.Select (groupName => (IServoGroup)new SynthesizedServoGroup (
                vessel, groupName, byGroup [groupName].OrderBy (m => m.Key).Select (m => m.Value).ToList ())).ToList ();
        }

        IList<IRServo> Wrap ()
        {
            return modules.Select (x => new IRServo (x)).ToList ();
        }

        public string Name {
            get { return name; }
            set {
                // Rewrite this group's membership token on each servo, keeping the
                // "<name>;<index>" encoding and the servo's other groups
                foreach (var servo in modules) {
                    var memberships = (API.GroupName.Get (servo) ?? string.Empty).Split ('|');
                    for (var i = 0; i < memberships.Length; i++) {
                        var parts = memberships [i].Split (';');
                        if (parts [0] == name) {
                            parts [0] = value;
                            memberships [i] = string.Join (";", parts);
                        }
                    }
                    API.GroupName.Set (servo, string.Join ("|", memberships));
                }
                name = value;
            }
        }

        public global::Vessel Vessel {
            get { return vessel; }
        }

        public IList<IRServo> Servos {
            get { return Wrap (); }
        }

        public void MoveLeft ()
        {
            foreach (var servo in Wrap ())
                servo.MoveLeft ();
        }

        public void MoveCenter ()
        {
            foreach (var servo in Wrap ())
                servo.MoveCenter ();
        }

        public void MoveRight ()
        {
            foreach (var servo in Wrap ())
                servo.MoveRight ();
        }

        public void Stop ()
        {
            foreach (var servo in Wrap ())
                servo.Stop ();
        }

        // The error for group state that lives only in IR's controller. The controller holds
        // the active vessel's groups once it has loaded them, shortly after a flight scene loads.
        Exception NotAvailable ()
        {
            if (vessel == FlightGlobals.ActiveVessel)
                return new InvalidOperationException (
                    "This member is not available until Infernal Robotics has loaded the vessel's groups.");
            return new InvalidOperationException (
                "This member is not available for a servo group on a non-active vessel. " +
                "Such groups support Name, Vessel and Servos, movement (MoveLeft, MoveRight, " +
                "MoveCenter, Stop) and full per-servo control; preset, key, speed-factor, " +
                "expanded, moving-direction, advanced-mode, electric-charge, build-aid and " +
                "IK state are only available for the active vessel.");
        }

        public float GroupSpeedFactor {
            get { throw NotAvailable (); }
            set { throw NotAvailable (); }
        }

        public bool Expanded {
            get { throw NotAvailable (); }
            set { throw NotAvailable (); }
        }

        public int MovingDirection {
            get { throw NotAvailable (); }
        }

        public bool AdvancedMode {
            get { throw NotAvailable (); }
            set { throw NotAvailable (); }
        }

        public float TotalElectricChargeRequirement {
            get { throw NotAvailable (); }
        }

        public bool BuildAid {
            get { throw NotAvailable (); }
            set { throw NotAvailable (); }
        }

        public bool IKActive {
            get { throw NotAvailable (); }
            set { throw NotAvailable (); }
        }

        public string ForwardKey {
            get { throw NotAvailable (); }
            set { throw NotAvailable (); }
        }

        public string ReverseKey {
            get { throw NotAvailable (); }
            set { throw NotAvailable (); }
        }

        public void MoveNextPreset ()
        {
            throw NotAvailable ();
        }

        public void MovePrevPreset ()
        {
            throw NotAvailable ();
        }
    }
}
