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
            var obj1 = Connection.TestService ().CreateTestObject ("jeb");
            var obj2 = Connection.TestService ().CreateTestObject ("jeb");
            Assert.IsTrue (obj1 == obj2);
            Assert.IsFalse (obj1 != obj2);

            var obj3 = Connection.TestService ().CreateTestObject ("bob");
            Assert.IsFalse (obj1 == obj3);
            Assert.IsTrue (obj1 != obj3);

            Connection.TestService ().ObjectProperty = obj1;
            var obj1a = Connection.TestService ().ObjectProperty;
            Assert.IsTrue (obj1 == obj1a);

            Assert.IsFalse (obj1 == null);
            Assert.IsTrue (obj1 != null);
            Assert.IsFalse (null == obj1);
            Assert.IsTrue (null != obj1);
        }

        [Test]
        public void Hash ()
        {
            var obj1 = Connection.TestService ().CreateTestObject ("jeb");
            var obj2 = Connection.TestService ().CreateTestObject ("jeb");
            var obj3 = Connection.TestService ().CreateTestObject ("bob");
            Assert.AreEqual (obj1.id.GetHashCode (), obj1.GetHashCode ());
            Assert.AreEqual (obj2.id.GetHashCode (), obj2.GetHashCode ());
            Assert.AreNotEqual (obj1.id.GetHashCode (), obj3.GetHashCode ());
            Assert.AreEqual (obj1.GetHashCode (), obj2.GetHashCode ());
            Assert.AreNotEqual (obj1.GetHashCode (), obj3.GetHashCode ());

            Connection.TestService ().ObjectProperty = obj1;
            var obj1a = Connection.TestService ().ObjectProperty;
            Assert.AreEqual (obj1.GetHashCode (), obj1a.GetHashCode ());
        }

        [Test]
        public void MemoryAllocation ()
        {
            var obj1 = Connection.TestService ().CreateTestObject ("jeb");
            var obj2 = Connection.TestService ().CreateTestObject ("jeb");
            var obj3 = Connection.TestService ().CreateTestObject ("bob");
            Assert.AreEqual (obj1.id, obj2.id);
            Assert.AreNotEqual (obj1.id, obj3.id);
        }
    }
}
