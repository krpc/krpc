using System;
using System.Runtime.CompilerServices;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class CachedReferenceTest
    {
        [Test]
        public void NullArgumentsThrow ()
        {
            Assert.Throws<ArgumentNullException> (
                () => new CachedReference<object> (null, t => true));
            Assert.Throws<ArgumentNullException> (
                () => new CachedReference<object> (() => null, null));
        }

        [Test]
        public void ResolvesOnceThenServesFromCache ()
        {
            int calls = 0;
            var obj = new object ();
            var reference = new CachedReference<object> (
                () => { calls++; return obj; }, t => true);

            Assert.AreSame (obj, reference.Get ());
            Assert.AreSame (obj, reference.Get ());
            Assert.AreSame (obj, reference.Get ());
            Assert.AreEqual (1, calls);
        }

        [Test]
        public void SeededReferenceServesWithoutResolving ()
        {
            int calls = 0;
            var obj = new object ();
            var reference = new CachedReference<object> (
                () => { calls++; return null; }, t => true, obj);

            // The seed is used directly, so the resolver is never called.
            Assert.AreSame (obj, reference.Get ());
            Assert.AreEqual (0, calls);
        }

        [Test]
        public void NonAliveSeedIsIgnored ()
        {
            int calls = 0;
            var dead = new object ();
            var live = new object ();
            var reference = new CachedReference<object> (
                () => { calls++; return live; }, t => t != dead, dead);

            // The seed is not alive, so it is dropped and the first access resolves.
            Assert.AreSame (live, reference.Get ());
            Assert.AreEqual (1, calls);
        }

        [Test]
        public void ReResolvesWhenCachedObjectNotAlive ()
        {
            int calls = 0;
            bool alive = true;
            var objects = new[] { new object (), new object () };
            var reference = new CachedReference<object> (
                () => objects[Math.Min (calls++, objects.Length - 1)],
                t => alive);

            Assert.AreSame (objects[0], reference.Get ());
            Assert.AreEqual (1, calls);

            // The cached object is now considered dead, so the next access re-resolves.
            alive = false;
            Assert.AreSame (objects[1], reference.Get ());
            Assert.AreEqual (2, calls);
        }

        [Test]
        public void ReturnsNullWithoutCachingWhenResolverReturnsNull ()
        {
            int calls = 0;
            var reference = new CachedReference<object> (
                () => { calls++; return null; }, t => t != null);

            Assert.IsNull (reference.Get ());
            Assert.IsNull (reference.Get ());
            // Nothing was cached, so every access re-resolves.
            Assert.AreEqual (2, calls);
        }

        [Test]
        public void ReResolvesAfterCachedObjectCollected ()
        {
            int calls = 0;
            var reference = new CachedReference<object> (
                () => { calls++; return new object (); }, t => true);

            // Prime the cache without letting the object escape into a rooted local,
            // so it can be collected once the weak reference is all that remains.
            Prime (reference);
            Assert.AreEqual (1, calls);

            GC.Collect ();
            GC.WaitForPendingFinalizers ();
            GC.Collect ();

            // The weakly-held object has been collected, so this re-resolves.
            reference.Get ();
            Assert.AreEqual (2, calls);
        }

        [MethodImpl (MethodImplOptions.NoInlining)]
        static void Prime (CachedReference<object> reference)
        {
            reference.Get ();
            reference.Get ();
        }
    }
}
