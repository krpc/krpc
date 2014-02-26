using System;
using NUnit.Framework;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class ObjectStoreTest
    {
        static object a = new object ();
        static object b = new object ();
        static object c = new object ();

        [Test]
        public void BasicUsage ()
        {
            var store = new ObjectStore ();
            Assert.AreEqual (0, store.AddInstance (a));
            Assert.AreEqual (1, store.AddInstance (b));
            Assert.AreEqual (2, store.AddInstance (c));
            Assert.AreEqual (0, store.GetObjectId (a));
            Assert.AreEqual (1, store.GetObjectId (b));
            Assert.AreEqual (2, store.GetObjectId (c));
            Assert.AreSame (a, store.GetInstance (0));
            Assert.AreSame (b, store.GetInstance (1));
            Assert.AreSame (c, store.GetInstance (2));
            store.RemoveInstance (a);
            Assert.Throws<ArgumentException> (() => store.GetInstance (0));
            store.RemoveInstance (b);
            Assert.Throws<ArgumentException> (() => store.GetInstance (1));
            store.RemoveInstance (c);
            Assert.Throws<ArgumentException> (() => store.GetInstance (2));
        }

        [Test]
        public void NonExistantInstance ()
        {
            var store = new ObjectStore ();
            Assert.Throws<ArgumentException> (() => store.GetObjectId (a));
            Assert.Throws<ArgumentException> (() => store.GetInstance (0));
            Assert.DoesNotThrow (() => store.RemoveInstance (a));
        }

        [Test]
        public void InstanceAlreadyExists ()
        {
            var store = new ObjectStore ();
            Assert.AreEqual (0, store.AddInstance (a));
            Assert.AreEqual (0, store.GetObjectId (a));
            Assert.AreSame (a, store.GetInstance (0));
            Assert.AreEqual (0, store.AddInstance (a));
            Assert.AreEqual (0, store.GetObjectId (a));
            Assert.AreSame (a, store.GetInstance (0));
        }
    }
}
