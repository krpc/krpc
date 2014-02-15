using System;
using System.Net.Sockets;

namespace KRPC.Server
{
    class ClientRequestingConnectionArgs<In,Out> : EventArgs 
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
            get { return !ShouldAllow; }
        }

        public void Allow () {
            allow = true;
        }

        public void Deny () {
            deny = true;
        }
    }
}

