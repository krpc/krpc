using System;
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
            Assert.AreEqual (RemoteObject.GetObjectId (obj1).GetHashCode (), obj1.GetHashCode ());
            Assert.AreEqual (RemoteObject.GetObjectId (obj2).GetHashCode (), obj2.GetHashCode ());
            Assert.AreNotEqual (RemoteObject.GetObjectId (obj1).GetHashCode (), obj3.GetHashCode ());
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
            Assert.AreEqual (RemoteObject.GetObjectId (obj1), RemoteObject.GetObjectId (obj2));
            Assert.AreNotEqual (RemoteObject.GetObjectId (obj1), RemoteObject.GetObjectId (obj3));
        }

        [Test]
        public void GetObjectId ()
        {
            var obj = new TestClass (Connection, 42);
            Assert.AreEqual (42, RemoteObject.GetObjectId (obj));
        }

        [Test]
        public void GetObjectIdNull ()
        {
            Assert.Throws<ArgumentNullException> (() => RemoteObject.GetObjectId (null));
        }
    }
}
