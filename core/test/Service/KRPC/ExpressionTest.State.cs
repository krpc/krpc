using System.Collections.Generic;
using KRPC.Service.KRPC;
using NUnit.Framework;

namespace KRPC.Test.Service.KRPC
{
    public partial class ExpressionTest
    {
        static T Run<T> (Expression function)
        {
            var bytes = global::KRPC.Service.KRPC.KRPC.RunFunction (function);
            return (T)global::KRPC.Server.ProtocolBuffers.Encoder.Decode (
                Google.Protobuf.ByteString.CopyFrom (bytes),
                global::KRPC.Service.TypeSpec.Create (typeof (T)));
        }

        static Expression Increment (Expression state)
        {
            return Expression.Block (new List<Expression> {
                Expression.Assign (state, Expression.Add (state, Expression.ConstantInt (1))),
                state
            });
        }

        [Test]
        public void StateVariableKeepsItsValueBetweenRuns ()
        {
            var function = Increment (Expression.StateVariable ("count", Expression.ConstantInt (0)));
            Assert.AreEqual (1, Run<int> (function));
            Assert.AreEqual (2, Run<int> (function));
            Assert.AreEqual (3, Run<int> (function));
        }

        [Test]
        public void StateVariablesHoldSeparateValues ()
        {
            var initial = Expression.ConstantInt (10);
            var first = Increment (Expression.StateVariable ("count", initial));
            var second = Increment (Expression.StateVariable ("count", initial));
            Assert.AreEqual (11, Run<int> (first));
            Assert.AreEqual (12, Run<int> (first));
            Assert.AreEqual (11, Run<int> (second));
        }

        [Test]
        public void StateVariableSharedByTwoFunctions ()
        {
            var state = Expression.StateVariable ("count", Expression.ConstantInt (0));
            var increment = Increment (state);
            var read = Expression.Multiply (state, Expression.ConstantInt (10));
            Run<int> (increment);
            Run<int> (increment);
            Assert.AreEqual (20, Run<int> (read));
        }

        [Test]
        public void StateVariableCollectionChangesInPlace ()
        {
            var values = Expression.StateVariable ("values", Expression.CreateList (
                new List<Expression> { Expression.ConstantInt (1) }));
            var function = Expression.Block (new List<Expression> {
                Expression.Append (values, Expression.ConstantInt (2)),
                values
            });
            CollectionAssert.AreEqual (new List<int> { 1, 2 }, Run<IList<int>> (function));
            CollectionAssert.AreEqual (new List<int> { 1, 2, 2 }, Run<IList<int>> (function));
        }

        [Test]
        public void StreamOfAStateVariableCollectionSendsEachChange ()
        {
            var values = Expression.StateVariable ("values", Expression.CreateList (
                new List<Expression> { Expression.ConstantInt (1) }));
            var stream = new global::KRPC.Service.FunctionStream (Expression.Block (new List<Expression> {
                Expression.Append (values, Expression.ConstantInt (2)),
                values
            }));
            stream.UpdateInternal ();
            stream.Sent ();
            stream.UpdateInternal ();
            Assert.IsTrue (stream.Changed);
            CollectionAssert.AreEqual (new List<int> { 1, 2, 2 }, (IList<int>)stream.Result.Value);
        }

        [Test]
        public void StateVariableTypeIsTheInitialValues ()
        {
            var total = Expression.StateVariable ("total", Expression.ConstantDouble (0.5));
            var function = Expression.Block (new List<Expression> {
                Expression.Assign (total, Expression.Add (total, Expression.ConstantInt (1))),
                total
            });
            Assert.AreEqual (1.5, Run<double> (function));
            var count = Expression.StateVariable ("count", Expression.ConstantInt (0));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Assign (count, Expression.ConstantDouble (1.5)));
        }

        [Test]
        public void StateVariableNeedsAValue ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.StateVariable ("none", Expression.IfThen (
                    Expression.ConstantBool (true), Expression.ConstantInt (1))));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.StateVariable ("function", Expression.Lambda (
                    new List<Expression> (), Expression.ConstantInt (1))));
        }

        [Test]
        public void StateVariableCannotHoldALazyCollection ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var lazy = Expression.Select (
                Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) }),
                Expression.Lambda (new List<Expression> { x }, x));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.StateVariable ("lazy", lazy));
            StringAssert.Contains ("Use ToList or ToSet", exn.Message);
            Assert.DoesNotThrow (() => Expression.StateVariable ("list", Expression.ToList (lazy)));
        }

        [Test]
        public void StateVariableCannotBeStreamed ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => new global::KRPC.Service.ProcedureCallStream (
                    new global::KRPC.Service.Messages.ProcedureCall ("KRPC", "Expression_static_StateVariable")));
            StringAssert.Contains ("KRPC.Expression_static_StateVariable cannot be streamed", exn.Message);
        }

        [Test]
        public void StateVariableNullableInitialValue ()
        {
            var last = Expression.StateVariable ("last", Expression.ConstantNull (Type.Double ()));
            var function = Expression.Block (new List<Expression> {
                Expression.Assign (last, Expression.ConstantDouble (2)),
                last
            });
            Assert.AreEqual (2.0, Run<double> (function));
        }
    }
}
