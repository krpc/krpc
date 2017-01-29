using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [SuppressMessage ("Gendarme.Rules.Design", "ImplementEqualsAndGetHashCodeInPairRule")]
    sealed class MyEquatable : KRPC.Utils.Equatable<MyEquatable>
    {
        public readonly string Key;

        public MyEquatable (string key)
        {
            Key = key;
        }

        public sealed override bool Equals (MyEquatable other)
        {
            return Key == other.Key;
        }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public sealed override int GetHashCode ()
        {
            return Key.GetHashCode ();
        }
    }

    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    public class EquatableTest
    {
        static readonly MyEquatable obj1 = new MyEquatable ("foo");
        static readonly MyEquatable obj1a = obj1;
        static readonly MyEquatable obj2 = new MyEquatable ("foo");
        static readonly MyEquatable obj3 = new MyEquatable ("bar");

        [Test]
        public void EqualsTMethod ()
        {
            Assert.True (obj1.Equals (obj1a));
            Assert.True (obj1.Equals (obj2));
            Assert.False (obj1.Equals (obj3));
        }

        [Test]
        public void EqualsObjectMethod ()
        {
            Assert.True (obj1.Equals ((object)obj1a));
            Assert.True (obj1.Equals ((object)obj2));
            Assert.False (obj1.Equals ((object)obj3));
            Assert.False (obj1.Equals ("foo"));
        }

        [Test]
        public void EqualityOperator ()
        {
            Assert.True (obj1 == obj1a);
            Assert.True (obj1 == obj2);
            Assert.False (obj1 == obj3);
            Assert.False (obj1 != obj1a);
            Assert.False (obj1 != obj2);
            Assert.True (obj1 != obj3);
        }

        [Test]
        public void HashCodes ()
        {
            Assert.AreEqual (obj1.GetHashCode (), obj1.GetHashCode ());
            Assert.AreEqual (obj1.GetHashCode (), obj2.GetHashCode ());
            Assert.AreNotEqual (obj1.GetHashCode (), obj3.GetHashCode ());
        }

        [Test]
        public void NullReferences ()
        {
            Assert.False (obj1 == null);
            Assert.False (null == obj1);
            Assert.True (obj1 != null);
            Assert.True (null != obj1);
        }
    }
}
