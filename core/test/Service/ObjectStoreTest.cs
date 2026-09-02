using System;
using KRPC.Service;
using KRPC.Utils;
using NUnit.Framework;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.Test.Service
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
            Assert.Throws<ObjectDestroyedException> (() => store.GetInstance (1));
            store.RemoveInstance (b);
            Assert.Throws<ObjectDestroyedException> (() => store.GetInstance (2));
            store.RemoveInstance (c);
            Assert.Throws<ObjectDestroyedException> (() => store.GetInstance (3));
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
        public void RemovedAndUnissuedObjectIds ()
        {
            var store = new ObjectStore ();
            var id = store.AddInstance (a);
            store.RemoveInstance (a);
            Assert.Throws<ObjectDestroyedException> (() => store.GetInstance (id));
            Assert.Throws<ArgumentException> (() => store.GetInstance (id + 1));
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
        public void SweepRemovesDeadInstances ()
        {
            var store = new ObjectStore ();
            var alive = new MortalObject ();
            var dead = new MortalObject ();
            var aliveId = store.AddInstance (alive);
            var deadId = store.AddInstance (dead);
            dead.GameObjectState = GameObjectState.Destroyed;

            Assert.AreEqual (1, store.Sweep ());
            Assert.AreSame (alive, store.GetInstance (aliveId));
            Assert.AreEqual (aliveId, store.GetObjectId (alive));
            Assert.Throws<ArgumentException> (() => store.GetObjectId (dead));
            Assert.Throws<ObjectDestroyedException> (() => store.GetInstance (deadId));
        }

        [Test]
        public void SweepKeepsDormantInstances ()
        {
            var store = new ObjectStore ();
            var dormant = new MortalObject ();
            var id = store.AddInstance (dormant);
            dormant.GameObjectState = GameObjectState.Dormant;

            Assert.AreEqual (0, store.Sweep ());
            Assert.AreSame (dormant, store.GetInstance (id));
        }

        [Test]
        public void SweepKeepsInstancesWithoutAGameObjectState ()
        {
            var store = new ObjectStore ();
            var id = store.AddInstance (a);
            Assert.AreEqual (0, store.Sweep ());
            Assert.AreSame (a, store.GetInstance (id));
        }

        [Test]
        public void SweepKeepsAnInstanceWhoseStateThrows ()
        {
            var store = new ObjectStore ();
            var broken = new MortalObject { StateThrows = true };
            var dead = new MortalObject ();
            var brokenId = store.AddInstance (broken);
            var deadId = store.AddInstance (dead);
            dead.GameObjectState = GameObjectState.Destroyed;

            // The rest of the store is still swept, and the instance that threw is kept
            Assert.AreEqual (1, store.Sweep ());
            Assert.AreSame (broken, store.GetInstance (brokenId));
            Assert.Throws<ObjectDestroyedException> (() => store.GetInstance (deadId));
        }

        [Test]
        public void SweepRemovesAnInstanceOnlyOnce ()
        {
            var store = new ObjectStore ();
            var dead = new MortalObject ();
            store.AddInstance (dead);
            dead.GameObjectState = GameObjectState.Destroyed;
            Assert.AreEqual (1, store.Sweep ());
            Assert.AreEqual (0, store.Sweep ());
        }

        [Test]
        public void SweepDoesNotReuseObjectIds ()
        {
            var store = new ObjectStore ();
            var dead = new MortalObject ();
            var deadId = store.AddInstance (dead);
            dead.GameObjectState = GameObjectState.Destroyed;
            store.Sweep ();
            Assert.AreNotEqual (deadId, store.AddInstance (new MortalObject ()));
        }

        [Test]
        public void NullIsNotAnInstance ()
        {
            // A null belongs to the position a value sits in, so no identifier stands for one
            var store = new ObjectStore ();
            Assert.Throws<ArgumentNullException> (() => store.AddInstance (null));
            Assert.Throws<ArgumentNullException> (() => store.GetObjectId (null));
            Assert.Throws<ArgumentException> (() => store.GetInstance (0));
            Assert.DoesNotThrow (() => store.RemoveInstance (null));
        }
    }
}
