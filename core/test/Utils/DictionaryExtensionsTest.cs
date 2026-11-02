using System.Collections.Generic;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class DictionaryExtensionsTest
    {
        [Test]
        public void GetOrAddCreatesOnce ()
        {
            var dictionary = new Dictionary<int, string> ();
            int calls = 0;
            Assert.AreEqual ("1", dictionary.GetOrAdd (1, key => { calls++; return key.ToString (System.Globalization.CultureInfo.InvariantCulture); }));
            Assert.AreEqual ("1", dictionary.GetOrAdd (1, key => { calls++; return "other"; }));
            Assert.AreEqual (1, calls);
        }

        [Test]
        public void GetOrAddCachesNull ()
        {
            var dictionary = new Dictionary<int, string> ();
            int calls = 0;
            Assert.IsNull (dictionary.GetOrAdd (1, key => { calls++; return null; }));
            Assert.IsNull (dictionary.GetOrAdd (1, key => { calls++; return null; }));
            Assert.AreEqual (1, calls);
            Assert.IsTrue (dictionary.ContainsKey (1));
        }
    }
}
