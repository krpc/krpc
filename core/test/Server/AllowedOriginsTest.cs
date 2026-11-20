using System;
using KRPC.Server;
using NUnit.Framework;

namespace KRPC.Test.Server
{
    [TestFixture]
    public class AllowedOriginsTest
    {
        [Test]
        public void Empty ()
        {
            foreach (var setting in new [] { null, "", " , " }) {
                var origins = new AllowedOrigins (setting);
                Assert.IsTrue (origins.None);
                Assert.IsFalse (origins.Allows ("http://localhost:8080"));
                Assert.AreEqual ("", origins.ToString ());
            }
        }

        [Test]
        public void List ()
        {
            var origins = new AllowedOrigins ("http://localhost:8080, https://editor.swagger.io");
            Assert.IsFalse (origins.None);
            Assert.IsTrue (origins.Allows ("http://localhost:8080"));
            Assert.IsTrue (origins.Allows ("https://editor.swagger.io"));
            Assert.IsFalse (origins.Allows ("http://localhost:8081"));
            Assert.IsFalse (origins.Allows ("http://editor.swagger.io"));
            Assert.IsFalse (origins.Allows ("null"));
            Assert.AreEqual ("http://localhost:8080, https://editor.swagger.io", origins.ToString ());
        }

        [Test]
        public void Canonical ()
        {
            // An origin header leaves out a default port, and has a lower case host
            var origins = new AllowedOrigins ("HTTP://Example.COM:80 https://example.org:443");
            Assert.IsTrue (origins.Allows ("http://example.com"));
            Assert.IsTrue (origins.Allows ("https://example.org"));
            Assert.IsTrue (origins.Allows ("http://EXAMPLE.com"));
        }

        [Test]
        public void Any ()
        {
            var origins = new AllowedOrigins ("*");
            Assert.IsTrue (origins.Any);
            Assert.IsTrue (origins.Allows ("http://anything.example"));
            Assert.IsTrue (origins.Allows ("null"));
            Assert.AreEqual ("*", origins.ToString ());
        }

        [TestCase ("localhost:8080")]
        [TestCase ("http://localhost:8080/")]
        [TestCase ("http://localhost:8080/path")]
        [TestCase ("http://localhost?x=1")]
        [TestCase ("http://user@localhost")]
        [TestCase ("ftp://localhost")]
        [TestCase ("null")]
        [TestCase ("http://localhost:8080, nope")]
        public void Invalid (string setting)
        {
            Assert.IsFalse (AllowedOrigins.IsValid (setting));
            Assert.Throws<ArgumentException> (() => new AllowedOrigins (setting));
        }

        [TestCase ("")]
        [TestCase ("*")]
        [TestCase ("http://localhost:8080,https://editor.swagger.io")]
        [TestCase ("http://[::1]:8080")]
        public void Valid (string setting)
        {
            Assert.IsTrue (AllowedOrigins.IsValid (setting));
        }
    }
}
