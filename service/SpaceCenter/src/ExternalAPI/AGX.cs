using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    static class AGX
    {
        static MethodInfo groupActionsMethod;
        static FieldInfo partField;
        static FieldInfo actionField;

        public static void Load ()
        {
            var type = APILoader.Load (typeof(AGX), "AGExt", "ActionGroupsExtended.AGExtExternal");
            IsAvailable = (type != null);
            if (IsAvailable) {
                groupActionsMethod = type.GetMethod (
                    "AGX2VslGroupActions",
                    BindingFlags.Public | BindingFlags.Static, null,
                    new Type[] { typeof(uint), typeof(int) }, null);
            }
        }

        public static bool IsAvailable { get; private set; }

        public static Func<uint, int, bool> AGX2VslGroupState { get; internal set; }

        public static Func<uint, int, bool> AGX2VslToggleGroup { get; internal set; }

        public static Func<uint, int, bool, bool> AGX2VslActivateGroup { get; internal set; }

        /// <summary>
        /// Returns the parts and actions assigned to the given action group, using the
        /// Extended Action Groups API. Each entry pairs a part with one of its actions.
        /// </summary>
        /// <remarks>
        /// The API returns a list of AGXAction objects whose type is not known at compile
        /// time, so the part (field "prt") and action (field "ba") are read by reflection.
        /// </remarks>
        public static IList<Tuple<global::Part, BaseAction>> GroupActions (uint flightId, int group)
        {
            var result = new List<Tuple<global::Part, BaseAction>> ();
            if (groupActionsMethod == null)
                return result;
            var actions = groupActionsMethod.Invoke (null, new object[] { flightId, group }) as IEnumerable;
            if (actions == null)
                return result;
            foreach (var action in actions) {
                if (action == null)
                    continue;
                if (partField == null) {
                    var actionType = action.GetType ();
                    partField = actionType.GetField ("prt");
                    actionField = actionType.GetField ("ba");
                }
                if (partField == null || actionField == null)
                    continue;
                var part = partField.GetValue (action) as global::Part;
                var baseAction = actionField.GetValue (action) as BaseAction;
                if (part != null && baseAction != null)
                    result.Add (Tuple.Create (part, baseAction));
            }
            return result;
        }
    }
}
