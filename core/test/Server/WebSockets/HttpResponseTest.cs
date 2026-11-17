using System;
using KRPC.Server.WebSockets;
using NUnit.Framework;

namespace KRPC.Test.Server.WebSockets
{
    [TestFixture]
    public class HttpResponseTest
    {
        [Test]
        public void ValidResponse ()
        {
            var response = new HttpResponse (404, "Not Found");
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\n\r\n", response.ToString ());
        }

        [Test]
        public void ValidResponseWithHeaderFiields ()
        {
            var response = new HttpResponse (404, "Not Found");
            response.AddHeaderField ("Foo", "Bar");
            response.AddHeaderField ("Baz", "Alpha");
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\nFoo: Bar\r\nBaz: Alpha\r\n\r\n", response.ToString ());
        }

        [Test]
        public void Responses ()
        {
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\n", HttpResponse.CreateBadRequest ().ToString ());
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\n\r\n", HttpResponse.CreateNotFound ().ToString ());
            Assert.AreEqual ("HTTP/1.1 405 Method Not Allowed\r\n\r\n", HttpResponse.CreateMethodNotAllowed ().ToString ());
            Assert.AreEqual ("HTTP/1.1 426 Upgrade Required\r\n\r\n", HttpResponse.CreateUpgradeRequired ().ToString ());
            Assert.AreEqual ("HTTP/1.1 505 HTTP Version Not Supported\r\n\r\n", HttpResponse.CreateHTTPVersionNotSupported ().ToString ());
        }

        [Test]
        public void MalformedReason ()
        {
            Assert.DoesNotThrow (() => new HttpResponse (404, "Not Found"));
            Assert.Throws<ArgumentException> (() => new HttpResponse (404, string.Empty));
            Assert.Throws<ArgumentException> (() => new HttpResponse (404, "Foo\nBar\r"));
        }

        [Test]
        public void MalformedHeaders ()
        {
            Assert.DoesNotThrow (() => new HttpResponse (200, "OK").AddHeaderField ("Foo", "Bar"));
            Assert.Throws<ArgumentException> (() => new HttpResponse (200, "OK").AddHeaderField (string.Empty, string.Empty));
            Assert.Throws<ArgumentException> (() => new HttpResponse (200, "OK").AddHeaderField ("Foo", string.Empty));
            Assert.Throws<ArgumentException> (() => new HttpResponse (200, "OK").AddHeaderField (string.Empty, "Foo"));
        }
    }
}
