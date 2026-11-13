using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Utils;
using UnityEngine;
using ButtonAPI = KRPC.RasterPropMonitor.API.Button;
using MonitorAPI = KRPC.RasterPropMonitor.API.Monitor;
using PageAPI = KRPC.RasterPropMonitor.API.Page;

namespace KRPC.RasterPropMonitor
{
    // The page buttons a script has taken from RPM, per monitor. A captured button shows the
    // page the script chose, in place of the pages RPM assigns it.
    //
    // RPM's SmarterButton runs a page trigger per monitor on a click, which calls the
    // monitor's PageButtonClick. We take the monitor's trigger out of the button, and add a
    // click handler of our own. KSP builds the IVA again on vessel switch and IVA entry, with
    // new buttons, so the addon below applies the captures again to the new buttons.
    sealed class ButtonCaptures
    {
        static readonly Dictionary<Tuple<uint, int, int>, ButtonCaptures> monitors =
            new Dictionary<Tuple<uint, int, int>, ButtonCaptures> ();

        sealed class Applied
        {
            public Component Button;
            public InternalModule Monitor;
            public List<object> Triggers;
            public Action Handler;
        }

        readonly Dictionary<string, string> pages = new Dictionary<string, string> ();
        readonly Dictionary<string, int> presses = new Dictionary<string, int> ();
        readonly Dictionary<string, Applied> applied = new Dictionary<string, Applied> ();

        public static ButtonCaptures Of (uint flightID, int propID, int index)
        {
            var key = Tuple.Create (flightID, propID, index);
            ButtonCaptures captures;
            if (!monitors.TryGetValue (key, out captures)) {
                captures = new ButtonCaptures ();
                monitors [key] = captures;
            }
            return captures;
        }

        /// <summary>
        /// The page buttons of a monitor, by name, in the order of the prop's transforms. A
        /// captured button is one of them.
        /// </summary>
        public IList<string> PageButtons (InternalModule monitor)
        {
            return Buttons (monitor)
                .Where (button => IsCaptured (button, monitor) || Triggers (button, monitor).Any ())
                .Select (button => button.gameObject.name)
                .Distinct ()
                .ToList ();
        }

        /// <summary>
        /// The page button of the monitor with the name, or null if there is none.
        /// </summary>
        public Component PageButton (InternalModule monitor, string name)
        {
            return Buttons (monitor).FirstOrDefault (button => button.gameObject.name == name &&
                (IsCaptured (button, monitor) || Triggers (button, monitor).Any ()));
        }

        public int Presses (string name)
        {
            int count;
            return presses.TryGetValue (name, out count) ? count : 0;
        }

        public void Capture (InternalModule monitor, string name, string page)
        {
            pages [name] = page;
            Apply (monitor, name);
        }

        public void Release (string name)
        {
            pages.Remove (name);
            Applied capture;
            if (!applied.TryGetValue (name, out capture))
                return;
            applied.Remove (name);
            // A button of a rebuilt IVA is new, and has RPM's triggers
            if (capture.Button == null)
                return;
            ButtonAPI.ClickHandlers (capture.Button).Remove (capture.Handler);
            var triggers = ButtonAPI.PageTriggers (capture.Button);
            foreach (var trigger in capture.Triggers)
                triggers.Add (trigger);
        }

        /// <summary>
        /// Apply the captures to the buttons of the active vessel's IVA, where KSP has built
        /// it again since they were applied.
        /// </summary>
        public static void ApplyAll ()
        {
            if (monitors.Count == 0)
                return;
            var vessel = FlightGlobals.ActiveVessel;
            if (vessel == null)
                return;
            foreach (var entry in monitors) {
                var captures = entry.Value;
                if (captures.pages.Count == 0)
                    continue;
                var key = entry.Key;
                var part = vessel.parts.FirstOrDefault (p => p.flightID == key.Item1);
                if (part == null || part.internalModel == null)
                    continue;
                var monitor = Monitor.Find (part.internalModel, key.Item2, key.Item3);
                if (monitor == null || !MonitorAPI.StartupComplete (monitor))
                    continue;
                foreach (var name in captures.pages.Keys.ToList ())
                    captures.Apply (monitor, name);
            }
        }

        void Apply (InternalModule monitor, string name)
        {
            Applied capture;
            if (applied.TryGetValue (name, out capture) && capture.Button != null && capture.Monitor == monitor)
                return;
            var button = Buttons (monitor).FirstOrDefault (b => b.gameObject.name == name);
            if (button == null)
                return;
            var triggers = Triggers (button, monitor).ToList ();
            var pageTriggers = ButtonAPI.PageTriggers (button);
            foreach (var trigger in triggers)
                pageTriggers.Remove (trigger);
            Action handler = () => Press (monitor, name);
            ButtonAPI.ClickHandlers (button).Add (handler);
            applied [name] = new Applied { Button = button, Monitor = monitor, Triggers = triggers, Handler = handler };
        }

        void Press (InternalModule monitor, string name)
        {
            presses [name] = Presses (name) + 1;
            string pageName;
            if (!pages.TryGetValue (name, out pageName))
                return;
            var page = MonitorAPI.Pages (monitor).Cast<object> ().FirstOrDefault (p => PageAPI.Name (p) == pageName);
            if (page != null)
                MonitorAPI.PageButtonClick (monitor, page);
        }

        bool IsCaptured (Component button, InternalModule monitor)
        {
            Applied capture;
            return applied.TryGetValue (button.gameObject.name, out capture) &&
                ReferenceEquals (capture.Button, button) && ReferenceEquals (capture.Monitor, monitor);
        }

        // The buttons of the monitor's prop
        static IEnumerable<Component> Buttons (InternalModule monitor)
        {
            if (ButtonAPI.Type == null)
                return new Component [0];
            return monitor.internalProp.GetComponentsInChildren (ButtonAPI.Type, true);
        }

        // The button's page triggers that switch the monitor's pages
        static IEnumerable<object> Triggers (Component button, InternalModule monitor)
        {
            return ButtonAPI.PageTriggers (button).Cast<object> ()
                .Where (trigger => ReferenceEquals (ButtonAPI.TriggerSelector (trigger).Target, monitor));
        }
    }

    /// <summary>
    /// Applies the button captures to the IVA that KSP builds again on vessel switch and IVA
    /// entry.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class ButtonCaptureAddon : MonoBehaviour
    {
        /// <summary>
        /// Apply the captures to a rebuilt IVA.
        /// </summary>
        public void Update ()
        {
            if (ModAPI.Get<API> ().IsAvailable)
                ButtonCaptures.ApplyAll ();
        }
    }
}
