using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KRPC.Utils;
using UnityEngine;
using Logger = KRPC.Utils.Logger;

namespace KRPC.RasterPropMonitor
{
    // Binds to the members of Raster Prop Monitor 1.1.0.1 by reflection. The persistent
    // variables and the variable cache are internal to the mod.
    sealed class API : ModAPI
    {
        public API () : base (ServiceInfo.Mods.RasterPropMonitor.Name, ServiceInfo.Mods.RasterPropMonitor.Assembly)
        {
        }

        // The variable that props read a persistent variable through
        const string persistentPrefix = "PERSISTENT_";
        // The separator of a format string from its variables, in RPM page text
        const string variableListSeparator = "$&$";

        static Type computerType;

        // RasterPropMonitorComputer
        static Func<object, string, bool, bool> hasPersistentVariable;
        static Func<object, string, double, bool, double> getPersistentVariable;
        static Action<object, string, double, bool> setPersistentVariable;
        static Func<object, object> partVariables;
        static Func<object, object> variableCache;
        static Func<object, HashSet<string>> unrecognizedVariables;
        static Func<object, string, object> instantiateVariable;

        // RPMVesselComputer
        static Func<global::Vessel, object> vesselComputer;
        static Func<object, object> vesselVariables;

        // PersistentVariableCollection
        static Func<object, Dictionary<string, double>> persistentVariables;

        // VariableCollection
        static Func<object, string, object> cachedVariable;

        // VariableOrNumber
        static Action<object, object> updateVariable;
        static Func<object, object> variableValue;

        // StringProcessorFormatter
        static Func<IFormatProvider> formatProvider;

        // The members a Monitor reaches
        public static class Monitor
        {
            public static Type Type;
            public static Func<object, IList> Pages;
            public static Func<object, object> ActivePage;
            public static Action<object, object> PageButtonClick;
            public static Action<object, int> GlobalButtonClick;
            public static Action<object, int> GlobalButtonRelease;
            public static Func<object, string> GlobalButtons;
            public static Func<object, int> ScreenWidth;
            public static Func<object, int> ScreenHeight;
            public static Func<object, int> ScreenPixelWidth;
            public static Func<object, int> ScreenPixelHeight;
            public static Func<object, int> LetterWidth;
            public static Func<object, int> LetterHeight;
            public static Func<object, bool> StartupComplete;
            public static Func<object, RenderTexture> ScreenTexture;
            public static Func<object, object> TextRenderer;
            public static Func<global::Part, bool> ShouldUpdate;
            public static Func<object, PartModule> Computer;
            public static Action<object> FillScreenBuffer;
            public static Action<object> RenderScreen;
            public static Action<object> UpdateVariables;
        }

        public static class Camera
        {
            public static Type SelectorType;
            public static Func<object, string> Prefix;
            public static Func<object, int> Current;
            public static Func<object, float> MinFoV;
            public static Func<object, float> MaxFoV;
            public static Func<global::Part, float, object> NewFlyingCamera;
            public static Func<object, string, float, bool> PointCamera;
            public static Func<object, RenderTexture, float, float, bool> Render;
            public static Func<object, UnityEngine.Camera []> Cameras;
            public static Action<object> Cleanup;
        }

        public static class Button
        {
            public static Type Type;
            public static Func<object, IList> PageTriggers;
            public static Func<object, List<Action>> ClickHandlers;
            public static Func<object, Delegate> TriggerSelector;
            public static Action<object> MouseDown;
            public static Action<object> MouseUp;
        }

        public static class Page
        {
            public static Func<object, string> Name;
            public static Func<object, string> ProcessedText;
            public static Func<object, string> TextOverlay;
            public static Func<object, Color> DefaultColor;
            public static Func<object, int> ScreenXMin;
            public static Func<object, int> ScreenYMin;
            public static Func<object, string> Text;
            public static Func<object, MonoBehaviour> PageHandler;
            public static Func<object, MonoBehaviour> BackgroundHandler;
            public static Func<object, Dictionary<char, Rect>> FontCharacters;
        }

