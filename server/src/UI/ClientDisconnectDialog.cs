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
            if (client.Name == "")
                Message = "Are you sure you want to disconnect the client at address " + client.Address + "?";
            else
                Message = "Are you sure you want to disconnect '" + client.Name + "' at address " + client.Address + "?";
        }

        protected override void Closed ()
        {
            client = null;
        }

        public void Show (IClient client)
        {
            if (!Visible) {
                this.client = client;
                Open ();
            }
        }
    }
}
