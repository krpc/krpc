using System;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using KRPC.Server;
using KRPC.Utils;

namespace KRPC.UI
{
    abstract class OptionDialog : MonoBehaviour
    {
        private MultiOptionDialog dialog;
        private bool hasInit = false;

        protected string Title { get; set; }

        protected string Message { get; set; }

        protected GUISkin Skin { get; set; }

        protected List<DialogOption> Options { get; private set; }

        public event EventHandler OnOpen;
        public event EventHandler OnClose;

        public bool Visible { get; private set; }

        public void Awake ()
        {
            Options = new List<DialogOption> ();
            RenderingManager.AddToPostDrawQueue (1, UpdateGUI);
        }

        protected abstract void Init ();

        protected abstract void Opened ();

        protected abstract void Closed ();

        public void OnDestroy ()
        {
            RenderingManager.RemoveFromPostDrawQueue (1, UpdateGUI);
        }

        public void Open ()
        {
            if (!Visible) {
                if (Skin == null)
                    Skin = GUI.skin;
                Visible = true;
                Opened ();
                if (OnOpen != null)
                    OnOpen (this, EventArgs.Empty);
                dialog = new MultiOptionDialog (Message, Title, Skin, Options.ToArray ());
            }
        }

        public void Close ()
        {
            if (Visible) {
                Visible = false;
                dialog = null;
                Closed ();
                if (OnClose != null)
                    OnClose (this, EventArgs.Empty);
            }
        }

        private void UpdateGUI ()
        {
            if (!hasInit) {
                Init ();
                hasInit = true;
            }
            if (Visible) {
                dialog.DrawWindow ();
            }
        }
    }
}
