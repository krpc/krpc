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
        public void Throw ()
        {
            var expr = Expression.Throw (
                "KRPC", "InvalidOperationException", Expression.ConstantString ("boom"));
            Assert.IsFalse (expr.HasReturnType);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => expr.Evaluate ());
            Assert.AreEqual ("boom", exn.Message);
        }

        [Test]
        public void ThrowAsABranchOfAConditional ()
        {
            var boom = Expression.Throw (
                "KRPC", "InvalidOperationException", Expression.ConstantString ("boom"));
            Assert.AreEqual (1, Eval<int> (Expression.Conditional (
                Expression.ConstantBool (true), Expression.ConstantInt (1), boom)));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Eval<int> (Expression.Conditional (
                    Expression.ConstantBool (true), boom, Expression.ConstantInt (1))));
            Assert.AreEqual ("boom", exn.Message);
        }

        [Test]
        public void ThrowRejectsAnUnknownException ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Throw (
                    "KRPC", "NoSuchException", Expression.ConstantString ("boom")));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Throw (
                    "NoSuchService", "InvalidOperationException",
                    Expression.ConstantString ("boom")));
        }

        [Test]
        public void TryCatchCatchesAThrow ()
        {
            var caught = Expression.Variable ("caught", Type.String ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { caught },
                new List<Expression> {
                    Expression.Assign (caught, Expression.ConstantString ("")),
                    Expression.TryCatch (
                        Expression.Throw (
                            "KRPC", "InvalidOperationException",
                            Expression.ConstantString ("boom")),
                        "KRPC", "InvalidOperationException", caught,
                        Expression.Assign (caught, Expression.StringConcat (
                            new List<Expression> {
                                Expression.ConstantString ("caught "), caught
                            }))),
                    caught
                });
            Assert.AreEqual ("caught boom", Eval<string> (expr));
        }

        // A call to a procedure that throws a System.ArgumentOutOfRangeException
        static Expression ThrowArgumentOutOfRange ()
        {
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgReturns (It.IsAny<string> ()))
                .Throws (new System.ArgumentOutOfRangeException ("x"));
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            return Expression.Call (BuildProcedureCall ("ProcedureSingleArgReturns", new Argument (0, "a")));
        }

        [Test]
        public void TryCatchCatchesTheMappedException ()
        {
            // A service throws the CLR exception types, which the client sees under
            // the kRPC name, so naming that name catches them
            var expr = Expression.TryCatch (
                ThrowArgumentOutOfRange (),
                "KRPC", "ArgumentOutOfRangeException", null,
                Expression.ConstantString ("caught"));
            Assert.DoesNotThrow (() => expr.Evaluate ());
        }

        [Test]
        public void TryCatchCatchesEachNamedException ()
        {
            var values = Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) });
            var cases = new Dictionary<string, Expression> {
                { "IndexOutOfRangeException", Expression.Get (values, Expression.ConstantInt (1)) },
                { "DivideByZeroException", Expression.Divide (Expression.ConstantInt (1), Expression.ConstantInt (0)) },
                { "Error", Expression.Throw ("KRPC", "Error", Expression.ConstantString ("boom")) }
            };
            foreach (var entry in cases) {
                var expr = Expression.TryCatch (
                    entry.Value, "KRPC", entry.Key, null, Expression.ConstantString ("caught"));
                Assert.DoesNotThrow (() => expr.Evaluate (), entry.Key);
            }
        }

        [Test]
        public void TryCatchDoesNotCatchAnotherException ()
        {
            var expr = Expression.TryCatch (
                ThrowArgumentOutOfRange (),
                "KRPC", "ObjectDestroyedException", null,
                Expression.ConstantString ("caught"));
            Assert.Throws<System.ArgumentOutOfRangeException> (() => expr.Evaluate ());
        }

        [Test]
        public void TryCatchDoesNotCatchASubclass ()
        {
            // ArgumentOutOfRangeException derives from ArgumentException, and reaches
            // the client under its own name, so naming ArgumentException leaves it alone
            var expr = Expression.TryCatch (
                ThrowArgumentOutOfRange (),
                "KRPC", "ArgumentException", null,
                Expression.ConstantString ("caught"));
            Assert.Throws<System.ArgumentOutOfRangeException> (() => expr.Evaluate ());
        }

        [Test]
        public void TryCatchAllCatchesAnything ()
        {
            var caught = Expression.Variable ("caught", Type.String ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { caught },
                new List<Expression> {
                    Expression.Assign (caught, Expression.ConstantString ("")),
                    Expression.TryCatchAll (
                        Expression.StringGet (
                            Expression.ConstantString ("ab"), Expression.ConstantInt (5)),
                        caught, Expression.ConstantString ("")),
                    Expression.StringLength (caught)
                });
            Assert.Greater (Eval<int> (expr), 0);
        }

        [Test]
        public void TryCatchAllDoesNotCatchAYield ()
        {
            // A procedure that pauses execution unwinds by throwing YieldException,
            // which the catch-all lets through
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => {
                    throw new global::KRPC.Service.YieldException<System.Func<int>> (() => 0);
                });
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var expr = Expression.TryCatchAll (
                Expression.Call (BuildProcedureCall (
                    "BlockingProcedureReturns", new Argument (0, 1))),
                null, Expression.ConstantString ("caught"));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => expr.Evaluate ());
            StringAssert.Contains ("paused execution", exn.Message);
        }

        [Test]
        public void TryFinallyRunsTheFinalizer ()
        {
            var ran = Expression.Variable ("ran", Type.Bool ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { ran },
                new List<Expression> {
                    Expression.Assign (ran, Expression.ConstantBool (false)),
                    Expression.TryCatchAll (
                        Expression.TryFinally (
                            Expression.Throw (
                                "KRPC", "InvalidOperationException",
                                Expression.ConstantString ("boom")),
                            Expression.Assign (ran, Expression.ConstantBool (true))),
                        null, Expression.ConstantString ("")),
                    ran
                });
            Assert.IsTrue (Eval<bool> (expr));
        }

        [Test]
        public void NullableMessage ()
        {
            var thrown = Expression.Throw ("KRPC", "ArgumentException", NullableString (null));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => System.Linq.Expressions.Expression.Lambda<System.Action> (thrown).Compile () ());
            StringAssert.Contains ("The third argument of Throw is null", exn.Message);
        }

        [Test]
        public void ThrowRejectsAMessageThatIsNotAString ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Throw ("KRPC", "ArgumentException", Expression.ConstantInt (1)));
            StringAssert.StartsWith ("The third argument of Throw is not a string", exn.Message);
        }

        [Test]
        public void CatchRejectsAnInvalidMessage ()
        {
            var exn1 = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.TryCatchAll (
                    Expression.ConstantInt (1), Expression.ConstantString ("a"), Expression.ConstantInt (2)));
            Assert.AreEqual ("The second argument of TryCatchAll must be a variable or parameter", exn1.Message);
            var exn2 = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.TryCatch (
                    Expression.ConstantInt (1), "KRPC", "ArgumentException",
                    Expression.Variable ("v", Type.Int ()), Expression.ConstantInt (2)));
            Assert.AreEqual ("The fourth argument of TryCatch is not a string", exn2.Message);
        }

        [Test]
        public void JumpOutOfAFinallyIsRejected ()
        {
            var function = Expression.Lambda (new List<Expression> (), Expression.Block (
                new List<Expression> {
                    Expression.TryFinally (
                        Expression.ConstantInt (1), Expression.Return (Expression.ConstantInt (2))),
                    Expression.ConstantInt (3)
                }));
            var loop = Expression.While (
                Expression.ConstantBool (true),
                Expression.TryFinally (Expression.ConstantInt (1), Expression.Break ()));
            foreach (var expr in new [] { function, loop }) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                    () => expr.Check ());
                Assert.AreEqual ("A break, continue or return cannot leave a finally block", exn.Message);
            }
        }

        [Test]
        public void JumpWithinAFinally ()
        {
            var i = Expression.Variable ("i", Type.Int ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { i },
                new List<Expression> {
                    Expression.Assign (i, Expression.ConstantInt (0)),
                    Expression.TryFinally (
                        Expression.ConstantInt (1),
                        Expression.While (Expression.ConstantBool (true), Expression.Block (
                            new List<Expression> {
                                Expression.Assign (i, Expression.Add (i, Expression.ConstantInt (1))),
                                Expression.Break ()
                            }))),
                    i
                });
            expr.Check ();
            Assert.AreEqual (1, Eval<int> (expr));
        }

        [Test]
        public void ReturnFromATryBody ()
        {
            var function = Expression.Lambda (new List<Expression> (), Expression.Block (
                new List<Expression> {
                    Expression.TryFinally (
                        Expression.Return (Expression.ConstantInt (1)), Expression.ConstantInt (2)),
                    Expression.ConstantInt (3)
                }));
            var call = Expression.Invoke (function, new Dictionary<string, Expression> ());
            call.Check ();
            Assert.AreEqual (1, Eval<int> (call));
        }
    }
}
