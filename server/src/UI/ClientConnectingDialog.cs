using System.Collections.Generic;
using KRPC.Server;
using KRPC.Utils;

namespace KRPC.UI
{
    sealed class ClientConnectingDialog : OptionDialog
    {
        ClientRequestingConnectionEventArgs args;

        protected override void Init ()
        {
            Title = "kRPC";
        }

        protected override IList<DialogGUIButton> Options {
            get {
                var options = new List<DialogGUIButton> ();
                options.Add (new DialogGUIButton ("Allow", () => args.Request.Allow ()));
                options.Add (new DialogGUIButton ("Allow (don't ask again)", () => {
                    Addon.config.AutoAcceptConnections = true;
                    Addon.config.Save ();
                    args.Request.Allow ();
                }));
                options.Add (new DialogGUIButton ("Deny", args.Request.Deny));
                return options;
            }
        }

        protected override void Opened ()
        {
            var clientName = args.Client.Name;
            var clientAddress = args.Client.Address;
            Message = (clientName.Length == 0 ? "A client" : "'" + clientName + "'") + " is attempting to connect from " + clientAddress;
        }

        protected override void Closed ()
        {
            args = null;
        }

        public void OnClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs eventArgs)
        {
            // Not open, so open the dialog
            if (!Visible) {
                Logger.WriteLine ("Asking player to allow/deny connection attempt...");
                args = eventArgs;
                Open ();
                return;
            }

            // Already open for a different request, so ignore
            if (Visible && args.Client != eventArgs.Client)
                return;

            // Open, and we have a decision (must be the correct client at this point), to close the dialog
            if (Visible && !args.Request.StillPending) {
                if (args.Request.ShouldAllow)
                    eventArgs.Request.Allow ();
                else
                    eventArgs.Request.Deny ();
                Close ();
            }
        }
    }
}
