using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Server.HTTP;
using NUnit.Framework;

namespace KRPC.Test.Server.HTTP
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    public class ResponseTest
    {
        [Test]
        public void ValidResponse ()
        {
            var response = new Response (404, "Not Found");
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\n\r\n", response.ToString ());
        }

        [Test]
        public void ValidResponseWithHeaderFiields ()
        {
            var response = new Response (404, "Not Found");
            response.AddHeaderField ("Foo", "Bar");
            response.AddHeaderField ("Baz", "Jeb");
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\nFoo: Bar\r\nBaz: Jeb\r\n\r\n", response.ToString ());
        }

        [Test]
        public void Responses ()
        {
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\n", Response.CreateBadRequest ().ToString ());
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\n\r\n", Response.CreateNotFound ().ToString ());
            Assert.AreEqual ("HTTP/1.1 405 Method Not Allowed\r\n\r\n", Response.CreateMethodNotAllowed ().ToString ());
            Assert.AreEqual ("HTTP/1.1 426 Upgrade Required\r\n\r\n", Response.CreateUpgradeRequired ().ToString ());
            Assert.AreEqual ("HTTP/1.1 505 HTTP Version Not Supported\r\n\r\n", Response.CreateHTTPVersionNotSupported ().ToString ());
        }

        [Test]
        public void MalformedReason ()
        {
            Assert.DoesNotThrow (() => new Response (404, "Not Found"));
            Assert.Throws<ArgumentException> (() => new Response (404, string.Empty));
            Assert.Throws<ArgumentException> (() => new Response (404, "Foo\nBar\r"));
        }

        [Test]
        public void MalformedHeaders ()
        {
            Assert.DoesNotThrow (() => new Response (200, "OK").AddHeaderField ("Foo", "Bar"));
            Assert.Throws<ArgumentException> (() => new Response (200, "OK").AddHeaderField (string.Empty, string.Empty));
            Assert.Throws<ArgumentException> (() => new Response (200, "OK").AddHeaderField ("Foo", string.Empty));
            Assert.Throws<ArgumentException> (() => new Response (200, "OK").AddHeaderField (string.Empty, "Foo"));
        }
    }
}
