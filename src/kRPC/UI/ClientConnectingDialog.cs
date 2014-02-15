using System;
using System.Net.Sockets;
using UnityEngine;
using KRPC.Server;
using KRPC.Server.Net;
using KRPC.Utils;
using KRPC.Schema.RPC;

namespace KRPC.UI
{
    sealed class ClientConnectingDialog : MonoBehaviour
    {
        private volatile bool show = false;
        private ClientRequestingConnectionArgs<Request,Response> args;

        public void Awake () {
            RenderingManager.AddToPostDrawQueue(5, DrawGUI);
        }

        public void Show (object sender, ClientRequestingConnectionArgs<Request,Response> args)
        {
            Logger.WriteLine("Asking player to allow/deny connection attempt...");
            show = true;
            this.args = args;
            //TODO: This spin lock is horrible. But it works...
            while (show) {
                System.Threading.Thread.Sleep(50);
            }
        }

        public void Cancel () {
            show = false;
        }

        private void DrawGUI () {
            if (show) {
                DialogOption[] options = {
                    new DialogOption ("Allow", () => {
                        args.Allow ();
                        show = false;
                    }),
                    new DialogOption ("Deny", () => {
                        args.Deny ();
                        show = false;
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
