using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.UI
{
    abstract class Window : MonoBehaviour
    {
        int id;
        bool hasInit;
        GUIStyle closeButtonStyle;
        bool rescale = true;
        int uiScale;

        protected int Id { get { return id; } }

        protected string Title { get; set; }

        protected GUIStyle Style { get; set; }

        public event EventHandler OnShow;
        public event EventHandler OnHide;
        public event EventHandler<MovedEventArgs> OnStartMoving;
        public event EventHandler<MovedEventArgs> OnMoved;
        public event EventHandler<MovedEventArgs> OnFinishMoving;

        bool visible = true;

        public bool Visible {
            get { return visible; }
            set {
                if (!visible && value)
                    EventHandlerExtensions.Invoke (OnShow, this);
                if (visible && !value)
                    EventHandlerExtensions.Invoke (OnHide, this);
                visible = value;
            }
        }

        public bool Closable { get; set; }

        Rect position;
        bool moving;
        int movingCounter;

        public Rect Position {
            get { return position; }
            set {
                ConstrainToScreen (ref value);
                if (position != value)
                    EventHandlerExtensions.Invoke (OnMoved, this, new MovedEventArgs (value));
                position = value;
            }
        }

        private void Awake()
        {
            id = UnityEngine.Random.Range(1000, 2000000);
        }

        protected abstract void Init ();

        protected abstract void Draw (bool needRescale);

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void OnGUI ()
        {
            if (!hasInit) {
                var skin = Skin.DefaultSkin;
                Style = new GUIStyle (skin.window);
                Init ();
                closeButtonStyle = new GUIStyle (skin.button);
                closeButtonStyle.margin = new RectOffset (0, 0, 0, 0);
                closeButtonStyle.padding = new RectOffset (0, 0, 0, 0);
                hasInit = true;
            }
            if (Visible) {
                var newUiScale = (int)(GameSettings.UI_SCALE * 100);
                if (uiScale != newUiScale) {
                    rescale = true;
                    uiScale = newUiScale;
                    Style.fontSize = (int)(14 * GameSettings.UI_SCALE);
                    closeButtonStyle.fixedWidth = 16 * GameSettings.UI_SCALE;
                    closeButtonStyle.fixedHeight = 16 * GameSettings.UI_SCALE;
                }
                var newPosition = GUILayout.Window (id, Position, DrawWindow, Title, Style);
                if (newPosition != Position) {
                    if (!moving) {
                        moving = true;
                        EventHandlerExtensions.Invoke(OnStartMoving, this, new MovedEventArgs(Position));
                    }
                    movingCounter = 0;
                }
                Position = newPosition;
            }
            if (moving) {
                if (movingCounter > 50) {
                    moving = false;
                    EventHandlerExtensions.Invoke(OnFinishMoving, this, new MovedEventArgs(Position));
                }
                movingCounter++;
            }
        }

        void DrawWindow (int windowId)
        {
            if (Closable) {
                if (GUI.Button (new Rect (Position.width - (2 + closeButtonStyle.fixedWidth), 2, closeButtonStyle.fixedWidth, closeButtonStyle.fixedHeight),
                        new GUIContent (Icons.Instance.ButtonCloseWindow, "Close window"), closeButtonStyle)) {
                    Visible = false;
                }
            }
            // FIXME: dirty hack to make the title bar slightly bigger when the UI is scaled up
            GUILayout.Space ((int)(20 * (GameSettings.UI_SCALE - 1)));
            Draw (rescale);
            rescale = false;
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
