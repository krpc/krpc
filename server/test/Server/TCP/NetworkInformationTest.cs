using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using KRPC.Server.TCP;
using NUnit.Framework;

namespace KRPC.Test.Server.TCP
{
    [TestFixture]
    public class NetworkInformationTest
    {
        [Test]
        public void NetworkAdapters ()
        {
            List<IPAddress> addresses = NetworkInformation.LocalIPAddresses.ToList ();
            Assert.IsTrue (addresses.Contains (IPAddress.Loopback));
            //foreach (var address in addresses)
            //    Console.WriteLine (address);
        }

        [Test]
        [Ignore ("Native method not available on some platforms")]
        public void GetLoopbackSubnetMask ()
        {
            Assert.AreEqual (String.Empty, NetworkInformation.GetSubnetMask (IPAddress.Loopback).ToString ());
        }
    }
}
