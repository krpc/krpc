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
    public class Canvas : Object
    {
        static Canvas stockCanvas;

        internal static Canvas StockCanvas {
            get {
                // The wrapper is cached so that repeated calls return the same object, but the
                // game object it wraps is destroyed by a scene change. Comparing the game object
                // against null uses Unity's equality, which is true once it has been destroyed,
                // so a stale wrapper is replaced rather than handed out.
                if (stockCanvas == null || stockCanvas.GameObject == null) {
                    var controller = UIMasterController.Instance;
                    if (controller == null || controller.appCanvas == null)
                        throw new InvalidOperationException (
                            "The stock UI canvas is not available in the current game scene");
                    stockCanvas = new Canvas (controller.appCanvas);
                }
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
            // Without this the canvas would ignore the interface scale the player has set,
            // and the same interface would come out a different size on it than on the
            // stock canvas.
            GameObject.AddComponent<CanvasScale> ();
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
