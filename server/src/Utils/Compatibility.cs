using System;
using System.Linq;
using PreFlightTests;
using UnityEngine;

namespace KRPC.Utils
{
    /// <summary>
    /// Utilities to aid in compatibility between different versions of KSP
    /// </summary>
    public static class Compatibility
    {
        /// <summary>
        /// Calls PopupDialog.SpawnPopupDialog
        /// </summary>
        public static PopupDialog SpawnPopupDialog (
            Vector2 anchorMin, Vector2 anchorMax, string dialogName, string title, string message,
            string buttonMessage, bool persistAcrossScenes, UISkinDef skin,
            bool isModal = true, string titleExtra = "") {
            if (Versioning.version_major * 100 + Versioning.version_minor >= 103) {
                // KSP 1.3.0 and up
                var method = typeof(PopupDialog).GetMethod("SpawnPopupDialog", new Type[] {
                    typeof(Vector2), typeof(Vector2), typeof(string), typeof(string), typeof(string),
                    typeof(string), typeof(bool), typeof(UISkinDef), typeof(bool), typeof(string)
                });
                return (PopupDialog)method.Invoke(null, new object[] {
                    anchorMin, anchorMax, dialogName, title, message, buttonMessage,
                    persistAcrossScenes, skin, isModal, titleExtra
                });
            } else {
                // KSP 1.2.2 and below
                var method = typeof(PopupDialog).GetMethod("SpawnPopupDialog", new Type[] {
                    typeof(Vector2), typeof(Vector2), typeof(string), typeof(string),
                    typeof(string), typeof(bool), typeof(UISkinDef), typeof(bool), typeof(string)
                });
                return (PopupDialog)method.Invoke(null, new object[] {
                    anchorMin, anchorMax, title, message, buttonMessage,
                    persistAcrossScenes, skin, isModal, titleExtra
                });
            }
        }

        /// <summary>
        /// Constructs a MultiOptionDialog
        /// </summary>
        public static MultiOptionDialog NewMultiOptionDialog (
            string name, string msg, string windowTitle, UISkinDef skin,
            params DialogGUIBase[] options) {
            if (Versioning.version_major * 100 + Versioning.version_minor >= 108)
            {
                // KSP 1.8.0 and up
                var ctor = typeof(MultiOptionDialog).GetConstructor(new Type[] {
                    typeof(string), typeof(string), typeof(string),
                    typeof(UISkinDef), typeof(DialogGUIBase[])
                });
                return (MultiOptionDialog)ctor.Invoke( new object[] {
                    name, msg, windowTitle, skin, options
                });
            } else if (Versioning.version_major * 100 + Versioning.version_minor >= 103) {
                // KSP 1.3.0 and up
                var ctor = typeof(MultiOptionDialog).GetConstructor(new Type[] {
                    typeof(string), typeof(string), typeof(string),
                    typeof(UISkinDef), typeof(DialogGUIBase[])
                });
                return (MultiOptionDialog)ctor.Invoke(null, new object[] {
                    name, msg, windowTitle, skin, options
                });
            } else {
                // KSP 1.2.2 and below
                var ctor = typeof(MultiOptionDialog).GetConstructor(new Type[] {
                    typeof(string), typeof(string),
                    typeof(UISkinDef), typeof(DialogGUIBase[])
                });
                return (MultiOptionDialog)ctor.Invoke(null, new object[] {
                    msg, windowTitle, skin, options
                });
            }
        }

        /// <summary>
        /// Create a LaunchSiteClear object
        /// </summary>
        public static LaunchSiteClear NewLaunchSiteClear(string launchSite, Game game)
        {
            if (Versioning.version_major * 100 + Versioning.version_minor >= 103) {
                // KSP 1.3.0 and up
                var ctor = typeof(LaunchSiteClear).GetConstructor(new Type[] {
                    typeof(string), typeof(string), typeof(Game)
                });
                return (LaunchSiteClear)ctor.Invoke(null, new object[] {
                    launchSite, launchSite, game
                });
            } else {
                // KSP 1.2.2 and below
                var ctor = typeof(LaunchSiteClear).GetConstructor(new Type[] {
                    typeof(string), typeof(Game)
                });
                return (LaunchSiteClear)ctor.Invoke(null, new object[] {
                    launchSite, game
                });
            }
        }

