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

        protected string Name { get; set; }

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
                dialog = Compatibility.NewMultiOptionDialog (Name, Message, Title, Skin, Options.ToArray ());
                popup = PopupDialog.SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), dialog, false, HighLogic.UISkin);
            }
        }

        public void Close ()
        {
            if (Visible) {
                // The popup may have already been destroyed by other UI logic (e.g. a
                // scene switch); Unity's lifetime check reports it as null in that case.
                if (popup != null)
                    Destroy (popup.gameObject);
                Visible = false;
                dialog = null;
                popup = null;
                Closed ();
                EventHandlerExtensions.Invoke (OnClose, this);
            }
        }
    }
}
