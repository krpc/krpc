using System;
using System.Collections;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// A servo group held by IR's controller, an IServoGroup of the mod.
    /// </summary>
    sealed class ControllerServoGroup : IServoGroup
    {
        static Field<object, string> name;
        static Func<object, global::Vessel> vessel;
        static Field<object, float> groupSpeedFactor;
        static Field<object, bool> expanded;
        static Func<object, int> movingDirection;
        static Field<object, bool> advancedMode;
        static Func<object, float> totalElectricChargeRequirement;
        static Field<object, bool> buildAid;
        static Field<object, bool> ikActive;
        static Field<object, string> forwardKey;
        static Field<object, string> reverseKey;
        static Func<object, IList> servos;
        static Action<object> moveLeft;
        static Action<object> moveCenter;
        static Action<object> moveRight;
        static Action<object> moveNextPreset;
        static Action<object> movePrevPreset;
        static Action<object> stop;

        internal static void Bind (ModBinder mod)
        {
            var group = mod.Type ("InfernalRobotics_v3.Interfaces.IServoGroup");
            name = group.Field<object, string> ("Name");
            vessel = group.Getter<object, global::Vessel> ("Vessel");
            groupSpeedFactor = group.Field<object, float> ("GroupSpeedFactor");
            expanded = group.Field<object, bool> ("Expanded");
            movingDirection = group.Getter<object, int> ("MovingDirection");
            advancedMode = group.Field<object, bool> ("AdvancedMode");
            totalElectricChargeRequirement = group.Getter<object, float> ("TotalElectricChargeRequirement");
            buildAid = group.Field<object, bool> ("BuildAid");
            ikActive = group.Field<object, bool> ("IKActive");
            forwardKey = group.Field<object, string> ("ForwardKey");
            reverseKey = group.Field<object, string> ("ReverseKey");
            servos = group.Getter<object, IList> ("Servos");
            moveLeft = group.Method<Action<object>> ("MoveLeft");
            moveCenter = group.Method<Action<object>> ("MoveCenter");
            moveRight = group.Method<Action<object>> ("MoveRight");
            moveNextPreset = group.Method<Action<object>> ("MoveNextPreset");
            movePrevPreset = group.Method<Action<object>> ("MovePrevPreset");
            stop = group.Method<Action<object>> ("Stop");
        }

        readonly object group;

        public ControllerServoGroup (object instance)
        {
            group = instance;
        }

        public string Name {
            get { return name.Get (group); }
            set { name.Set (group, value); }
        }

        public global::Vessel Vessel {
            get { return vessel (group); }
        }

        public float GroupSpeedFactor {
            get { return groupSpeedFactor.Get (group); }
            set { groupSpeedFactor.Set (group, value); }
        }

        public bool Expanded {
            get { return expanded.Get (group); }
            set { expanded.Set (group, value); }
        }

        public int MovingDirection {
            get { return movingDirection (group); }
        }

        public bool AdvancedMode {
            get { return advancedMode.Get (group); }
            set { advancedMode.Set (group, value); }
        }

        public float TotalElectricChargeRequirement {
            get { return totalElectricChargeRequirement (group); }
        }

        public bool BuildAid {
            get { return buildAid.Get (group); }
            set { buildAid.Set (group, value); }
        }

        public bool IKActive {
            get { return ikActive.Get (group); }
            set { ikActive.Set (group, value); }
        }

        public string ForwardKey {
            get { return forwardKey.Get (group); }
            set { forwardKey.Set (group, value); }
        }

        public string ReverseKey {
            get { return reverseKey.Get (group); }
            set { reverseKey.Set (group, value); }
        }

        public IList<IRServo> Servos {
            get {
                var result = new List<IRServo> ();
                var list = servos (group);
                if (list == null)
                    return result;
                // Skip a servo listed twice through a duplicate groupName token
                var seen = new HashSet<IRServo> ();
                foreach (var item in list) {
                    var servo = new IRServo (item);
                    if (seen.Add (servo))
                        result.Add (servo);
                }
                return result;
            }
        }

        public void MoveLeft ()
        {
            moveLeft (group);
        }

        public void MoveCenter ()
        {
            moveCenter (group);
        }

        public void MoveRight ()
        {
            moveRight (group);
        }

        public void MoveNextPreset ()
        {
            moveNextPreset (group);
        }

        public void MovePrevPreset ()
        {
            movePrevPreset (group);
        }

        public void Stop ()
        {
            stop (group);
        }
    }
}
