using System;

namespace KRPC.Server
{
    class ClientConnectionRequest
    {
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

        public void Allow ()
        {
            allow = true;
        }

        public void Deny ()
        {
            deny = true;
        }
    }
}
