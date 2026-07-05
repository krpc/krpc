using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Expansions.Missions.Adjusters;
using KRPC.Server;
using KRPC.Service;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that controls individual actuators: engine gimbals, RCS and aero control
    /// surfaces.  It holds the override state for each actuator, keyed by the
    /// underlying part module, and is responsible for its lifecycle: overrides are
    /// dropped when the controlling client disconnects, when the part is destroyed, and
    /// when the scene changes.
    /// </summary>
    /// <remarks>
    /// Gimbals and RCS are overridden using KSP's own part-module "adjuster" mechanism,
    /// which the module invokes mid-update to transform its control input just before
    /// it is applied. The public entry point for installing an adjuster
    /// (PartModule.AddPartModuleAdjuster) is gated behind the Making History expansion,
    /// so instead the adjuster is added directly to the module's (private)
    /// adjusterCache by reflection - the module's apply path iterates that list with no
    /// such gate.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class ActuatorControlAddon : MonoBehaviour
    {
        /// <summary>
        /// Overrides a gimbal's actuation. ModuleGimbal calls this mid-FixedUpdate,
        /// after populating the pitch/roll/yaw actuation from the cooked control input
        /// and before running its own kinematics, so returning the stored command
        /// overrides it cleanly.
        /// </summary>
        sealed class GimbalAdjuster : AdjusterGimbalBase
        {
            // KSP's Missions system reflects over every Adjuster subclass at load and
            // instantiates it, so both base constructors must be present.
            public GimbalAdjuster () { }
            public GimbalAdjuster (Expansions.Missions.MENode node) : base (node) { }

            public Vector3 Setting = Vector3.zero;

            public override Vector3 ApplyControlAdjustment (Vector3 control)
            {
                return Setting;
            }
        }

        /// <summary>
        /// Overrides an RCS block's rotation and translation demand. ModuleRCS calls
        /// these mid-Update, after rotating the cooked control input into world space
        /// and before it is used to allocate thrust across the thrusters. The command
        /// is stored in the vessel's control axes, so it is rotated into world space
        /// here to match.
        /// </summary>
        sealed class RCSAdjuster : AdjusterRCSBase
        {
            public RCSAdjuster () { }
            public RCSAdjuster (Expansions.Missions.MENode node) : base (node) { }

            public ModuleRCS Module;
            public Vector3 Rotation = Vector3.zero;
            public Vector3 Linear = Vector3.zero;

            Quaternion ToWorld ()
            {
                return Module != null && Module.vessel != null && Module.vessel.ReferenceTransform != null
                    ? Module.vessel.ReferenceTransform.rotation : Quaternion.identity;
            }

            public override Vector3 ApplyInputRotationAdjustment (Vector3 inputRotation)
            {
                return ToWorld () * Rotation;
            }

            public override Vector3 ApplyInputLinearAdjustment (Vector3 inputLinear)
            {
                return ToWorld () * Linear;
            }
        }

        /// <summary>
        /// An override on a single actuator, tagged with the client that owns it (so it
        /// can be released when that client disconnects).
        /// </summary>
        abstract class Entry
        {
            public IClient Client;
        }

        /// <summary>
        /// A gimbal override, installed as an adjuster on the module. The prior active
        /// state is saved and forced on so the module's FixedUpdate (which is skipped
        /// when inactive) runs the adjuster.
        /// </summary>
        sealed class GimbalEntry : Entry
        {
            public GimbalAdjuster Adjuster;
            public bool SavedGimbalActive;
        }

        /// <summary>
        /// An RCS override, installed as an adjuster on the module.
        /// </summary>
        sealed class RCSEntry : Entry
        {
            public RCSAdjuster Adjuster;
        }

        /// <summary>
        /// A control surface override. Control surfaces have no deflection adjuster
        /// (the KSP adjuster only overrides actuation speed), so the override is driven
        /// through the persistent <c>deploy</c>/<c>deployAngle</c> fields with cooked
        /// control removed via the <c>ignore*</c> flags. The prior state is saved so it
        /// can be restored on release.
        /// </summary>
        sealed class ControlSurfaceEntry : Entry
        {
            public float Deflection;
            public bool SavedIgnorePitch;
            public bool SavedIgnoreYaw;
            public bool SavedIgnoreRoll;
            public bool SavedDeploy;
        }

        /// <summary>
        /// A set of actuator overrides of one kind, keyed by the part module. Owns the
        /// shared lifecycle: installing an override on enable, releasing it on disable,
        /// and dropping overrides whose owning client has disconnected or whose module
        /// has been destroyed. The per-kind behaviour is supplied as delegates.
        /// </summary>
        sealed class OverrideRegistry<TModule, TEntry>
            where TModule : PartModule
            where TEntry : Entry
        {
            readonly IDictionary<TModule, TEntry> entries = new Dictionary<TModule, TEntry> ();
            readonly Func<TModule, TEntry> install;
            readonly Action<TModule, TEntry> release;

            public OverrideRegistry (Func<TModule, TEntry> install, Action<TModule, TEntry> release)
            {
                this.install = install;
                this.release = release;
            }

            public bool IsSet (TModule module)
            {
                return entries.ContainsKey (module);
            }

            /// <summary>
            /// The override for the given module, or null if it is not being overridden.
            /// </summary>
            public TEntry Get (TModule module)
            {
                TEntry entry;
                return entries.TryGetValue (module, out entry) ? entry : null;
            }

            /// <summary>
            /// The override for the given module, re-tagged with the calling client, or
            /// null if it is not being overridden. Used when changing a live override's
            /// command.
            /// </summary>
            public TEntry Own (TModule module)
            {
                var entry = Get (module);
                if (entry != null)
                    entry.Client = CallContext.Client;
                return entry;
            }

            public void Set (TModule module, bool enabled)
            {
                var entry = Get (module);
                if (entry != null) {
                    if (enabled)
                        entry.Client = CallContext.Client;
                    else {
                        release (module, entry);
                        entries.Remove (module);
                    }
                } else if (enabled) {
                    entry = install (module);
                    entry.Client = CallContext.Client;
                    entries [module] = entry;
                }
            }

            /// <summary>
            /// Drop overrides whose module has been destroyed or whose client has
            /// disconnected.
            /// </summary>
            public void Sweep ()
            {
                foreach (var module in entries.Keys.ToList ()) {
                    var entry = entries [module];
                    if (Destroyed (module) || Disconnected (entry.Client)) {
                        release (module, entry);
                        entries.Remove (module);
                    }
                }
            }

            public void Clear ()
            {
                foreach (var entry in entries)
                    release (entry.Key, entry.Value);
                entries.Clear ();
            }
        }

        static readonly OverrideRegistry<ModuleGimbal, GimbalEntry> gimbals =
            new OverrideRegistry<ModuleGimbal, GimbalEntry> (InstallGimbal, ReleaseGimbal);
        static readonly OverrideRegistry<ModuleRCS, RCSEntry> rcs =
            new OverrideRegistry<ModuleRCS, RCSEntry> (InstallRCS, ReleaseRCS);
        static readonly OverrideRegistry<ModuleControlSurface, ControlSurfaceEntry> controlSurfaces =
            new OverrideRegistry<ModuleControlSurface, ControlSurfaceEntry> (
                InstallControlSurface, RestoreControlSurface);

        // The module's adjuster list is private; AddPartModuleAdjuster (which would
        // populate it) is gated behind the Making History expansion, so add/remove
        // directly by reflection.
        static readonly FieldInfo gimbalAdjusterCache =
            typeof (ModuleGimbal).GetField ("adjusterCache", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo rcsAdjusterCache =
            typeof (ModuleRCS).GetField ("adjusterCache", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Wake the addon
        /// </summary>
        public void Awake ()
        {
            Clear ();
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        public void OnDestroy ()
        {
            Clear ();
        }

        static void Clear ()
        {
            gimbals.Clear ();
            rcs.Clear ();
            controlSurfaces.Clear ();
        }

        /// <summary>
        /// Drop overrides whose controlling client has disconnected or whose part module has been
        /// destroyed, returning those actuators to normal control. The overrides themselves are
        /// applied by the modules calling into the installed adjusters, so nothing is driven here.
        /// </summary>
        public void FixedUpdate ()
        {
            gimbals.Sweep ();
            rcs.Sweep ();
            controlSurfaces.Sweep ();
        }

        static bool Destroyed (UnityEngine.Object module)
        {
            return module == null;
        }

        static bool Disconnected (IClient client)
        {
            return client != null && !client.Connected;
        }

        static void CacheAdd (FieldInfo cache, PartModule module, object adjuster)
        {
            var list = cache?.GetValue (module) as System.Collections.IList;
            if (list != null)
                list.Add (adjuster);
        }

        static void CacheRemove (FieldInfo cache, PartModule module, object adjuster)
        {
            var list = cache?.GetValue (module) as System.Collections.IList;
            if (list != null)
                list.Remove (adjuster);
        }

        static GimbalEntry InstallGimbal (ModuleGimbal module)
        {
            var entry = new GimbalEntry {
                Adjuster = new GimbalAdjuster (),
                SavedGimbalActive = module.gimbalActive
            };
            // ModuleGimbal.FixedUpdate (which runs the adjuster) returns early when
            // inactive, so ensure it runs.
            module.gimbalActive = true;
            CacheAdd (gimbalAdjusterCache, module, entry.Adjuster);
            return entry;
        }

        static void ReleaseGimbal (ModuleGimbal module, GimbalEntry entry)
        {
            if (Destroyed (module))
                return;
            CacheRemove (gimbalAdjusterCache, module, entry.Adjuster);
            module.gimbalActive = entry.SavedGimbalActive;
        }

        internal static bool GetGimbalOverride (ModuleGimbal module)
        {
            return gimbals.IsSet (module);
        }

        internal static void SetGimbalOverride (ModuleGimbal module, bool enabled)
        {
            gimbals.Set (module, enabled);
        }

        internal static Vector3 GetGimbalActuation (ModuleGimbal module)
        {
            var entry = gimbals.Get (module);
            return entry != null ? entry.Adjuster.Setting : (Vector3)module.actuation;
        }

        internal static void SetGimbalActuation (ModuleGimbal module, Vector3 actuation)
        {
            var entry = gimbals.Own (module);
            if (entry != null)
                entry.Adjuster.Setting = actuation.Clamp (-1f, 1f);
        }

        static RCSEntry InstallRCS (ModuleRCS module)
        {
            var entry = new RCSEntry { Adjuster = new RCSAdjuster { Module = module } };
            CacheAdd (rcsAdjusterCache, module, entry.Adjuster);
            return entry;
        }

        static void ReleaseRCS (ModuleRCS module, RCSEntry entry)
        {
            if (!Destroyed (module))
                CacheRemove (rcsAdjusterCache, module, entry.Adjuster);
        }

        internal static bool GetRCSOverride (ModuleRCS module)
        {
            return rcs.IsSet (module);
        }

        internal static void SetRCSOverride (ModuleRCS module, bool enabled)
        {
            rcs.Set (module, enabled);
        }

        internal static Vector3 GetRCSRotation (ModuleRCS module)
        {
            var entry = rcs.Get (module);
            return entry != null ? entry.Adjuster.Rotation : Vector3.zero;
        }

        internal static void SetRCSRotation (ModuleRCS module, Vector3 rotation)
        {
            var entry = rcs.Own (module);
            if (entry != null)
                entry.Adjuster.Rotation = rotation.Clamp (-1f, 1f);
        }

        internal static Vector3 GetRCSTranslation (ModuleRCS module)
        {
            var entry = rcs.Get (module);
            return entry != null ? entry.Adjuster.Linear : Vector3.zero;
        }

        internal static void SetRCSTranslation (ModuleRCS module, Vector3 translation)
        {
            var entry = rcs.Own (module);
            if (entry != null)
                entry.Adjuster.Linear = translation.Clamp (-1f, 1f);
        }

        static ControlSurfaceEntry InstallControlSurface (ModuleControlSurface module)
        {
            var entry = new ControlSurfaceEntry {
                Deflection = 0f,
                SavedIgnorePitch = module.ignorePitch,
                SavedIgnoreYaw = module.ignoreYaw,
                SavedIgnoreRoll = module.ignoreRoll,
                SavedDeploy = module.deploy
            };
            // Remove the cooked-control contribution and drive the surface through the
            // deploy offset, which the module applies persistently every frame.
            module.ignorePitch = true;
            module.ignoreYaw = true;
            module.ignoreRoll = true;
            module.deploy = true;
            ApplyControlSurfaceDeflection (module, entry);
            return entry;
        }

        static void RestoreControlSurface (ModuleControlSurface module, ControlSurfaceEntry entry)
        {
            if (Destroyed (module))
                return;
            module.ignorePitch = entry.SavedIgnorePitch;
            module.ignoreYaw = entry.SavedIgnoreYaw;
            module.ignoreRoll = entry.SavedIgnoreRoll;
            module.deploy = entry.SavedDeploy;
        }

        static void ApplyControlSurfaceDeflection (ModuleControlSurface module, ControlSurfaceEntry entry)
        {
            // Map the normalized command onto the module's deploy angle limits (min,
            // max). The exact mapping/sign may need in-game calibration.
            var limits = module.deployAngleLimits;
            module.deployAngle = entry.Deflection >= 0f
                ? entry.Deflection * limits.y
                : entry.Deflection * -limits.x;
        }

        internal static bool GetControlSurfaceOverride (ModuleControlSurface module)
        {
            return controlSurfaces.IsSet (module);
        }

        internal static void SetControlSurfaceOverride (ModuleControlSurface module, bool enabled)
        {
            controlSurfaces.Set (module, enabled);
        }

        internal static float GetControlSurfaceDeflection (ModuleControlSurface module)
        {
            var entry = controlSurfaces.Get (module);
            return entry != null ? entry.Deflection : 0f;
        }

        internal static void SetControlSurfaceDeflection (ModuleControlSurface module, float deflection)
        {
            var entry = controlSurfaces.Own (module);
            if (entry != null) {
                entry.Deflection = Mathf.Clamp (deflection, -1f, 1f);
                ApplyControlSurfaceDeflection (module, entry);
            }
        }
    }
}
