using KRPC.Server.HTTP;
using NUnit.Framework;

namespace KRPC.Test.Server.HTTP
{
    [TestFixture]
    public class RoutesTest
    {
        [TestCase ("SpaceCenter", "space-center")]
        [TestCase ("ActiveVessel", "active-vessel")]
        [TestCase ("MeanAnomalyAtUT", "mean-anomaly-at-ut")]
        [TestCase ("UInt64ToString", "uint64-to-string")]
        [TestCase ("KRPC", "krpc")]
        public void KebabCase (string name, string expected)
        {
            Assert.AreEqual (expected, Routes.KebabCase (name));
        }
    }
}
