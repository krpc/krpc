using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace KRPC.Test.Server.TCP
{
    [TestFixture]
    public class AssumptionsTest
    {
        /// <summary>
        /// Test for checking assumptions about loopback IP addresses
        /// </summary>
        [Test]
        [SuppressMessage ("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public void LoopbackIPAddress ()
        {
            var localAddress = IPAddress.Parse ("127.0.0.1");
            Assert.AreEqual (IPAddress.Loopback.ToString (), localAddress.ToString ());
            Assert.AreEqual (IPAddress.Loopback, localAddress);
            Assert.IsTrue (IPAddress.IsLoopback (localAddress));
        }

        /// <summary>
        /// Check that TcpListener.Stop works as expected
        /// </summary>
        [Test]
        public void StopTcpListener ()
        {
            var listener = new TcpListener (IPAddress.Loopback, 0);
            listener.Start ();
            listener.Stop ();
            Assert.Throws<InvalidOperationException> (() => listener.AcceptTcpClient ());
        }
    }
}
