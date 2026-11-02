using System;
using System.Collections;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    static class AGExt
    {
        static Func<uint, int, object> groupActions;
        static Func<object, global::Part> actionPart;
        static Func<object, BaseAction> actionAction;

        public static void Load ()
        {
            var type = APILoader.Load (typeof(AGExt), "AGExt", "ActionGroupsExtended.AGExtExternal");
            IsAvailable = (type != null);
            if (IsAvailable) {
                groupActions = Accessor.Method<Func<uint, int, object>> (type, "AGX2VslGroupActions");
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
            if (groupActions == null)
                return result;
            var actions = groupActions (flightId, group) as IEnumerable;
            if (actions == null)
                return result;
            foreach (var action in actions) {
                if (action == null)
                    continue;
                if (actionPart == null) {
                    var actionType = action.GetType ();
                    actionPart = Accessor.Getter<object, global::Part> (actionType, "prt");
                    actionAction = Accessor.Getter<object, BaseAction> (actionType, "ba");
                }
                if (actionPart == null || actionAction == null)
                    continue;
                var part = actionPart (action);
                var baseAction = actionAction (action);
                if (part != null && baseAction != null)
                    result.Add (Tuple.Create (part, baseAction));
            }
            return result;
        }
    }
}
