using System;
using System.Collections.Generic;
using System.Linq;
using KSA;

namespace KRPC.KSA.UI
{
    /// <summary>
    /// A modal question with a row of answers, drawn by the game with its other popups.
    /// Answering closes it.
    /// </summary>
    sealed class PromptPopup : Popup
    {
        readonly string message;
        readonly PopupButton<PromptPopup> [] buttons;

        /// <summary>
        /// Open the popup. The first answer is the primary one.
        /// </summary>
        public PromptPopup (string text, IList<KeyValuePair<string, Action>> answers)
        {
            message = text;
            buttons = answers.Select ((answer, i) => CreateButton<PromptPopup> (
                answer.Key, popup => {
                    popup.Active = false;
                    answer.Value ();
                }, i == 0 ? PopupButtonKind.Primary : PopupButtonKind.Neutral)).ToArray ();
        }

        protected override void OnDrawUi ()
        {
            if (!BeginConsoleModal (WindowId, "kRPC", DEFAULT_SIZE_UV))
                return;
            ConsoleStyle.BeginBody ();
            ConsoleStyle.PushWidgetStyle ();
            CenteredWrappedText (message);
            EndConsoleBody ();
            DrawConsoleButtonRow (this, buttons);
            EndConsoleModal ();
        }
    }
}
