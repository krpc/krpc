using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using MonitorAPI = KRPC.RasterPropMonitor.API.Monitor;
using PageAPI = KRPC.RasterPropMonitor.API.Page;

namespace KRPC.RasterPropMonitor
{
    /// <summary>
    /// A Raster Prop Monitor screen in an IVA, such as an MFD.
    /// Obtained by calling <see cref="RasterPropMonitor.Monitors"/>.
    /// </summary>
    /// <remarks>
    /// Raster Prop Monitor draws a monitor only while the player is in the IVA of its pod, or
    /// the IVA overlay is on. Reading another monitor's text or image updates it, in any camera
    /// mode.
    /// </remarks>
    [KRPCClass (Service = "RasterPropMonitor", GameScene = GameScene.Flight)]
    public class Monitor : Equatable<Monitor>, IGameObjectState
    {
        readonly SpaceCenter.Services.Parts.Part part;
        // KSP rebuilds a part's internal model, so the monitor is found again on each use,
        // by its prop and its place among the prop's monitor modules
        readonly int propID;
        readonly int index;

        internal Monitor (SpaceCenter.Services.Parts.Part innerPart, int innerPropID, int innerIndex)
        {
            part = innerPart;
            propID = innerPropID;
            index = innerIndex;
        }

        /// <summary>
        /// The monitors in the IVAs of a vessel's parts.
        /// </summary>
        internal static IList<Monitor> Of (global::Vessel vessel)
        {
            var monitors = new List<Monitor> ();
            foreach (var innerPart in vessel.parts) {
                var model = innerPart.internalModel;
                if (model == null)
                    continue;
                SpaceCenter.Services.Parts.Part wrapped = null;
                foreach (var prop in model.props) {
                    int count = 0;
                    foreach (var module in prop.internalModules) {
                        if (!MonitorAPI.Type.IsInstanceOfType (module))
                            continue;
                        if (wrapped == null)
                            wrapped = new SpaceCenter.Services.Parts.Part (innerPart);
                        monitors.Add (new Monitor (wrapped, prop.propID, count++));
                    }
                }
            }
            return monitors;
        }

        /// <summary>
        /// Check that the monitors are the same.
        /// </summary>
        public override bool Equals (Monitor other)
        {
            return !ReferenceEquals (other, null) && part == other.part && propID == other.propID && index == other.index;
        }

        /// <summary>
        /// Hash the monitor.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (part).And (propID).And (index);
        }

        /// <summary>
        /// The state of the monitor. It is dormant while its part has no IVA, as KSP builds
        /// the IVA again when the vessel becomes active.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = part.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                var model = part.InternalPart.internalModel;
                if (model == null)
                    return GameObjectState.Dormant;
                return Find (model) != null ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        InternalModule Find (InternalModel model)
        {
            var prop = model.props.FirstOrDefault (p => p.propID == propID);
            if (prop == null)
                return null;
            return prop.internalModules.Where (MonitorAPI.Type.IsInstanceOfType).Skip (index).FirstOrDefault ();
        }

        // The monitor module. Every member that reaches the mod goes through this
        InternalModule InternalMonitor {
            get {
                var model = part.InternalPart.internalModel;
                if (model == null)
                    throw new KRPC.Service.KRPC.ObjectNotLoadedException (
                        "The monitor is not loaded, as its part has no IVA. Only the active vessel has IVAs.");
                var monitor = Find (model);
                if (monitor == null)
                    throw new KRPC.Service.KRPC.ObjectDestroyedException ("The monitor no longer exists.");
                return monitor;
            }
        }

        // The started monitor module. A monitor that failed to start has no pages
        InternalModule StartedMonitor {
            get {
                var monitor = InternalMonitor;
                if (!MonitorAPI.StartupComplete (monitor))
                    throw new InvalidOperationException ("The monitor has not started");
                return monitor;
            }
        }

        // The frame in which each computer's variables, and each monitor's text and screen,
        // were last updated by us, by instance id
        static readonly Dictionary<int, int> variablesUpdated = new Dictionary<int, int> ();
        static readonly Dictionary<int, int> textUpdated = new Dictionary<int, int> ();
        static readonly Dictionary<int, int> screenUpdated = new Dictionary<int, int> ();

        // Whether the object was not yet updated in this frame, which marks it updated
        static bool Due (Dictionary<int, int> updated, UnityEngine.Object obj)
        {
            int frame;
            var id = obj.GetInstanceID ();
            if (updated.TryGetValue (id, out frame) && frame == Time.frameCount)
                return false;
            updated [id] = Time.frameCount;
            return true;
        }

