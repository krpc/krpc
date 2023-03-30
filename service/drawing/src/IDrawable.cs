using UnityEngine;

namespace KRPC.Drawing
{
    /// <summary>
    /// Interface for objects that can be drawn.
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// Update the drawable.
        /// </summary>
        void Update ();

        /// <summary>
        /// Destroy the drawable.
        /// </summary>
        void Destroy ();

        /// <summary>
        /// The game object for the drawable.
        /// </summary>
        GameObject GameObject { get; }
    }
}
