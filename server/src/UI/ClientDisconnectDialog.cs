using KRPC.Server;

namespace KRPC.UI
{
    sealed class ClientDisconnectDialog : OptionDialog
    {
        IClient client;

        protected override void Init ()
        {
            Title = "kRPC";
            Options.Add (new DialogGUIButton ("Yes", () => {
                client.Close ();
                Close ();
            }));
            Options.Add (new DialogGUIButton ("Yes (don't ask again)", () => {
                client.Close ();
                Close ();
                Addon.config.ConfirmRemoveClient = false;
                Addon.config.Save ();
            }));
            Options.Add (new DialogGUIButton ("No", Close));
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