        // The monitor module, with its text current and, when the screen is wanted, its screen.
        // RPM updates a monitor only while the player is in its pod's IVA, or the IVA overlay
        // is on. For any other monitor, we run the steps of RPM's own update ourselves, once a
        // frame: the computer's variables, the page text, and the screen.
        InternalModule CurrentMonitor (bool screen)
        {
            var monitor = StartedMonitor;
            if (MonitorAPI.ShouldUpdate (monitor.part))
                return monitor;
            var computer = MonitorAPI.Computer (monitor);
            if (Due (variablesUpdated, computer))
                MonitorAPI.UpdateVariables (computer);
            if (Due (textUpdated, monitor))
                MonitorAPI.FillScreenBuffer (monitor);
            if (screen && Due (screenUpdated, monitor))
                MonitorAPI.RenderScreen (monitor);
            return monitor;
        }

        /// <summary>
        /// The part the monitor is in.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part {
            get { return part; }
        }

        /// <summary>
        /// The name of the prop, e.g. <c>RasterPropMonitorBasicMFD</c>.
        /// </summary>
        [KRPCProperty]
        public string PropName {
            get { return InternalMonitor.internalProp.propName; }
        }

        /// <summary>
        /// The names of the pages, in the order of the prop's configuration.
        /// </summary>
        [KRPCProperty]
        public IList<string> Pages {
            get { return MonitorAPI.Pages (StartedMonitor).Cast<object> ().Select (PageAPI.Name).ToList (); }
        }

        /// <summary>
        /// Returns the name of the page the monitor shows, and sets it as a press of the page's
        /// button does. Raster Prop Monitor can refuse the switch, or redirect it to another
        /// page, as the prop's configuration sets. Throws an exception if the monitor has no page with
        /// the name.
        /// </summary>
        [KRPCProperty]
        public string ActivePage {
            get { return PageAPI.Name (MonitorAPI.ActivePage (StartedMonitor)); }
            set {
                var monitor = StartedMonitor;
                var page = MonitorAPI.Pages (monitor).Cast<object> ().FirstOrDefault (p => PageAPI.Name (p) == value);
                if (page == null)
                    throw new ArgumentException ("The monitor has no page named " + value, nameof (value));
                MonitorAPI.PageButtonClick (monitor, page);
            }
        }

        static IList<string> ButtonNames (InternalModule monitor)
        {
            var buttons = MonitorAPI.GlobalButtons (monitor);
            if (string.IsNullOrEmpty (buttons))
                return new List<string> ();
            return buttons.Split (',').Select (name => name.Trim ()).ToList ();
        }

        /// <summary>
        /// The names of the monitor's buttons, e.g. <c>button_UP</c>. A button's index in
        /// this list is its identifier. A name is empty where the configuration leaves a gap.
        /// </summary>
        /// <remarks>
        /// These are the buttons that the active page handles. The buttons that select pages
        /// are not in the list.
        /// </remarks>
        [KRPCProperty]
        public IList<string> Buttons {
            get { return ButtonNames (InternalMonitor); }
        }

        /// <summary>
        /// Press and release a button, as the player does. The release is on the next frame.
        /// Throws an exception if the monitor has no button with the identifier.
        /// </summary>
        /// <param name="id">The button's index in <see cref="Buttons"/>.</param>
        [KRPCMethod]
        public void PressButton (int id)
        {
            var monitor = StartedMonitor;
            var buttons = ButtonNames (monitor);
            if (id < 0 || id >= buttons.Count || buttons [id].Length == 0)
                throw new ArgumentException ("The monitor has no button " + id, nameof (id));
            MonitorAPI.GlobalButtonClick (monitor, id);
            int frame = Time.frameCount;
            throw new YieldException<Action> (() => ReleaseButton (id, frame));
        }

        void ReleaseButton (int id, int frame)
        {
            // The server can update more than once in a frame
            if (Time.frameCount == frame)
                throw new YieldException<Action> (() => ReleaseButton (id, frame));
            MonitorAPI.GlobalButtonRelease (StartedMonitor, id);
        }

        /// <summary>
        /// The width of the screen, in characters.
        /// </summary>
        [KRPCProperty]
        public int ScreenWidth {
            get { return MonitorAPI.ScreenWidth (InternalMonitor); }
        }

        /// <summary>
        /// The height of the screen, in lines.
        /// </summary>
        [KRPCProperty]
        public int ScreenHeight {
            get { return MonitorAPI.ScreenHeight (InternalMonitor); }
        }

        /// <summary>
        /// The width of the screen, in pixels.
        /// </summary>
        [KRPCProperty]
        public int ScreenPixelWidth {
            get { return MonitorAPI.ScreenPixelWidth (InternalMonitor); }
        }

        /// <summary>
        /// The height of the screen, in pixels.
        /// </summary>
        [KRPCProperty]
        public int ScreenPixelHeight {
            get { return MonitorAPI.ScreenPixelHeight (InternalMonitor); }
        }

        /// <summary>
        /// The width of a character, in pixels.
        /// </summary>
        [KRPCProperty]
        public int LetterWidth {
            get { return MonitorAPI.LetterWidth (InternalMonitor); }
        }

        /// <summary>
        /// The height of a character, in pixels.
        /// </summary>
        [KRPCProperty]
        public int LetterHeight {
            get { return MonitorAPI.LetterHeight (InternalMonitor); }
        }

        /// <summary>
        /// The color of text with no color tag, on the active page.
        /// </summary>
        [KRPCProperty]
        public ColorRGBA DefaultColor {
            get { return PageAPI.DefaultColor (MonitorAPI.ActivePage (StartedMonitor)).ToColorRGBA (); }
        }

        /// <summary>
        /// The text of the active page, as the screen shows it: one line per screen line,
        /// holding the characters Raster Prop Monitor draws. The tags it draws nothing for,
        /// such as colors, are removed.
        /// </summary>
        [KRPCProperty]
        public string Text {
            get {
                var monitor = CurrentMonitor (false);
                var page = MonitorAPI.ActivePage (monitor);
                return ScreenText.Draw (
                    PageAPI.ProcessedText (page), PageAPI.TextOverlay (page),
                    PageAPI.ScreenXMin (page), PageAPI.ScreenYMin (page),
                    MonitorAPI.LetterWidth (monitor), MonitorAPI.LetterHeight (monitor),
                    MonitorAPI.ScreenPixelWidth (monitor), MonitorAPI.ScreenPixelHeight (monitor),
                    PageAPI.FontCharacters (MonitorAPI.TextRenderer (monitor)).Keys);
            }
        }

        /// <summary>
        /// The text of the active page with its variables formatted, and Raster Prop
        /// Monitor's markup kept: colors <c>[#rrggbb]</c> and <c>[#rrggbbaa]</c>, nudges
        /// <c>[@xN]</c> and <c>[@yN]</c> in pixels, <c>[hw]</c> and <c>[dw]</c> for half and
        /// double width, <c>[sup]</c>, <c>[sub]</c> and <c>[fontN]</c>.
        /// </summary>
        /// <remarks>
        /// The page's text overlay, drawn over the text, is not included.
        /// </remarks>
        [KRPCProperty]
        public string RawText {
            get { return PageAPI.ProcessedText (MonitorAPI.ActivePage (CurrentMonitor (false))) ?? string.Empty; }
        }

        /// <summary>
        /// The names of the variables the active page shows, in the order of its text.
        /// </summary>
        [KRPCProperty]
        public IList<string> Variables {
            get { return API.VariableNames (PageAPI.Text (MonitorAPI.ActivePage (CurrentMonitor (false)))); }
        }

        /// <summary>
        /// The screen as an image, at <see cref="ScreenPixelWidth"/> by
        /// <see cref="ScreenPixelHeight"/> pixels.
        /// </summary>
        /// <param name="format">The format of the image.</param>
        /// <param name="quality">The quality of a JPEG image, from 1 to 100.</param>
        /// <returns>The image data.</returns>
        [KRPCMethod]
        public byte[] Image (ImageFormat format = ImageFormat.PNG, int quality = 75)
        {
            ImageEncoder.CheckQuality (format, quality);
            var screen = MonitorAPI.ScreenTexture (CurrentMonitor (true));
            if (screen == null || !screen.IsCreated ())
                throw new InvalidOperationException ("The monitor has not drawn its screen");
            var image = new Texture2D (screen.width, screen.height, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            try {
                RenderTexture.active = screen;
                image.ReadPixels (new Rect (0, 0, screen.width, screen.height), 0, 0);
                image.Apply ();
                return ImageEncoder.Encode (image, format, quality);
            } finally {
                RenderTexture.active = active;
                UnityEngine.Object.Destroy (image);
            }
        }
    }
}
