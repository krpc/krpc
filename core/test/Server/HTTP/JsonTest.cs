using System.Collections.Generic;
using KRPC.Server.HTTP;
using NUnit.Framework;

namespace KRPC.Test.Server.HTTP
{
    [TestFixture]
    public class JsonTest
    {
        [Test]
        public void ParseValues ()
        {
            Assert.IsNull (Json.Parse ("null"));
            Assert.AreEqual (true, Json.Parse (" true "));
            Assert.AreEqual (false, Json.Parse ("false"));
            Assert.AreEqual ("a\"b\\c\n\u00e9\u2603/", Json.Parse ("\"a\\\"b\\\\c\\n\u00e9\\u2603\\/\""));
            Assert.AreEqual ("\U0001F680", Json.Parse ("\"\\ud83d\\ude80\""));
            Assert.AreEqual ("-12.5e+3", ((JsonNumber)Json.Parse ("-12.5e+3")).Text);
            Assert.AreEqual ("18446744073709551615", ((JsonNumber)Json.Parse ("18446744073709551615")).Text);
        }

        [Test]
        public void ParseCollections ()
        {
            var value = (Dictionary<string, object>)Json.Parse ("{\"a\": [1, \"x\", null], \"b\": {}}");
            Assert.AreEqual (2, value.Count);
            var list = (List<object>)value ["a"];
            Assert.AreEqual ("1", ((JsonNumber)list [0]).Text);
            Assert.AreEqual ("x", list [1]);
            Assert.IsNull (list [2]);
            Assert.AreEqual (0, ((Dictionary<string, object>)value ["b"]).Count);
            Assert.AreEqual (0, ((List<object>)Json.Parse ("[]")).Count);
        }

        [TestCase ("")]
        [TestCase ("{")]
        [TestCase ("[1,]")]
        [TestCase ("{\"a\": 1,}")]
        [TestCase ("{\"a\": 1, \"a\": 2}")]
        [TestCase ("{a: 1}")]
        [TestCase ("01")]
        [TestCase ("1.")]
        [TestCase ("-")]
        [TestCase ("tru")]
        [TestCase ("\"\\x\"")]
        [TestCase ("\"a")]
        [TestCase ("1 2")]
        [TestCase ("NaN")]
        [TestCase ("\"\\ud800\"")]
        [TestCase ("\"\\udc00\\ud800\"")]
        public void ParseMalformed (string text)
        {
            Assert.Throws<JsonException> (() => Json.Parse (text));
        }

        [Test]
        public void ParseNestedTooDeeply ()
        {
            Assert.Throws<JsonException> (() => Json.Parse (new string ('[', 100) + new string (']', 100)));
        }

        [Test]
        public void Write ()
        {
            var value = new List<KeyValuePair<string, object>> {
                new KeyValuePair<string, object> ("b", new List<object> { 1, -2L, 3u, 4ul, 0.5, 0.1f, true, null }),
                new KeyValuePair<string, object> ("a", "x\"\\\n\u0001\u00e9")
            };
            Assert.AreEqual (
                "{\"b\": [1, -2, 3, 4, 0.5, 0.1, true, null], \"a\": \"x\\\"\\\\\\n\\u0001\u00e9\"}",
                Json.Write (value));
        }

        [Test]
        public void WriteNonFinite ()
        {
            Assert.AreEqual (
                "[\"NaN\", \"Infinity\", \"-Infinity\"]",
                Json.Write (new List<object> { double.NaN, double.PositiveInfinity, float.NegativeInfinity }));
        }
    }
}
