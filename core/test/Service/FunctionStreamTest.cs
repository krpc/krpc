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
    public class FunctionStreamTest
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
            var stream = new FunctionStream (expr);

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
        public void LambdaWithNoParameters ()
        {
            var obj = new TestService.TestClass ("foo");
            obj.IntProperty = 42;
            var expr = Expression.Lambda (new List<Expression> (), Expression.Call (BuildProcedureCall (
                "TestClass_get_IntProperty", new Argument (0, obj))));
            var stream = new FunctionStream (expr);
            stream.UpdateInternal ();
            Assert.AreEqual (42, stream.Result.Value);
            Assert.AreEqual (TypeSpec.Create (typeof (int)), stream.Result.Spec);
        }

        [Test]
        public void ValueIsEncodedOnce ()
        {
            var obj = new TestService.TestClass ("foo");
            obj.IntProperty = 42;
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_get_IntProperty", new Argument (0, obj)));
            var stream = new FunctionStream (expr);
            stream.UpdateInternal ();
            var encoded = stream.Result.EncodedValue;
            Assert.AreEqual (
                global::KRPC.Server.ProtocolBuffers.Encoder.Encode (42, TypeSpec.Create (typeof (int))),
                encoded);
            stream.Sent ();
            stream.UpdateInternal ();
            Assert.AreSame (encoded, stream.Result.EncodedValue);
        }

        [Test]
        public void NullValue ()
        {
            var obj = new TestService.TestClass ("foo");
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_get_ObjectProperty", new Argument (0, obj)));
            var stream = new FunctionStream (expr);

            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsTrue (stream.Result.HasValue);
            Assert.IsNull (stream.Result.Value);

            stream.Sent ();
            stream.UpdateInternal ();
            Assert.IsFalse (stream.Changed);

            obj.ObjectProperty = new TestService.TestClass ("bar");
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.AreEqual (obj.ObjectProperty, stream.Result.Value);

            stream.Sent ();
            obj.ObjectProperty = null;
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsNull (stream.Result.Value);
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
            var stream = new FunctionStream (expr);
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
            var stream = new FunctionStream (expr);
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsFalse (stream.Result.HasValue);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("not available in game scene", stream.Result.Error.Description);
        }

        [Test]
        public void ValueThatCannotBeEncodedIsAnError ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoListOfObjects (It.IsAny<IList<TestService.TestClass>> ()))
                .Returns (new List<TestService.TestClass> { null });
            TestService.Service = mock.Object;
            var expr = Expression.Call (BuildProcedureCall (
                "EchoListOfObjects", new Argument (0, new List<TestService.TestClass> ())));
            var stream = new FunctionStream (expr);
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsFalse (stream.Result.HasValue);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("is null", stream.Result.Error.Description);
        }

        [Test]
        public void NullableElementsAreStreamed ()
        {
            var obj = new TestService.TestClass ("foo");
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoListOfNullableObjects (It.IsAny<IList<TestService.TestClass>> ()))
                .Returns (new List<TestService.TestClass> { obj, null });
            TestService.Service = mock.Object;
            var expr = Expression.Call (BuildProcedureCall (
                "EchoListOfNullableObjects", new Argument (0, new List<TestService.TestClass> ())));
            var stream = new FunctionStream (expr);
            stream.UpdateInternal ();
            Assert.IsFalse (stream.Result.HasError);
            Assert.IsTrue (stream.Result.Spec.Types [0].Nullable);
            CollectionAssert.AreEqual (
                new List<TestService.TestClass> { obj, null }, (IList<TestService.TestClass>)stream.Result.Value);
        }

        [Test]
        public void NullValueThatIsNotNullableIsAnError ()
        {
            var stream = new FunctionStream (System.Linq.Expressions.Expression.Constant (null, typeof (string)));
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsFalse (stream.Result.HasValue);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("evaluated to null", stream.Result.Error.Description);
        }

        [Test]
        public void YieldingProcedureIsReported ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => {
                    throw new YieldException<Func<int>> (() => 0);
                });
            TestService.Service = mock.Object;
            var expr = Expression.Call (BuildProcedureCall (
                "BlockingProcedureReturns", new Argument (0, 1)));
            var stream = new FunctionStream (expr);
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            Assert.IsFalse (stream.Result.HasValue);
            Assert.IsTrue (stream.Result.HasError);
            StringAssert.Contains ("paused execution", stream.Result.Error.Description);
            Assert.IsNotNull (stream.Result.Error.StackTrace);
        }

        [Test]
        public void LazyCollectionRejected ()
        {
            var list = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (1), Expression.ConstantInt (2)
            });
            var param = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { param },
                Expression.Multiply (param, Expression.ConstantInt (2)));
            var selected = Expression.Select (list, func);
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new FunctionStream (selected));

            var stream = new FunctionStream (Expression.ToList (selected));
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Result.HasValue);
            CollectionAssert.AreEqual (new [] { 2, 4 }, (List<int>)stream.Result.Value);
        }

        [Test]
        public void UnboundMarkerRejected ()
        {
            var expr = Expression.Block (new List<Expression> {
                Expression.Break (),
                Expression.ConstantInt (1)
            });
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new FunctionStream (expr));
        }

        [Test]
        public void TwoStreamsOfOneFunctionAdvanceItsStateTwice ()
        {
            var count = Expression.StateVariable ("count", Expression.ConstantInt (0));
            var increment = Expression.Block (new List<Expression> {
                Expression.Assign (count, Expression.Add (count, Expression.ConstantInt (1))),
                count
            });
            var first = new FunctionStream (increment);
            var second = new FunctionStream (increment);
            first.UpdateInternal ();
            second.UpdateInternal ();
            Assert.AreEqual (1, first.Result.Value);
            Assert.AreEqual (2, second.Result.Value);
            first.UpdateInternal ();
            second.UpdateInternal ();
            Assert.AreEqual (3, first.Result.Value);
            Assert.AreEqual (4, second.Result.Value);
        }
    }
}
