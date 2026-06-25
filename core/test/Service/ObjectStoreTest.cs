using System;
using KRPC.Service;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ObjectStoreTest
    {
        static object a = new object ();
        static object b = new object ();
        static object c = new object ();

        sealed class FakeValidatable : IValidatable
        {
            public bool IsValid { get; set; }
        }

        [Test]
        public void BasicUsage ()
        {
            var store = new ObjectStore ();
            Assert.AreEqual (1, store.AddInstance (a));
            Assert.AreEqual (2, store.AddInstance (b));
            Assert.AreEqual (3, store.AddInstance (c));
            Assert.AreEqual (1, store.GetObjectId (a));
            Assert.AreEqual (2, store.GetObjectId (b));
            Assert.AreEqual (3, store.GetObjectId (c));
            Assert.AreSame (a, store.GetInstance (1));
            Assert.AreSame (b, store.GetInstance (2));
            Assert.AreSame (c, store.GetInstance (3));
            store.RemoveInstance (a);
            Assert.Throws<ArgumentException> (() => store.GetInstance (1));
            store.RemoveInstance (b);
            Assert.Throws<ArgumentException> (() => store.GetInstance (2));
            store.RemoveInstance (c);
            Assert.Throws<ArgumentException> (() => store.GetInstance (3));
        }

        [Test]
        public void NonExistantInstance ()
        {
            var store = new ObjectStore ();
            Assert.Throws<ArgumentException> (() => store.GetObjectId (a));
            Assert.Throws<ArgumentException> (() => store.GetInstance (1));
            Assert.DoesNotThrow (() => store.RemoveInstance (a));
        }

        [Test]
        public void InstanceAlreadyExists ()
        {
            var store = new ObjectStore ();
            Assert.AreEqual (1, store.AddInstance (a));
            Assert.AreEqual (1, store.GetObjectId (a));
            Assert.AreSame (a, store.GetInstance (1));
            Assert.AreEqual (1, store.AddInstance (a));
            Assert.AreEqual (1, store.GetObjectId (a));
            Assert.AreSame (a, store.GetInstance (1));
        }

        [Test]
        public void NullValues ()
        {
            var store = new ObjectStore ();
            Assert.AreEqual (0, store.AddInstance (null));
            Assert.DoesNotThrow (() => store.RemoveInstance (null));
            Assert.AreEqual (null, store.GetInstance (0));
            Assert.AreEqual (0, store.GetObjectId (null));
        }

        [Test]
        public void RemoveInvalid ()
        {
            var store = new ObjectStore ();
            var valid = new FakeValidatable { IsValid = true };
            var invalid = new FakeValidatable { IsValid = false };
            var plain = new object ();
            var validId = store.AddInstance (valid);
            var invalidId = store.AddInstance (invalid);
            var plainId = store.AddInstance (plain);

            store.RemoveInvalid ();

            // The invalid object is removed from the store.
            Assert.Throws<ArgumentException> (() => store.GetInstance (invalidId));
            Assert.Throws<ArgumentException> (() => store.GetObjectId (invalid));
            // The valid object and the non-validatable object are left untouched.
            Assert.AreSame (valid, store.GetInstance (validId));
            Assert.AreSame (plain, store.GetInstance (plainId));
        }
    }
}
