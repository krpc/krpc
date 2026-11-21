using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using HarmonyLib;
using KRPC.Service;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Testing
{
    /// <summary>
    /// Procedures used by the KSA integration tests. Installed only for testing.
    /// </summary>
    [KRPCService]
    public static class TestingTools
    {
        /// <summary>
        /// Add a vehicle to the game's default vehicles, from a folder holding its
        /// <c>vehicle.xml</c> and <c>meta.toml</c>, so that it can be launched.
        /// </summary>
        /// <param name="directory">Path of the folder.</param>
        /// <returns>The name of the vehicle.</returns>
        [KRPCProcedure]
        public static string RegisterVehicle (string directory)
        {
            var save = UncompressedVehicleSave.FromDirectory (directory, defaultVehicle: true);
            if (save == null)
                throw new ArgumentException ("No vehicle in '" + directory + "'");
            return save.Id;
        }

        /// <summary>
        /// Add a fuel link between two parts of a vehicle, with propellant flowing either way.
        /// </summary>
        /// <param name="partA">One part.</param>
        /// <param name="partB">The other part.</param>
        [KRPCProcedure]
        public static void AddFuelLink (KRPC.KSA.Services.Part partA, KRPC.KSA.Services.Part partB)
        {
            var a = partA.InternalPart.FullPart;
            var b = partB.InternalPart.FullPart;
            if (!a.Tree.FuelLinks.Add (a, b, FuelLinkFlow.Both))
                throw new InvalidOperationException ("Failed to add the fuel link");
        }

        /// <summary>
        /// The number of objects in the server's object store.
        /// </summary>
        [KRPCProperty]
        public static int ObjectCount {
            get { return (int)objectCount.GetValue (objectStore.GetValue (null)); }
        }

        // The object store, which the core does not expose
        static readonly Type objectStoreType = typeof (GameState).Assembly.GetType ("KRPC.Service.ObjectStore");
        static readonly PropertyInfo objectStore = objectStoreType.GetProperty ("Instance");
        static readonly PropertyInfo objectCount = objectStoreType.GetProperty ("Count");

        /// <summary>
        /// Close the game's update available pop-up, and any shown later.
        /// </summary>
        /// <remarks>
        /// The pop-up holds the keyboard, and the game clears the control inputs of the
        /// controlled vehicle each frame while the keyboard is held.
        /// </remarks>
        [KRPCProcedure]
        public static void SuppressUpdatePopup ()
        {
            if (harmony == null) {
                harmony = new Harmony ("kRPC.TestingTools");
                harmony.Patch (AccessTools.Method (typeof (Popup), nameof (Popup.DrawAll)),
                    prefix: new HarmonyMethod (typeof (TestingTools), nameof (CloseUpdatePopups)));
            }
            CloseUpdatePopups ();
        }

        static Harmony harmony;

        // The open pop-ups, which the game does not expose
        static readonly FieldInfo popups = AccessTools.Field (typeof (Popup), "Popups");

        static void CloseUpdatePopups ()
        {
            ((List<Popup>)popups.GetValue (null)).RemoveAll (popup => popup is UpdateAvailablePopup);
        }

        /// <summary>
        /// The RPCs that use game members missing from the running game, found whatever the
        /// game version, and the RPCs that need a game patch that failed to apply.
        /// </summary>
        [KRPCProperty]
        public static IList<string> CompatibilityBreakages {
            get { return GameCompatibility.FindBreakages ().Union (GameCompatibility.PatchBreakages).ToList (); }
        }

        /// <summary>
        /// The text of the messages on screen.
        /// </summary>
        [KRPCProperty]
        public static IList<string> Messages {
            get {
                var result = new List<string> ();
                foreach (var alert in (List<TimedAlert>)alerts.GetValue (null))
                    result.Add ((string)alertText.GetValue (alert));
                return result;
            }
        }

        // The messages on screen, which the game does not expose
        static readonly FieldInfo alerts = AccessTools.Field (typeof (TimedAlert), "All");
        static readonly FieldInfo alertText = AccessTools.Field (typeof (TimedAlert), "_displayName");

        /// <summary>
        /// Click a button with the mouse.
        /// </summary>
        /// <returns>Whether the button has been drawn, and so was clicked.</returns>
        [KRPCProcedure]
        public static bool ClickButton (KRPC.KSA.Services.Button button)
        {
            return Click (button, 0.5f);
        }

        /// <summary>
        /// Click a toggle with the mouse.
        /// </summary>
        /// <returns>Whether the toggle has been drawn, and so was clicked.</returns>
        [KRPCProcedure]
        public static bool ClickToggle (KRPC.KSA.Services.Toggle toggle)
        {
            return Click (toggle, 0.5f);
        }

        /// <summary>
        /// Click a slider with the mouse, a fraction of the way along it. The widget includes
        /// the label, so the fraction is of the slider only when the label is empty.
        /// </summary>
        /// <returns>Whether the slider has been drawn, and so was clicked.</returns>
        [KRPCProcedure]
        public static bool ClickSlider (KRPC.KSA.Services.Slider slider, float fraction)
        {
            return Click (slider, fraction);
        }

        /// <summary>
        /// Click an input field with the mouse and type a text.
        /// </summary>
        /// <returns>Whether the field has been drawn, and so was typed in.</returns>
        [KRPCProcedure]
        public static bool TypeText (KRPC.KSA.Services.InputField field, string text)
        {
            if (!Click (field, 0.5f))
                return false;
            ImGui.GetIO ().AddInputCharactersUTF8 (text);
            return true;
        }

        /// <summary>
        /// Click a dropdown with the mouse, which opens or closes its list.
        /// </summary>
        /// <returns>Whether the dropdown has been drawn, and so was clicked.</returns>
        [KRPCProcedure]
        public static bool ClickDropdown (KRPC.KSA.Services.Dropdown dropdown)
        {
            return Click (dropdown, 0.5f);
        }

        /// <summary>
        /// Click an option in the open list of a dropdown with the mouse.
        /// </summary>
        /// <returns>Whether the list has been drawn, and so the option was clicked.</returns>
        [KRPCProcedure]
        public static bool ClickDropdownOption (KRPC.KSA.Services.Dropdown dropdown, int index)
        {
            if (index >= dropdown.OptionCenters.Count)
                return false;
            Click (dropdown.OptionCenters [index]);
            return true;
        }

        // Click a fraction of the way along a widget, at the middle of its height
        static bool Click (KRPC.KSA.Services.Widget widget, float fraction)
        {
            var rect = widget.ScreenRect;
            if (rect == null)
                return false;
            var (min, max) = rect;
            Click (new float2 (min.X + fraction * (max.X - min.X), (min.Y + max.Y) / 2));
            return true;
        }

        // ImGui takes the queued events one per frame
        static void Click (float2 position)
        {
            var io = ImGui.GetIO ();
            io.AddMousePosEvent (position.X, position.Y);
            io.AddMouseButtonEvent (0, true);
            io.AddMouseButtonEvent (0, false);
        }

        /// <summary>
        /// Quit the game at the end of the frame.
        /// </summary>
        [KRPCProcedure]
        public static void Quit ()
        {
            Program.Instance.RequestExit (0);
        }
    }
}
