using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class TupleTest
    {
        [Test]
        public void TestCreate ()
        {
            var t1 = Tuple.Create (false);
            Assert.AreEqual (false, t1.Item1);

            var t2 = Tuple.Create (false, 42);
            Assert.AreEqual (false, t2.Item1);
            Assert.AreEqual (42, t2.Item2);

            var t3 = Tuple.Create (false, 42, 3.14159f);
            Assert.AreEqual (false, t3.Item1);
            Assert.AreEqual (42, t3.Item2);
            Assert.AreEqual (3.14159f, t3.Item3);

            var t4 = Tuple.Create (false, 42, 3.14159f, "foo");
            Assert.AreEqual (false, t4.Item1);
            Assert.AreEqual (42, t4.Item2);
            Assert.AreEqual (3.14159f, t4.Item3);
            Assert.AreEqual ("foo", t4.Item4);
        }
    }
}
