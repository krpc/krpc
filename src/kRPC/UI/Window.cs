using System;
using UnityEngine;

namespace KRPC.UI
{
    abstract class Window : MonoBehaviour
    {
        int id = UnityEngine.Random.Range (1000, 2000000);
        bool hasInit;
        GUIStyle closeButtonStyle;

        protected string Title { get; set; }

        protected GUIStyle Style { get; set; }

        public event EventHandler OnShow;
        public event EventHandler OnHide;
        public event EventHandler<MovedArgs> OnMoved;

        bool visible = true;
        bool closable = false;

        public bool Visible {
            get { return visible; }
            set {
                if (OnShow != null && !visible && value)
                    OnShow (this, EventArgs.Empty);
                if (OnHide != null && visible && !value)
                    OnHide (this, EventArgs.Empty);
                visible = value;
            }
        }

        public bool Closable {
            get { return closable; }
            set { closable = value; }
        }

        Rect position = new Rect ();

        public Rect Position {
            get { return position; }
            set {
                ConstrainToScreen (ref value);
                if (OnMoved != null && position != value)
                    OnMoved (this, new MovedArgs (value));
                position = value;
            }
        }

        public void Awake ()
        {
            RenderingManager.AddToPostDrawQueue (1, UpdateGUI);
        }

        protected abstract void Init ();

        protected abstract void Draw ();

        public void OnDestroy ()
        {
            RenderingManager.RemoveFromPostDrawQueue (1, UpdateGUI);
        }

        void UpdateGUI ()
        {
            if (!hasInit) {
                Style = new GUIStyle (GUI.skin.window);
                Init ();
                closeButtonStyle = new GUIStyle (GUI.skin.button);
                closeButtonStyle.margin = new RectOffset (0, 0, 0, 0);
                closeButtonStyle.padding = new RectOffset (0, 0, 0, 0);
                closeButtonStyle.fixedWidth = 16;
                closeButtonStyle.fixedHeight = 16;
                hasInit = true;
            }
            if (Visible) {
                Position = GUILayout.Window (id, Position, DrawWindow, Title, Style);
            }
        }

        void DrawWindow (int windowId)
        {
            if (Closable) {
                if (GUI.Button (new Rect (Position.width - 18, 2, closeButtonStyle.fixedWidth, closeButtonStyle.fixedHeight),
                        new GUIContent (Icons.Instance.buttonCloseWindow, "Close window"), closeButtonStyle)) {
                    Visible = false;
                }
            }
            Draw ();
        }

        static void ConstrainToScreen (ref Rect rect)
        {
            const int border = 10;
            rect.x = Math.Max (-(rect.width - border), rect.x);
            rect.y = Math.Max (-(rect.height - border), rect.y);
            rect.x = Math.Min (Screen.width - border, rect.x);
            rect.y = Math.Min (Screen.height - border, rect.y);
        }
    }
}

