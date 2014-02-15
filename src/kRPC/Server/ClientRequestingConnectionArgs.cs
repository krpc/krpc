using System;
using System.Net.Sockets;

namespace KRPC.Server
{
    class ClientRequestingConnectionArgs<In,Out> : EventArgs, IClientEventArgs<In,Out>
    {
        public IClient<In,Out> Client { get; private set; }

        public ClientRequestingConnectionArgs(IClient<In,Out> client)
        {
            Client = client;
        }

        private bool allow = false;
        private bool deny = false;

        public bool ShouldAllow {
            get { return allow && !deny; }
        }

        public bool ShouldDeny {
            get { return deny; }
        }

        public bool StillPending {
            get { return !allow && !deny; }
        }

        public void Allow () {
            allow = true;
        }

        public void Deny () {
            deny = true;
        }
    }
}

