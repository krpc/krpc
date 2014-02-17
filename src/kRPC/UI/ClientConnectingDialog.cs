using System;
using System.Net.Sockets;
using UnityEngine;
using KRPC.Server;
using KRPC.Server.Net;
using KRPC.Utils;
using KRPC.Schema.KRPC;

namespace KRPC.UI
{
    sealed class ClientConnectingDialog : MonoBehaviour
    {
        private volatile bool show = false;
        private volatile bool decided = false;
        private ClientRequestingConnectionArgs<Request,Response> args;

        public void Init() {
        }

        public void Awake () {
            RenderingManager.AddToPostDrawQueue(5, DrawGUI);
        }

        public void Show (object sender, ClientRequestingConnectionArgs<Request,Response> args)
        {
            // Already open, and no decision, so no updates necessary
            if (show && !decided)
                return;

            // Not open, so open a dialog
            if (show == false) {
                Logger.WriteLine ("Asking player to allow/deny connection attempt...");
                show = true;
                decided = false;
                this.args = args;
                return;
            }

            // Open, and we have a decision, so send the decision and close the dialog
            if (show && decided && this.args.Client == args.Client) {
                if (this.args.ShouldAllow)
                    args.Allow ();
                else
                    args.Deny ();
                show = false;
                decided = false;
                args = null;
            }
        }

        public void Cancel () {
            show = false;
            decided = false;
            args = null;
        }

        private void DrawGUI () {
            if (show) {
                DialogOption[] options = {
                    new DialogOption ("Allow", () => {
                        args.Allow ();
                        decided = true;
                    }),
                    new DialogOption ("Deny", () => {
                        args.Deny ();
                        decided = true;
                    })
                };
                string message;
                if (args.Client.Name == "")
                    message = "A client is attempting to connect from " + args.Client.Address;
                else
                    message = "'" + args.Client.Name + "' is attempting to connect from " + args.Client.Address;
                var dialog = new MultiOptionDialog (message, "kRPC", UnityEngine.GUI.skin, options);
                dialog.DrawWindow ();
            }
        }
    }
}
