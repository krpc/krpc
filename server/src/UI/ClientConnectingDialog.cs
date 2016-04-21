using UnityEngine;
using KRPC.Server;
using KRPC.Utils;

namespace KRPC.UI
{
    sealed class ClientConnectingDialog : OptionDialog
    {
        ClientRequestingConnectionArgs args;

        protected override void Init ()
        {
            Title = "kRPC";
            Options.Add (new DialogGUIButton ("Allow", () => args.Request.Allow ()));
            Options.Add (new DialogGUIButton ("Deny", () => args.Request.Deny ()));
        }

        protected override void Opened ()
        {
            if (args.Client.Name == "")
                Message = "A client is attempting to connect from " + args.Client.Address;
            else
                Message = "'" + args.Client.Name + "' is attempting to connect from " + args.Client.Address;
        }

        protected override void Closed ()
        {
            args = null;
        }

        public void OnClientRequestingConnection (object sender, ClientRequestingConnectionArgs args)
        {
            // Not open, so open the dialog
            if (!Visible) {
                Logger.WriteLine ("Asking player to allow/deny connection attempt...");
                this.args = args;
                Open ();
                return;
            }

            // Already open for a different request, so ignore
            if (Visible && this.args.Client != args.Client)
                return;

            // Open, and we have a decision (must be the correct client at this point), to close the dialog
            if (Visible && !this.args.Request.StillPending) {
                if (this.args.Request.ShouldAllow)
                    args.Request.Allow ();
                else
                    args.Request.Deny ();
                Close ();
            }
        }
    }
}
