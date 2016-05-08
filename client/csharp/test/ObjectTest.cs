using KRPC.Client.Services.TestService;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class ObjectTest : ServerTestCase
    {
        [Test]
        public void Equality ()
        {
            var obj1 = connection.TestService ().CreateTestObject ("jeb");
            var obj2 = connection.TestService ().CreateTestObject ("jeb");
            Assert.IsTrue (obj1 == obj2);
            Assert.IsFalse (obj1 != obj2);

            var obj3 = connection.TestService ().CreateTestObject ("bob");
            Assert.IsFalse (obj1 == obj3);
            Assert.IsTrue (obj1 != obj3);

            connection.TestService ().ObjectProperty = obj1;
            var obj1a = connection.TestService ().ObjectProperty;
            Assert.IsTrue (obj1 == obj1a);

            Assert.IsFalse (obj1 == null);
            Assert.IsTrue (obj1 != null);
            Assert.IsFalse (null == obj1);
            Assert.IsTrue (null != obj1);
        }

        [Test]
        public void Hash ()
        {
            var obj1 = connection.TestService ().CreateTestObject ("jeb");
            var obj2 = connection.TestService ().CreateTestObject ("jeb");
            var obj3 = connection.TestService ().CreateTestObject ("bob");
            Assert.AreEqual (obj1._ID.GetHashCode (), obj1.GetHashCode ());
            Assert.AreEqual (obj2._ID.GetHashCode (), obj2.GetHashCode ());
            Assert.AreNotEqual (obj1._ID.GetHashCode (), obj3.GetHashCode ());
            Assert.AreEqual (obj1.GetHashCode (), obj2.GetHashCode ());
            Assert.AreNotEqual (obj1.GetHashCode (), obj3.GetHashCode ());

            connection.TestService ().ObjectProperty = obj1;
            var obj1a = connection.TestService ().ObjectProperty;
            Assert.AreEqual (obj1.GetHashCode (), obj1a.GetHashCode ());
        }

        [Test]
        public void MemoryAllocation ()
        {
            var obj1 = connection.TestService ().CreateTestObject ("jeb");
            var obj2 = connection.TestService ().CreateTestObject ("jeb");
            var obj3 = connection.TestService ().CreateTestObject ("bob");
            Assert.AreEqual (obj1._ID, obj2._ID);
            Assert.AreNotEqual (obj1._ID, obj3._ID);
        }
    }
}
