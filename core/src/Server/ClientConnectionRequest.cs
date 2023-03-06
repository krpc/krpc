namespace KRPC.Server
{
    /// <summary>
    /// Used by a client requesting connection event to determine
    /// if the request should be allowed or denied.
    /// </summary>
    public sealed class ClientConnectionRequest
    {
        bool allow;
        bool deny;

        /// <summary>
        /// Should the connection be allowed?
        /// </summary>
        public bool ShouldAllow {
            get { return allow && !deny; }
        }

        /// <summary>
        /// Should the connection be denied?
        /// </summary>
        public bool ShouldDeny {
            get { return deny; }
        }

        /// <summary>
        /// Is a decision still pending?
        /// </summary>
        public bool StillPending {
            get { return !allow && !deny; }
        }

        /// <summary>
        /// Allow the connection
        /// </summary>
        public void Allow ()
        {
            allow = true;
        }

        /// <summary>
        /// Deny the connection
        /// </summary>
        public void Deny ()
        {
            deny = true;
        }
    }
}
