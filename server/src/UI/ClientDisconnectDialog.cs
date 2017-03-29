using System.Collections.Generic;
using KRPC.Server;

namespace KRPC.UI
{
    sealed class ClientDisconnectDialog : OptionDialog
    {
        IClient client;

        protected override void Init ()
        {
            Title = "kRPC";
        }

        protected override IList<DialogGUIButton> Options {
            get {
                var options = new List<DialogGUIButton> ();
                options.Add (new DialogGUIButton ("Yes", () => {
                    client.Close ();
                    Close ();
                }));
                options.Add (new DialogGUIButton ("Yes (don't ask again)", () => {
                    client.Close ();
                    Close ();
                    Addon.config.ConfirmRemoveClient = false;
                    Addon.config.Save ();
                }));
                options.Add (new DialogGUIButton ("No", Close));
                return options;
            }
        }

        protected override void Opened ()
        {
            var clientName = client.Name;
            Message = "Are you sure you want to disconnect " +
            (clientName.Length == 0 ? "the client" : "'" + clientName + "'") +
            " at address " + client.Address + "?";
        }

        protected override void Closed ()
        {
            client = null;
        }

        public void Show (IClient connectingClient)
        {
            if (!Visible) {
                client = connectingClient;
                Open ();
            }
        }
    }
}
