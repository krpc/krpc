using System;
using System.Net.Sockets;
using UnityEngine;
using KRPC.Server;
using KRPC.Server.Net;
using KRPC.Utils;
using KRPC.Schema.KRPC;

namespace KRPC.UI
{
    sealed class ClientConnectingDialog : OptionDialog
    {
        private ClientRequestingConnectionArgs<Request,Response> args;

        protected override void Init()
        {
            Title = "kRPC";
            Skin = GUI.skin;
            Options.Add(
                new DialogOption ("Allow", () => {
                    args.Allow ();
                }));
            Options.Add (
                new DialogOption ("Deny", () => {
                    args.Deny ();
                }));
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
            this.args = null;
        }

        public void OnClientRequestingConnection (object sender, ClientRequestingConnectionArgs<Request,Response> args)
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
            if (Visible && !this.args.StillPending) {
                if (this.args.ShouldAllow)
                    args.Allow ();
                else
                    args.Deny ();
                Close ();
            }
        }
    }
}
