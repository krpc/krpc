using NUnit.Framework;

namespace KRPCTest.Utils
{
    class MyEquatable : KRPC.Utils.Equatable<MyEquatable>
    {
        readonly public string key;

        public MyEquatable (string key)
        {
            this.key = key;
        }

        public override bool Equals (MyEquatable obj)
        {
            return key == obj.key;
        }

        public override int GetHashCode ()
        {
            return key.GetHashCode ();
        }
    }

    [TestFixture]
    public class EquatableTest
    {
        [Test]
        public void EqualityOperators ()
        {
            var obj1 = new MyEquatable ("foo");
            var obj1a = obj1;
            var obj2 = new MyEquatable ("foo");
            var obj3 = new MyEquatable ("bar");
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
            var obj1 = new MyEquatable ("foo");
            var obj2 = new MyEquatable ("foo");
            var obj3 = new MyEquatable ("bar");
            Assert.AreEqual (obj1.GetHashCode (), obj1.GetHashCode ());
            Assert.AreEqual (obj1.GetHashCode (), obj2.GetHashCode ());
            Assert.AreNotEqual (obj1.GetHashCode (), obj3.GetHashCode ());
        }

        [Test]
        public void NullReferences ()
        {
            var obj = new MyEquatable ("foo");
            Assert.False (obj == null);
            Assert.False (null == obj);
            Assert.True ((MyEquatable)null == (MyEquatable)null);
            Assert.True (obj != null);
            Assert.True (null != obj);
            Assert.False ((MyEquatable)null != (MyEquatable)null);
        }
    }
}

