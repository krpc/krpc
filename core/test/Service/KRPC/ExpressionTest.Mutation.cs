using System.Collections.Generic;
using System.Linq;
using KRPC.Service.KRPC;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;

namespace KRPC.Test.Service.KRPC
{
    public partial class ExpressionTest
    {
        [Test]
        public void ForEachLoopVisitsElementsAppendedByTheBody ()
        {
            // items = [1]; for x in items: if x < 4: items.append (x + 1);
            //   if x == 2: continue; total += x
            var items = Expression.Variable ("items", Type.ListType (Type.Int ()));
            var total = Expression.Variable ("total", Type.Int ());
            var x = Expression.Variable ("x", Type.Int ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { items, total, x },
                new List<Expression> {
                    Expression.Assign (items, Expression.CreateEmptyList (Type.Int ())),
                    Expression.Append (items, Expression.ConstantInt (1)),
                    Expression.Assign (total, Expression.ConstantInt (0)),
                    Expression.ForEach (x, items, Expression.Block (new List<Expression> {
                        Expression.IfThen (
                            Expression.LessThan (x, Expression.ConstantInt (4)),
                            Expression.Append (items, Expression.Add (x, Expression.ConstantInt (1)))),
                        Expression.IfThen (
                            Expression.Equal (x, Expression.ConstantInt (2)), Expression.Continue ()),
                        Expression.Assign (total, Expression.Add (total, x))
                    })),
                    total
                });
            Assert.AreEqual (1 + 3 + 4, Eval<int> (expr));
        }

