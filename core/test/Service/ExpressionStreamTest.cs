using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;
using Expression = KRPC.Service.KRPC.Expression;
using Type = KRPC.Service.KRPC.Type;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ExpressionStreamTest
    {
        [SetUp]
        public void SetUp ()
        {
            CallContext.GameScene = GameScene.Flight;
        }

        static ProcedureCall BuildProcedureCall (string procedure, params Argument[] args)
        {
            var call = new ProcedureCall ("TestService", procedure);
            foreach (var arg in args)
                call.Arguments.Add (arg);
            return call;
        }

        [Test]
        public void UpdatesAndChangeDetection ()
        {
            var obj = new TestService.TestClass ("foo");
            obj.IntProperty = 42;
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_get_IntProperty", new Argument (0, obj)));
            var stream = new ExpressionStream (expr);

            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsTrue (stream.Result.HasValue);
            Assert.AreEqual (42, stream.Result.Value);

            stream.Sent ();
            Assert.IsFalse (stream.Changed);
            stream.UpdateInternal ();
            Assert.IsFalse (stream.Changed);

            obj.IntProperty = 43;
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.AreEqual (43, stream.Result.Value);
        }

        [Test]
        public void ComputedValue ()
        {
            var obj = new TestService.TestClass ("foo");
            obj.IntProperty = 21;
            var expr = Expression.Multiply (
                Expression.Call (BuildProcedureCall (
                    "TestClass_get_IntProperty", new Argument (0, obj))),
                Expression.ConstantDouble (2));
            var stream = new ExpressionStream (expr);
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Result.HasValue);
            Assert.AreEqual (42d, stream.Result.Value);
        }

        [Test]
        public void ErrorsAreCaptured ()
        {
            var obj = new TestService.TestClass ("foo");
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_MethodAvailableInSpecifiedGameScene", new Argument (0, obj)));
            var stream = new ExpressionStream (expr);
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsFalse (stream.Result.HasValue);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("not available in game scene", stream.Result.Error.Description);
        }

        [Test]
        public void YieldingProcedureIsReported ()
        {
            // Evaluating the expression again would repeat everything it did before
            // the procedure paused, so the pause is reported rather than retried
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => {
                    throw new YieldException<Func<int>> (() => 0);
                });
            TestService.Service = mock.Object;
            var expr = Expression.Call (BuildProcedureCall (
                "BlockingProcedureReturns", new Argument (0, 1)));
            var stream = new ExpressionStream (expr);
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsFalse (stream.Result.HasValue);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("paused execution", stream.Result.Error.Description);
        }

        [Test]
        public void LazyCollectionRejected ()
        {
            var list = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (1), Expression.ConstantInt (2)
            });
            var param = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Function (
                new List<Expression> { param },
                Expression.Multiply (param, Expression.ConstantInt (2)));
            var selected = Expression.Select (list, func);
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new ExpressionStream (selected));

            var stream = new ExpressionStream (Expression.ToList (selected));
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Result.HasValue);
            CollectionAssert.AreEqual (new [] { 2, 4 }, (List<int>)stream.Result.Value);
        }

        [Test]
        public void UnboundMarkerRejected ()
        {
            // Rejected when the stream is created, rather than producing an error
            // result on every update.
            var expr = Expression.Block (new List<Expression> {
                Expression.Break (),
                Expression.ConstantInt (1)
            });
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new ExpressionStream (expr));
        }
    }
}
