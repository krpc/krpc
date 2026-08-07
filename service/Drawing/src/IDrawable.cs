using KRPC.Utils;
using UnityEngine;

namespace KRPC.Drawing
{
    /// <summary>
    /// Interface for objects that can be drawn.
    /// </summary>
    /// <remarks>
    /// A drawable says what the game holds for it, so that the addon stops drawing one
    /// whose game object has been destroyed rather than reaching into it every frame.
    /// </remarks>
    public interface IDrawable : IGameObjectState
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
