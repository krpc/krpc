using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that warns when a loaded save or craft has name tags stored by a name tag part
    /// module that the parts in this install do not carry. KSP drops a saved module that does
    /// not match a module on the part, so these tags are lost the next time the game or craft
    /// is saved.
    /// </summary>
    /// <remarks>
    /// A <c>KOSNameTag</c> node is kept when the parts carry <c>ModuleNameTag</c> and no
    /// <c>KOSNameTag</c> type is loaded, as KSPCommunityPartModules renames the node on load.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.Instantly, true)]
    public sealed class NameTagWarningAddon : MonoBehaviour
    {
        string activeModuleName;
        bool activeModuleNameFound;
        readonly HashSet<string> warnedSaves = new HashSet<string> ();

        /// <summary>
        /// Wake the addon.
        /// </summary>
        public void Awake ()
        {
            DontDestroyOnLoad (this);
            GameEvents.onGameStateLoad.Add (OnGameStateLoad);
            GameEvents.onEditorLoad.Add (OnEditorLoad);
        }

        /// <summary>
        /// Destroy the addon.
        /// </summary>
        public void OnDestroy ()
        {
            GameEvents.onGameStateLoad.Remove (OnGameStateLoad);
            GameEvents.onEditorLoad.Remove (OnEditorLoad);
        }

        void OnGameStateLoad (ConfigNode game)
        {
            // Warn once per save per session
            if (game == null || warnedSaves.Contains (HighLogic.SaveFolder))
                return;
            var flightState = game.GetNode ("FLIGHTSTATE");
            if (flightState == null)
                return;
            var lost = 0;
            foreach (var vessel in flightState.GetNodes ("VESSEL"))
                lost += CountLost (vessel);
            if (lost == 0)
                return;
            warnedSaves.Add (HighLogic.SaveFolder);
            Warn (lost, "this save", "the game is saved");
        }

        void OnEditorLoad (ShipConstruct ship, CraftBrowserDialog.LoadType loadType)
        {
            var craft = ShipConstruction.ShipConfig;
            if (craft == null)
                return;
            var lost = CountLost (craft);
            if (lost > 0)
                Warn (lost, "this craft", "the craft is saved");
        }

        int CountLost (ConfigNode partsNode)
        {
            var lost = 0;
            foreach (var part in partsNode.GetNodes ("PART")) {
                foreach (var module in part.GetNodes ("MODULE")) {
                    var name = module.GetValue ("name");
                    if (name != NameTag.ModuleName && name != NameTag.LegacyModuleName)
                        continue;
                    if (string.IsNullOrEmpty (module.GetValue (NameTag.FieldName)))
                        continue;
                    if (!IsKept (name))
                        lost++;
                }
            }
            return lost;
        }

        bool IsKept (string name)
        {
            var active = ActiveModuleName;
            if (name == active)
                return true;
            return name == NameTag.LegacyModuleName && active == NameTag.ModuleName &&
                AssemblyLoader.GetClassByName (typeof (PartModule), NameTag.LegacyModuleName) == null;
        }

        /// <summary>
        /// The name of the name tag part module on the part prefabs, or null if they have none.
        /// </summary>
        string ActiveModuleName {
            get {
                if (!activeModuleNameFound) {
                    foreach (var info in PartLoader.LoadedPartsList) {
                        if (info.partPrefab == null)
                            continue;
                        var module = NameTag.Module (info.partPrefab);
                        if (module != null) {
                            activeModuleName = module.GetType ().Name;
                            break;
                        }
                    }
                    activeModuleNameFound = true;
                }
                return activeModuleName;
            }
        }

        void Warn (int lost, string source, string when)
        {
            var tags = lost == 1 ? "1 part name tag in " + source + " is" : lost + " part name tags in " + source + " are";
            var message = tags + " stored by a name tag part module that is not on the parts in this install. " +
                "They will be lost when " + when + ".";
            if (ActiveModuleName == null)
                message += " Install KSPCommunityPartModules 0.5 or later to keep them.";
            KRPC.Utils.Logger.WriteLine (message, KRPC.Utils.Logger.Severity.Warning);
            PopupDialog.SpawnPopupDialog (
                new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), "krpc-name-tag-warning", "kRPC Name Tags",
                message, "OK", true, HighLogic.UISkin);
        }
    }
}
