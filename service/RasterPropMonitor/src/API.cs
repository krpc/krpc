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
    static class API
    {
        const string assemblyName = "RasterPropMonitor";
        // The variable that props read a persistent variable through
        const string persistentPrefix = "PERSISTENT_";
        // The separator of a format string from its variables, in RPM page text
        const string variableListSeparator = "$&$";

        static readonly List<string> missing = new List<string> ();

        public static bool IsAvailable { get; private set; }

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
            public static Type FlyingCameraType;
            public static Func<object, string, float, bool> PointCamera;
            public static Func<object, RenderTexture, float, float, bool> Render;
            public static Func<object, UnityEngine.Camera []> Cameras;
            public static Action<object> Cleanup;
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

        public static void Load ()
        {
            IsAvailable = false;
            var loaded = AssemblyLoader.loadedAssemblies.FirstOrDefault (a => a.assembly.GetName ().Name == assemblyName);
            if (loaded == null) {
                Logger.WriteLine ("Load API: " + assemblyName + " not found; skipping");
                return;
            }
            var assembly = loaded.assembly;
            missing.Clear ();

            computerType = Type (assembly, "JSI.RasterPropMonitorComputer");
            hasPersistentVariable = Method<Func<object, string, bool, bool>> (computerType, "HasPersistentVariable");
            getPersistentVariable = Method<Func<object, string, double, bool, double>> (computerType, "GetPersistentVariable");
            setPersistentVariable = Method<Action<object, string, double, bool>> (computerType, "SetPersistentVariable");
            partVariables = Field<object> (computerType, "m_persistentVariables");
            variableCache = Field<object> (computerType, "variableCollection");
            unrecognizedVariables = Field<HashSet<string>> (computerType, "unrecognizedVariables");
            instantiateVariable = Method<Func<object, string, object>> (computerType, "InstantiateVariableOrNumber");

            var vesselComputerType = Type (assembly, "JSI.RPMVesselComputer");
            vesselComputer = Method<Func<global::Vessel, object>> (vesselComputerType, "Instance");
            vesselVariables = Field<object> (vesselComputerType, "PersistentVariables");

            var collectionType = Type (assembly, "JSI.Core.PersistentVariableCollection");
            persistentVariables = Field<Dictionary<string, double>> (collectionType, "persistentVars");

            var cacheType = Type (assembly, "JSI.VariableCollection");
            cachedVariable = Method<Func<object, string, object>> (cacheType, "GetVariable");

            var variableType = Type (assembly, "JSI.VariableOrNumber");
            updateVariable = Method<Action<object, object>> (variableType, "Update");
            variableValue = Method<Func<object, object>> (variableType, "Get");

            var formatterType = Type (assembly, "JSI.StringProcessorFormatter");
            formatProvider = formatterType == null ? null : Accessor.StaticGetter<IFormatProvider> (formatterType, "fp");
            if (formatProvider == null)
                missing.Add ("StringProcessorFormatter.fp");

            LoadMonitor (assembly);

            if (missing.Count > 0) {
                Logger.WriteLine ("Load API: " + assemblyName + " members not found: " +
                                  string.Join (", ", missing.ToArray ()) + "; skipping", Logger.Severity.Error);
                return;
            }
            IsAvailable = true;
            Logger.WriteLine ("Load API: Successfully loaded " + assemblyName + " version " + assembly.GetName ().Version);
        }

        static void LoadMonitor (System.Reflection.Assembly assembly)
        {
            var monitor = Monitor.Type = Type (assembly, "JSI.RasterPropMonitor");
            Monitor.Pages = Field<IList> (monitor, "pages");
            Monitor.ActivePage = Field<object> (monitor, "activePage");
            Monitor.PageButtonClick = Method<Action<object, object>> (monitor, "PageButtonClick");
            Monitor.GlobalButtonClick = Method<Action<object, int>> (monitor, "GlobalButtonClick");
            Monitor.GlobalButtonRelease = Method<Action<object, int>> (monitor, "GlobalButtonRelease");
            Monitor.GlobalButtons = Field<string> (monitor, "globalButtons");
            Monitor.ScreenWidth = Field<int> (monitor, "screenWidth");
            Monitor.ScreenHeight = Field<int> (monitor, "screenHeight");
            Monitor.ScreenPixelWidth = Field<int> (monitor, "screenPixelWidth");
            Monitor.ScreenPixelHeight = Field<int> (monitor, "screenPixelHeight");
            Monitor.LetterWidth = Field<int> (monitor, "fontLetterWidth");
            Monitor.LetterHeight = Field<int> (monitor, "fontLetterHeight");
            Monitor.StartupComplete = Field<bool> (monitor, "startupComplete");
            Monitor.ScreenTexture = Field<RenderTexture> (monitor, "screenTexture");
            Monitor.TextRenderer = Field<object> (monitor, "textRenderer");
            Monitor.ShouldUpdate = Method<Func<global::Part, bool>> (Type (assembly, "JSI.JUtil"), "RasterPropMonitorShouldUpdate");
            Monitor.Computer = Field<PartModule> (monitor, "rpmComp");
            Monitor.FillScreenBuffer = Method<Action<object>> (monitor, "FillScreenBuffer");
            Monitor.RenderScreen = Method<Action<object>> (monitor, "RenderScreen");
            Monitor.UpdateVariables = Method<Action<object>> (computerType, "UpdateVariables");

            var selector = Camera.SelectorType = Type (assembly, "JSI.JSIExternalCameraSelector");
            Camera.Prefix = Field<string> (selector, "cameraIDPrefix");
            Camera.Current = Field<int> (selector, "current");
            Camera.MinFoV = Field<float> (selector, "cameraFoVMin");
            Camera.MaxFoV = Field<float> (selector, "cameraFoVMax");
            var flying = Camera.FlyingCameraType = Type (assembly, "JSI.FlyingCamera");
            Camera.PointCamera = Method<Func<object, string, float, bool>> (flying, "PointCamera");
            Camera.Render = Method<Func<object, RenderTexture, float, float, bool>> (flying, "Render");
            Camera.Cameras = Field<UnityEngine.Camera []> (flying, "cameraObject");
            Camera.Cleanup = Method<Action<object>> (flying, "CleanupCameraObjects");

            var page = Type (assembly, "JSI.MonitorPage");
            Page.Name = Field<string> (page, "name");
            Page.ProcessedText = Field<string> (page, "ProcessedText");
            Page.TextOverlay = Field<string> (page, "textOverlay");
            Page.DefaultColor = Field<Color> (page, "defaultColor");
            Page.ScreenXMin = Field<int> (page, "screenXMin");
            Page.ScreenYMin = Field<int> (page, "screenYMin");
            Page.Text = Field<string> (page, "text");
            Page.PageHandler = Field<MonoBehaviour> (page, "pageHandlerModule");
            Page.BackgroundHandler = Field<MonoBehaviour> (page, "backgroundHandlerModule");
            Page.FontCharacters = Field<Dictionary<char, Rect>> (Type (assembly, "JSI.TextRenderer"), "fontCharacters");
        }

        static Type Type (System.Reflection.Assembly assembly, string name)
        {
            var type = assembly.GetType (name);
            if (type == null)
                missing.Add (name);
            return type;
        }

        static TDelegate Method<TDelegate> (Type type, string name) where TDelegate : class
        {
            var method = type == null ? null : Accessor.Method<TDelegate> (type, name);
            if (method == null)
                missing.Add ((type == null ? "?" : type.Name) + "." + name);
            return method;
        }

        static Func<object, T> Field<T> (Type type, string name)
        {
            var getter = type == null ? null : Accessor.Getter<object, T> (type, name);
            if (getter == null)
                missing.Add ((type == null ? "?" : type.Name) + "." + name);
            return getter;
        }

        /// <summary>
        /// The RPM computer module of a part, or null if it has none.
        /// </summary>
        public static PartModule Computer (global::Part part)
        {
            var modules = part.Modules;
            for (int i = 0; i < modules.Count; i++) {
                if (computerType.IsInstanceOfType (modules [i]))
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
