using System;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
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
            if (Versioning.version_major * 100 + Versioning.version_minor >= 103) {
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
            if (Versioning.version_major * 100 + Versioning.version_minor >= 103)
            {
                // KSP 1.3.0 and up
                var ctor = typeof(LaunchSiteClear).GetConstructor(new Type[] {
                    typeof(string), typeof(Game)
                });
                return (LaunchSiteClear)ctor.Invoke(null, new object[] {
                    launchSite, game
                });
            }
            else {
                // KSP 1.2.2 and below
                var ctor = typeof(LaunchSiteClear).GetConstructor(new Type[] {
                    typeof(string), typeof(string), typeof(Game)
                });
                return (LaunchSiteClear)ctor.Invoke(null, new object[] {
                    launchSite, launchSite, game
                });
            }
        }
    }
}
