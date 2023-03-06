using KRPC.Continuations;
using NUnit.Framework;

namespace KRPC.Test.Continuations
{
    [TestFixture]
    public class ParameterizedContinuationTest
    {
        int value;

        public int FnEcho (double x)
        {
            return (int)x;
        }

        public void FnSet (int x)
        {
            value = x;
        }

        public void FnYield (int x)
        {
            throw new YieldException (new ParameterizedContinuation<int, double> (FnEcho, x));
        }

        public void FnAdd (double x, float y)
        {
            value = (int)(x + y);
        }

        public int FnAddReturn (float x, double y)
        {
            return (int)(x + y);
        }

        [Test]
        public void ContinuationWithReturn ()
        {
            IContinuation cont = new ParameterizedContinuation<int,double> (FnEcho, 42d);
            Assert.AreEqual (42, cont.RunUntyped ());
        }

        [Test]
        public void ContinuationWithoutReturn ()
        {
            value = 0;
            IContinuation cont = new ParameterizedContinuationVoid<int> (FnSet, 42);
            Assert.AreEqual (null, cont.RunUntyped ());
            Assert.AreEqual (42, value);
        }

        [Test]
        public void ContinuationWithYield ()
        {
            IContinuation cont = new ParameterizedContinuationVoid<int> (FnYield, 1234);
            int result = 0;
            try {
                cont.RunUntyped ();
            } catch (YieldException e) {
                result = (int)e.Continuation.RunUntyped ();
            }
            Assert.AreEqual (1234, result);
        }

        [Test]
        public void ContinuationWithoutReturnWithMultipleArguments ()
        {
            IContinuation cont = new ParameterizedContinuationVoid<double,float> (FnAdd, 40d, 2f);
            Assert.AreEqual (null, cont.RunUntyped ());
            Assert.AreEqual (42, value);
        }

        [Test]
        public void ContinuationWithReturnWithMultipleArguments ()
        {
            IContinuation cont = new ParameterizedContinuation<int,float,double> (FnAddReturn, 40f, 2d);
            Assert.AreEqual (42, cont.RunUntyped ());
            Assert.AreEqual (42, value);
        }
    }
}