        /// <summary>
        /// Returns true if the given game mode is Game.Modes.Mission
        /// </summary>
        public static bool GameModeIsMission(Game.Modes mode)
        {
            if (Versioning.version_major * 100 + Versioning.version_minor < 104)
                // Below KSP 1.4.0
                return false;
            // KSP 1.4.0 and up
            return mode.ToString() == "MISSION";
        }

        /// <summary>
        /// Returns true if the given game mode is Game.Modes.MissionBuilder
        /// </summary>
        public static bool GameModeIsMissionBuilder(Game.Modes mode)
        {
            if (Versioning.version_major * 100 + Versioning.version_minor < 104)
                // Below KSP 1.4.0
                return false;
            // KSP 1.4.0 and up
            return mode.ToString() == "MISSION_BUILDER";
        }

        /// <summary>
        /// Returns true if the given game scene is GameScenes.MissionBuilder
        /// </summary>
        public static bool GameSceneIsMissionBuilder(GameScenes scene)
        {
            if (Versioning.version_major * 100 + Versioning.version_minor < 104)
                // Below KSP 1.4.0
                return false;
            // KSP 1.4.0 and up
            return scene.ToString() == "MISSIONBUILDER";
        }

        /// <summary>
        /// Methods mimicking ModuleDecouplerBase that can be used in KSP 1.6.1 and below
        /// Uses reflection to call these methods, as the name of the base class changed in KSP 1.7
        /// </summary>
        public class ModuleDecoupler
        {
            private object decoupler;
            private Type type;

            /// <summary>
            /// Create a decoupler part module wrapper for the given part. Part must have a decoupler part module.
            /// </summary>
            public ModuleDecoupler(global::Part part)
            {
                var moduleDecouple = part.Modules.OfType<ModuleDecouple>().FirstOrDefault();
                if (moduleDecouple != null) {
                    decoupler = moduleDecouple;
                } else {
                    var moduleAnchoredDecoupler = part.Modules.OfType<ModuleAnchoredDecoupler>().FirstOrDefault();
                    decoupler = moduleAnchoredDecoupler;
                }
                if (decoupler != null)
                    type = decoupler.GetType();
            }

            /// <summary>
            /// Get the intance of the part module that this object is wrapping.
            /// </summary>
            public object Instance {
                get { return decoupler; }
            }

            /// <summary>
            /// Returns true if the decoupler is enabled
            /// </summary>
            public bool IsEnabled
            {
                get { return (bool)type.GetField("isEnabled").GetValue(decoupler); }
            }

            /// <summary>
            /// Returns true if the decoupler is decoupled
            /// </summary>
            public bool IsDecoupled
            {
                get { return (bool)type.GetField("isDecoupled").GetValue(decoupler); }
            }

            /// <summary>
            /// Decouples the decoupler
            /// </summary>
            public void Decouple()
            {
                type.GetMethod("Decouple").Invoke(decoupler, null);
            }

            /// <summary>
            /// Returns true if staging is enabled for the decoupler
            /// </summary>
            public bool StagingEnabled
            {
                get { return (bool)type.GetMethod("StagingEnabled").Invoke(decoupler, null); }
            }

            /// <summary>
            /// Returns the ejection force of the decoupler in kN
            /// </summary>
            public float EjectionForce
            {
                get { return (float)type.GetField("ejectionForce").GetValue(decoupler); }
            }

            /// <summary>
            /// Returns the attachment node for the decoupler explosive
            /// </summary>
            public AttachNode ExplosiveNode
            {
                get { return (AttachNode)type.GetProperty("ExplosiveNode").GetValue(decoupler, null); }
            }

            /// <summary>
            /// Returns true if the decoupler is an omni decoupler.
            /// Always returns false for anchored decouplers.
            /// </summary>
            public bool IsOmniDecoupler
            {
                get {
                    if (type.Name == "ModuleDecouple")
                        return (bool)type.GetField("isOmniDecoupler").GetValue(decoupler);
                    return false;
                }
            }
        }
    }
}
