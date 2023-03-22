using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Server.HTTP;
using NUnit.Framework;

namespace KRPC.Test.Server.HTTP
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Portability", "DoNotHardcodePathsRule")]
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    public class RequestTest
    {
        [Test]
        public void ValidRequest ()
        {
            var request = Request.FromString (
                              "GET /path/to/resource?arg1=value1&arg2=value1&arg3=value3 HTTP/1.1\r\n" +
                              "Header1: Value1\r\n" +
                              "Header2:Value2\r\n" +
                              "Header3 : Value3, Value4\r\n" +
                              "\r\n"
                          );
            Assert.AreEqual ("http/1.1", request.Protocol);
            Assert.AreEqual ("get", request.Method);
            Assert.AreEqual ("/path/to/resource", request.URI.LocalPath);
            Assert.AreEqual ("?arg1=value1&arg2=value1&arg3=value3", request.URI.Query);
            var expectedHeaders = new Dictionary<string, IList<string>>();
            expectedHeaders["header1"] = new List<string> { "Value1" };
            expectedHeaders["header2"] = new List<string> { "Value2" };
            expectedHeaders["header3"] = new List<string> { "Value3" };
            expectedHeaders["header3"] = new List<string> { "Value3", "Value4" };
            CollectionAssert.AreEquivalent(expectedHeaders, request.Headers);
        }

        [Test]
        public void MalformedRequestLine ()
        {
            Assert.Throws <MalformedRequestException> (() => Request.FromString ("GET HTTP/1.1\r\n\r\n"));
        }

        [Test]
        public void MalformedMethod ()
        {
            Assert.DoesNotThrow (() => Request.FromString ("GET / HTTP/1.1\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (" / HTTP/1.1\r\n\r\n"));
        }

        [Test]
        public void MalformedURI ()
        {
            Assert.DoesNotThrow (() => Request.FromString ("GET / HTTP/1.1\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString ("GET /////\\\\\\\\ HTTP/1.1\r\n\r\n"));
        }

        [Test]
        public void MalformedProtocol ()
        {
            Assert.DoesNotThrow (() => Request.FromString ("GET / HTTP/1.1\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString ("GET / \r\n\r\n"));
        }

        [Test]
        public void MalformedHeaders ()
        {
            const string requestString = "GET / HTTP/1.1\r\n";
            Assert.DoesNotThrow (() => Request.FromString (requestString + "Key: Value\r\n\r\n"));
            Assert.DoesNotThrow (() => Request.FromString (requestString + "Key:Value\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + "Key Value\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + "Key:\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + ":Value\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + "Key: Value\r\nKey: Value\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + "Key: Value\r\nkey: Value\r\n\r\n"));
        }

        [Test]
        public void EmptyRequest ()
        {
            Assert.Throws <MalformedRequestException> (() => Request.FromString (string.Empty));
        }

        [Test]
        public void TruncatedRequest ()
        {
            const string requestString = "GET / HTTP/1.1";
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + "\r\n"));
            Assert.DoesNotThrow (() => Request.FromString (requestString + "\r\n\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + "\r\nField: Value\r\n"));
            Assert.DoesNotThrow (() => Request.FromString (requestString + "\r\nField: Value\r\n\r\n"));
        }

        [Test]
        public void RequestTooLong ()
        {
            const string requestString = "GET / HTTP/1.1\r\nField: Value\r\n\r\n";
            Assert.DoesNotThrow (() => Request.FromString (requestString));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + "\r\n"));
            Assert.Throws <MalformedRequestException> (() => Request.FromString (requestString + "foo"));
        }
    }
}

