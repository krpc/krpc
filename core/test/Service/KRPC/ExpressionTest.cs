using System.Collections.Generic;
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
                objects, Expression.Function (new List<Expression> { param }, getProperty))));
            CollectionAssert.AreEqual (new [] { 1, 2, 3 }, selected);

            // Whether any object's property satisfies a predicate
            var equalsTwo = Expression.Function (
                new List<Expression> { param },
                Expression.Equal (getProperty, Expression.ConstantInt (2)));
            Assert.IsTrue (Eval<bool> (Expression.Any (objects, equalsTwo)));
            var equalsFour = Expression.Function (
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
            var function = Expression.Function (
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
            var function = Expression.Function (
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
            var function = Expression.Function (
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
            var func = Expression.Function (
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
            var keyFunc = Expression.Function (
                new List<Expression> { param }, Expression.ConvertToString (param));
            var valueFunc = Expression.Function (
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
            Assert.AreEqual ("a2", Eval<string> (Expression.ConcatStrings (
                new List<Expression> {
                    Expression.ConstantString ("a"),
                    Expression.ConvertToString (Expression.ConstantInt (2))
                })));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.ConcatStrings (new List<Expression> {
                    Expression.ConstantString ("a"), Expression.ConstantInt (2)
                }));
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
                        Expression.ListAdd (result, Expression.Multiply (x, Expression.ConstantInt (2)))),
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
                    Expression.ListAdd (numbers, Expression.ConstantInt (1)),
                    Expression.ListAdd (numbers, Expression.ConstantInt (2)),
                    Expression.ListSet (numbers, Expression.ConstantInt (0), Expression.ConstantInt (10)),
                    numbers
                });
            CollectionAssert.AreEqual (new [] { 10, 2 }, Eval<IList<int>> (expr));

            var values = Expression.Variable ("values", Type.DictionaryType (Type.String (), Type.Int ()));
            var dictionaryExpr = Expression.BlockWithVariables (
                new List<Expression> { values },
                new List<Expression> {
                    Expression.Assign (values, Expression.CreateEmptyDictionary (Type.String (), Type.Int ())),
                    Expression.DictionarySet (values, Expression.ConstantString ("a"), Expression.ConstantInt (1)),
                    Expression.DictionarySet (values, Expression.ConstantString ("a"), Expression.ConstantInt (2)),
                    Expression.Get (values, Expression.ConstantString ("a"))
                });
            Assert.AreEqual (2, Eval<int> (dictionaryExpr));

            var seen = Expression.Variable ("seen", Type.SetType (Type.Int ()));
            var setExpr = Expression.BlockWithVariables (
                new List<Expression> { seen },
                new List<Expression> {
                    Expression.Assign (seen, Expression.CreateEmptySet (Type.Int ())),
                    Expression.SetAdd (seen, Expression.ConstantInt (1)),
                    Expression.SetAdd (seen, Expression.ConstantInt (1)),
                    Expression.Count (seen)
                });
            Assert.AreEqual (1, Eval<int> (setExpr));
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
            // An unbound marker compiles, so it is caught before compiling rather than
            // being left to throw on every evaluation.
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
            // The same markers, correctly enclosed, must still be allowed through.
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
        public void ReturnTypeOfLazyCollection ()
        {
            var param = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Function (
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
                () => Expression.Function (
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
        public void ConstantObjectInvalid ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.ConstantObject (0));
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
            var func = Expression.Function (
                new List<Expression> { x, y },
                Expression.Divide (x, y));
            var call = Expression.Invoke (func, new Dictionary<string, Expression> {
                { "x", Expression.ConstantInt (6) }, { "y", Expression.ConstantInt (3) }});
            Assert.AreEqual (2, Eval<int> (call));
        }

        [Test]
        public void CreateTuple ()
        {
            Assert.AreEqual (
                System.Tuple.Create (1, false),
                Eval<System.Tuple<int, bool>> (tuple));
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
            var func = Expression.Function (
                new List<Expression> { x },
                Expression.Multiply (Expression.ConstantInt (2), x));
            Assert.AreEqual (new List<int> { 2, 4, 6, 8, 10 },
                             Eval<IList<int>> (Expression.ToList (Expression.Select (list, func))));
        }

        [Test]
        public void Where ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Function (
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
        }

        [Test]
        public void Aggregate ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            var func = Expression.Function (
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
            var func = Expression.Function (
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
            var func = Expression.Function (
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
                var func = Expression.Function (
                    new List<Expression> { x },
                    Expression.LessThan (x, Expression.ConstantInt (2)));
                Assert.AreEqual (false, Eval<bool> (Expression.All (list, func)));
            }
            {
                var func = Expression.Function (
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
                var func = Expression.Function (
                    new List<Expression> { x },
                    Expression.LessThan (x, Expression.ConstantInt (2)));
                Assert.AreEqual (true, Eval<bool> (Expression.Any (list, func)));
            }
            {
                var func = Expression.Function (
                    new List<Expression> { x },
                    Expression.LessThan (x, Expression.ConstantInt (100)));
                Assert.AreEqual (true, Eval<bool> (Expression.Any (list, func)));
            }
            {
                var func = Expression.Function (
                    new List<Expression> { x },
                    Expression.GreaterThan (x, Expression.ConstantInt (100)));
                Assert.AreEqual (false, Eval<bool> (Expression.Any (list, func)));
            }
        }
    }
}
