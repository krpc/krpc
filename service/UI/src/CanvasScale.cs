using KSP.UI;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// Keeps the canvas it is attached to at the scale the game draws its own interface
    /// at, so that a canvas a client adds matches the stock one. The player can change
    /// the interface scale at any time, and the game applies it to its own canvases by
    /// setting their scale factor, so it is followed rather than read once.
    /// </summary>
    sealed class CanvasScale : MonoBehaviour
    {
        UnityEngine.Canvas canvas;

        /// <summary>
        /// Find the canvas being scaled, and scale it before it is first drawn rather than
        /// leaving it unscaled for a frame.
        /// </summary>
        public void Awake ()
        {
            canvas = GetComponent<UnityEngine.Canvas> ();
            Apply ();
        }

        /// <summary>
        /// Follow the interface scale.
        /// </summary>
        public void Update ()
        {
            Apply ();
        }

        void Apply ()
        {
            var controller = UIMasterController.Instance;
            if (canvas != null && controller != null && controller.uiScale > 0)
                canvas.scaleFactor = controller.uiScale;
        }
    }
}
