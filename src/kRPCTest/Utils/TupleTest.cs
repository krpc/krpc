using NUnit.Framework;
using KRPC.Utils;

namespace KRPCTest.Utils
{
    [TestFixture()]
    public class TupleTest
    {
        [Test()]
        public void SimpleTest()
        {
            Tuple<int,string> t = new Tuple<int, string>(42,"foo");
            Assert.AreEqual(42, t.Item1);
            Assert.AreEqual("foo", t.Item2);
        }
    }
}
