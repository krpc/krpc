using NUnit.Framework;
using System;
using KRPC.Server;

namespace KRPCTest.Server
{
    [TestFixture]
    public class ClientRequestingConnectionArgsTest
    {
        [Test]
        public void DefaultBehaviour ()
        {
            var attempt = new ClientRequestingConnectionArgs (null, null);
            Assert.IsTrue (attempt.ShouldDeny);
            Assert.IsFalse (attempt.ShouldAllow);
        }

        [Test]
        public void Deny ()
        {
            var attempt = new ClientRequestingConnectionArgs (null, null);
            attempt.Deny();
            Assert.IsTrue (attempt.ShouldDeny);
            Assert.IsFalse (attempt.ShouldAllow);
        }

        [Test]
        public void Allow ()
        {
            var attempt = new ClientRequestingConnectionArgs (null, null);
            attempt.Allow();
            Assert.IsFalse (attempt.ShouldDeny);
            Assert.IsTrue (attempt.ShouldAllow);
        }

        [Test]
        public void AllowAndDeny ()
        {
            var attempt = new ClientRequestingConnectionArgs (null, null);
            attempt.Allow();
            attempt.Deny();
            Assert.IsTrue (attempt.ShouldDeny);
            Assert.IsFalse (attempt.ShouldAllow);
        }
    }
}

