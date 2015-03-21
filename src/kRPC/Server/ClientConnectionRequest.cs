namespace KRPC.Server
{
    public class ClientConnectionRequest
    {
        bool allow;
        bool deny;

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
