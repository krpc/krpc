using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KRPC.Utils
{
    /// <remarks>
    /// Adapted from public domain code by Majiir
    /// http://forum.kerbalspaceprogram.com/threads/79889-Expanded-KSPAddon-modes
    /// </remarks>
    [AttributeUsage (AttributeTargets.Class)]
    class KSPAddonImproved : Attribute
    {
        [Flags]
        public enum Startup
        {
            None = 0,

            MainMenu = 1 << 0,
            Settings = 1 << 1,
            Credits = 1 << 2,

            SpaceCenter = 1 << 3,
            Flight = 1 << 4,
            TrackingStation = 1 << 5,
            EditorVAB = 1 << 6,
            EditorSPH = 1 << 7,

            PSystemSpawn = 1 << 8,
            Instantly = 1 << 9,

            Editor = EditorVAB | EditorSPH,
            RealTime = Flight | TrackingStation | SpaceCenter,
            All = ~0
        }

        internal bool Once { get; private set; }

        internal Startup Scenes { get; private set; }

        public KSPAddonImproved (Startup scenes, bool once = false)
        {
            Scenes = scenes;
            Once = once;
        }

        public static GameScenes CurrentGameScene { get; set; }
    }

    class _KSPAddonImproved : KSPAddon, IEquatable<_KSPAddonImproved>
    {
        readonly Type type;

        public _KSPAddonImproved (KSPAddon.Startup startup, bool once, Type type)
            : base (startup, once)
        {
            this.type = type;
        }

        public override bool Equals (object obj)
        {
            return obj.GetType () == GetType () && Equals ((_KSPAddonImproved)obj);
        }

        public bool Equals (_KSPAddonImproved other)
        {
            if (once != other.once) {
                return false;
            }
            if (startup != other.startup) {
                return false;
            }
            return type == other.type;
        }

        public override int GetHashCode ()
        {
            return startup.GetHashCode () ^ once.GetHashCode () ^ type.GetHashCode ();
        }
    }

    [_KSPAddonImproved (KSPAddon.Startup.Instantly, true, typeof(CustomAddonLoader))]
    class CustomAddonLoader : MonoBehaviour
    {
        readonly List<AddonInfo> addons = new List<AddonInfo> ();

        class AddonInfo
        {
            public readonly Type type;
            public readonly KSPAddonImproved addon;
            public bool created;

            internal AddonInfo (Type t, KSPAddonImproved add)
            {
                type = t;
                addon = add;
                created = false;
            }

            internal bool RunOnce {
                get { return addon.Once; }
            }

            internal KSPAddonImproved.Startup Scenes {
                get { return addon.Scenes; }
            }
        }

        void Awake ()
        {
            DontDestroyOnLoad (this);

            // Examine our assembly for loaded types
            foreach (var ourType in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()) {
                var attr = ((KSPAddonImproved[])ourType.GetCustomAttributes (typeof(KSPAddonImproved), true)).SingleOrDefault ();
                if (attr != null) {
                    addons.Add (new AddonInfo (ourType, attr));
                }
            }

            // Special case here: since we're already in the first scene,
            // OnLevelWasLoaded won't be invoked so we need to fire off any
            // "instant" loading addons now
            OnLevelWasLoaded ((int)GameScenes.LOADING);
        }

        void OnLevelWasLoaded (int level)
        {
            var scene = (GameScenes)level;
            KSPAddonImproved.Startup mask = 0;

            if (scene == GameScenes.LOADINGBUFFER)
                return;

            switch (scene) {
            case GameScenes.CREDITS:
                mask = KSPAddonImproved.Startup.Credits;
                break;
            case GameScenes.EDITOR:
                mask = KSPAddonImproved.Startup.Editor;
                break;
            case GameScenes.FLIGHT:
                mask = KSPAddonImproved.Startup.Flight;
                break;
            case GameScenes.LOADING:
                mask = KSPAddonImproved.Startup.Instantly;
                break;
            case GameScenes.LOADINGBUFFER:
                // intentionally left unset
                break;
            case GameScenes.MAINMENU:
                mask = KSPAddonImproved.Startup.MainMenu;
                break;
            case GameScenes.PSYSTEM:
                mask = KSPAddonImproved.Startup.PSystemSpawn;
                break;
            case GameScenes.SETTINGS:
                mask = KSPAddonImproved.Startup.Settings;
                break;
            case GameScenes.SPACECENTER:
                mask = KSPAddonImproved.Startup.SpaceCenter;
                break;
            case GameScenes.TRACKSTATION:
                mask = KSPAddonImproved.Startup.TrackingStation;
                break;
            default:
                throw new ArgumentException ("Unknown game scene");
            }

            KSPAddonImproved.CurrentGameScene = scene;

            int counter = 0;

            foreach (var addon in addons) {
                if (addon.created && addon.RunOnce)
                    continue;
                // This addon was already loaded, should it be initialized for the current scene?
                if ((addon.Scenes & mask) != 0) {
                    var go = new GameObject (addon.type.Name);
                    go.AddComponent (addon.type);
                    addon.created = true;
                    ++counter;
                }
            }
        }
    }
}
