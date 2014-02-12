using NUnit.Framework;
using System;
using KRPC.Server;

namespace KRPCTest.Server
{
	[TestFixture]
	public class ConnectionAttemptTest
	{
		[Test]
		public void DefaultBehaviour ()
		{
			var attempt = new ConnectionAttempt ();
			Assert.IsTrue (attempt.ShouldDeny);
			Assert.IsFalse (attempt.ShouldAllow);
		}

		[Test]
		public void Deny ()
		{
			var attempt = new ConnectionAttempt ();
			attempt.Deny();
			Assert.IsTrue (attempt.ShouldDeny);
			Assert.IsFalse (attempt.ShouldAllow);
		}

		[Test]
		public void Accept ()
		{
			var attempt = new ConnectionAttempt ();
			attempt.Allow();
			Assert.IsFalse (attempt.ShouldDeny);
			Assert.IsTrue (attempt.ShouldAllow);
		}

		[Test]
		public void AcceptAndDeny ()
		{
			var attempt = new ConnectionAttempt ();
			attempt.Allow();
			attempt.Deny();
			Assert.IsTrue (attempt.ShouldDeny);
			Assert.IsFalse (attempt.ShouldAllow);
		}
	}
}

