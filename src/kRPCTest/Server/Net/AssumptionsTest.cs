using System.Net;
using NUnit.Framework;

namespace KRPCTest.Server.Net
{
    [TestFixture]
    public class AssumptionsTest
    {
        /// <summary>
        /// Test for checking assumptions about the IPAddress class
        /// </summary>
        [Test]
        public void TestCase ()
        {
            var localAddress = IPAddress.Parse ("127.0.0.1");
            Assert.AreEqual (IPAddress.Loopback.ToString (), localAddress.ToString ());
            Assert.AreEqual (IPAddress.Loopback, localAddress);
            Assert.IsTrue (IPAddress.IsLoopback (localAddress));
        }
    }
}

