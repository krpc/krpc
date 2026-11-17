using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// Addon that counts the game's frames for the update loop benchmarks, from the first
    /// scene on.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Instantly, true)]
    public sealed class FrameCounter : MonoBehaviour
    {
        /// <summary>
        /// Keep the addon across scene changes.
        /// </summary>
        public void Awake ()
        {
            DontDestroyOnLoad (this);
        }

        /// <summary>
        /// Count a frame. Unity runs Update once a frame, and <c>unscaledDeltaTime</c> is the
        /// frame's length.
        /// </summary>
        public void Update ()
        {
            KRPC.Benchmarks.GameLoop.FrameEnded (Time.unscaledDeltaTime);
        }
    }
}
