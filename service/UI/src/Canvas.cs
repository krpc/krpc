using KRPC.Service.Attributes;
using KSP.UI;
using UnityEngine;
using Tuple2 = KRPC.Utils.Tuple<double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// A canvas for user interface elements. See <see cref="UI.StockCanvas" /> and <see cref="UI.AddCanvas" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Canvas : UIObject
    {
        static Canvas stockCanvas;

        internal static Canvas StockCanvas {
            get {
                if (stockCanvas == null)
                    stockCanvas = new Canvas (UIMasterController.Instance.appCanvas);
                return stockCanvas;
            }
        }

        Canvas (UnityEngine.Canvas canvas)
            : base (canvas)
        {
        }

        internal Canvas ()
            : base (new GameObject ("krpc.canvas", typeof(UnityEngine.Canvas)), true)
        {
            obj.GetComponent<UnityEngine.Canvas> ().renderMode = RenderMode.ScreenSpaceOverlay;
            obj.GetComponent<UnityEngine.RectTransform> ().sizeDelta = new Vector2 (Screen.width, Screen.height);
        }

        /// <summary>
        /// The rect transform for the canvas.
        /// </summary>
        [KRPCProperty]
        public RectTransform RectTransform {
            get{ return new RectTransform (obj.GetComponent<UnityEngine.RectTransform> ()); }
        }

        /// <summary>
        /// Create a new container for user interface elements.
        /// </summary>
        /// <param name="visible">Whether the panel is visible.</param>
        [KRPCMethod]
        public Panel AddPanel (bool visible = true)
        {
            return new Panel (obj, visible);
        }
    }
}
