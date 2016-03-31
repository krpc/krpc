using System;
using System.Collections.Generic;
using UnityEngine;

namespace KRPC.UI
{
    abstract class OptionDialog : MonoBehaviour
    {
        MultiOptionDialog dialog;
        bool hasInit;

        protected string Title { get; set; }

        protected string Message { get; set; }

        protected UISkinDef Skin { get; set; }

        protected List<DialogGUIButton> Options { get; private set; }

        public event EventHandler OnOpen;
        public event EventHandler OnClose;

        public bool Visible { get; private set; }

        protected OptionDialog ()
        {
            Options = new List<DialogGUIButton> ();
        }

        protected abstract void Init ();

        protected abstract void Opened ();

        protected abstract void Closed ();

        public void Open ()
        {
            if (!Visible) {
                if (Skin == null)
                    Skin = HighLogic.UISkin;
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

        void OnGUI ()
        {
            if (!hasInit) {
                Init ();
                hasInit = true;
            }
            if (Visible) {
                dialog.Update ();
            }
        }
    }
}
