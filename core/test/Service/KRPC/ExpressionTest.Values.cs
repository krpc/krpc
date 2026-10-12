using System.Collections.Generic;
using System.Linq;
using KRPC.Service.KRPC;
using KRPC.Service.Messages;
using NUnit.Framework;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Test.Service.KRPC
{
    public partial class ExpressionTest
    {
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

        [Test]
        public void ReturnTypeOfAList ()
        {
            var listType = list.ReturnType;
            Assert.AreEqual (TypeCode.List, listType.Code);
            Assert.AreEqual (TypeCode.SInt32, listType.Types [0].Code);
        }

        [Test]
        public void EqualityOfTuplesAndBytes ()
        {
            var otherTuple = Expression.CreateTuple (new List<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantBool (false)
            });
            Assert.IsTrue (Eval<bool> (Expression.Equal (tuple, otherTuple)));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantBytes (new byte[] { 1, 2 }),
                Expression.ConstantBytes (new byte[] { 1, 2 }))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                Expression.ConstantBytes (new byte[] { 1, 2 }),
                Expression.ConstantBytes (new byte[] { 1, 3 }))));
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
        public void CreateWithAnElementOfNoValue ()
        {
            var none = Expression.Call (BuildProcedureCall ("ProcedureNoArgsNoReturn"));
            var one = Expression.ConstantInt (1);
            var cases = new Dictionary<string, System.Func<Expression>> {
                { "An element of a tuple", () => Expression.CreateTuple (new List<Expression> { one, none }) },
                { "A value of a list", () => Expression.CreateList (new List<Expression> { none }) },
                { "A value of a set", () => Expression.CreateSet (new HashSet<Expression> { none }) },
                { "A key of a dictionary", () => Expression.CreateDictionary (
                    new List<Expression> { none }, new List<Expression> { one }) },
                { "A value of a dictionary", () => Expression.CreateDictionary (
                    new List<Expression> { one }, new List<Expression> { none }) }
            };
            foreach (var entry in cases) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                    () => entry.Value ());
                Assert.AreEqual (entry.Key + " is an expression with no value", exn.Message);
            }
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
        public void CreateStructOfANullableStructureType ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var value = Eval<global::KRPC.Test.Service.TestService.TestStruct> (Expression.CreateStruct (
                Type.NullableType (Type.StructType ("TestService", "TestStruct")),
                new List<Expression> {
                    Expression.ConstantInt (42),
                    Expression.ConstantString ("bar"),
                    Expression.Cast (
                        Expression.ConstantInt (1),
                        Type.EnumerationType ("TestService", "TestEnum")),
                    Expression.ConstantObject (AddInstance (obj)),
                    Expression.CreateList (new List<Expression> { Expression.ConstantString ("a") })
                }));
            Assert.AreEqual (42, value.IntField);
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
        public void CreatedCollectionsHaveTheTypesAClientBuilds ()
        {
            var listType = Type.ListType (Type.Int ());
            var dictionaryType = Type.DictionaryType (Type.String (), Type.Int ());
            Assert.AreEqual (listType, list.ReturnType);
            Assert.AreEqual (listType, Expression.CreateEmptyList (Type.Int ()).ReturnType);
            Assert.AreEqual (dictionaryType, dictionary.ReturnType);
            Assert.AreEqual (
                dictionaryType, Expression.CreateEmptyDictionary (Type.String (), Type.Int ()).ReturnType);
        }

        [Test]
        public void CollectionElementsWiden ()
        {
            var widenedList = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (1), Expression.ConstantDouble (2.5)
            });
            Assert.AreEqual (new List<double> { 1, 2.5 }, Eval<IList<double>> (widenedList));
            var widenedSet = Expression.CreateSet (new HashSet<Expression> {
                Expression.ConstantUInt (1), Expression.ConstantInt (2)
            });
            Assert.AreEqual (new HashSet<long> { 1, 2 }, Eval<HashSet<long>> (widenedSet));
            var widenedDictionary = Expression.CreateDictionary (
                new List<Expression> { Expression.ConstantInt (1), Expression.ConstantLong (2) },
                new List<Expression> { Expression.ConstantFloat (1), Expression.ConstantInt (2) });
            Assert.AreEqual (
                new Dictionary<long, float> { { 1, 1 }, { 2, 2 } },
                Eval<IDictionary<long, float>> (widenedDictionary));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateList (new List<Expression> {
                    Expression.ConstantULong (1), Expression.ConstantInt (2)
                }));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateList (new List<Expression> {
                    Expression.ConstantInt (1), Expression.ConstantString ("a")
                }));
        }

        [Test]
        public void CollectionElementsWidenInAnyOrder ()
        {
            // An int and a ulong have no common type, but widen to a float with them
            var values = new List<Expression> {
                Expression.ConstantInt (1), Expression.ConstantULong (2), Expression.ConstantFloat (3)
            };
            var numbers = new List<float> { 1, 2, 3 };
            for (var i = 0; i < values.Count; i++) {
                var rotated = values.Skip (i).Concat (values.Take (i)).ToList ();
                Assert.AreEqual (
                    numbers.Skip (i).Concat (numbers.Take (i)).ToList (),
                    Eval<IList<float>> (Expression.CreateList (rotated)));
                CollectionAssert.AreEquivalent (
                    new HashSet<float> { 1, 2, 3 },
                    Eval<HashSet<float>> (Expression.CreateSet (new HashSet<Expression> (rotated))));
            }
        }

        [Test]
        public void TupleElementsWidenInAnyOrder ()
        {
            // The common type at a position of a tuple is taken over every value at once
            var values = new List<Expression> {
                Tuple (Expression.ConstantInt (1)),
                Tuple (Expression.ConstantULong (2)),
                Tuple (Expression.ConstantFloat (3))
            };
            var numbers = new List<System.Tuple<float>> {
                System.Tuple.Create (1f), System.Tuple.Create (2f), System.Tuple.Create (3f)
            };
            for (var i = 0; i < values.Count; i++) {
                var rotated = values.Skip (i).Concat (values.Take (i)).ToList ();
                Assert.AreEqual (
                    numbers.Skip (i).Concat (numbers.Take (i)).ToList (),
                    Eval<IList<System.Tuple<float>>> (Expression.CreateList (rotated)));
                CollectionAssert.AreEquivalent (
                    numbers,
                    Eval<HashSet<System.Tuple<float>>> (Expression.CreateSet (new HashSet<Expression> (rotated))));
            }
            // Without the float, an int and a ulong have no common type in either order
            var mixed = values.Take (2).ToList ();
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateList (mixed));
            var reversed = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateList (Enumerable.Reverse (mixed).ToList ()));
            Assert.AreEqual (exn.Message, reversed.Message);
        }

        static Expression Tuple (Expression value)
        {
            return Expression.CreateTuple (new List<Expression> { value });
        }

        [Test]
        public void CollectionElementsKeepANullableNumber ()
        {
            Expression none = LinqExpression.Constant (null, typeof (int?));
            Expression three = LinqExpression.Constant (3, typeof (int?));
            Assert.AreEqual (
                new List<int?> { null, 1 },
                Eval<IList<int?>> (Expression.CreateList (new List<Expression> {
                    none, Expression.ConstantInt (1)
                })));
            Assert.AreEqual (
                new List<double?> { 2.5, 3 },
                Eval<IList<double?>> (Expression.CreateList (new List<Expression> {
                    Expression.ConstantDouble (2.5), three
                })));
            Assert.AreEqual (
                new HashSet<long> { 3, 2 },
                Eval<HashSet<long>> (Expression.CreateSet (new HashSet<Expression> {
                    three, Expression.ConstantLong (2)
                })));
            Assert.AreEqual (
                new Dictionary<string, int?> { { "a", 3 }, { "b", 1 } },
                Eval<IDictionary<string, int?>> (Expression.CreateDictionary (
                    new List<Expression> { Expression.ConstantString ("a"), Expression.ConstantString ("b") },
                    new List<Expression> { three, Expression.ConstantInt (1) })));
            var nullableEnum = typeof (global::KRPC.Test.Service.TestService.TestEnum?);
            Assert.AreEqual (
                new List<global::KRPC.Test.Service.TestService.TestEnum?> {
                    null, global::KRPC.Test.Service.TestService.TestEnum.Y
                },
                Eval<IList<global::KRPC.Test.Service.TestService.TestEnum?>> (
                    Expression.CreateList (new List<Expression> {
                        LinqExpression.Constant (null, nullableEnum),
                        Expression.ConstantEnum ("TestService", "TestEnum", 1)
                    })));
        }

        [Test]
        public void CollectionElementsJoinPositionByPosition ()
        {
            Expression none = LinqExpression.Constant (null, typeof (int?));
            var withNull = Expression.CreateTuple (new List<Expression> { Expression.ConstantString ("a"), none });
            var withValue = Expression.CreateTuple (new List<Expression> {
                Expression.ConstantString ("b"), Expression.ConstantInt (1)
            });
            Assert.AreEqual (
                new List<System.Tuple<string, int?>> { System.Tuple.Create ("a", (int?)null), System.Tuple.Create ("b", (int?)1) },
                Eval<IList<System.Tuple<string, int?>>> (Expression.CreateList (new List<Expression> { withNull, withValue })));
            Assert.AreEqual (
                new Dictionary<int, System.Tuple<string, int?>> {
                    { 1, System.Tuple.Create ("b", (int?)1) }, { 2, System.Tuple.Create ("a", (int?)null) }
                },
                Eval<IDictionary<int, System.Tuple<string, int?>>> (Expression.CreateDictionary (
                    new List<Expression> { Expression.ConstantInt (1), Expression.ConstantInt (2) },
                    new List<Expression> { withValue, withNull })));
            var nested = Eval<IList<IList<int?>>> (Expression.CreateList (new List<Expression> {
                Expression.CreateList (new List<Expression> { none }),
                Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) })
            }));
            Assert.AreEqual (new int? [] { null }, nested [0]);
            Assert.AreEqual (new int? [] { 1 }, nested [1]);
            var widened = Eval<IList<IList<double>>> (Expression.CreateList (new List<Expression> {
                Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) }),
                Expression.CreateList (new List<Expression> { Expression.ConstantDouble (2.5) })
            }));
            Assert.AreEqual (new double [] { 1 }, widened [0]);
            Assert.AreEqual (new double [] { 2.5 }, widened [1]);
            Assert.AreEqual (
                System.Tuple.Create ("b", (int?)1),
                Eval<System.Tuple<string, int?>> (Expression.Conditional (Expression.ConstantBool (false), withNull, withValue)));
            // An int and a ulong have no common type
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Conditional (
                    Expression.ConstantBool (true),
                    Expression.CreateTuple (new List<Expression> { Expression.ConstantInt (1) }),
                    Expression.CreateTuple (new List<Expression> { Expression.ConstantULong (1) })));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateList (new List<Expression> {
                    withValue,
                    Expression.CreateTuple (new List<Expression> {
                        Expression.ConstantInt (1), Expression.ConstantInt (1)
                    })
                }));
        }

        [Test]
        public void SetValuesAndDictionaryKeysCannotBeNull ()
        {
            Expression none = LinqExpression.Constant (null, typeof (int?));
            Expression three = LinqExpression.Constant (3, typeof (int?));
            var set = Expression.CreateSet (new HashSet<Expression> { none, Expression.ConstantLong (2) });
            Assert.IsFalse (set.ReturnType.Types [0].Nullable);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<HashSet<long>> (set));
            StringAssert.Contains ("A value of a set is null", exn.Message);
            Assert.AreEqual (
                new Dictionary<long, string> { { 3, "a" }, { 2, "b" } },
                Eval<IDictionary<long, string>> (Expression.CreateDictionary (
                    new List<Expression> { three, Expression.ConstantLong (2) },
                    new List<Expression> { Expression.ConstantString ("a"), Expression.ConstantString ("b") })));
            var key = Expression.CreateDictionary (
                new List<Expression> { none }, new List<Expression> { Expression.ConstantInt (1) });
            Assert.IsFalse (key.ReturnType.Types [0].Nullable);
            exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<IDictionary<int, int>> (key));
            StringAssert.Contains ("A key of a dictionary is null", exn.Message);
        }

        [Test]
        public void CreateDictionaryNamesTheNullArgument ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.CreateDictionary (null, new List<Expression> { Expression.ConstantInt (1) }));
            Assert.AreEqual ("keys", exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.CreateDictionary (new List<Expression> { Expression.ConstantInt (1) }, null));
            Assert.AreEqual ("values", exn.Message);
        }

        [Test]
        public void CreateDictionaryChecksTheKeyType ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateDictionary (
                    new List<Expression> { list }, new List<Expression> { Expression.ConstantInt (1) }));
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
                    new List<Expression> { null, one })
            }) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (build);
                StringAssert.Contains ("cannot be null", exn.Message);
            }
        }

        [Test]
        public void CollectionsOfNullableReferences ()
        {
            var list = Expression.CreateList (new List<Expression> {
                Expression.ConstantString ("a"), NullableString (null) });
            Assert.IsTrue (list.ReturnType.Types [0].Nullable);
            CollectionAssert.AreEqual (new List<string> { "a", null }, Eval<IList<string>> (list));
            var plain = Expression.CreateList (new List<Expression> { Expression.ConstantString ("a") });
            Assert.IsFalse (plain.ReturnType.Types [0].Nullable);
            var tuple = Expression.CreateTuple (new List<Expression> {
                Expression.ConstantInt (1), NullableString ("b") });
            Assert.IsFalse (tuple.ReturnType.Types [0].Nullable);
            Assert.IsTrue (tuple.ReturnType.Types [1].Nullable);
            var dictionary = Expression.CreateDictionary (
                new List<Expression> { Expression.ConstantString ("k") },
                new List<Expression> { NullableString (null) });
            Assert.IsTrue (dictionary.ReturnType.Types [1].Nullable);
            var set = Expression.CreateSet (new HashSet<Expression> { NullableString (null) });
            Assert.IsFalse (set.ReturnType.Types [0].Nullable);
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (() => Eval<ISet<string>> (set));
            var key = Expression.CreateDictionary (
                new List<Expression> { NullableString (null) },
                new List<Expression> { Expression.ConstantInt (1) });
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (() => Eval<IDictionary<string, int>> (key));
        }

        [Test]
        public void CreateEmptyCollections ()
        {
            CollectionAssert.IsEmpty (Eval<IList<int>> (Expression.CreateEmptyList (Type.Int ())));
            CollectionAssert.IsEmpty (Eval<HashSet<string>> (Expression.CreateEmptySet (Type.String ())));
            CollectionAssert.IsEmpty (Eval<IDictionary<string, double>> (
                Expression.CreateEmptyDictionary (Type.String (), Type.Double ())));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateEmptyDictionary (Type.Double (), Type.Int ()));
        }

        [Test]
        public void EmptyCollectionsOfNullableReferences ()
        {
            var nullableString = Type.NullableType (Type.String ());
            Assert.IsTrue (Expression.CreateEmptyList (nullableString).ReturnType.Types [0].Nullable);
            Assert.IsTrue (Expression.CreateEmptyDictionary (
                Type.String (), nullableString).ReturnType.Types [1].Nullable);
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateEmptySet (nullableString));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.CreateEmptyDictionary (nullableString, Type.Int ()));
        }

        [Test]
        public void EmptyCollectionsKeepNestedNullableTypes ()
        {
            var tuple = Type.TupleType (new List<Type> { Type.Int (), Type.NullableType (Type.String ()) });
            var elements = new [] {
                Expression.CreateEmptyList (tuple).ReturnType.Types [0],
                Expression.CreateEmptySet (tuple).ReturnType.Types [0],
                Expression.CreateEmptyDictionary (Type.Int (), tuple).ReturnType.Types [1]
            };
            foreach (var element in elements) {
                Assert.IsFalse (element.Types [0].Nullable);
                Assert.IsTrue (element.Types [1].Nullable);
            }
        }

        [Test]
        public void CreateDictionaryWithARepeatedKeyTakesTheLastValue ()
        {
            var dictionary = Eval<IDictionary<int, string>> (Expression.CreateDictionary (
                new List<Expression> { Expression.ConstantInt (1), Expression.ConstantInt (1) },
                new List<Expression> { Expression.ConstantString ("a"), Expression.ConstantString ("b") }));
            Assert.AreEqual (1, dictionary.Count);
            Assert.AreEqual ("b", dictionary [1]);
        }

        [Test]
        public void SetValuesCompareByValue ()
        {
            Expression One ()
            {
                return Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) });
            }
            var lists = Expression.CreateSet (new HashSet<Expression> { One (), One () });
            Assert.AreEqual (1, Eval<ICollection<IList<int>>> (lists).Count);
            var tuples = Expression.CreateSet (new HashSet<Expression> {
                Expression.CreateTuple (new List<Expression> { One () }),
                Expression.CreateTuple (new List<Expression> { One () })
            });
            Assert.AreEqual (1, Eval<ICollection<System.Tuple<IList<int>>>> (tuples).Count);
        }
    }
}
