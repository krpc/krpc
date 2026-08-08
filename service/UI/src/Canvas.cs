using System;
using KRPC.Service.Attributes;
using KSP.UI;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A canvas for user interface elements. See <see cref="UI.StockCanvas" /> and <see cref="UI.AddCanvas" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Canvas : Container
    {
        internal static Canvas StockCanvas {
            get {
                var controller = UIMasterController.Instance;
                if (controller == null || controller.appCanvas == null)
                    throw new InvalidOperationException (
                        "The stock UI canvas is not available in the current game scene");
                return new Canvas (controller.appCanvas);
            }
        }

        internal Canvas (UnityEngine.Canvas canvas)
            : base (canvas)
        {
        }

        /// <inheritdoc />
        internal override bool CanHaveLayoutElement {
            get { return false; }
        }

        internal Canvas ()
            : base (new GameObject ("krpc.canvas", typeof(UnityEngine.Canvas)), true)
        {
            GameObject.AddComponent<KSPGraphicRaycaster> ();
            GameObject.GetComponent<UnityEngine.Canvas> ().renderMode = RenderMode.ScreenSpaceOverlay;
            GameObject.GetComponent<UnityEngine.RectTransform> ().sizeDelta = new Vector2 (Screen.width, Screen.height);
            // Without this the canvas would ignore the interface scale the player has set,
            // and the same interface would come out a different size on it than on the
            // stock canvas.
            GameObject.AddComponent<CanvasScale> ();
        }
    }
}
