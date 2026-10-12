using System.Collections.Generic;
using System.Linq;
using KRPC.Service.KRPC;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Test.Service.KRPC
{
    public partial class ExpressionTest
    {
        [Test]
        public void AVariableOfACreatedListsTypeTakesAProceduresList ()
        {
            var list = Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) });
            Expression returned = LinqExpression.Constant (new List<int> { 1 }, typeof (IList<int>));
            var variable = Expression.Variable ("x", list.ReturnType);
            Assert.DoesNotThrow (() => Expression.Assign (variable, returned));
        }

        [Test]
        public void BlockVariablesAndWhile ()
        {
            // sum = 0; i = 0
            // while i < 5: i += 1; if i == 3: continue; if i == 5: break; sum += i
            // value of block = sum = 1 + 2 + 4 = 7
            var sum = Expression.Variable ("sum", Type.Int ());
            var i = Expression.Variable ("i", Type.Int ());
            var body = Expression.Block (new List<Expression> {
                Expression.Assign (i, Expression.Add (i, Expression.ConstantInt (1))),
                Expression.IfThen (
                    Expression.Equal (i, Expression.ConstantInt (3)), Expression.Continue ()),
                Expression.IfThen (
                    Expression.Equal (i, Expression.ConstantInt (5)), Expression.Break ()),
                Expression.Assign (sum, Expression.Add (sum, i))
            });
            var expr = Expression.BlockWithVariables (
                new List<Expression> { sum, i },
                new List<Expression> {
                    Expression.Assign (sum, Expression.ConstantInt (0)),
                    Expression.Assign (i, Expression.ConstantInt (0)),
                    Expression.While (
                        Expression.LessThan (i, Expression.ConstantInt (5)), body),
                    sum
                });
            Assert.AreEqual (7, Eval<int> (expr));
        }

        [Test]
        public void TupleElementsWidenOnAssignment ()
        {
            var type = Type.TupleType (new List<Type> {
                Type.TupleType (new List<Type> { Type.Double () }), Type.Long ()
            });
            var t = Expression.Variable ("t", type);
            var expr = Expression.BlockWithVariables (
                new List<Expression> { t },
                new List<Expression> {
                    Expression.Assign (t, Expression.CreateTuple (new List<Expression> {
                        Expression.CreateTuple (new List<Expression> { Expression.ConstantInt (1) }),
                        Expression.ConstantInt (2)
                    })),
                    t
                });
            Assert.AreEqual (
                System.Tuple.Create (System.Tuple.Create (1d), 2L),
                Eval<System.Tuple<System.Tuple<double>, long>> (expr));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Assign (
                    Expression.Variable ("u", Type.TupleType (new List<Type> { Type.Int () })),
                    Expression.CreateTuple (new List<Expression> { Expression.ConstantDouble (1) })));
        }

        [Test]
        public void FunctionBodyWithTooManyNodesIsRejected ()
        {
            var expr = ChainWithTooManyNodes ();
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Lambda (new List<Expression> (), expr));
            StringAssert.Contains ("more than 1000000 nodes", exn.Message);
        }

        [Test]
        public void ForEachLoop ()
        {
            // total = 0; for x in [1, 2, 3, 4, 5]: if x == 4: break; total += x
            var total = Expression.Variable ("total", Type.Int ());
            var x = Expression.Variable ("x", Type.Int ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { total, x },
                new List<Expression> {
                    Expression.Assign (total, Expression.ConstantInt (0)),
                    Expression.ForEach (x, list, Expression.Block (new List<Expression> {
                        Expression.IfThen (
                            Expression.Equal (x, Expression.ConstantInt (4)), Expression.Break ()),
                        Expression.Assign (total, Expression.Add (total, x))
                    })),
                    total
                });
            Assert.AreEqual (6, Eval<int> (expr));
        }

        [Test]
        public void IfThenElseStatements ()
        {
            var result = Expression.Variable ("result", Type.String ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { result },
                new List<Expression> {
                    Expression.IfThenElse (
                        Expression.ConstantBool (false),
                        Expression.Assign (result, Expression.ConstantString ("then")),
                        Expression.Assign (result, Expression.ConstantString ("else"))),
                    result
                });
            Assert.AreEqual ("else", Eval<string> (expr));
        }

        [Test]
        public void FunctionWithStatementsAndEarlyReturn ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (
                new List<Expression> { x },
                Expression.Block (new List<Expression> {
                    Expression.IfThen (
                        Expression.GreaterThan (x, Expression.ConstantInt (2)),
                        Expression.Return (Expression.ConstantInt (100))),
                    Expression.Multiply (x, Expression.ConstantInt (2))
                }));
            Assert.AreEqual (4, Eval<int> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (2) } })));
            Assert.AreEqual (100, Eval<int> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (3) } })));
        }

        [Test]
        public void FunctionEndingWithAnIfThatReturns ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (
                new List<Expression> { x },
                Expression.IfThenElse (
                    Expression.GreaterThan (x, Expression.ConstantInt (2)),
                    Expression.Return (Expression.ConstantInt (100)),
                    Expression.Return (Expression.ConstantInt (200))));
            Assert.AreEqual (100, Eval<int> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (3) } })));
            Assert.AreEqual (200, Eval<int> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (2) } })));
        }

        [Test]
        public void ReturnsWidenToTheirCommonType ()
        {
            var x = Expression.Parameter ("x", Type.Bool ());
            var both = new [] { false, true };
            var arguments = both.Select (value => new Dictionary<string, Expression> {
                { "x", Expression.ConstantBool (value) } }).ToList ();
            foreach (var swap in both) {
                Expression one = Expression.ConstantInt (1);
                Expression half = Expression.ConstantDouble (0.5);
                var function = Expression.Lambda (
                    new List<Expression> { x },
                    Expression.IfThenElse (
                        x, Expression.Return (swap ? half : one), Expression.Return (swap ? one : half)));
                Assert.AreEqual (swap ? 1 : 0.5, Eval<double> (Expression.Invoke (function, arguments [0])));
                Assert.AreEqual (swap ? 0.5 : 1, Eval<double> (Expression.Invoke (function, arguments [1])));
            }
            // An int and a ulong have no common type, in either order, until a float joins them
            var returns = new List<Expression> {
                Expression.Return (Expression.ConstantInt (1)),
                Expression.Return (Expression.ConstantULong (2)),
                Expression.Return (Expression.ConstantFloat (3))
            };
            foreach (var mixed in new [] { returns.Take (2), returns.Take (2).Reverse () })
                Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                    () => Expression.Lambda (new List<Expression> (), Expression.Block (mixed.ToList ())));
            for (var i = 0; i < returns.Count; i++) {
                var rotated = returns.Skip (i).Concat (returns.Take (i)).ToList ();
                var function = Expression.Lambda (new List<Expression> (), Expression.Block (rotated));
                Assert.AreEqual (
                    (float)(i == 0 ? 1 : i == 1 ? 2 : 3),
                    Eval<float> (Expression.Invoke (function, new Dictionary<string, Expression> ())));
            }
        }

        [Test]
        public void FunctionEndingWithoutReturningAValue ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (
                new List<Expression> { x },
                Expression.IfThen (
                    Expression.GreaterThan (x, Expression.ConstantInt (2)),
                    Expression.Return (Expression.ConstantInt (100))));
            Assert.AreEqual (100, Eval<int> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (3) } })));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Eval<int> (Expression.Invoke (
                    function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (2) } })));
        }

        [Test]
        public void FunctionClosesOverVariables ()
        {
            // v = 10; f = () => v + 1; v = 20; f() == 21
            var v = Expression.Variable ("v", Type.Int ());
            var function = Expression.Lambda (
                new List<Expression> (),
                Expression.Add (v, Expression.ConstantInt (1)));
            var expr = Expression.BlockWithVariables (
                new List<Expression> { v },
                new List<Expression> {
                    Expression.Assign (v, Expression.ConstantInt (20)),
                    Expression.Invoke (function, new Dictionary<string, Expression> ())
                });
            Assert.AreEqual (21, Eval<int> (expr));
        }

        [Test]
        public void VoidCallStatement ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("effects");
            obj.IntProperty = 1;
            var expr = Expression.Block (new List<Expression> {
                Expression.Call (BuildProcedureCall (
                    "TestClass_set_IntProperty", new Argument (0, obj), new Argument (1, 42))),
                Expression.Call (BuildProcedureCall (
                    "TestClass_get_IntProperty", new Argument (0, obj)))
            });
            Assert.AreEqual (42, Eval<int> (expr));
            Assert.AreEqual (42, obj.IntProperty);
        }

        [Test]
        public void AssignedValuesAreWidenedNotNarrowed ()
        {
            var total = Expression.Variable ("total", Type.Double ());
            var widened = Expression.BlockWithVariables (
                new List<Expression> { total },
                new List<Expression> {
                    Expression.Assign (total, Expression.ConstantInt (1)),
                    total
                });
            Assert.AreEqual (1.0, Eval<double> (widened));

            var count = Expression.Variable ("count", Type.Int ());
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Assign (count, Expression.ConstantDouble (1.5)));
            StringAssert.Contains ("No implicit conversion", exn.Message);

            var narrowed = Expression.BlockWithVariables (
                new List<Expression> { count },
                new List<Expression> {
                    Expression.Assign (count, Expression.Cast (
                        Expression.ConstantDouble (2.5), Type.Int ())),
                    count
                });
            Assert.AreEqual (2, Eval<int> (narrowed));
        }

        [Test]
        public void RunFunctionChecksTheReturnTypeBeforeEvaluating ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("run");
            obj.IntProperty = 1;
            var expr = Expression.Block (new List<Expression> {
                Expression.Call (BuildProcedureCall (
                    "TestClass_set_IntProperty", new Argument (0, obj), new Argument (1, 5))),
                Expression.Skip (list, Expression.ConstantInt (1))
            });
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (expr));
            Assert.AreEqual (1, obj.IntProperty);
        }

        static Expression StatementWithMarker (Expression marker)
        {
            return Expression.Block (new List<Expression> {
                marker,
                Expression.ConstantInt (1)
            });
        }

        [Test]
        public void UnboundMarkersAreRejectedWhenRunAsAFunction ()
        {
            foreach (var marker in new [] {
                Expression.Break (), Expression.Continue (),
                Expression.Return (Expression.ConstantInt (1))
            }) {
                var expr = StatementWithMarker (marker);
                Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                    () => global::KRPC.Service.KRPC.KRPC.RunFunction (expr));
                Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                    () => global::KRPC.Service.KRPC.KRPC.CompileFunction (expr));
            }
        }

        [Test]
        public void UnboundMarkersAreRejectedWhenAddedAsAnEvent ()
        {
            var expr = Expression.Block (new List<Expression> {
                Expression.Break (),
                Expression.ConstantBool (true)
            });
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.AddEvent (expr));
        }

        [Test]
        public void MarkersBoundByTheirLoopOrFunctionAreAccepted ()
        {
            // The same markers, correctly enclosed, are accepted
            var i = Expression.Variable ("i", Type.Int ());
            var loop = Expression.BlockWithVariables (
                new List<Expression> { i },
                new List<Expression> {
                    Expression.Assign (i, Expression.ConstantInt (0)),
                    Expression.While (
                        Expression.LessThan (i, Expression.ConstantInt (3)),
                        Expression.Block (new List<Expression> {
                            Expression.Assign (i, Expression.Add (i, Expression.ConstantInt (1))),
                            Expression.IfThen (
                                Expression.Equal (i, Expression.ConstantInt (2)),
                                Expression.Break ())
                        })),
                    i
                });
            Assert.DoesNotThrow (() => global::KRPC.Service.KRPC.KRPC.RunFunction (loop));
        }

        [Test]
        public void ReturnOfAValuelessExpression ()
        {
            var statement = Expression.Call (BuildProcedureCall (
                "ProcedureSingleArgNoReturn", new Argument (0, "foo")));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Return (statement));
            StringAssert.Contains ("must be given a value", exn.Message);
        }

        [Test]
        public void NullableNumberAssignedToAVariable ()
        {
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoNullableInt (It.IsAny<int?> ())).Returns ((int? x) => x);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var echo = Expression.Call (BuildProcedureCall ("EchoNullableInt", new Argument (0, 3)));
            var variable = Expression.Variable ("x", Type.Double ());
            var assigned = Expression.BlockWithVariables (
                new List<Expression> { variable },
                new List<Expression> { Expression.Assign (variable, echo), variable });
            Assert.AreEqual (3.0, Eval<double> (assigned));
        }

        [Test]
        public void ReturnTypeOfAValuelessExpression ()
        {
            var expr = Expression.ReturnNothing ();
            Assert.IsFalse (expr.HasReturnType);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => { var unused = expr.ReturnType; });
            StringAssert.Contains ("does not evaluate to a value", exn.Message);
        }

        [Test]
        public void BlockVariablesMustBeVariables ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.BlockWithVariables (
                    new List<Expression> { Expression.ConstantInt (1) },
                    new List<Expression> { Expression.ConstantInt (2) }));
            StringAssert.Contains ("Expected a variable, created with Variable", exn.Message);
        }

        [Test]
        public void CountOfACollectionInterface ()
        {
            var values = Expression.Variable ("values", Type.ListType (Type.Int ()));
            Assert.AreEqual (5, Eval<int> (Expression.BlockWithVariables (
                new List<Expression> { values },
                new List<Expression> {
                    Expression.Assign (values, list),
                    Expression.Count (values)
                })));
            var entries = Expression.Variable (
                "entries", Type.DictionaryType (Type.String (), Type.Int ()));
            Assert.AreEqual (3, Eval<int> (Expression.BlockWithVariables (
                new List<Expression> { entries },
                new List<Expression> {
                    Expression.Assign (entries, dictionary),
                    Expression.Count (entries)
                })));
        }

        [Test]
        public void BlocksGivenANullElement ()
        {
            var one = Expression.ConstantInt (1);
            foreach (var build in new List<TestDelegate> {
                () => Expression.Block (new List<Expression> { one, null }),
                () => Expression.BlockWithVariables (
                    new List<Expression> { Expression.Variable ("x", Type.Int ()) },
                    new List<Expression> { one, null })
            }) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (build);
                StringAssert.Contains ("cannot be null", exn.Message);
            }
        }

        [Test]
        public void NullableValueAssignedToAVariable ()
        {
            var variable = Expression.Variable ("s", Type.String ());
            var block = Expression.BlockWithVariables (
                new List<Expression> { variable },
                new List<Expression> { Expression.Assign (variable, NullableString (null)), variable });
            Assert.IsFalse (block.ReturnType.Nullable);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (block));
            StringAssert.Contains ("assigned to s", exn.Message);
            var nullable = Expression.Variable ("t", Type.NullableType (Type.String ()));
            var nullableBlock = Expression.BlockWithVariables (
                new List<Expression> { nullable },
                new List<Expression> { Expression.Assign (nullable, NullableString (null)), nullable });
            Assert.IsTrue (nullableBlock.ReturnType.Nullable);
            Assert.IsNull (Eval<string> (nullableBlock));
        }

        [Test]
        public void FunctionResultIsNullableWhenAReturnIs ()
        {
            var parameter = Expression.Parameter ("x", Type.Bool ());
            var function = Expression.Lambda (
                new List<Expression> { parameter },
                Expression.IfThenElse (
                    parameter,
                    Expression.Return (Expression.ConstantString ("a")),
                    Expression.Return (NullableString (null))));
            var invoke = Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantBool (false) } });
            Assert.IsTrue (invoke.ReturnType.Nullable);
            Assert.IsNull (Eval<string> (invoke));
        }

        [Test]
        public void NullReturnLiftsTheOtherReturns ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (
                new List<Expression> { x },
                Expression.IfThenElse (
                    Expression.GreaterThan (x, Expression.ConstantInt (2)),
                    Expression.Return (Expression.ConstantNull (Type.Int ())),
                    Expression.Return (x)));
            var small = Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (2) } });
            Assert.IsTrue (small.ReturnType.Nullable);
            Assert.AreEqual (2, Eval<int?> (small));
            Assert.IsNull (Eval<int?> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (3) } })));
            var valueLast = Expression.Lambda (
                new List<Expression> { x },
                Expression.Block (new List<Expression> {
                    Expression.IfThen (
                        Expression.GreaterThan (x, Expression.ConstantInt (2)),
                        Expression.Return (Expression.ConstantNull (Type.Int ()))),
                    x
                }));
            Assert.AreEqual (1, Eval<int?> (Expression.Invoke (
                valueLast, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (1) } })));
        }

        [Test]
        public void NullableBooleanCondition ()
        {
            Expression nullBool = LinqExpression.Constant (null, typeof (bool?));
            var loop = Expression.While (nullBool, Expression.Break ());
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => LinqExpression.Lambda<System.Action> (loop).Compile () ());
        }

        [Test]
        public void ForEachOverNullableValues ()
        {
            var list = Expression.CreateList (new List<Expression> {
                Expression.ConstantString ("a"), NullableString (null) });
            var variable = Expression.Variable ("s", Type.String ());
            var loop = Expression.BlockWithVariables (
                new List<Expression> { variable },
                new List<Expression> { Expression.ForEach (variable, list, Expression.Continue ()) });
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => LinqExpression.Lambda<System.Action> (loop).Compile () ());
        }

        [Test]
        public void ForEachConvertsValuesToTheLoopVariable ()
        {
            var ints = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (1), Expression.ConstantInt (2) });
            var nullable = Expression.Variable ("x", Type.NullableType (Type.Int ()));
            Assert.AreEqual (2, Eval<int?> (Expression.BlockWithVariables (
                new List<Expression> { nullable },
                new List<Expression> { Expression.ForEach (nullable, ints, Expression.Continue ()), nullable })));
            var wide = Expression.Variable ("y", Type.Double ());
            Assert.AreEqual (2.0, Eval<double> (Expression.BlockWithVariables (
                new List<Expression> { wide },
                new List<Expression> { Expression.ForEach (wide, ints, Expression.Continue ()), wide })));
            var text = Expression.Variable ("s", Type.String ());
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.ForEach (text, ints, Expression.Continue ()));
            StringAssert.Contains ("No implicit conversion", exn.Message);
        }

        [Test]
        public void AssignChecksTheValueType ()
        {
            var variable = Expression.Variable ("x", Type.Int ());
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Assign (variable, Expression.ConstantString ("a")));
            StringAssert.Contains ("No implicit conversion", exn.Message);
        }

        [Test]
        public void AssignAndForEachTakeAVariable ()
        {
            var one = Expression.ConstantInt (1);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Assign (one, one));
            Assert.AreEqual (
                "The assignment target must be a local variable, state variable or function parameter",
                exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ForEach (one, list, one));
            Assert.AreEqual ("The loop variable must be a local variable or function parameter", exn.Message);
        }

        [Test]
        public void ConditionMustBeABoolean ()
        {
            var body = Expression.ConstantInt (1);
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.IfThen (null, body));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.IfThenElse (null, body, body));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.IfThen (Expression.ConstantInt (1), body));
            StringAssert.Contains ("must evaluate to a boolean", exn.Message);
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.IfThenElse (Expression.ConstantInt (1), body, body));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.While (Expression.ConstantInt (1), body));
        }

        [Test]
        public void ReturnUsedAsAValue ()
        {
            var x = Expression.Parameter ("x", Type.Bool ());
            var function = Expression.Lambda (new List<Expression> { x }, Expression.Add (
                Expression.Conditional (x, Expression.Return (Expression.ConstantInt (1)), Expression.ConstantInt (2)),
                Expression.ConstantInt (10)));
            Assert.AreEqual (1, Eval<int> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantBool (true) } })));
            Assert.AreEqual (12, Eval<int> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantBool (false) } })));
        }

        [Test]
        public void BlockDeclaresAVariableOnce ()
        {
            var variable = Expression.Variable ("x", Type.Int ());
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.BlockWithVariables (
                    new List<Expression> { variable, variable }, new List<Expression> { variable }));
            StringAssert.Contains ("more than once", exn.Message);
        }
    }
}
