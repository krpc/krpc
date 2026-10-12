using System.Collections.Generic;
using KRPC.Service.KRPC;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;

namespace KRPC.Test.Service.KRPC
{
    public partial class ExpressionTest
    {
        [Test]
        public void RunFunctionValue ()
        {
            var bytes = global::KRPC.Service.KRPC.KRPC.RunFunction (
                Expression.Multiply (Expression.ConstantInt (6), Expression.ConstantInt (7)));
            var value = global::KRPC.Server.ProtocolBuffers.Encoder.Decode (
                Google.Protobuf.ByteString.CopyFrom (bytes),
                global::KRPC.Service.TypeSpec.Create (typeof (int)));
            Assert.AreEqual (42, value);
        }

        [Test]
        public void RunFunctionNullValue ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("run");
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_get_ObjectProperty", new Argument (0, obj)));
            Assert.IsNull (global::KRPC.Service.KRPC.KRPC.RunFunction (expr));
            // The same function is not null once the property holds a value
            obj.ObjectProperty = new global::KRPC.Test.Service.TestService.TestClass ("other");
            Assert.IsNotNull (global::KRPC.Service.KRPC.KRPC.RunFunction (expr));
        }

        [Test]
        public void RunFunctionNullConstant ()
        {
            Assert.IsNull (global::KRPC.Service.KRPC.KRPC.RunFunction (
                Expression.ConstantNull (Type.String ())));
            Assert.IsNull (global::KRPC.Service.KRPC.KRPC.RunFunction (
                Expression.ConstantNull (Type.Int ())));
        }

