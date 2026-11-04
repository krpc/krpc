using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Scanner;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ProcedureSignatureTest
    {
        public static global::KRPC.Service.Messages.Stream ReturnsAStream ()
        {
            return null;
        }

        public static IList<global::KRPC.Service.Messages.Stream> ReturnsStreamsInAList ()
        {
            return null;
        }

        public static global::KRPC.Service.Messages.Event ReturnsAnEvent ()
        {
            return null;
        }

        public static IList<global::KRPC.Service.Messages.Event> ReturnsEventsInAList ()
        {
            return null;
        }

        static ProcedureSignature Signature (string method, bool isInternal, string service = "KRPC")
        {
            var handler = new ProcedureHandler (typeof(ProcedureSignatureTest).GetMethod (method), false);
            return new ProcedureSignature (
                service, method, 1, string.Empty, handler, GameScene.All, false, string.Empty,
                isInternal);
        }

        [TestCase ("ReturnsAStream", true)]
        [TestCase ("ReturnsAnEvent", false)]
        [TestCase ("ReturnsAnEvent", true)]
        public void ProcedureReturnsAStreamAsItsWholeValue (string method, bool isInternal)
        {
            Assert.DoesNotThrow (() => Signature (method, isInternal));
        }

        [TestCase ("ReturnsAStream", false)]
        [TestCase ("ReturnsStreamsInAList", false)]
        [TestCase ("ReturnsStreamsInAList", true)]
        [TestCase ("ReturnsEventsInAList", false)]
        public void ProcedureReturnsAStream (string method, bool isInternal)
        {
            var exn = Assert.Throws<ServiceException> (() => Signature (method, isInternal));
            StringAssert.Contains ("KRPC." + method, exn.Message);
            StringAssert.Contains ("holds a stream", exn.Message);
        }

        [Test]
        public void InternalProcedureOutsideTheKRPCService ()
        {
            var exn = Assert.Throws<ServiceException> (() => Signature ("ReturnsAStream", true, "TestService"));
            StringAssert.Contains ("TestService.ReturnsAStream is internal", exn.Message);
        }
    }
}