        protected override void Bind (ModBinder mod)
        {
            var computer = mod.Type ("JSI.RasterPropMonitorComputer");
            computerType = computer.Type;
            hasPersistentVariable = computer.Method<Func<object, string, bool, bool>> ("HasPersistentVariable");
            getPersistentVariable = computer.Method<Func<object, string, double, bool, double>> ("GetPersistentVariable");
            setPersistentVariable = computer.Method<Action<object, string, double, bool>> ("SetPersistentVariable");
            partVariables = computer.Getter<object, object> ("m_persistentVariables");
            variableCache = computer.Getter<object, object> ("variableCollection");
            unrecognizedVariables = computer.Getter<object, HashSet<string>> ("unrecognizedVariables");
            instantiateVariable = computer.Method<Func<object, string, object>> ("InstantiateVariableOrNumber");

            var vesselComputerType = mod.Type ("JSI.RPMVesselComputer");
            vesselComputer = vesselComputerType.Method<Func<global::Vessel, object>> ("Instance");
            vesselVariables = vesselComputerType.Getter<object, object> ("PersistentVariables");

            var collectionType = mod.Type ("JSI.Core.PersistentVariableCollection");
            persistentVariables = collectionType.Getter<object, Dictionary<string, double>> ("persistentVars");

            var cacheType = mod.Type ("JSI.VariableCollection");
            cachedVariable = cacheType.Method<Func<object, string, object>> ("GetVariable");

            var variableType = mod.Type ("JSI.VariableOrNumber");
            updateVariable = variableType.Method<Action<object, object>> ("Update");
            variableValue = variableType.Method<Func<object, object>> ("Get");

            var formatterType = mod.Type ("JSI.StringProcessorFormatter");
            formatProvider = formatterType.StaticGetter<IFormatProvider> ("fp");

            LoadMonitor (mod, computer);
        }

