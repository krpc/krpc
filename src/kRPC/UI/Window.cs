using System;
using UnityEngine;

namespace KRPC.UI
{
    abstract class Window : MonoBehaviour
    {
        private int id = UnityEngine.Random.Range(1000, 2000000);
        private bool hasInit = false;

        protected GUIStyle Style { get; set; }

        public event EventHandler OnShow;
        public event EventHandler OnHide;
        public event EventHandler<MovedArgs> OnMoved;

        private bool visible;
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

        private Rect position;
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
            Visible = true;
            Position = new Rect();
            RenderingManager.AddToPostDrawQueue(1, UpdateGUI);
        }

        protected abstract void Init ();

        protected abstract void Draw ();

        public void OnDestroy ()
        {
            RenderingManager.RemoveFromPostDrawQueue(1, UpdateGUI);
        }

        private void UpdateGUI ()
        {
            if (!hasInit) {
                Style = new GUIStyle(UnityEngine.GUI.skin.window);
                Init ();
                hasInit = true;
            }
            if (Visible) {
                Position = GUILayout.Window (id, Position, DrawWindow, "kRPC Server", Style);
            }
        }

        private void DrawWindow (int id)
        {
            Draw ();
        }
    }
}

