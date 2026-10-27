using System.Collections.Generic;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

using Expr = KRPC.Client.Services.KRPC.Expression;
using KType = KRPC.Client.Services.KRPC.Type;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class FunctionStreamTest : ServerTestCase
    {

        [Test]
        public void TestConvertedKRPCCallStream ()
        {
            // A KRPC procedure cannot be called in a function, so the call is streamed
            var krpc = Services.KRPC.ExtensionMethods.KRPC (Connection);
            var stream = Connection.AddStream (() => (int)krpc.GameScene);
            Assert.AreEqual ((int)Services.KRPC.GameScene.SpaceCenter, stream.Get ());
            Assert.AreEqual (
                (double)Services.KRPC.GameScene.SpaceCenter,
                Connection.AddStream (() => (double)(int)krpc.GameScene).Get ());
            stream.Remove ();
        }

        [Test]
        public void TestTypeParameterHoldingTheValue ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("held");
            obj.IntProperty = 3;
            var value = Connection.Compile (() => obj.IntProperty + 1);
            Assert.AreEqual (4, Connection.Run<object> (value));
            var list = Connection.Compile (() => testService.IncrementList (new List<int> { 1, 2 }));
            CollectionAssert.AreEqual (new [] { 2, 3 }, Connection.Run<IEnumerable<int>> (list));
            CollectionAssert.AreEqual (new [] { 2, 3 }, Connection.Run<ICollection<int>> (list));
            Assert.AreEqual (obj, Connection.Run<object> (Connection.Compile (() => obj)));
        }

        [Test]
        public void TestTypeParameterOfAnEffect ()
        {
            var testService = Connection.TestService ();
            var effect = Connection.Compile (
                () => testService.ResetInvalidOperationExceptionLater ());
            var exn = Assert.Throws<System.InvalidOperationException> (
                () => Connection.Run<int> (effect));
            StringAssert.Contains ("without a type parameter", exn.Message);
            exn = Assert.Throws<System.InvalidOperationException> (() => Connection.AddStream<int> (effect));
            StringAssert.Contains ("cannot be streamed", exn.Message);
        }

        [Test]
        public void TestRunFunctionArgumentConversions ()
        {
            var types = new Dictionary<string, KType> {
                { "int", KType.Int (Connection) }, { "long", KType.Long (Connection) },
                { "uint", KType.UInt (Connection) }, { "ulong", KType.ULong (Connection) },
                { "float", KType.Float (Connection) }, { "double", KType.Double (Connection) }
            };
            var functions = new Dictionary<string, Expr> ();
            foreach (var type in types) {
                var x = Expr.Parameter (Connection, "x", type.Value);
                functions [type.Key] = Expr.Lambda (Connection, new List<Expr> { x }, x);
            }
            // The implicit numeric conversions of C#
            Assert.AreEqual (3.0, Connection.Run<double> (functions ["double"], 3));
            Assert.AreEqual (3, Connection.Run<int> (functions ["int"], (short)3));
            Assert.AreEqual (3, Connection.Run<int> (functions ["int"], (sbyte)3));
            Assert.AreEqual (3, Connection.Run<int> (functions ["int"], (byte)3));
            Assert.AreEqual (3, Connection.Run<int> (functions ["int"], (ushort)3));
            Assert.AreEqual (97, Connection.Run<int> (functions ["int"], 'a'));
            Assert.AreEqual (3L, Connection.Run<long> (functions ["long"], (short)3));
            Assert.AreEqual (3u, Connection.Run<uint> (functions ["uint"], (byte)3));
            Assert.AreEqual (3ul, Connection.Run<ulong> (functions ["ulong"], (ushort)3));
            Assert.AreEqual (97.0, Connection.Run<double> (functions ["double"], 'a'));
            Assert.AreEqual (3.0f, Connection.Run<float> (functions ["float"], (short)3));
            // A signed integer that is not negative converts to an unsigned type at least as wide
            Assert.AreEqual (3u, Connection.Run<uint> (functions ["uint"], 3));
            Assert.AreEqual (3u, Connection.Run<uint> (functions ["uint"], (short)3));
            Assert.AreEqual (3ul, Connection.Run<ulong> (functions ["ulong"], (sbyte)3));
            Assert.AreEqual (3ul, Connection.Run<ulong> (functions ["ulong"], 3L));
            Assert.Throws<System.ArgumentException> (
                () => Connection.Run<uint> (functions ["uint"], 3L));
            Assert.Throws<System.ArgumentException> (
                () => Connection.Run<uint> (functions ["uint"], (short)-3));
            Assert.Throws<System.ArgumentException> (
                () => Connection.Run<int> (functions ["int"], 3L));
            Assert.Throws<System.ArgumentException> (
                () => Connection.Run<int> (functions ["int"], 3.0));
            // An array passed as the only argument is that argument
            var names = Expr.Parameter (Connection, "names", KType.ListType (Connection, KType.String (Connection)));
            var count = Expr.Lambda (Connection, new List<Expr> { names }, Expr.Count (Connection, names));
            Assert.AreEqual (2, Connection.Run<int> (count, new [] { "a", "b" }));
            Assert.AreEqual (0, Connection.Run<int> (count, new string [0]));
            // The overload without a type parameter checks the arguments too
            var exn = Assert.Throws<System.ArgumentException> (() => Connection.Run (count));
            StringAssert.Contains ("takes 1 argument, got 0", exn.Message);
        }

        [Test]
        public void TestSingleCallLambdaWithParametersCannotBeStreamed ()
        {
            // The lambda is rejected before it is streamed as the call
            var obj = Connection.TestService ().CreateTestObject ("streamedparameter");
            System.Linq.Expressions.Expression<System.Func<int, int>> single = x => obj.IntProperty;
            Assert.Throws<FunctionCompilationException> (
                () => Connection.AddStream<int> ((System.Linq.Expressions.LambdaExpression)single));
        }
    }
}
