using NUnit.Framework;
using System;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class ProcedureHandlerTest
    {
        public static float TestProcedure (int x, float y)
        {
            return x + y;
        }

        [Test]
        public void SimpleUsage ()
        {
            var handler = new ProcedureHandler (typeof(ProcedureHandlerTest).GetMethod ("TestProcedure"));
            Assert.AreEqual (new []{ typeof(int), typeof(float) }, handler.GetParameters ());
            Assert.AreEqual (typeof(float), handler.ReturnType);
            Assert.AreEqual (3.14159f, handler.Invoke (new object[] { 3, 0.14159f }));
        }
    }
}