        static void LoadMonitor (ModBinder mod, MemberBinder computer)
        {
            var monitor = mod.Type ("JSI.RasterPropMonitor");
            Monitor.Type = monitor.Type;
            Monitor.Pages = monitor.Getter<object, IList> ("pages");
            Monitor.ActivePage = monitor.Getter<object, object> ("activePage");
            Monitor.PageButtonClick = monitor.Method<Action<object, object>> ("PageButtonClick");
            Monitor.GlobalButtonClick = monitor.Method<Action<object, int>> ("GlobalButtonClick");
            Monitor.GlobalButtonRelease = monitor.Method<Action<object, int>> ("GlobalButtonRelease");
            Monitor.GlobalButtons = monitor.Getter<object, string> ("globalButtons");
            Monitor.ScreenWidth = monitor.Getter<object, int> ("screenWidth");
            Monitor.ScreenHeight = monitor.Getter<object, int> ("screenHeight");
            Monitor.ScreenPixelWidth = monitor.Getter<object, int> ("screenPixelWidth");
            Monitor.ScreenPixelHeight = monitor.Getter<object, int> ("screenPixelHeight");
            Monitor.LetterWidth = monitor.Getter<object, int> ("fontLetterWidth");
            Monitor.LetterHeight = monitor.Getter<object, int> ("fontLetterHeight");
            Monitor.StartupComplete = monitor.Getter<object, bool> ("startupComplete");
            Monitor.ScreenTexture = monitor.Getter<object, RenderTexture> ("screenTexture");
            Monitor.TextRenderer = monitor.Getter<object, object> ("textRenderer");
            Monitor.ShouldUpdate = mod.Type ("JSI.JUtil").Method<Func<global::Part, bool>> ("RasterPropMonitorShouldUpdate");
            Monitor.Computer = monitor.Getter<object, PartModule> ("rpmComp");
            Monitor.FillScreenBuffer = monitor.Method<Action<object>> ("FillScreenBuffer");
            Monitor.RenderScreen = monitor.Method<Action<object>> ("RenderScreen");
            Monitor.UpdateVariables = computer.Method<Action<object>> ("UpdateVariables");

            var selector = mod.Type ("JSI.JSIExternalCameraSelector");
            Camera.SelectorType = selector.Type;
            Camera.Prefix = selector.Getter<object, string> ("cameraIDPrefix");
            Camera.Current = selector.Getter<object, int> ("current");
            Camera.MinFoV = selector.Getter<object, float> ("cameraFoVMin");
            Camera.MaxFoV = selector.Getter<object, float> ("cameraFoVMax");
            var flying = mod.Type ("JSI.FlyingCamera");
            Camera.NewFlyingCamera = flying.Constructor<Func<global::Part, float, object>> ();
            Camera.PointCamera = flying.Method<Func<object, string, float, bool>> ("PointCamera");
            Camera.Render = flying.Method<Func<object, RenderTexture, float, float, bool>> ("Render");
            Camera.Cameras = flying.Getter<object, UnityEngine.Camera []> ("cameraObject");
            Camera.Cleanup = flying.Method<Action<object>> ("CleanupCameraObjects");

            var button = mod.Type ("JSI.SmarterButton");
            Button.Type = button.Type;
            Button.PageTriggers = button.Getter<object, IList> ("pageTriggers");
            Button.ClickHandlers = button.Getter<object, List<Action>> ("clickHandlers");
            Button.TriggerSelector = mod.Type ("JSI.SmarterButton+PageTriggerSet").Getter<object, Delegate> ("selector");
            Button.MouseDown = button.Method<Action<object>> ("OnMouseDown");
            Button.MouseUp = button.Method<Action<object>> ("OnMouseUp");

            var page = mod.Type ("JSI.MonitorPage");
            Page.Name = page.Getter<object, string> ("name");
            Page.ProcessedText = page.Getter<object, string> ("ProcessedText");
            Page.TextOverlay = page.Getter<object, string> ("textOverlay");
            Page.DefaultColor = page.Getter<object, Color> ("defaultColor");
            Page.ScreenXMin = page.Getter<object, int> ("screenXMin");
            Page.ScreenYMin = page.Getter<object, int> ("screenYMin");
            Page.Text = page.Getter<object, string> ("text");
            Page.PageHandler = page.Getter<object, MonoBehaviour> ("pageHandlerModule");
            Page.BackgroundHandler = page.Getter<object, MonoBehaviour> ("backgroundHandlerModule");
            Page.FontCharacters = mod.Type ("JSI.TextRenderer").Getter<object, Dictionary<char, Rect>> ("fontCharacters");
        }

        /// <summary>
        /// Whether the object is an instance of a type of the mod. False for a type that was not
        /// found.
        /// </summary>
        public static bool IsInstance (Type type, object obj)
        {
            return type != null && type.IsInstanceOfType (obj);
        }

        /// <summary>
        /// The RPM computer module of a part, or null if it has none.
        /// </summary>
        public static PartModule Computer (global::Part part)
        {
            var modules = part.Modules;
            for (int i = 0; i < modules.Count; i++) {
                if (IsInstance (computerType, modules [i]))
                    return modules [i];
            }
            return null;
        }

        /// <summary>
        /// The RPM computer modules of a vessel's parts.
        /// </summary>
        public static IEnumerable<PartModule> Computers (global::Vessel vessel)
        {
            return vessel.parts.Select (Computer).Where (computer => computer != null);
        }

        static void CheckName (string name)
        {
            if (string.IsNullOrEmpty (name) || name.Trim ().Length == 0)
                throw new ArgumentException ("The name is empty", nameof (name));
        }

        /// <summary>
        /// The value of a persistent variable, of the part or else of the vessel when not
        /// global, or null if it is not set.
        /// </summary>
        public static double? PersistentVariable (PartModule computer, string name, bool global)
        {
            CheckName (name);
            if (!global && hasPersistentVariable (computer, name, false))
                return getPersistentVariable (computer, name, 0, false);
            if (hasPersistentVariable (computer, name, true))
                return getPersistentVariable (computer, name, 0, true);
            return null;
        }

