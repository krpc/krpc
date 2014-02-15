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
            var attempt = new ClientRequestingConnectionArgs<byte,byte> (null);
            Assert.IsFalse (attempt.ShouldDeny);
            Assert.IsFalse (attempt.ShouldAllow);
            Assert.IsTrue (attempt.StillPending);
        }

        [Test]
        public void Deny ()
        {
            var attempt = new ClientRequestingConnectionArgs<byte,byte> (null);
            attempt.Deny();
            Assert.IsTrue (attempt.ShouldDeny);
            Assert.IsFalse (attempt.ShouldAllow);
            Assert.IsFalse (attempt.StillPending);
        }

        [Test]
        public void Allow ()
        {
            var attempt = new ClientRequestingConnectionArgs<byte,byte> (null);
            attempt.Allow();
            Assert.IsFalse (attempt.ShouldDeny);
            Assert.IsTrue (attempt.ShouldAllow);
            Assert.IsFalse (attempt.StillPending);
        }

        [Test]
        public void AllowAndDeny ()
        {
            var attempt = new ClientRequestingConnectionArgs<byte,byte> (null);
            attempt.Allow();
            attempt.Deny();
            Assert.IsTrue (attempt.ShouldDeny);
            Assert.IsFalse (attempt.ShouldAllow);
            Assert.IsFalse (attempt.StillPending);
        }
    }
}

