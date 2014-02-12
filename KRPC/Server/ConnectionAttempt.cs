using System;

namespace KRPC.Server
{
	public class ConnectionAttempt {
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
	};
}

