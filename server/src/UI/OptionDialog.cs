using System;
using System.Collections.Generic;
using UnityEngine;

namespace KRPC.UI
{
    abstract class OptionDialog : MonoBehaviour
    {
        MultiOptionDialog dialog;
        PopupDialog popup;
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
            if (!hasInit) {
                Init ();
                hasInit = true;
            }
            if (!Visible) {
                if (Skin == null)
                    Skin = HighLogic.UISkin;
                Visible = true;
                Opened ();
                if (OnOpen != null)
                    OnOpen (this, EventArgs.Empty);
                dialog = new MultiOptionDialog (Message, Title, Skin, Options.ToArray ());
                popup = PopupDialog.SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), dialog, true, HighLogic.UISkin);
            }
        }

        public void Close ()
        {
            if (Visible) {
                try {
                    UnityEngine.Object.Destroy (popup.gameObject);
                } catch (NullReferenceException) {
                    //FIXME: Nasty hack catching this. Dialog may have already been removed by other UI logic.
                }
                Visible = false;
                dialog = null;
                popup = null;
                Closed ();
                if (OnClose != null)
                    OnClose (this, EventArgs.Empty);
            }
        }
    }
}
