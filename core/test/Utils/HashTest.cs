using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class HashTest
    {
        [Test]
        public void SameFieldsHashTheSame ()
        {
            Assert.AreEqual (
                Hash.Of ("a").And (2).And (true).Value,
                Hash.Of ("a").And (2).And (true).Value);
        }

        [Test]
        public void EqualFieldsDoNotCancel ()
        {
            // An exclusive-or gets this wrong: a pair of fields holding the same value must
            // not leave the hash where it would have been without them
            Assert.AreNotEqual (Hash.Of ("a").And ("a").Value, Hash.Of ("b").And ("b").Value);
            Assert.AreNotEqual (Hash.Of ("a").And ("a").Value, Hash.Of (0).And (0).Value);
        }

        [Test]
        public void TheFieldAValueIsInMatters ()
        {
            Assert.AreNotEqual (Hash.Of ("a").And ("b").Value, Hash.Of ("b").And ("a").Value);
        }

        [Test]
        public void NullCountsAsZero ()
        {
            Assert.AreEqual (Hash.Of ((string)null).Value, Hash.Of (0).Value);
            Assert.AreNotEqual (Hash.Of ((string)null).And ("a").Value, Hash.Of ("a").Value);
        }

        [Test]
        public void EveryFieldCounts ()
        {
            // Past eight fields as well, where a value tuple stops looking.
            for (int i = 0; i < 12; i++) {
                var fields = new int [12];
                fields [i] = 1;
                Assert.AreNotEqual (HashOf (new int [12]), HashOf (fields));
            }
        }

        [Test]
        public void AHashIsItsOwnHashCode ()
        {
            // Every use of Hash is inside a GetHashCode, where calling GetHashCode on the
            // hash instead of reading its value is an easy slip
            var hash = Hash.Of ("a").And (2);
            Assert.AreEqual (hash.Value, hash.GetHashCode ());
            Assert.AreEqual (hash, Hash.Of ("a").And (2));
            Assert.AreNotEqual (hash, Hash.Of ("a").And (3));
        }

        static int HashOf (int [] fields)
        {
            var hash = Hash.Of (fields [0]);
            for (int i = 1; i < fields.Length; i++)
                hash = hash.And (fields [i]);
            return hash;
        }
    }
}