        [Test]
        public void RunFunctionNullValueThatIsNotNullable ()
        {
            Expression nullString = System.Linq.Expressions.Expression.Constant (null, typeof (string));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (nullString));
            StringAssert.Contains ("evaluated to null", exn.Message);
        }

        [Test]
        public void RunFunctionNullableElements ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("run");
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoListOfNullableObjects (
                It.IsAny<IList<global::KRPC.Test.Service.TestService.TestClass>> ()))
                .Returns (new List<global::KRPC.Test.Service.TestService.TestClass> { obj, null });
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var expr = Expression.Call (BuildProcedureCall (
                "EchoListOfNullableObjects",
                new Argument (0, new List<global::KRPC.Test.Service.TestService.TestClass> ())));
            var bytes = global::KRPC.Service.KRPC.KRPC.RunFunction (expr);
            var value = global::KRPC.Server.ProtocolBuffers.Encoder.Decode (
                Google.Protobuf.ByteString.CopyFrom (bytes), expr.ReturnType.Spec);
            CollectionAssert.AreEqual (
                new List<global::KRPC.Test.Service.TestService.TestClass> { obj, null },
                (IList<global::KRPC.Test.Service.TestService.TestClass>)value);
        }

        [Test]
        public void RunFunctionIsNullable ()
        {
            var procedure = global::KRPC.Service.Services.Instance
                .Signatures ["KRPC"].Procedures ["RunFunction"];
            Assert.IsTrue (procedure.ReturnSpec.Nullable);
        }

        [Test]
        public void RunFunctionEffects ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("run");
            obj.IntProperty = 1;
            var bytes = global::KRPC.Service.KRPC.KRPC.RunFunction (
                Expression.Call (BuildProcedureCall (
                    "TestClass_set_IntProperty", new Argument (0, obj), new Argument (1, 5))));
            Assert.AreEqual (0, bytes.Length);
            Assert.AreEqual (5, obj.IntProperty);
        }

        [Test]
        public void RunFunctionWithAPausingProcedureIsAnError ()
        {
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => {
                    throw new global::KRPC.Service.YieldException<System.Func<int>> (() => 0);
                });
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var expr = Expression.Call (BuildProcedureCall (
                "BlockingProcedureReturns", new Argument (0, 1)));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (expr));
            StringAssert.Contains ("paused execution", exn.Message);
        }

        [Test]
        public void AddFunctionStreamWithNoValueIsRejected ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("run");
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_set_IntProperty", new Argument (0, obj), new Argument (1, 5)));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.AddFunctionStream (expr));
            StringAssert.Contains ("does not evaluate to a value", exn.Message);
        }

        [Test]
        public void RunFunctionWithTooManyNodesIsRejected ()
        {
            var expr = ChainWithTooManyNodes ();
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (expr));
            StringAssert.Contains ("more than 1000000 nodes", exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.AddFunctionStream (expr));
            StringAssert.Contains ("more than 1000000 nodes", exn.Message);
        }

        [Test]
        public void CompileFunctionBeforeRunning ()
        {
            var expr = Expression.Multiply (Expression.ConstantInt (6), Expression.ConstantInt (7));
            global::KRPC.Service.KRPC.KRPC.CompileFunction (expr);
            global::KRPC.Service.KRPC.KRPC.CompileFunction (expr);
            var bytes = global::KRPC.Service.KRPC.KRPC.RunFunction (expr);
            var value = global::KRPC.Server.ProtocolBuffers.Encoder.Decode (
                Google.Protobuf.ByteString.CopyFrom (bytes),
                global::KRPC.Service.TypeSpec.Create (typeof (int)));
            Assert.AreEqual (42, value);
        }

        [Test]
        public void CompileFunctionChecksTheFunction ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => global::KRPC.Service.KRPC.KRPC.CompileFunction (null));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.CompileFunction (ChainWithTooManyNodes ()));
            StringAssert.Contains ("more than 1000000 nodes", exn.Message);
        }

        [Test]
        public void FunctionProceduresCannotBeStreamed ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new global::KRPC.Service.ProcedureCallStream (new ProcedureCall ("KRPC", "RunFunction")));
            StringAssert.Contains (
                "KRPC.RunFunction cannot be streamed. Use KRPC.AddFunctionStream to stream a function.", exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new global::KRPC.Service.ProcedureCallStream (new ProcedureCall ("KRPC", "AddFunctionStream")));
            StringAssert.Contains ("KRPC.AddFunctionStream returns a stream, and cannot be streamed", exn.Message);
        }

        static byte[] Encode (object value, System.Type type)
        {
            return global::KRPC.Server.ProtocolBuffers.Encoder.Encode (
                value, global::KRPC.Service.TypeSpec.Create (type)).ToByteArray ();
        }

        static object Decode (byte[] bytes, System.Type type)
        {
            return global::KRPC.Server.ProtocolBuffers.Encoder.Decode (
                Google.Protobuf.ByteString.CopyFrom (bytes),
                global::KRPC.Service.TypeSpec.Create (type));
        }

        static Expression Multiplier ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            return Expression.Lambda (new List<Expression> { x, y }, Expression.Multiply (x, y));
        }

        [Test]
        public void RunFunctionWithArguments ()
        {
            var function = Multiplier ();
            Assert.AreEqual (42, Decode (global::KRPC.Service.KRPC.KRPC.RunFunction (
                function, new List<byte[]> { Encode (6, typeof (int)), Encode (7, typeof (int)) }),
                typeof (int)));
            Assert.AreEqual (-10, Decode (global::KRPC.Service.KRPC.KRPC.RunFunction (
                function, new List<byte[]> { Encode (5, typeof (int)), Encode (-2, typeof (int)) }),
                typeof (int)));
        }

        [Test]
        public void RunFunctionWithAnObjectArgument ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("run");
            obj.IntProperty = 3;
            var param = Expression.Parameter ("obj", Type.ClassType ("TestService", "TestClass"));
            var function = Expression.Lambda (new List<Expression> { param },
                Expression.CallWithArguments (BuildProcedureCall ("TestClass_get_IntProperty"),
                    new Dictionary<int, Expression> { { 0, param } }));
            var bytes = global::KRPC.Service.KRPC.KRPC.RunFunction (function, new List<byte[]> {
                Encode (obj, typeof (global::KRPC.Test.Service.TestService.TestClass)) });
            Assert.AreEqual (3, Decode (bytes, typeof (int)));
        }

        [Test]
        public void RunFunctionWithANullArgument ()
        {
            var param = Expression.Parameter ("s", Type.NullableType (Type.String ()));
            var function = Expression.Lambda (new List<Expression> { param }, Expression.IsNull (param));
            Assert.AreEqual (true, Decode (global::KRPC.Service.KRPC.KRPC.RunFunction (
                function, new List<byte[]> { null }), typeof (bool)));
            Assert.AreEqual (false, Decode (global::KRPC.Service.KRPC.KRPC.RunFunction (
                function, new List<byte[]> { Encode ("foo", typeof (string)) }), typeof (bool)));
        }

        [Test]
        public void RunFunctionWithANullArgumentThatIsNotNullable ()
        {
            var param = Expression.Parameter ("s", Type.String ());
            var function = Expression.Lambda (new List<Expression> { param }, param);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (function, new List<byte[]> { null }));
            StringAssert.Contains ("parameter s is null", exn.Message);
        }

        [Test]
        public void RunFunctionWithTheWrongNumberOfArguments ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (
                    Multiplier (), new List<byte[]> { Encode (6, typeof (int)) }));
            StringAssert.Contains ("takes 2 arguments, got 1", exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (Multiplier ()));
            StringAssert.Contains ("takes 2 arguments, got 0", exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (
                    Expression.ConstantInt (1), new List<byte[]> { Encode (6, typeof (int)) }));
            StringAssert.Contains ("takes 0 arguments, got 1", exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (Expression.Lambda (
                    new List<Expression> { Expression.Parameter ("x", Type.Int ()) }, Expression.ConstantInt (1))));
            StringAssert.Contains ("takes 1 argument, got 0", exn.Message);
        }

        [Test]
        public void RunFunctionWithAnArgumentThatDoesNotDecode ()
        {
            var param = Expression.Parameter ("x", Type.Double ());
            var function = Expression.Lambda (new List<Expression> { param }, param);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (
                    function, new List<byte[]> { new byte [] { 1 } }));
            StringAssert.Contains ("parameter x is not a value of type", exn.Message);
        }

        [Test]
        public void RunFunctionWithAnObjectOfTheWrongClass ()
        {
            var param = Expression.Parameter ("p", Type.ClassType ("TestService", "TestClass"));
            var function = Expression.Lambda (new List<Expression> { param }, param);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (
                    function, new List<byte[]> { Encode (Type.Double (), typeof (Type)) }));
            StringAssert.Contains ("parameter p is not a value of type", exn.Message);
        }

        [Test]
        public void RunFunctionWithALambdaWithNoParameters ()
        {
            var function = Expression.Lambda (new List<Expression> (), Expression.Block (
                new List<Expression> { Expression.Return (Expression.ConstantInt (42)) }));
            Assert.IsTrue (function.HasReturnType);
            Assert.AreEqual (Type.Int (), function.ReturnType);
            Assert.AreEqual (42, Decode (global::KRPC.Service.KRPC.KRPC.RunFunction (function), typeof (int)));
        }

        [Test]
        public void ParameterTypes ()
        {
            var function = Multiplier ();
            CollectionAssert.AreEqual (new [] { Type.Int (), Type.Int () }, function.ParameterTypes);
            Assert.AreEqual (Type.Int (), function.ReturnType);
            var nullable = Expression.Lambda (
                new List<Expression> { Expression.Parameter ("s", Type.NullableType (Type.String ())) },
                Expression.ConstantInt (1));
            CollectionAssert.AreEqual (
                new [] { Type.NullableType (Type.String ()) }, nullable.ParameterTypes);
            CollectionAssert.IsEmpty (Expression.ConstantInt (1).ParameterTypes);
        }

        [Test]
        public void StreamsAndEventsTakeNoParameters ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.AddFunctionStream (Multiplier ()));
            StringAssert.Contains ("cannot take parameters", exn.Message);
            var param = Expression.Parameter ("b", Type.Bool ());
            var predicate = Expression.Lambda (new List<Expression> { param }, param);
            exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => global::KRPC.Service.KRPC.KRPC.AddEvent (predicate));
            StringAssert.Contains ("cannot take parameters", exn.Message);
        }
    }
}