        /// <summary>
        /// Set a persistent variable. RPM updates the props that show it.
        /// </summary>
        public static void SetPersistentVariable (PartModule computer, string name, double value, bool global)
        {
            CheckName (name);
            setPersistentVariable (computer, name, value, global);
        }

        /// <summary>
        /// Remove a persistent variable, and update the props that show it.
        /// </summary>
        public static void RemovePersistentVariable (PartModule computer, string name, bool global)
        {
            CheckName (name);
            var vessel = computer.vessel;
            var collection = global ? vesselVariables (vesselComputer (vessel)) : partVariables (computer);
            if (!persistentVariables (collection).Remove (name))
                return;
            // RPM has no removal, so we update the PERSISTENT_ variables as its setter does
            var computers = global ? Computers (vessel) : new [] { computer };
            var comp = vesselComputer (vessel);
            foreach (var other in computers) {
                var variable = cachedVariable (variableCache (other), persistentPrefix + name);
                if (variable != null)
                    updateVariable (variable, comp);
            }
        }

        /// <summary>
        /// The value of an RPM variable, a string or a double.
        /// Throws an exception if RPM does not recognize the name.
        /// </summary>
        public static object Variable (PartModule computer, string name)
        {
            var value = RawVariable (computer, name);
            var str = value as string;
            if (str != null)
                return str;
            return Convert.ToDouble (value, CultureInfo.InvariantCulture);
        }

        // The value as RPM gives it to a formatter: a string, or a number boxed as any numeric
        // type or a bool
        static object RawVariable (PartModule computer, string name)
        {
            CheckName (name);
            var variable = instantiateVariable (computer, name);
            // RPM caches an unrecognized name as a constant holding the name
            if (unrecognizedVariables (computer).Contains (name.Trim ()))
                throw new ArgumentException ("Raster Prop Monitor does not recognize the variable " + name.Trim (), nameof (name));
            updateVariable (variable, vesselComputer (computer.vessel));
            return variableValue (variable);
        }

        /// <summary>
        /// Text with its variables formatted, as an RPM page formats its lines.
        /// Throws an exception if RPM does not recognize a variable, or a format is invalid.
        /// </summary>
        public static string FormatVariables (PartModule computer, string text)
        {
            if (text == null)
                throw new ArgumentNullException (nameof (text));
            // A page with no variables shows its text as written
            if (text.IndexOf (variableListSeparator [0]) < 0)
                return text;
            var lines = text.Split ('\n');
            for (int i = 0; i < lines.Length; i++)
                lines [i] = FormatLine (computer, lines [i]);
            return string.Join ("\n", lines);
        }

        /// <summary>
        /// The names of the variables in page text, in order, as the page's formatters read
        /// them.
        /// </summary>
        public static IList<string> VariableNames (string text)
        {
            var names = new List<string> ();
            if (text == null)
                return names;
            foreach (var line in text.Split ('\n')) {
                if (line.IndexOf (variableListSeparator, StringComparison.Ordinal) < 0)
                    continue;
                var tokens = line.Split (new [] { variableListSeparator }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 2)
                    continue;
                foreach (var name in tokens [1].Split ((char [])null, StringSplitOptions.RemoveEmptyEntries)) {
                    if (!names.Contains (name))
                        names.Add (name);
                }
            }
            return names;
        }

        // A line of page text, as StringProcessorFormatter formats it
        static string FormatLine (PartModule computer, string line)
        {
            if (line.IndexOf (variableListSeparator, StringComparison.Ordinal) < 0)
                return line.TrimEnd ();
            var tokens = line.Split (new [] { variableListSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 2)
                throw new ArgumentException ("Invalid format string: " + line, "text");
            var names = tokens [1].Split ((char [])null, StringSplitOptions.RemoveEmptyEntries);
            var values = names.Select (name => RawVariable (computer, name)).ToArray ();
            try {
                return string.Format (formatProvider (), tokens [0].TrimEnd (), values);
            } catch (FormatException e) {
                throw new ArgumentException ("Invalid format string: " + line + ": " + e.Message, "text");
            }
        }
    }
}
