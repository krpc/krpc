using System;
using UnityEngine;

namespace KRPC.UI
{
    abstract class Window : MonoBehaviour
    {
        int id = UnityEngine.Random.Range (1000, 2000000);
        bool hasInit;

        protected GUIStyle Style { get; set; }

        public event EventHandler OnShow;
        public event EventHandler OnHide;
        public event EventHandler<MovedArgs> OnMoved;

        bool visible;

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
        // TODO: ensure position is not outside the KSP window
        Rect position = new Rect ();

        public Rect Position {
            get { return position; }
            set {
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
                hasInit = true;
            }
            if (Visible) {
                Position = GUILayout.Window (id, Position, DrawWindow, "kRPC Server", Style);
            }
        }

        void DrawWindow (int windowId)
        {
            Draw ();
        }
    }
}

