using System.Collections.Generic;
using System.Linq;
using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// The camera properties whose writes are deferred across a mode switch.
    /// </summary>
    enum CameraProperty
    {
        Distance,
        Pitch,
        Heading,
        FoV
    }

    /// <summary>
    /// Addon that re-applies camera property writes (distance, pitch, heading, field of
    /// view) that were issued while the camera was switching mode.
    /// </summary>
    /// <remarks>
    /// A camera mode switch (Flight/IVA/Map) plays a fade that takes a second or two
    /// before the destination camera is live and settled. A write issued in that window
    /// is lost: while the destination camera settles it drives its own distance/pitch/
    /// heading each frame, snapping back to a default and overwriting anything written
    /// earlier. KSP exposes no "transition in progress" flag - the manager's mode is set
    /// synchronously, ahead of the visible fade - so rather than detect the transition,
    /// each pending write is re-applied whenever the live value has drifted away from it,
    /// and is only cleared once the value has held on its own (without needing to be
    /// re-applied) for a short settle window. Re-applying keeps the value pinned through
    /// the transition; the settle check tells when the camera has stopped fighting it.
    /// This is mode-agnostic and does not depend on the per-transition fade mechanics.
    /// There is a single global camera in KSP, so the pending writes are held in a static
    /// table (last writer wins) rather than per client.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class CameraAddon : UnityEngine.MonoBehaviour
    {
        sealed class PendingWrite
        {
            public CameraMode TargetMode;
            public float Value;
            public int FramesLeft;
            public int SettledFrames;
        }

        // Consecutive frames the live value must hold on its own (no re-apply needed)
        // before the write is considered settled and cleared. Long enough to outlast the
        // brief snap-backs during a transition, short enough to release promptly once the
        // camera settles. Update runs on the render tick, so this is rendered frames.
        const int SettleFrames = 30;

        // Overall budget to keep re-applying a write before giving up, comfortably longer
        // than the longest mode-switch settle. Guards a value the camera never accepts
        // (e.g. requested out of range) or a target mode that never arrives because the
        // script switched again, so neither can pin a write forever.
        const int RetryFrames = 1200;

        static readonly Dictionary<CameraProperty, PendingWrite> pending =
            new Dictionary<CameraProperty, PendingWrite> ();

        /// <summary>
        /// Record a camera property write to be re-applied until it takes. A superseding
        /// write to the same property overwrites the entry, so rapid re-requests cannot
        /// accumulate.
        /// </summary>
        internal static void Request (CameraProperty property, CameraMode mode, float value)
        {
            pending [property] = new PendingWrite {
                TargetMode = mode,
                Value = value,
                FramesLeft = RetryFrames,
                SettledFrames = 0
            };
        }

        /// <summary>
        /// Wake the addon.
        /// </summary>
        public void Awake ()
        {
            pending.Clear ();
        }

        /// <summary>
        /// Destroy the addon.
        /// </summary>
        public void OnDestroy ()
        {
            pending.Clear ();
        }

        /// <summary>
        /// Re-apply pending camera writes. Runs on the render tick, where the flight
        /// camera updates, rather than in FixedUpdate.
        /// </summary>
        public void Update ()
        {
            if (pending.Count == 0)
                return;
            CameraMode mode;
            try {
                mode = Camera.CurrentMode;
            } catch {
                // The camera is momentarily in an unknown state; try again next frame.
                return;
            }
            foreach (var property in pending.Keys.ToList ()) {
                var entry = pending [property];
                if (mode != entry.TargetMode) {
                    // Still transitioning, or the script switched to a different mode.
                    // Keep waiting, but count down so a mode that never arrives cannot
                    // pin the write forever.
                    if (--entry.FramesLeft <= 0)
                        pending.Remove (property);
                    continue;
                }
                try {
                    // The live value is read before re-applying, so it reflects whether
                    // the previous frame's write survived the camera's own updates. Once
                    // it has held on its own for the settle window the camera has stopped
                    // overwriting it and the write is done; until then, re-apply it.
                    if (Camera.Converged (property, entry.Value)) {
                        if (++entry.SettledFrames >= SettleFrames)
                            pending.Remove (property);
                    } else {
                        entry.SettledFrames = 0;
                        Camera.ApplyRaw (property, entry.Value);
                    }
                    if (--entry.FramesLeft <= 0)
                        pending.Remove (property);
                } catch {
                    // The property is not supported in this mode (e.g. distance in IVA);
                    // stop retrying.
                    pending.Remove (property);
                }
            }
        }
    }
}
