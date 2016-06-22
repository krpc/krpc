using KRPC.Server;
using NUnit.Framework;

namespace KRPC.Test.Server
{
    [TestFixture]
    public class ClientRequestingConnectionArgsTest
    {
        [Test]
        public void DefaultBehaviour ()
        {
            var attempt = new ClientRequestingConnectionEventArgs<byte,byte> (null);
            Assert.IsFalse (attempt.Request.ShouldDeny);
            Assert.IsFalse (attempt.Request.ShouldAllow);
            Assert.IsTrue (attempt.Request.StillPending);
        }

        [Test]
        public void Deny ()
        {
            var attempt = new ClientRequestingConnectionEventArgs<byte,byte> (null);
            attempt.Request.Deny ();
            Assert.IsTrue (attempt.Request.ShouldDeny);
            Assert.IsFalse (attempt.Request.ShouldAllow);
            Assert.IsFalse (attempt.Request.StillPending);
        }

        [Test]
        public void Allow ()
        {
            var attempt = new ClientRequestingConnectionEventArgs<byte,byte> (null);
            attempt.Request.Allow ();
            Assert.IsFalse (attempt.Request.ShouldDeny);
            Assert.IsTrue (attempt.Request.ShouldAllow);
            Assert.IsFalse (attempt.Request.StillPending);
        }

        [Test]
        public void AllowAndDeny ()
        {
            var attempt = new ClientRequestingConnectionEventArgs<byte,byte> (null);
            attempt.Request.Allow ();
            attempt.Request.Deny ();
            Assert.IsTrue (attempt.Request.ShouldDeny);
            Assert.IsFalse (attempt.Request.ShouldAllow);
            Assert.IsFalse (attempt.Request.StillPending);
        }
    }
}
