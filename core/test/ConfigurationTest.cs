using NUnit.Framework;

namespace KRPC.Test
{
    [TestFixture]
    public class ConfigurationTest
    {
        [Test]
        public void DefaultConfig ()
        {
            var config = new Configuration ();
            Assert.AreEqual (true, config.MainWindowVisible);
            Assert.AreEqual (false, config.AutoAcceptConnections);
        }
    }
}
