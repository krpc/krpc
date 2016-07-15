using KRPC.Server;

namespace KRPC.UI
{
    sealed class ClientDisconnectDialog : OptionDialog
    {
        IClient client;

        protected override void Init ()
        {
            Title = "kRPC";
            Options.Add (new DialogGUIButton ("Yes, disconnect the client", () => {
                client.Close ();
                Close ();
            }));
            Options.Add (new DialogGUIButton ("No, don't disconnect the client", Close));
        }

        protected override void Opened ()
        {
            var clientName = client.Name;
            var clientAddress = client.Address;
            if (clientName.Length == 0)
                Message = "Are you sure you want to disconnect the client at address " + clientAddress + "?";
            else
                Message = "Are you sure you want to disconnect '" + clientName + "' at address " + clientAddress + "?";
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
