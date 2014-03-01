using NUnit.Framework;
using KRPC.Utils;
using System.Linq;

namespace KRPCTest.Utils
{
    [TestFixture]
    public class IEnumerableExtensionsTest
    {
        [Test]
        public void TestDuplicates ()
        {
            var withDups = new [] { "foo", "bar", "baz", "bar", "bar", "baz" };
            Assert.AreEqual (new [] { "bar", "baz" }, withDups.Duplicates ().ToArray ());
            var withoutDups = new [] { "foo", "bar", "baz" };
            Assert.AreEqual (new string[] { }, withoutDups.Duplicates ().ToArray ());
        }
    }
}