        [Test]
        public void ChangingACallResultLeavesTheProcedureCollectionUnchanged ()
        {
            var held = new List<string> { "a" };
            var nested = new Dictionary<int, IList<string>> { { 1, held } };
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ())).Returns (held);
            mock.Setup (x => x.EchoNestedCollection (It.IsAny<IDictionary<int, IList<string>>> ()))
                .Returns (nested);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var list = Expression.Call (BuildProcedureCall (
                "EchoList", new Argument (0, new List<string> ())));
            var dictionary = Expression.Call (BuildProcedureCall (
                "EchoNestedCollection", new Argument (0, new Dictionary<int, IList<string>> ())));
            var state = Expression.StateVariable ("state", list);
            var b = Expression.ConstantString ("b");
            foreach (var statement in new [] {
                    Expression.Append (list, b),
                    Expression.Set (list, Expression.ConstantInt (0), b),
                    Expression.RemoveAt (list, Expression.ConstantInt (0)),
                    Expression.Clear (list),
                    Expression.Append (state, b),
                    Expression.Append (Expression.Get (dictionary, Expression.ConstantInt (1)), b),
                    Expression.Clear (dictionary)
                })
                statement.Evaluate ();
            CollectionAssert.AreEqual (new List<string> { "a" }, held);
            Assert.AreEqual (1, nested.Count);
        }

        [Test]
        public void ListRemoval ()
        {
            var values = Expression.Variable ("values", Type.ListType (Type.Int ()));
            Expression Build (Expression removal)
            {
                return Expression.BlockWithVariables (
                    new List<Expression> { values },
                    new List<Expression> {
                        Expression.Assign (values, Expression.CreateList (
                            new List<Expression> {
                                Expression.ConstantInt (1),
                                Expression.ConstantInt (2),
                                Expression.ConstantInt (3)
                            })),
                        removal,
                        values
                    });
            }
            CollectionAssert.AreEqual (
                new List<int> { 1, 3 },
                Eval<IList<int>> (Build (
                    Expression.Remove (values, Expression.ConstantInt (2)))));
            CollectionAssert.AreEqual (
                new List<int> { 2, 3 },
                Eval<IList<int>> (Build (
                    Expression.RemoveAt (values, Expression.ConstantInt (0)))));
            CollectionAssert.IsEmpty (
                Eval<IList<int>> (Build (Expression.Clear (values))));
            Assert.IsFalse (Eval<bool> (Expression.Remove (
                list, Expression.ConstantInt (99))));
        }

        [Test]
        public void SetRemoval ()
        {
            var values = Expression.Variable ("values", Type.SetType (Type.Int ()));
            Expression Build (Expression removal)
            {
                return Expression.BlockWithVariables (
                    new List<Expression> { values },
                    new List<Expression> {
                        Expression.Assign (values, Expression.CreateSet (
                            new HashSet<Expression> {
                                Expression.ConstantInt (1),
                                Expression.ConstantInt (2)
                            })),
                        removal,
                        Expression.Count (values)
                    });
            }
            Assert.AreEqual (1, Eval<int> (Build (
                Expression.Remove (values, Expression.ConstantInt (2)))));
            Assert.AreEqual (0, Eval<int> (Build (Expression.Clear (values))));
        }

        [Test]
        public void DictionaryRemoval ()
        {
            var values = Expression.Variable (
                "values", Type.DictionaryType (Type.String (), Type.Int ()));
            Expression Build (Expression removal)
            {
                return Expression.BlockWithVariables (
                    new List<Expression> { values },
                    new List<Expression> {
                        Expression.Assign (values, Expression.CreateDictionary (
                            new List<Expression> {
                                Expression.ConstantString ("a"),
                                Expression.ConstantString ("b")
                            },
                            new List<Expression> {
                                Expression.ConstantInt (1),
                                Expression.ConstantInt (2)
                            })),
                        removal,
                        Expression.Count (values)
                    });
            }
            Assert.AreEqual (1, Eval<int> (Build (
                Expression.Remove (values, Expression.ConstantString ("a")))));
            Assert.AreEqual (0, Eval<int> (Build (Expression.Clear (values))));
            Assert.IsFalse (Eval<bool> (Expression.Remove (dictionary, Expression.ConstantString ("z"))));
        }

        [Test]
        public void RemovalOfANullFindsNoMatch ()
        {
            Expression none = System.Linq.Expressions.Expression.Constant (null, typeof (int?));
            Expression Nullables ()
            {
                return Expression.CreateList (new List<Expression> { Expression.ConstantInt (1), none });
            }
            var set = Expression.CreateSet (new HashSet<Expression> { Expression.ConstantInt (1) });
            var strings = Expression.CreateList (new List<Expression> { Expression.ConstantString ("a") });
            Assert.IsFalse (Eval<bool> (Expression.Remove (list, none)));
            Assert.IsFalse (Eval<bool> (Expression.Remove (set, none)));
            Assert.IsFalse (Eval<bool> (Expression.Remove (strings, NullableString (null))));
            Assert.IsFalse (Eval<bool> (Expression.Remove (dictionary, NullableString (null))));
            Assert.IsTrue (Eval<bool> (Expression.Remove (Nullables (), none)));
            Assert.IsTrue (Eval<bool> (Expression.Remove (dictionary, NullableString ("a"))));
        }

        [Test]
        public void SetIsAStatement ()
        {
            var numbers = Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) });
            var values = Expression.CreateEmptyDictionary (Type.String (), Type.Int ());
            foreach (var set in new [] {
                Expression.Set (numbers, Expression.ConstantInt (0), Expression.ConstantInt (2)),
                Expression.Set (values, Expression.ConstantString ("a"), Expression.ConstantInt (2))
            }) {
                Assert.IsFalse (set.HasReturnType);
            }
        }

        [Test]
        public void ListIndexOutOfRange ()
        {
            var values = Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) });
            foreach (var index in new [] { -1, 1 }) {
                var i = Expression.ConstantInt (index);
                foreach (var expr in new List<Expression> {
                    Expression.Set (values, i, Expression.ConstantInt (2)),
                    Expression.RemoveAt (values, i)
                }) {
                    Assert.Throws<global::KRPC.Service.KRPC.IndexOutOfRangeException> (
                        () => expr.Evaluate ());
                }
            }
        }

        [Test]
        public void ListOperationsRejectASet ()
        {
            var values = Expression.CreateSet (new HashSet<Expression> {
                Expression.ConstantInt (1)
            });
            foreach (var build in new List<TestDelegate> {
                () => Expression.Set (
                    values, Expression.ConstantInt (0), Expression.ConstantInt (2)),
                () => Expression.RemoveAt (values, Expression.ConstantInt (0))
            }) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                    build);
                StringAssert.Contains ("Expected a list", exn.Message);
            }
        }

        [Test]
        public void CollectionOperationErrors ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Union (list, Expression.CreateList (
                    new List<Expression> { Expression.ConstantString ("a") })));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.DictionaryKeys (list));
            foreach (var build in new List<TestDelegate> {
                () => Expression.First (Expression.ConstantString ("abc")),
                () => Expression.Distinct (Expression.ConstantString ("abc")),
                () => Expression.Clear (Expression.ConstantString ("abc")),
                () => Expression.Set (
                    Expression.ConstantString ("abc"), Expression.ConstantInt (0), Expression.ConstantString ("x"))
            }) {
                var exn = Assert.Catch (build);
                StringAssert.Contains ("string is not a collection", exn.Message);
            }
        }

        [Test]
        public void ForEachAppendAndRemoveAtRejectADictionary ()
        {
            var parameter = Expression.Parameter ("x", Type.String ());
            var exn = Assert.Catch (
                () => Expression.ForEach (parameter, dictionary, Expression.Clear (list)));
            StringAssert.Contains ("dictionary cannot be used as a collection of values", exn.Message);
            // Appending and removing by index name the operations on an entry instead
            var append = Assert.Catch (
                () => Expression.Append (dictionary, Expression.ConstantString ("a")));
            StringAssert.Contains ("added to a dictionary with Set", append.Message);
            var removeAt = Assert.Catch (
                () => Expression.RemoveAt (dictionary, Expression.ConstantInt (0)));
            StringAssert.Contains ("removed from a dictionary with Remove", removeAt.Message);
        }

        [Test]
        public void BuildListInLoop ()
        {
            // result = []; for x in [1..5]: result.append(x * 2)
            var result = Expression.Variable ("result", Type.ListType (Type.Int ()));
            var x = Expression.Variable ("x", Type.Int ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { result, x },
                new List<Expression> {
                    Expression.Assign (result, Expression.CreateEmptyList (Type.Int ())),
                    Expression.ForEach (x, list,
                        Expression.Append (result, Expression.Multiply (x, Expression.ConstantInt (2)))),
                    result
                });
            CollectionAssert.AreEqual (new [] { 2, 4, 6, 8, 10 }, Eval<IList<int>> (expr));
        }

        [Test]
        public void CollectionMutation ()
        {
            var numbers = Expression.Variable ("numbers", Type.ListType (Type.Int ()));
            var expr = Expression.BlockWithVariables (
                new List<Expression> { numbers },
                new List<Expression> {
                    Expression.Assign (numbers, Expression.CreateEmptyList (Type.Int ())),
                    Expression.Append (numbers, Expression.ConstantInt (1)),
                    Expression.Append (numbers, Expression.ConstantInt (2)),
                    Expression.Set (numbers, Expression.ConstantInt (0), Expression.ConstantInt (10)),
                    numbers
                });
            CollectionAssert.AreEqual (new [] { 10, 2 }, Eval<IList<int>> (expr));

            var values = Expression.Variable ("values", Type.DictionaryType (Type.String (), Type.Int ()));
            var dictionaryExpr = Expression.BlockWithVariables (
                new List<Expression> { values },
                new List<Expression> {
                    Expression.Assign (values, Expression.CreateEmptyDictionary (Type.String (), Type.Int ())),
                    Expression.Set (values, Expression.ConstantString ("a"), Expression.ConstantInt (1)),
                    Expression.Set (values, Expression.ConstantString ("a"), Expression.ConstantInt (2)),
                    Expression.Get (values, Expression.ConstantString ("a"))
                });
            Assert.AreEqual (2, Eval<int> (dictionaryExpr));

            var seen = Expression.Variable ("seen", Type.SetType (Type.Int ()));
            var setExpr = Expression.BlockWithVariables (
                new List<Expression> { seen },
                new List<Expression> {
                    Expression.Assign (seen, Expression.CreateEmptySet (Type.Int ())),
                    Expression.Append (seen, Expression.ConstantInt (1)),
                    Expression.Append (seen, Expression.ConstantInt (1)),
                    Expression.Count (seen)
                });
            Assert.AreEqual (1, Eval<int> (setExpr));
        }

        [Test]
        public void CollectionElementsAreWidenedNotNarrowed ()
        {
            var values = Expression.Variable ("values", Type.ListType (Type.Double ()));
            var widened = Expression.BlockWithVariables (
                new List<Expression> { values },
                new List<Expression> {
                    Expression.Assign (values, Expression.CreateEmptyList (Type.Double ())),
                    Expression.Append (values, Expression.ConstantInt (1)),
                    values
                });
            CollectionAssert.AreEqual (new [] { 1.0 }, Eval<IList<double>> (widened));

            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Append (
                    Expression.CreateEmptyList (Type.Int ()),
                    Expression.ConstantDouble (1.5)));
            StringAssert.Contains ("No implicit conversion", exn.Message);

            var numbers = Expression.Variable ("numbers", Type.ListType (Type.Int ()));
            var narrowed = Expression.BlockWithVariables (
                new List<Expression> { numbers },
                new List<Expression> {
                    Expression.Assign (numbers, Expression.CreateEmptyList (Type.Int ())),
                    Expression.Append (numbers, Expression.Cast (
                        Expression.ConstantDouble (2.5), Type.Int ())),
                    numbers
                });
            CollectionAssert.AreEqual (new [] { 2 }, Eval<IList<int>> (narrowed));
        }

        [Test]
        public void DictionaryKeysAreWidenedNotNarrowed ()
        {
            var values = Expression.Variable (
                "values", Type.DictionaryType (Type.Long (), Type.Int ()));
            var widened = Expression.BlockWithVariables (
                new List<Expression> { values },
                new List<Expression> {
                    Expression.Assign (values, Expression.CreateEmptyDictionary (
                        Type.Long (), Type.Int ())),
                    Expression.Set (
                        values, Expression.ConstantInt (1), Expression.ConstantInt (2)),
                    Expression.Get (values, Expression.ConstantInt (1))
                });
            Assert.AreEqual (2, Eval<int> (widened));

            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Get (
                    Expression.CreateEmptyDictionary (Type.UInt (), Type.Int ()),
                    Expression.ConstantInt (1)));
            StringAssert.Contains ("No implicit conversion", exn.Message);
        }

        [Test]
        public void SetIndexesMustBeIntegers ()
        {
            var numbers = Expression.Variable ("numbers", Type.ListType (Type.Int ()));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Set (
                    numbers, Expression.ConstantDouble (0), Expression.ConstantInt (1)));
            StringAssert.Contains ("Expected an integer", exn.Message);
        }

        [Test]
        public void NullableValueAppendedToAList ()
        {
            var list = Expression.CreateEmptyList (Type.String ());
            var nullableList = Expression.CreateEmptyList (Type.NullableType (Type.String ()));
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => System.Linq.Expressions.Expression.Lambda<System.Action> (
                    Expression.Append (list, NullableString (null))).Compile () ());
            System.Linq.Expressions.Expression.Lambda<System.Action> (
                Expression.Append (nullableList, NullableString (null))).Compile () ();
        }

        [Test]
        public void ValueWithANestedNullAppendedToASet ()
        {
            System.Func<Type, Expression> appendToSet = itemType => {
                var tupleType = Type.TupleType (new List<Type> { Type.Int (), itemType });
                var set = Expression.Variable ("set", Type.SetType (tupleType));
                var tuple = Expression.CreateTuple (new List<Expression> {
                    Expression.ConstantInt (1), Expression.ConstantNull (Type.String ()) });
                return Expression.BlockWithVariables (new List<Expression> { set }, new List<Expression> {
                    Expression.Assign (set, Expression.CreateEmptySet (tupleType)),
                    Expression.Append (set, tuple),
                    Expression.Count (set) });
            };
            Assert.AreEqual (1, Eval<int> (appendToSet (Type.NullableType (Type.String ()))));
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<int> (appendToSet (Type.String ())));
        }

        [Test]
        public void MutationComparesByValue ()
        {
            Expression One ()
            {
                return Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) });
            }
            var listType = Type.ListType (Type.Int ());
            var values = Expression.Variable ("values", Type.ListType (listType));
            Assert.AreEqual (1, Eval<int> (Expression.BlockWithVariables (
                new List<Expression> { values },
                new List<Expression> {
                    Expression.Assign (values, Expression.CreateList (new List<Expression> { One (), One () })),
                    Expression.Remove (values, One ()),
                    Expression.Count (values)
                })));
            var set = Expression.Variable ("set", Type.SetType (listType));
            Assert.AreEqual (1, Eval<int> (Expression.BlockWithVariables (
                new List<Expression> { set },
                new List<Expression> {
                    Expression.Assign (set, Expression.CreateEmptySet (listType)),
                    Expression.Append (set, One ()),
                    Expression.Append (set, One ()),
                    Expression.Count (set)
                })));

            // A set a procedure returns compares its values by reference
            var parameter = Expression.Parameter ("set", Type.SetType (listType));
            var given = new HashSet<IList<int>> { new List<int> { 1 }, new List<int> { 1 } };
            System.Delegate Compile (Expression body)
            {
                var function = Expression.Lambda (new List<Expression> { parameter }, body);
                return ((System.Linq.Expressions.LambdaExpression)(System.Linq.Expressions.Expression)function).Compile ();
            }
            Compile (Expression.Append (parameter, One ())).DynamicInvoke (given);
            Assert.AreEqual (2, given.Count);
            Assert.IsTrue ((bool)Compile (Expression.Remove (parameter, One ())).DynamicInvoke (given));
            Assert.AreEqual (0, given.Count);
        }
    }
}
