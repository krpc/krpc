using System;
using System.Net.Sockets;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    /// <summary>
    /// The endpoint a unix domain socket is opened with. The runtime hands it the address it
    /// serializes to and nothing else, so what it puts in there is what decides which socket
    /// a connection reaches.
    /// </summary>
    [TestFixture]
    public class UnixEndPointTest
    {
        [Test]
        public void Path ()
        {
            var endPoint = new UnixEndPoint ("/tmp/krpc/rpc");
            Assert.AreEqual ("/tmp/krpc/rpc", endPoint.Path);
            Assert.AreEqual ("/tmp/krpc/rpc", endPoint.ToString ());
            Assert.AreEqual (AddressFamily.Unix, endPoint.AddressFamily);
        }

        [Test]
        public void NullPath ()
        {
            Assert.Throws<ArgumentNullException> (() => new UnixEndPoint (null));
        }

        [Test]
        public void Serialize ()
        {
            // A sockaddr_un is the address family, the path, and a terminating zero
            var address = new UnixEndPoint ("/tmp/rpc").Serialize ();
            Assert.AreEqual (AddressFamily.Unix, address.Family);
            Assert.AreEqual (2 + "/tmp/rpc".Length + 1, address.Size);
            for (int i = 0; i < "/tmp/rpc".Length; i++)
                Assert.AreEqual ((byte)"/tmp/rpc" [i], address [2 + i]);
            Assert.AreEqual (0, address [2 + "/tmp/rpc".Length]);
        }

        [Test]
        public void SerializeRejectsAPathTooLongToFit ()
        {
            var endPoint = new UnixEndPoint (new string ('x', UnixEndPoint.PathLength));
            Assert.Throws<ArgumentException> (() => endPoint.Serialize ());
        }

        [Test]
        public void RoundTrip ()
        {
            var endPoint = new UnixEndPoint ("/tmp/krpc/stream");
            var result = endPoint.Create (endPoint.Serialize ());
            Assert.IsInstanceOf<UnixEndPoint> (result);
            Assert.AreEqual ("/tmp/krpc/stream", ((UnixEndPoint)result).Path);
            Assert.AreEqual (endPoint, result);
            Assert.AreEqual (endPoint.GetHashCode (), result.GetHashCode ());
        }

        [Test]
        public void Equality ()
        {
            Assert.AreEqual (new UnixEndPoint ("/tmp/rpc"), new UnixEndPoint ("/tmp/rpc"));
            Assert.AreNotEqual (new UnixEndPoint ("/tmp/rpc"), new UnixEndPoint ("/tmp/stream"));
            Assert.AreNotEqual (new UnixEndPoint ("/tmp/rpc"), null);
        }
    }
}
