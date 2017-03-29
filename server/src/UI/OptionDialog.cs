using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Utils;
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

        public event EventHandler OnOpen;
        public event EventHandler OnClose;

        public bool Visible { get; private set; }

        protected abstract void Init ();

        protected abstract void Opened ();

        protected abstract void Closed ();

        protected abstract IList<DialogGUIButton> Options { get; }

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
                EventHandlerExtensions.Invoke (OnOpen, this);
                dialog = new MultiOptionDialog (Message, Title, Skin, Options.ToArray ());
                popup = PopupDialog.SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), dialog, false, HighLogic.UISkin);
            }
        }

        public void Close ()
        {
            if (Visible) {
                try {
                    Destroy (popup.gameObject);
                } catch (NullReferenceException) {
                    // FIXME: Nasty hack catching this. Dialog may have already been removed by other UI logic.
                }
                Visible = false;
                dialog = null;
                popup = null;
                Closed ();
                EventHandlerExtensions.Invoke (OnClose, this);
            }
        }
    }
}
