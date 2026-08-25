using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KRPC.Service.KRPC;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Test.Service.KRPC
{
    [TestFixture]
    public class ExpressionTest
    {
        static T Eval<T> (Expression expression)
        {
            return LinqExpression.Lambda<System.Func<T>> (expression).Compile () ();
        }

        Expression tuple;
        Expression list;
        Expression set;
        Expression dictionary;

        [SetUp]
        public void SetUp ()
        {
            tuple = Expression.CreateTuple (new List<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantBool (false)
            });
            list = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantInt (2),
                Expression.ConstantInt (3),
                Expression.ConstantInt (4),
                Expression.ConstantInt (5)
            });
            set = Expression.CreateSet (new HashSet<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantInt (2),
                Expression.ConstantInt (3),
                Expression.ConstantInt (4),
                Expression.ConstantInt (4)
            });
            var keys = new List<Expression> {
                Expression.ConstantString ("a"),
                Expression.ConstantString ("b"),
                Expression.ConstantString ("c")
            };
            var values = new List<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantInt (2),
                Expression.ConstantInt (3)
            };
            dictionary = Expression.CreateDictionary (keys, values);
        }

        [Test]
        public void Constant ()
        {
            Assert.AreEqual (1.2, Eval<double> (Expression.ConstantDouble (1.2)));
            Assert.AreEqual (3.4f, Eval<float> (Expression.ConstantFloat (3.4f)));
            Assert.AreEqual (5, Eval<int> (Expression.ConstantInt (5)));
            Assert.IsFalse (Eval<bool> (Expression.ConstantBool (false)));
            Assert.IsTrue (Eval<bool> (Expression.ConstantBool (true)));
            Assert.AreEqual ("foo", Eval<string> (Expression.ConstantString ("foo")));
        }

        static ulong AddInstance (object obj)
        {
            return global::KRPC.Service.ObjectStore.Instance.AddInstance (obj);
        }

        /// <summary>
        /// An expression constructing a TestStruct whose object field is the instance
        /// with the given identifier, with a value in every other field.
        /// </summary>
        static Expression BuildTestStruct (ulong objectId)
        {
            return Expression.CreateStruct (
                Type.StructType ("TestService", "TestStruct"),
                new List<Expression> {
                    Expression.ConstantInt (42),
                    Expression.ConstantString ("bar"),
                    Expression.Cast (
                        Expression.ConstantInt (1),
                        Type.EnumerationType ("TestService", "TestEnum")),
                    Expression.ConstantObject (objectId),
                    Expression.CreateList (new List<Expression> {
                        Expression.ConstantString ("a"),
                        Expression.ConstantString ("b")
                    })
                });
        }

        static ProcedureCall BuildProcedureCall (string procedure, params Argument[] args)
        {
            var call = new ProcedureCall ("TestService", procedure);
            foreach (var arg in args)
                call.Arguments.Add (arg);
            return call;
        }

        [SetUp]
        public void SetUpGameScene ()
        {
            global::KRPC.Service.CallContext.GameScene = global::KRPC.Service.GameScene.Flight;
        }

        [Test]
        public void Call ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            obj.IntProperty = 42;
            var expr = Expression.Call (BuildProcedureCall ("TestClass_get_IntProperty", new Argument (0, obj)));
            Assert.AreEqual (42, Eval<int> (expr));
            var method = Expression.Call (BuildProcedureCall (
                "TestClass_FloatToString", new Argument (0, obj), new Argument (1, 0.5f)));
            Assert.AreEqual ("foo0.5", Eval<string> (method));
        }

        [Test]
        public void CallDefaultArgument ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.Call (BuildProcedureCall ("TestClass_IntToString", new Argument (0, obj)));
            Assert.AreEqual ("foo42", Eval<string> (expr));
        }

        [Test]
        public void CallMissingArgument ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Call (BuildProcedureCall ("TestClass_FloatToString", new Argument (0, obj))));
        }

        [Test]
        public void CallWrongGameScene ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_MethodAvailableInSpecifiedGameScene", new Argument (0, obj)));
            Assert.Throws<global::KRPC.Service.RPCException> (() => Eval<string> (expr));
        }

        [Test]
        public void CallNullReturnAllowed ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_get_ObjectProperty", new Argument (0, obj)));
            Assert.IsNull (Eval<global::KRPC.Test.Service.TestService.TestClass> (expr));
        }

        [Test]
        public void CallNullReturnNotAllowed ()
        {
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ReturnNullWhenNotAllowed ())
                .Returns ((global::KRPC.Test.Service.TestService.TestClass)null);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var expr = Expression.Call (BuildProcedureCall ("ReturnNullWhenNotAllowed"));
            Assert.Throws<global::KRPC.Service.RPCException> (
                () => Eval<global::KRPC.Test.Service.TestService.TestClass> (expr));
        }

        [Test]
        public void CallWithArguments ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString"),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (AddInstance (obj)) },
                    { 1, Expression.ConstantFloat (3.5f) }
                });
            Assert.AreEqual ("foo3.5", Eval<string> (expr));
        }

        [Test]
        public void CallWithArgumentsPartial ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            // Instance provided by an expression; float argument from the encoded call
            var expr = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (1, 0.5f)),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (AddInstance (obj)) }
                });
            Assert.AreEqual ("foo0.5", Eval<string> (expr));
            // Instance from the encoded call; float argument from an expression
            var expr2 = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (0, obj)),
                new Dictionary<int, Expression> {
                    { 1, Expression.ConstantFloat (1.5f) }
                });
            Assert.AreEqual ("foo1.5", Eval<string> (expr2));
        }

        [Test]
        public void CallWithArgumentsDefaultArgument ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_IntToString"),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (AddInstance (obj)) }
                });
            Assert.AreEqual ("foo42", Eval<string> (expr));
        }

        [Test]
        public void CallWithArgumentsNumericConversion ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            // int expression implicitly converted to the float parameter
            var expr = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString"),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (AddInstance (obj)) },
                    { 1, Expression.ConstantInt (3) }
                });
            Assert.AreEqual ("foo3", Eval<string> (expr));
        }

        [Test]
        public void CallWithArgumentsWrongType ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CallWithArguments (
                    BuildProcedureCall ("TestClass_FloatToString"),
                    new Dictionary<int, Expression> {
                        { 0, Expression.ConstantObject (AddInstance (obj)) },
                        { 1, Expression.ConstantString ("nope") }
                    }));
        }

        [Test]
        public void CallPerElement ()
        {
            var objs = new [] {
                new global::KRPC.Test.Service.TestService.TestClass ("a"),
                new global::KRPC.Test.Service.TestService.TestClass ("b"),
                new global::KRPC.Test.Service.TestService.TestClass ("c")
            };
            for (int i = 0; i < objs.Length; i++)
                objs [i].IntProperty = i + 1;
            var objects = Expression.CreateList (
                objs.Select (x => Expression.ConstantObject (AddInstance (x))).ToList ());
            var param = Expression.Parameter ("x", Type.ClassType ("TestService", "TestClass"));
            var getProperty = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_get_IntProperty"),
                new Dictionary<int, Expression> { { 0, param } });

            // Select the property value of each object in the list
            var selected = Eval<List<int>> (Expression.ToList (Expression.Select (
                objects, Expression.Lambda (new List<Expression> { param }, getProperty))));
            CollectionAssert.AreEqual (new [] { 1, 2, 3 }, selected);

            // Whether any object's property satisfies a predicate
            var equalsTwo = Expression.Lambda (
                new List<Expression> { param },
                Expression.Equal (getProperty, Expression.ConstantInt (2)));
            Assert.IsTrue (Eval<bool> (Expression.Any (objects, equalsTwo)));
            var equalsFour = Expression.Lambda (
                new List<Expression> { param },
                Expression.Equal (getProperty, Expression.ConstantInt (4)));
            Assert.IsFalse (Eval<bool> (Expression.Any (objects, equalsFour)));
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
        public void FunctionReusedAcrossExpressions ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (
                new List<Expression> { x },
                Expression.Multiply (x, Expression.ConstantInt (3)));
            var first = Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (1) } });
            var second = Expression.Add (
                Expression.Invoke (
                    function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (2) } }),
                Expression.ConstantInt (1));
            Assert.AreEqual (3, Eval<int> (first));
            Assert.AreEqual (7, Eval<int> (second));
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
        public void SkipAndTake ()
        {
            var expr = Expression.ToList (Expression.Take (
                Expression.Skip (list, Expression.ConstantInt (2)),
                Expression.ConstantInt (2)));
            CollectionAssert.AreEqual (new [] { 3, 4 }, Eval<List<int>> (expr));
        }

        [Test]
        public void SelectManyOp ()
        {
            var param = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { param },
                Expression.CreateList (new List<Expression> {
                    param, Expression.Multiply (param, Expression.ConstantInt (10))
                }));
            var expr = Expression.ToList (Expression.SelectMany (list, func));
            CollectionAssert.AreEqual (
                new [] { 1, 10, 2, 20, 3, 30, 4, 40, 5, 50 }, Eval<List<int>> (expr));
        }

        [Test]
        public void BuildDictionaryOp ()
        {
            var param = Expression.Parameter ("x", Type.Int ());
            var keyFunc = Expression.Lambda (
                new List<Expression> { param }, Expression.ConvertToString (param));
            var valueFunc = Expression.Lambda (
                new List<Expression> { param },
                Expression.Multiply (param, Expression.ConstantInt (2)));
            var expr = Expression.BuildDictionary (list, keyFunc, valueFunc);
            var dictionary = Eval<Dictionary<string, int>> (expr);
            Assert.AreEqual (5, dictionary.Count);
            Assert.AreEqual (2, dictionary ["1"]);
            Assert.AreEqual (10, dictionary ["5"]);
        }

        [Test]
        public void Strings ()
        {
            Assert.AreEqual ("1.5", Eval<string> (
                Expression.ConvertToString (Expression.ConstantDouble (1.5))));
            Assert.AreEqual ("42", Eval<string> (
                Expression.ConvertToString (Expression.ConstantInt (42))));
            Assert.AreEqual ("a2", Eval<string> (Expression.StringConcat (
                new List<Expression> {
                    Expression.ConstantString ("a"),
                    Expression.ConvertToString (Expression.ConstantInt (2))
                })));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.StringConcat (new List<Expression> {
                    Expression.ConstantString ("a"), Expression.ConstantInt (2)
                }));
        }


        [Test]
        public void ConstantEnum ()
        {
            var value = Eval<global::KRPC.Test.Service.TestService.TestEnum> (
                Expression.ConstantEnum ("TestService", "TestEnum", 1));
            Assert.AreEqual (global::KRPC.Test.Service.TestService.TestEnum.Y, value);
            // Naming a member is the same value as casting its number to the type
            Assert.AreSame (
                Expression.ConstantEnum ("TestService", "TestEnum", 1),
                Expression.ConstantEnum ("TestService", "TestEnum", 1));
        }

        [Test]
        public void ConstantEnumErrors ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantEnum ("NoSuchService", "TestEnum", 0));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantEnum ("TestService", "NoSuchEnum", 0));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantEnum ("TestService", "TestEnum", 99));
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
        public void DictionaryKeysAndValues ()
        {
            CollectionAssert.AreEquivalent (
                new List<string> { "a", "b", "c" },
                Eval<IList<string>> (Expression.DictionaryKeys (dictionary)));
            CollectionAssert.AreEquivalent (
                new List<int> { 1, 2, 3 },
                Eval<IList<int>> (Expression.DictionaryValues (dictionary)));
            // A dictionary cannot be iterated over, so its keys are what a loop reads
            Assert.AreEqual (3, Eval<int> (
                Expression.Count (Expression.DictionaryKeys (dictionary))));
        }

        [Test]
        public void ElementSelection ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.First (list)));
            Assert.AreEqual (5, Eval<int> (Expression.Last (list)));
            Assert.AreEqual (3, Eval<int> (
                Expression.ElementAt (list, Expression.ConstantInt (2))));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.ElementAt (list, Expression.ConstantDouble (2)));
        }

        [Test]
        public void MinAndMaxByKey ()
        {
            // The value furthest from three, so the largest key belongs to the
            // smallest value and vice versa
            var x = Expression.Parameter ("x", Type.Int ());
            var distance = Expression.Lambda (
                new List<Expression> { x },
                Expression.Multiply (
                    Expression.Subtract (x, Expression.ConstantInt (3)),
                    Expression.Subtract (x, Expression.ConstantInt (3))));
            Assert.AreEqual (3, Eval<int> (Expression.MinBy (list, distance)));
            Assert.AreEqual (1, Eval<int> (Expression.MaxBy (list, distance)));
        }

        [Test]
        public void Reshaping ()
        {
            var duplicated = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantInt (2),
                Expression.ConstantInt (1)
            });
            var other = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (2),
                Expression.ConstantInt (3)
            });
            CollectionAssert.AreEqual (
                new List<int> { 1, 2 },
                Eval<IList<int>> (Expression.ToList (Expression.Distinct (duplicated))));
            CollectionAssert.AreEqual (
                new List<int> { 1, 2, 1 },
                Eval<IList<int>> (Expression.ToList (
                    Expression.Reverse (Expression.Reverse (duplicated)))));
            CollectionAssert.AreEqual (
                new List<int> { 5, 4, 3, 2, 1 },
                Eval<IList<int>> (Expression.ToList (Expression.Reverse (list))));
            CollectionAssert.AreEqual (
                new List<int> { 1, 2, 3 },
                Eval<IList<int>> (Expression.ToList (
                    Expression.Union (duplicated, other))));
            CollectionAssert.AreEqual (
                new List<int> { 2 },
                Eval<IList<int>> (Expression.ToList (
                    Expression.Intersect (duplicated, other))));
            CollectionAssert.AreEqual (
                new List<int> { 1 },
                Eval<IList<int>> (Expression.ToList (
                    Expression.Except (duplicated, other))));
        }

        [Test]
        public void ZipTwoCollections ()
        {
            var other = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (10),
                Expression.ConstantInt (20)
            });
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            var add = Expression.Lambda (
                new List<Expression> { x, y }, Expression.Add (x, y));
            CollectionAssert.AreEqual (
                new List<int> { 11, 22 },
                Eval<IList<int>> (Expression.ToList (Expression.Zip (list, other, add))));
        }

        [Test]
        public void GroupByKey ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var isEven = Expression.Lambda (
                new List<Expression> { x },
                Expression.Equal (
                    Expression.Modulo (x, Expression.ConstantInt (2)),
                    Expression.ConstantInt (0)));
            var groups = Eval<IDictionary<bool, IList<int>>> (
                Expression.GroupBy (list, isEven));
            CollectionAssert.AreEqual (new List<int> { 2, 4 }, groups [true]);
            CollectionAssert.AreEqual (new List<int> { 1, 3, 5 }, groups [false]);
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
                () => Expression.Clear (Expression.ConstantString ("abc"))
            }) {
                var exn = Assert.Catch (build);
                StringAssert.Contains ("string is not a collection", exn.Message);
            }
        }
        [Test]
        public void StringOperations ()
        {
            var s = Expression.ConstantString ("Hello, World");
            Assert.AreEqual (12, Eval<int> (Expression.StringLength (s)));
            Assert.AreEqual ("H", Eval<string> (
                Expression.StringGet (s, Expression.ConstantInt (0))));
            Assert.AreEqual ("World", Eval<string> (Expression.StringSubstring (
                s, Expression.ConstantInt (7), Expression.ConstantInt (5))));
            Assert.AreEqual (7, Eval<int> (
                Expression.StringIndexOf (s, Expression.ConstantString ("World"))));
            Assert.AreEqual (-1, Eval<int> (
                Expression.StringIndexOf (s, Expression.ConstantString ("Mars"))));
            Assert.IsTrue (Eval<bool> (
                Expression.StringContains (s, Expression.ConstantString ("o, W"))));
            Assert.IsFalse (Eval<bool> (
                Expression.StringContains (s, Expression.ConstantString ("Mars"))));
            Assert.IsTrue (Eval<bool> (
                Expression.StringStartsWith (s, Expression.ConstantString ("Hello"))));
            Assert.IsFalse (Eval<bool> (
                Expression.StringStartsWith (s, Expression.ConstantString ("World"))));
            Assert.IsTrue (Eval<bool> (
                Expression.StringEndsWith (s, Expression.ConstantString ("World"))));
            Assert.AreEqual ("HELLO, WORLD", Eval<string> (Expression.StringToUpper (s)));
            Assert.AreEqual ("hello, world", Eval<string> (Expression.StringToLower (s)));
            Assert.AreEqual ("Hello, Mars", Eval<string> (Expression.StringReplace (
                s, Expression.ConstantString ("World"), Expression.ConstantString ("Mars"))));
            CollectionAssert.AreEqual (
                new List<string> { "Hello", "World" },
                Eval<IList<string>> (Expression.StringSplit (
                    s, Expression.ConstantString (", "))));
            Assert.AreEqual ("Hello, World", Eval<string> (Expression.StringJoin (
                Expression.ConstantString (", "),
                Expression.StringSplit (s, Expression.ConstantString (", ")))));
        }

        [Test]
        public void StringTrimming ()
        {
            var s = Expression.ConstantString ("  pad  ");
            Assert.AreEqual ("pad", Eval<string> (Expression.StringTrim (s)));
            Assert.AreEqual ("pad  ", Eval<string> (Expression.StringTrimStart (s)));
            Assert.AreEqual ("  pad", Eval<string> (Expression.StringTrimEnd (s)));
        }

        [Test]
        public void StringOperationsRejectNonStrings ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.StringLength (Expression.ConstantInt (1)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.StringGet (
                    Expression.ConstantString ("a"), Expression.ConstantDouble (0)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.StringJoin (
                    Expression.ConstantString (","), Expression.ConstantString ("a")));
        }

        [Test]
        public void StringOperationsDoNotDependOnTheCulture ()
        {
            // The same function must produce the same result whatever language the
            // game is running in. Turkish is the case that catches a culture
            // sensitive conversion, mapping i onto a dotted capital I
            var culture = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = new CultureInfo ("tr-TR");
                Assert.AreEqual ("I", Eval<string> (
                    Expression.StringToUpper (Expression.ConstantString ("i"))));
                Assert.AreEqual ("i", Eval<string> (
                    Expression.StringToLower (Expression.ConstantString ("I"))));
                // A zero width joiner is ignorable in a culture sensitive comparison
                // and is a character like any other in an ordinal one
                Assert.AreEqual (-1, Eval<int> (Expression.StringIndexOf (
                    Expression.ConstantString ("abc"),
                    Expression.ConstantString ("‍"))));
            } finally {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Test]
        public void StringsAreNotCollections ()
        {
            var s = Expression.ConstantString ("abc");
            foreach (var build in new List<TestDelegate> {
                () => Expression.Count (s),
                () => Expression.Get (s, Expression.ConstantInt (0)),
                () => Expression.Contains (s, Expression.ConstantString ("a")),
                () => Expression.ToList (s)
            }) {
                var exn = Assert.Catch (build);
                StringAssert.Contains ("string is not a collection", exn.Message);
            }
        }

        [Test]
        public void DictionariesAreNotCollectionsOfValues ()
        {
            var parameter = Expression.Parameter ("x", Type.String ());
            foreach (var build in new List<TestDelegate> {
                () => Expression.ToList (dictionary),
                () => Expression.Select (dictionary, Expression.Lambda (
                    new List<Expression> { parameter }, parameter)),
                () => Expression.ForEach (parameter, dictionary, Expression.Clear (list))
            }) {
                var exn = Assert.Catch (build);
                StringAssert.Contains (
                    "dictionary cannot be used as a collection of values", exn.Message);
            }
            // Appending names the operation that adds an entry instead
            var append = Assert.Catch (
                () => Expression.Append (dictionary, Expression.ConstantString ("a")));
            StringAssert.Contains ("added to a dictionary with Set", append.Message);
            // Reading the keys or the values gives a list, and counting works directly
            Assert.AreEqual (3, Eval<int> (Expression.Count (dictionary)));
            Assert.AreEqual (
                new List<string> { "a", "b", "c" },
                Eval<IList<string>> (Expression.DictionaryKeys (dictionary)));
        }

        [Test]
        public void Throw ()
        {
            var expr = Expression.Throw (
                "KRPC", "InvalidOperationException", Expression.ConstantString ("boom"));
            Assert.IsFalse (expr.HasReturnType);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => expr.Runner ());
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

        [Test]
        public void TryCatchCatchesTheMappedException ()
        {
            // A service throws the CLR exception types, which the client sees under
            // the kRPC name, so naming that name catches them
            var expr = Expression.TryCatch (
                // Substring past the end of the string, a System.ArgumentOutOfRangeException
                Expression.StringGet (
                    Expression.ConstantString ("ab"), Expression.ConstantInt (5)),
                "KRPC", "ArgumentOutOfRangeException", null,
                Expression.ConstantString ("caught"));
            Assert.DoesNotThrow (() => expr.Runner ());
        }

        [Test]
        public void TryCatchDoesNotCatchAnotherException ()
        {
            var expr = Expression.TryCatch (
                Expression.StringGet (
                    Expression.ConstantString ("ab"), Expression.ConstantInt (5)),
                "KRPC", "ObjectDestroyedException", null,
                Expression.ConstantString ("caught"));
            Assert.Throws<System.ArgumentOutOfRangeException> (() => expr.Runner ());
        }

        [Test]
        public void TryCatchDoesNotCatchASubclass ()
        {
            // ArgumentOutOfRangeException derives from ArgumentException, and reaches
            // the client under its own name, so naming ArgumentException leaves it alone
            var expr = Expression.TryCatch (
                Expression.StringGet (
                    Expression.ConstantString ("ab"), Expression.ConstantInt (5)),
                "KRPC", "ArgumentException", null,
                Expression.ConstantString ("caught"));
            Assert.Throws<System.ArgumentOutOfRangeException> (() => expr.Runner ());
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
            // A procedure that pauses execution unwinds by throwing YieldException.
            // The catch-all lets it through, so the stream evaluating the expression
            // still reports the pause
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
            Assert.Throws<global::KRPC.Service.YieldException<System.Func<int>>> (
                () => expr.Runner ());
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
        public void BuildListInLoop ()
        {
            // result = []; for x in [1..5]: result.add(x * 2)
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
        public void RunFunctionValue ()
        {
            var bytes = global::KRPC.Service.KRPC.KRPC.RunFunction (
                Expression.Multiply (Expression.ConstantInt (6), Expression.ConstantInt (7)));
            var value = global::KRPC.Server.ProtocolBuffers.Encoder.Decode (
                Google.Protobuf.ByteString.CopyFrom (bytes), typeof (int));
            Assert.AreEqual (42, value);
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
        public void BreakOutsideLoop ()
        {
            var expr = Expression.Block (new List<Expression> {
                Expression.Break (),
                Expression.ConstantInt (1)
            });
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Eval<int> (expr));
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
            // An unbound marker compiles, so it is caught before compiling
            foreach (var marker in new [] {
                Expression.Break (), Expression.Continue (),
                Expression.Return (Expression.ConstantInt (1))
            }) {
                var expr = StatementWithMarker (marker);
                Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                    () => global::KRPC.Service.KRPC.KRPC.RunFunction (expr));
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
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Return (statement));
            StringAssert.Contains ("must be given a value", exn.Message);
        }

        [Test]
        public void GetWithoutAnIndex ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.Get (tuple, null));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.Get (list, null));
        }

        [Test]
        public void IndexesMustBeIntegers ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Get (list, Expression.ConstantDouble (0)));
            StringAssert.Contains ("Expected an integer", exn.Message);
            var numbers = Expression.Variable ("numbers", Type.ListType (Type.Int ()));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Set (
                    numbers, Expression.ConstantDouble (0), Expression.ConstantInt (1)));
        }

        [Test]
        public void ReturnType ()
        {
            Assert.AreEqual (TypeCode.Double, Expression.ConstantDouble (1.2).ReturnType.Code);
            Assert.AreEqual (TypeCode.Bool, Expression.Equal (
                Expression.ConstantInt (1), Expression.ConstantInt (2)).ReturnType.Code);
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var call = Expression.Call (BuildProcedureCall ("TestClass_get_IntProperty", new Argument (0, obj)));
            Assert.AreEqual (TypeCode.SInt32, call.ReturnType.Code);
            var objConstant = Expression.ConstantObject (AddInstance (obj));
            var objType = objConstant.ReturnType;
            Assert.AreEqual (TypeCode.Class, objType.Code);
            Assert.AreEqual ("TestService", objType.Service);
            Assert.AreEqual ("TestClass", objType.Name);
            var listType = list.ReturnType;
            Assert.AreEqual (TypeCode.List, listType.Code);
            Assert.AreEqual (TypeCode.SInt32, listType.Types [0].Code);
        }

        [Test]
        public void ReturnTypeOfNullableValue ()
        {
            var expr = Expression.Call (BuildProcedureCall (
                "EchoNullableInt", new Argument (0, 3)));
            Assert.AreEqual (TypeCode.SInt32, expr.ReturnType.Code);
            var list = Expression.Call (BuildProcedureCall (
                "EchoListOfNullableInts",
                new Argument (0, new List<int?> { 1, null })));
            Assert.AreEqual (TypeCode.List, list.ReturnType.Code);
            Assert.AreEqual (TypeCode.SInt32, list.ReturnType.Types.Single ().Code);
        }

        [Test]
        public void ReturnTypeOfLazyCollection ()
        {
            var param = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { param },
                Expression.Multiply (param, Expression.ConstantInt (2)));
            var selected = Expression.Select (list, func);
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => { var unused = selected.ReturnType; });
            Assert.AreEqual (TypeCode.List, Expression.ToList (selected).ReturnType.Code);
        }

        [Test]
        public void ConstantObject ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var id = global::KRPC.Service.ObjectStore.Instance.AddInstance (obj);
            var expr = Expression.ConstantObject (id);
            Assert.AreEqual (typeof (global::KRPC.Test.Service.TestService.TestClass), ((LinqExpression)expr).Type);
            Assert.AreSame (obj, Eval<global::KRPC.Test.Service.TestService.TestClass> (expr));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantObject (id), Expression.ConstantObject (id))));
        }

        [Test]
        public void OperationsThatDoNotApplyAreReported ()
        {
            var strings = Expression.CreateList (new List<Expression> {
                Expression.ConstantString ("a"),
                Expression.ConstantString ("b")
            });
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Sum (strings));
            StringAssert.Contains ("Sum is not defined", exn.Message);

            exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Get (Expression.ConstantInt (1), Expression.ConstantInt (0)));
            StringAssert.Contains ("accessed by index", exn.Message);

            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Lambda (
                    new List<Expression> { Expression.ConstantInt (1) },
                    Expression.ConstantInt (2)));
        }

        [Test]
        public void CollectionValuesMustShareAType ()
        {
            var mixed = new List<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantString ("a")
            };
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateList (mixed));
            StringAssert.Contains ("values of a list", exn.Message);
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateSet (new HashSet<Expression> (mixed)));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateDictionary (
                    mixed, new List<Expression> {
                        Expression.ConstantInt (1),
                        Expression.ConstantInt (2)
                    }));
        }

        [Test]
        public void ConstantsAreShared ()
        {
            // A compiled function mentions the same literals over and over, so each
            // value gets one entry in the object store rather than one per mention
            var store = global::KRPC.Service.ObjectStore.Instance;
            Assert.AreEqual (store.AddInstance (Expression.ConstantInt (1)),
                             store.AddInstance (Expression.ConstantInt (1)));
            Assert.AreEqual (store.AddInstance (Expression.ConstantString ("a")),
                             store.AddInstance (Expression.ConstantString ("a")));
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantInt (1)),
                                store.AddInstance (Expression.ConstantInt (2)));
            // Constants of equal value but differing type stay distinct
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantInt (1)),
                                store.AddInstance (Expression.ConstantDouble (1)));
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantDouble (1)),
                                store.AddInstance (Expression.ConstantFloat (1)));
            // Negative zero is a distinct constant, though it compares equal to zero
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantDouble (0)),
                                store.AddInstance (Expression.ConstantDouble (-0.0)));
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantFloat (0)),
                                store.AddInstance (Expression.ConstantFloat (-0.0f)));
        }

        [Test]
        public void StringIsNotACollection ()
        {
            var text = Expression.ConstantString ("hello");
            var builders = new System.Func<Expression> [] {
                () => Expression.Count (text),
                () => Expression.ToList (text),
                () => Expression.Contains (text, Expression.ConstantString ("h")),
                () => Expression.Get (text, Expression.ConstantInt (0))
            };
            foreach (var build in builders) {
                var builder = build;
                var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                    () => builder ());
                StringAssert.Contains ("A string is not a collection", exn.Message);
            }
        }

        [Test]
        public void BytesIsNotACollection ()
        {
            var data = Expression.Variable ("data", Type.Bytes ());
            foreach (var build in new List<TestDelegate> {
                () => Expression.Count (data),
                () => Expression.ToList (data),
                () => Expression.First (data),
                () => Expression.Contains (data, Expression.ConstantInt (0))
            }) {
                var exn = Assert.Catch (build);
                StringAssert.Contains ("A bytes value is not a collection", exn.Message);
            }
        }

        [Test]
        public void OperationsGivenAValueInPlaceOfAFunction ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (new List<Expression> { x }, x);
            var value = Expression.ConstantInt (1);
            foreach (var build in new List<TestDelegate> {
                () => Expression.Select (list, value),
                () => Expression.SelectMany (list, value),
                () => Expression.Where (list, value),
                () => Expression.OrderBy (list, value),
                () => Expression.GroupBy (list, value),
                () => Expression.All (list, value),
                () => Expression.Any (list, value),
                () => Expression.BuildDictionary (list, value, func),
                () => Expression.BuildDictionary (list, func, value),
                () => Expression.Zip (list, list, value),
                // A function of the wrong arity is reported the same way
                () => Expression.Zip (list, list, func)
            }) {
                var exn = Assert.Catch (build);
                StringAssert.Contains ("Expected a function taking", exn.Message);
            }
            // SelectMany takes a function producing a collection
            var flatten = Assert.Catch (() => Expression.SelectMany (list, func));
            StringAssert.Contains ("must return a collection", flatten.Message);
        }

        [Test]
        public void OperationsGivenNoFunction ()
        {
            foreach (var build in new List<TestDelegate> {
                () => Expression.Select (list, null),
                () => Expression.SelectMany (list, null),
                () => Expression.OrderBy (list, null),
                () => Expression.All (list, null),
                () => Expression.Any (list, null),
                () => Expression.Zip (list, list, null)
            })
                Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (build);
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
        public void CollectionsGivenANullElement ()
        {
            var one = Expression.ConstantInt (1);
            foreach (var build in new List<TestDelegate> {
                () => Expression.CreateTuple (new List<Expression> { one, null }),
                () => Expression.CreateList (new List<Expression> { one, null }),
                () => Expression.CreateSet (new HashSet<Expression> { one, null }),
                () => Expression.CreateDictionary (
                    new List<Expression> { Expression.ConstantString ("a"), null },
                    new List<Expression> { one, one }),
                () => Expression.CreateDictionary (
                    new List<Expression> { Expression.ConstantString ("a") },
                    new List<Expression> { null }),
                () => Expression.CreateStruct (
                    Type.StructType ("TestService", "TestNestedStruct"),
                    new List<Expression> { null, one }),
                () => Expression.StringConcat (new List<Expression> { one, null }),
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
        public void ConstantObjectInvalid ()
        {
            // The object store issues identifiers from one, and reports both an
            // identifier it never issued and one it has not reached
            var zero = Assert.Throws<System.ArgumentException> (
                () => Expression.ConstantObject (0));
            StringAssert.Contains ("0 is not an object identifier", zero.Message);
            Assert.Throws<System.ArgumentException> (
                () => Expression.ConstantObject (ulong.MaxValue));
            var id = global::KRPC.Service.ObjectStore.Instance.AddInstance (new object ());
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantObject (id));
        }

        [Test]
        public void Equal ()
        {
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsFalse (Eval<bool> (Expression.Equal (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantString ("foo"), Expression.ConstantString ("foo"))));
            Assert.IsFalse (Eval<bool> (Expression.Equal (
                Expression.ConstantString ("foo"), Expression.ConstantString ("bar"))));
        }

        [Test]
        public void NotEqual ()
        {
            Assert.IsFalse (Eval<bool> (Expression.NotEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsFalse (Eval<bool> (Expression.NotEqual (
                Expression.ConstantString ("foo"), Expression.ConstantString ("foo"))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                Expression.ConstantString ("foo"), Expression.ConstantString ("bar"))));
        }

        [Test]
        public void GreaterThan ()
        {
            Assert.IsFalse (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsFalse (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsTrue (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
        }

        [Test]
        public void GreaterThanOrEqual ()
        {
            Assert.IsTrue (Eval<bool> (Expression.GreaterThanOrEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsFalse (Eval<bool> (Expression.GreaterThanOrEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsTrue (Eval<bool> (Expression.GreaterThanOrEqual (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
        }

        [Test]
        public void LessThan ()
        {
            Assert.IsFalse (Eval<bool> (Expression.LessThan (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsTrue (Eval<bool> (Expression.LessThan (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsFalse (Eval<bool> (Expression.LessThan (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
        }

        [Test]
        public void LessThanOrEqual ()
        {
            Assert.IsTrue (Eval<bool> (Expression.LessThanOrEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsTrue (Eval<bool> (Expression.LessThanOrEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsFalse (Eval<bool> (Expression.LessThanOrEqual (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
        }

        [Test]
        public void And ()
        {
            Assert.IsTrue (Eval<bool> (Expression.And (
                Expression.ConstantBool (true), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.And (
                Expression.ConstantBool (true), Expression.ConstantBool (false))));
            Assert.IsFalse (Eval<bool> (Expression.And (
                Expression.ConstantBool (false), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.And (
                Expression.ConstantBool (false), Expression.ConstantBool (false))));
        }

        [Test]
        public void Or ()
        {
            Assert.IsTrue (Eval<bool> (Expression.Or (
                Expression.ConstantBool (true), Expression.ConstantBool (true))));
            Assert.IsTrue (Eval<bool> (Expression.Or (
                Expression.ConstantBool (true), Expression.ConstantBool (false))));
            Assert.IsTrue (Eval<bool> (Expression.Or (
                Expression.ConstantBool (false), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.Or (
                Expression.ConstantBool (false), Expression.ConstantBool (false))));
        }

        [Test]
        public void ConditionalOperatorsShortCircuit ()
        {
            // Dividing by zero throws, so the second operand shows whether it was
            // evaluated at all
            var fails = Expression.Equal (
                Expression.Divide (Expression.ConstantInt (1), Expression.ConstantInt (0)),
                Expression.ConstantInt (0));
            Assert.IsFalse (Eval<bool> (
                Expression.ConditionalAnd (Expression.ConstantBool (false), fails)));
            Assert.IsTrue (Eval<bool> (
                Expression.ConditionalOr (Expression.ConstantBool (true), fails)));
            Assert.Throws<System.DivideByZeroException> (() => Eval<bool> (
                Expression.And (Expression.ConstantBool (false), fails)));
            Assert.Throws<System.DivideByZeroException> (() => Eval<bool> (
                Expression.Or (Expression.ConstantBool (true), fails)));
            Assert.IsTrue (Eval<bool> (Expression.ConditionalAnd (
                Expression.ConstantBool (true), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.ConditionalOr (
                Expression.ConstantBool (false), Expression.ConstantBool (false))));
        }

        [Test]
        public void ExclusiveOr ()
        {
            Assert.IsFalse (Eval<bool> (Expression.ExclusiveOr (
                Expression.ConstantBool (true), Expression.ConstantBool (true))));
            Assert.IsTrue (Eval<bool> (Expression.ExclusiveOr (
                Expression.ConstantBool (true), Expression.ConstantBool (false))));
            Assert.IsTrue (Eval<bool> (Expression.ExclusiveOr (
                Expression.ConstantBool (false), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.ExclusiveOr (
                Expression.ConstantBool (false), Expression.ConstantBool (false))));
        }

        [Test]
        public void Not ()
        {
            Assert.IsFalse (Eval<bool> (Expression.Not (Expression.ConstantBool (true))));
            Assert.IsTrue (Eval<bool> (Expression.Not (Expression.ConstantBool (false))));
        }

        [Test]
        public void Add ()
        {
            Assert.AreEqual (3, Eval<int> (Expression.Add (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
        }

        [Test]
        public void Subtract ()
        {
            Assert.AreEqual (-1, Eval<int> (Expression.Subtract (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
        }

        [Test]
        public void Multiply ()
        {
            Assert.AreEqual (6, Eval<int> (Expression.Multiply (
                Expression.ConstantInt (2), Expression.ConstantInt (3))));
            Assert.AreEqual (2.1f * 3.2f, Eval<float> (Expression.Multiply (
                Expression.ConstantFloat (2.1f), Expression.ConstantFloat (3.2f))));
        }

        [Test]
        public void Divide ()
        {
            Assert.AreEqual (0, Eval<int> (Expression.Divide (
                Expression.ConstantInt (2), Expression.ConstantInt (3))));
            Assert.AreEqual (2f / 3f, Eval<float> (Expression.Divide (
                Expression.ConstantFloat (2), Expression.ConstantFloat (3))));
        }

        [Test]
        public void Modulo ()
        {
            Assert.AreEqual (2, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (2), Expression.ConstantInt (3))));
            Assert.AreEqual (0, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
            Assert.AreEqual (0, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (6), Expression.ConstantInt (3))));
            Assert.AreEqual (1, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (6), Expression.ConstantInt (5))));
        }

        [Test]
        public void Power ()
        {
            Assert.AreEqual (8, Eval<int> (Expression.Power (
                Expression.ConstantInt (2), Expression.ConstantInt (3))));
            Assert.AreEqual (System.Math.Pow (2.1, 1.2), Eval<double> (Expression.Power (
                Expression.ConstantDouble (2.1), Expression.ConstantDouble (1.2))));
            Assert.AreEqual (System.Math.Pow (2.1, 1.2f), Eval<double> (Expression.Power (
                Expression.ConstantDouble (2.1), Expression.ConstantFloat (1.2f))));
            Assert.AreEqual ((float)System.Math.Pow (2.1f, 1.2f), Eval<float> (Expression.Power (
                Expression.ConstantFloat (2.1f), Expression.ConstantFloat (1.2f))));
            // Mixed-type operands promote, so a fractional exponent of an int gives
            // a double
            Assert.AreEqual (System.Math.Pow (2, 0.5), Eval<double> (Expression.Power (
                Expression.ConstantInt (2), Expression.ConstantDouble (0.5))));
            Assert.AreEqual (System.Math.Pow (2.5, 2), Eval<double> (Expression.Power (
                Expression.ConstantDouble (2.5), Expression.ConstantInt (2))));
        }

        [Test]
        public void Conditional ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.Conditional (
                Expression.ConstantBool (true),
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.AreEqual (2, Eval<int> (Expression.Conditional (
                Expression.ConstantBool (false),
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            // Branches of differing numeric types promote to a common type
            Assert.AreEqual (2.5, Eval<double> (Expression.Conditional (
                Expression.ConstantBool (false),
                Expression.ConstantInt (1), Expression.ConstantDouble (2.5))));
            Assert.AreEqual ("a", Eval<string> (Expression.Conditional (
                Expression.ConstantBool (true),
                Expression.ConstantString ("a"), Expression.ConstantString ("b"))));
        }

        [Test]
        public void NumericPromotion ()
        {
            // Mixed-type arithmetic promotes to the wider operand type
            Assert.AreEqual (5.5, Eval<double> (Expression.Multiply (
                Expression.ConstantDouble (2.75), Expression.ConstantInt (2))));
            Assert.AreEqual (5.5, Eval<double> (Expression.Multiply (
                Expression.ConstantInt (2), Expression.ConstantDouble (2.75))));
            Assert.AreEqual (3.5f, Eval<float> (Expression.Add (
                Expression.ConstantFloat (1.5f), Expression.ConstantInt (2))));
            Assert.AreEqual (2.5, Eval<double> (Expression.Divide (
                Expression.ConstantDouble (5), Expression.ConstantFloat (2f))));
            Assert.AreEqual (1.5, Eval<double> (Expression.Modulo (
                Expression.ConstantDouble (7.5), Expression.ConstantInt (2))));
            Assert.AreEqual (-1.5, Eval<double> (Expression.Subtract (
                Expression.ConstantInt (1), Expression.ConstantDouble (2.5))));
        }

        [Test]
        public void NumericPromotionComparison ()
        {
            Assert.IsTrue (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantDouble (2.5), Expression.ConstantInt (2))));
            Assert.IsFalse (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantInt (2), Expression.ConstantDouble (2.5))));
            Assert.IsTrue (Eval<bool> (Expression.LessThanOrEqual (
                Expression.ConstantInt (2), Expression.ConstantFloat (2f))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantInt (2), Expression.ConstantDouble (2))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                Expression.ConstantFloat (2.5f), Expression.ConstantInt (2))));
        }

        [Test]
        public void NumericPromotionIntegers ()
        {
            var longValue = Expression.Cast (Expression.ConstantInt (2), Type.Long ());
            var uintValue = Expression.Cast (Expression.ConstantInt (3), Type.UInt ());
            var ulongValue = Expression.Cast (Expression.ConstantInt (4), Type.ULong ());
            Assert.AreEqual (5L, Eval<long> (Expression.Add (
                longValue, Expression.ConstantInt (3))));
            // An unsigned integer and a signed one of the same width promote to the
            // wider signed type
            Assert.AreEqual (5L, Eval<long> (Expression.Add (
                uintValue, Expression.ConstantInt (2))));
            Assert.AreEqual (7UL, Eval<ulong> (Expression.Add (ulongValue, uintValue)));
        }

        [Test]
        public void NumericPromotionWithoutCommonType ()
        {
            var longValue = Expression.Cast (Expression.ConstantInt (2), Type.Long ());
            var ulongValue = Expression.Cast (Expression.ConstantInt (4), Type.ULong ());
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Add (ulongValue, Expression.ConstantInt (1)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Add (ulongValue, longValue));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.LessThan (longValue, ulongValue));
        }

        [Test]
        public void LeftShift ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.LeftShift (
                Expression.ConstantInt (1), Expression.ConstantInt (0))));
            Assert.AreEqual (2, Eval<int> (Expression.LeftShift (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.AreEqual (4, Eval<int> (Expression.LeftShift (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
        }

        [Test]
        public void RightShift ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.RightShift (
                Expression.ConstantInt (1), Expression.ConstantInt (0))));
            Assert.AreEqual (1, Eval<int> (Expression.RightShift (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
            Assert.AreEqual (1, Eval<int> (Expression.RightShift (
                Expression.ConstantInt (4), Expression.ConstantInt (2))));
        }

        [Test]
        public void Cast ()
        {
            Assert.AreEqual ((double)1, Eval<double> (Expression.Cast (Expression.ConstantInt (1), Type.Double ())));
            Assert.AreEqual ((float)1, Eval<float> (Expression.Cast (Expression.ConstantInt (1), Type.Float ())));
            Assert.AreEqual (1, Eval<int> (Expression.Cast (Expression.ConstantDouble (1.1), Type.Int ())));
            Assert.AreEqual (1, Eval<int> (Expression.Cast (Expression.ConstantFloat (1.1f), Type.Int ())));
        }

        [Test]
        public void Invoke ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { x, y },
                Expression.Divide (x, y));
            var call = Expression.Invoke (func, new Dictionary<string, Expression> {
                { "x", Expression.ConstantInt (6) }, { "y", Expression.ConstantInt (3) }});
            Assert.AreEqual (2, Eval<int> (call));
        }

        [Test]
        public void InvokeArgumentsAreWidenedNotNarrowed ()
        {
            var x = Expression.Parameter ("x", Type.Double ());
            var widening = Expression.Lambda (
                new List<Expression> { x },
                Expression.Multiply (x, Expression.ConstantDouble (2)));
            Assert.AreEqual (6.0, Eval<double> (Expression.Invoke (
                widening, new Dictionary<string, Expression> {
                    { "x", Expression.ConstantInt (3) }})));

            var count = Expression.Parameter ("count", Type.Int ());
            var narrowing = Expression.Lambda (new List<Expression> { count }, count);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Invoke (narrowing, new Dictionary<string, Expression> {
                    { "count", Expression.ConstantDouble (1.5) }}));
            StringAssert.Contains ("No implicit conversion", exn.Message);
        }

        [Test]
        public void CreateTuple ()
        {
            Assert.AreEqual (
                System.Tuple.Create (1, false),
                Eval<System.Tuple<int, bool>> (tuple));
        }

        [Test]
        public void CreateTupleElementCount ()
        {
            var elements = new List<Expression> ();
            for (var i = 0; i < 7; i++)
                elements.Add (Expression.ConstantInt (i));
            Assert.DoesNotThrow (() => Expression.CreateTuple (elements));
            elements.Add (Expression.ConstantInt (7));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateTuple (elements));
            StringAssert.Contains ("cannot have more than 7 elements", exn.Message);
            var empty = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateTuple (new List<Expression> ()));
            StringAssert.Contains ("at least one element", empty.Message);
        }

        [Test]
        public void CreateStruct ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var value = Eval<global::KRPC.Test.Service.TestService.TestStruct> (
                BuildTestStruct (AddInstance (obj)));
            Assert.AreEqual (42, value.IntField);
            Assert.AreEqual ("bar", value.StringField);
            Assert.AreEqual (global::KRPC.Test.Service.TestService.TestEnum.Y, value.EnumField);
            Assert.AreSame (obj, value.ObjectField);
            Assert.AreEqual (new List<string> { "a", "b" }, value.ListField);
        }

        [Test]
        public void CreateStructWithTheWrongNumberOfFieldValues ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateStruct (
                    Type.StructType ("TestService", "TestStruct"),
                    new List<Expression> { Expression.ConstantInt (42) }));
        }

        [Test]
        public void CreateStructWithAFieldValueOfTheWrongType ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateStruct (
                    Type.StructType ("TestService", "TestNestedStruct"),
                    new List<Expression> {
                        Expression.ConstantString ("not a structure"),
                        Expression.ConstantInt (1)
                    }));
        }

        [Test]
        public void CreateStructOfATypeThatIsNotAStructure ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateStruct (
                    Type.Int (), new List<Expression> { Expression.ConstantInt (42) }));
        }

        [Test]
        public void GetField ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var value = BuildTestStruct (AddInstance (obj));
            Assert.AreEqual (42, Eval<int> (Expression.GetField (value, "IntField")));
            Assert.AreEqual ("bar", Eval<string> (Expression.GetField (value, "StringField")));
            Assert.AreSame (
                obj,
                Eval<global::KRPC.Test.Service.TestService.TestClass> (
                    Expression.GetField (value, "ObjectField")));
        }

        [Test]
        public void GetFieldOfANestedStruct ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var nested = Expression.CreateStruct (
                Type.StructType ("TestService", "TestNestedStruct"),
                new List<Expression> {
                    BuildTestStruct (AddInstance (obj)),
                    Expression.ConstantInt (7)
                });
            Assert.AreEqual (
                "bar",
                Eval<string> (Expression.GetField (
                    Expression.GetField (nested, "StructField"), "StringField")));
            Assert.AreEqual (7, Eval<int> (Expression.GetField (nested, "IntField")));
        }

        [Test]
        public void GetFieldTheStructureDoesNotHave ()
        {
            var value = BuildTestStruct (AddInstance (
                new global::KRPC.Test.Service.TestService.TestClass ("foo")));
            // NotAField is a property of the C# struct, but is not marked as a field of it
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.GetField (value, "NotAField"));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.GetField (value, "NoSuchField"));
        }

        [Test]
        public void GetFieldOfAValueThatIsNotAStructure ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.GetField (tuple, "IntField"));
        }

        [Test]
        public void CreateList ()
        {
            Assert.AreEqual (new List<int> { 1, 2, 3, 4, 5 }, Eval<IList<int>> (list));
        }

        [Test]
        public void CreateSet ()
        {
            Assert.AreEqual (new HashSet<int> { 1, 2, 3, 4 }, Eval<HashSet<int>> (set));
        }

        [Test]
        public void CreateDictionary ()
        {
            Assert.AreEqual (
                new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } },
                Eval<IDictionary<string, int>> (dictionary));
        }

        [Test]
        public void ToList ()
        {
            Assert.AreEqual (
                new List<int> { 1, 2, 3, 4, 5 },
                Eval<List<int>> (Expression.ToList (list)));
        }

        [Test]
        public void ToSet ()
        {
            Assert.AreEqual (
                new HashSet<int> { 1, 2, 3, 4, 5 },
                Eval<HashSet<int>> (Expression.ToSet (list)));
        }

        [Test]
        public void GetTuple ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.Get (tuple, Expression.ConstantInt (0))));
            Assert.AreEqual (false, Eval<bool> (Expression.Get (tuple, Expression.ConstantInt (1))));
        }

        [Test]
        public void GetTupleWithAComputedIndex ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Get (tuple, Expression.Add (
                    Expression.ConstantInt (0), Expression.ConstantInt (1))));
            StringAssert.Contains ("constant integer", exn.Message);
        }

        [Test]
        public void GetList ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.Get (list, Expression.ConstantInt (0))));
            Assert.AreEqual (2, Eval<int> (Expression.Get (list, Expression.ConstantInt (1))));
            Assert.AreEqual (3, Eval<int> (Expression.Get (list, Expression.ConstantInt (2))));
            Assert.AreEqual (4, Eval<int> (Expression.Get (list, Expression.ConstantInt (3))));
            Assert.AreEqual (5, Eval<int> (Expression.Get (list, Expression.ConstantInt (4))));
        }

        [Test]
        public void GetDictionary ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.Get (dictionary, Expression.ConstantString ("a"))));
            Assert.AreEqual (2, Eval<int> (Expression.Get (dictionary, Expression.ConstantString ("b"))));
            Assert.AreEqual (3, Eval<int> (Expression.Get (dictionary, Expression.ConstantString ("c"))));
        }

        [Test]
        public void Count ()
        {
            Assert.AreEqual (5, Eval<int> (Expression.Count (list)));
            Assert.AreEqual (4, Eval<int> (Expression.Count (set)));
            Assert.AreEqual (3, Eval<int> (Expression.Count (dictionary)));
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
        public void CountOfALazySequence ()
        {
            var lazy = Expression.Skip (list, Expression.ConstantInt (1));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Count (lazy));
            StringAssert.Contains ("lazily evaluated sequence", exn.Message);
        }

        [Test]
        public void Sum ()
        {
            Assert.AreEqual (1 + 2 + 3 + 4 + 5, Eval<int> (Expression.Sum (list)));
            Assert.AreEqual (1 + 2 + 3 + 4, Eval<int> (Expression.Sum (set)));
        }

        [Test]
        public void Max ()
        {
            Assert.AreEqual (5, Eval<int> (Expression.Max (list)));
            Assert.AreEqual (4, Eval<int> (Expression.Max (set)));
        }

        [Test]
        public void Min ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.Min (list)));
            Assert.AreEqual (1, Eval<int> (Expression.Min (set)));
        }

        [Test]
        public void Average ()
        {
            Assert.AreEqual (3, Eval<double> (Expression.Average (list)));
            Assert.AreEqual (2.5, Eval<double> (Expression.Average (set)));
        }

        [Test]
        public void Select ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { x },
                Expression.Multiply (Expression.ConstantInt (2), x));
            Assert.AreEqual (new List<int> { 2, 4, 6, 8, 10 },
                             Eval<IList<int>> (Expression.ToList (Expression.Select (list, func))));
        }

        [Test]
        public void Where ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { x },
                Expression.LessThan (x, Expression.ConstantInt (3)));
            Assert.AreEqual (new List<int> { 1, 2 },
                             Eval<IList<int>> (Expression.ToList (Expression.Where (list, func))));
        }

        [Test]
        public void Contains ()
        {
            Assert.AreEqual (
                true,
                Eval<bool> (Expression.Contains (list, Expression.ConstantInt (1))));
            Assert.AreEqual (
                false,
                Eval<bool> (Expression.Contains (list, Expression.ConstantInt (10))));
            var doubles = Expression.CreateList (new List<Expression> {
                Expression.ConstantDouble (1.5),
                Expression.ConstantDouble (2)
            });
            Assert.AreEqual (
                true,
                Eval<bool> (Expression.Contains (doubles, Expression.ConstantInt (2))));
        }

        [Test]
        public void Aggregate ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { x, y },
                Expression.Multiply (x, y));
            Assert.AreEqual (1 * 2 * 3 * 4 * 5,
                             Eval<int> (Expression.Aggregate (list, func)));
        }

        [Test]
        public void AggregateWithSeed ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { x, y },
                Expression.Multiply (x, y));
            var seed = Expression.ConstantInt (42);
            Assert.AreEqual (42 * 1 * 2 * 3 * 4 * 5,
                             Eval<int> (Expression.AggregateWithSeed (list, seed, func)));
        }

        [Test]
        public void Concat ()
        {
            Assert.AreEqual (
                new List<int> { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 },
                Eval<List<int>> (Expression.ToList (Expression.Concat (list, list))));
        }

        [Test]
        public void OrderBy ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { x },
                Expression.Subtract (Expression.ConstantInt (0), x)
            );
            Assert.AreEqual (
                new List<int> { 5, 4, 3, 2, 1 },
                Eval<List<int>> (Expression.ToList (Expression.OrderBy (list, func))));
        }

        [Test]
        public void All ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            {
                var func = Expression.Lambda (
                    new List<Expression> { x },
                    Expression.LessThan (x, Expression.ConstantInt (2)));
                Assert.AreEqual (false, Eval<bool> (Expression.All (list, func)));
            }
            {
                var func = Expression.Lambda (
                    new List<Expression> { x },
                    Expression.LessThan (x, Expression.ConstantInt (100)));
                Assert.AreEqual (true, Eval<bool> (Expression.All (list, func)));
            }
        }

        [Test]
        public void Any ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            {
                var func = Expression.Lambda (
                    new List<Expression> { x },
                    Expression.LessThan (x, Expression.ConstantInt (2)));
                Assert.AreEqual (true, Eval<bool> (Expression.Any (list, func)));
            }
            {
                var func = Expression.Lambda (
                    new List<Expression> { x },
                    Expression.LessThan (x, Expression.ConstantInt (100)));
                Assert.AreEqual (true, Eval<bool> (Expression.Any (list, func)));
            }
            {
                var func = Expression.Lambda (
                    new List<Expression> { x },
                    Expression.GreaterThan (x, Expression.ConstantInt (100)));
                Assert.AreEqual (false, Eval<bool> (Expression.Any (list, func)));
            }
        }
    }
}
