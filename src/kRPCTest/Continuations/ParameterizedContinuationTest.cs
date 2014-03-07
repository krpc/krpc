using System;
using NUnit.Framework;
using KRPC.Continuations;

namespace KRPCTest.Continuations
{
    [TestFixture]
    public class ParameterizedContinuationTest
    {
        int x;

        public int FnEcho(int x)
        {
            return x;
        }

        public void FnSet(int x)
        {
            this.x = x;
        }

        public void FnYield(int x)
        {
            throw new YieldException(new ParameterizedContinuation<int, int> (FnEcho, x));
        }

        [Test]
        public void ContinuationWithoutReturn ()
        {
            IContinuation cont = new ParameterizedContinuation<int,int> (FnEcho, 42);
            Assert.AreEqual(42, cont.RunUntyped ());
        }

        [Test]
        public void ContinuationWithReturn ()
        {
            x = 0;
            IContinuation cont = new ParameterizedContinuation<int> (FnSet, 42);
            Assert.AreEqual(null, cont.RunUntyped ());
            Assert.AreEqual(42, x);
        }

        [Test]
        public void ContinuationWithYield ()
        {
            IContinuation cont = new ParameterizedContinuation<int> (FnYield, 1234);
            int result = 0;
            try {
                cont.RunUntyped ();
            } catch (YieldException e) {
                result = (int) e.Continuation.RunUntyped ();
            }
            Assert.AreEqual(1234, result);
        }
    }
}
