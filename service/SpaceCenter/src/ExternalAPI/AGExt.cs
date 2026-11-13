using System;
using System.Collections;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    sealed class AGExt : ModAPI
    {
        static Func<uint, int, object> groupActions;
        static Func<object, global::Part> actionPart;
        static Func<object, BaseAction> actionAction;

        public AGExt () : base (ServiceInfo.Mods.AGExt.Name, ServiceInfo.Mods.AGExt.Assembly)
        {
        }

        /// <summary>
        /// Whether AGExt is installed, so that it replaces the stock action groups. Throws an
        /// exception if AGExt is installed and not available.
        /// </summary>
        public static bool Installed {
            get { return Get<AGExt> ().CheckInstalled (); }
        }

        protected override void Bind (ModBinder mod)
        {
            mod.Statics (typeof (AGExt), "ActionGroupsExtended.AGExtExternal");
            groupActions = mod.Type ("ActionGroupsExtended.AGExtExternal").Method<Func<uint, int, object>> ("AGX2VslGroupActions");
        }

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
                    var actionType = new MemberBinder (ServiceInfo.Mods.AGExt.Name, action.GetType ());
                    actionPart = actionType.Getter<object, global::Part> ("prt");
                    actionAction = actionType.Getter<object, BaseAction> ("ba");
                }
                var part = actionPart (action);
                var baseAction = actionAction (action);
                if (part != null && baseAction != null)
                    result.Add (Tuple.Create (part, baseAction));
            }
            return result;
        }
    }
}
