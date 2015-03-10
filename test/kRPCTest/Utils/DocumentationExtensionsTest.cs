using System.Reflection;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPCTest.Utils
{
    [TestFixture]
    public class DocumentationExtentionsTest
    {
        /// <summary>
        /// Baz
        /// </summary>
        void foo ()
        {
        }

        void bar ()
        {
        }

        [Test]
        public void TestLoadDocumentation ()
        {
            var method = typeof(DocumentationExtentionsTest).GetMethod ("foo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual ("Baz", method.GetDocumentation ());
        }

        [Test]
        public void TestNoDocumentation ()
        {
            var method = typeof(DocumentationExtentionsTest).GetMethod ("bar", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual ("", method.GetDocumentation ());
        }
    }
}
