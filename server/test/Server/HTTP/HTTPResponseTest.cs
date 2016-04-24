using System;
using KRPC.Server.HTTP;
using NUnit.Framework;

namespace KRPC.Test.Server.HTTP
{
    [TestFixture]
    public class HTTPResponseTest
    {
        [Test]
        public void ValidResponse ()
        {
            var response = new HTTPResponse (404, "Not Found");
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\n\r\n", response.ToString ());
        }

        [Test]
        public void ValidResponseWithHeaderFiields ()
        {
            var response = new HTTPResponse (404, "Not Found");
            response.AddHeaderField ("Foo", "Bar");
            response.AddHeaderField ("Baz", "Jeb");
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\nFoo: Bar\r\nBaz: Jeb\r\n\r\n", response.ToString ());
        }

        [Test]
        public void Responses ()
        {
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\n", HTTPResponse.BadRequest.ToString ());
            Assert.AreEqual ("HTTP/1.1 403 Forbidden\r\n\r\n", HTTPResponse.Forbidden.ToString ());
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\n\r\n", HTTPResponse.NotFound.ToString ());
            Assert.AreEqual ("HTTP/1.1 405 Method Not Allowed\r\n\r\n", HTTPResponse.MethodNotAllowed.ToString ());
            Assert.AreEqual ("HTTP/1.1 426 Upgrade Required\r\n\r\n", HTTPResponse.UpgradeRequired.ToString ());
            Assert.AreEqual ("HTTP/1.1 500 Internal Server Error\r\n\r\n", HTTPResponse.InternalServerError.ToString ());
            Assert.AreEqual ("HTTP/1.1 505 HTTP Version Not Supported\r\n\r\n", HTTPResponse.HTTPVersionNotSupported.ToString ());
        }

        [Test]
        public void MalformedReason ()
        {
            Assert.DoesNotThrow (() => new HTTPResponse (404, "Not Found"));
            Assert.Throws<ArgumentException> (() => new HTTPResponse (404, ""));
            Assert.Throws<ArgumentException> (() => new HTTPResponse (404, "Foo\nBar\r"));
        }

        [Test]
        public void MalformedHeaders ()
        {
            Assert.DoesNotThrow (() => new HTTPResponse (200, "OK").AddHeaderField ("Foo", "Bar"));
            Assert.Throws<ArgumentException> (() => new HTTPResponse (200, "OK").AddHeaderField ("", ""));
            Assert.Throws<ArgumentException> (() => new HTTPResponse (200, "OK").AddHeaderField ("Foo", ""));
            Assert.Throws<ArgumentException> (() => new HTTPResponse (200, "OK").AddHeaderField ("", "Foo"));
        }
    }
}
