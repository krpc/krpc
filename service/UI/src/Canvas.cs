using KRPC.Service.Attributes;
using KSP.UI;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A canvas for user interface elements. See <see cref="UI.StockCanvas" /> and <see cref="UI.AddCanvas" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Canvas : Object
    {
        static Canvas stockCanvas;

        internal static Canvas StockCanvas {
            get {
                if (stockCanvas == null)
                    stockCanvas = new Canvas (UIMasterController.Instance.appCanvas);
                return stockCanvas;
            }
        }

        internal Canvas (UnityEngine.Canvas canvas)
            : base (canvas)
        {
        }

        internal Canvas ()
            : base (new GameObject ("krpc.canvas", typeof(UnityEngine.Canvas)), true)
        {
            GameObject.AddComponent<KSPGraphicRaycaster> ();
            GameObject.GetComponent<UnityEngine.Canvas> ().renderMode = RenderMode.ScreenSpaceOverlay;
            GameObject.GetComponent<UnityEngine.RectTransform> ().sizeDelta = new Vector2 (Screen.width, Screen.height);
        }

        /// <summary>
        /// The rect transform for the canvas.
        /// </summary>
        [KRPCProperty]
        public RectTransform RectTransform {
            get{ return new RectTransform (GameObject.GetComponent<UnityEngine.RectTransform> ()); }
        }

        /// <summary>
        /// Create a new container for user interface elements.
        /// </summary>
        /// <param name="visible">Whether the panel is visible.</param>
        [KRPCMethod]
        public Panel AddPanel (bool visible = true)
        {
            return new Panel (GameObject, visible);
        }

        /// <summary>
        /// Add text to the canvas.
        /// </summary>
        /// <param name="content">The text.</param>
        /// <param name="visible">Whether the text is visible.</param>
        [KRPCMethod]
        public Text AddText (string content, bool visible = true)
        {
            return new Text (GameObject, content, visible);
        }

        /// <summary>
        /// Add an input field to the canvas.
        /// </summary>
        /// <param name="visible">Whether the input field is visible.</param>
        [KRPCMethod]
        public InputField AddInputField (bool visible = true)
        {
            return new InputField (GameObject, visible);
        }

        /// <summary>
        /// Add a button to the canvas.
        /// </summary>
        /// <param name="content">The label for the button.</param>
        /// <param name="visible">Whether the button is visible.</param>
        [KRPCMethod]
        public Button AddButton (string content, bool visible = true)
        {
            return new Button (GameObject, content, visible);
        }
    }
}
