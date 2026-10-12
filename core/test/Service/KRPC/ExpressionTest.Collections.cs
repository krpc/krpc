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
    public partial class ExpressionTest
    {
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
            var selected = Eval<IList<int>> (Expression.ToList (Expression.Select (
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
        public void SkipAndTake ()
        {
            var expr = Expression.ToList (Expression.Take (
                Expression.Skip (list, Expression.ConstantInt (2)),
                Expression.ConstantInt (2)));
            CollectionAssert.AreEqual (new [] { 3, 4 }, Eval<IList<int>> (expr));
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
                new [] { 1, 10, 2, 20, 3, 30, 4, 40, 5, 50 }, Eval<IList<int>> (expr));
        }

        [Test]
        public void SelectManyOverANullCollection ()
        {
            var param = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { param },
                Expression.ConstantNull (Type.ListType (Type.Int ())));
            var expr = Expression.ToList (Expression.SelectMany (list, func));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<IList<int>> (expr));
            Assert.AreEqual (
                "A collection the function given to SelectMany returns is null, " +
                "which its type does not allow", exn.Message);
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
        public void ElementAtIndexOutOfRange ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var lazy = Expression.Select (list, Expression.Lambda (new List<Expression> { x }, x));
            Assert.AreEqual (2, Eval<int> (Expression.ElementAt (lazy, Expression.ConstantInt (1))));
            foreach (var collection in new [] { list, lazy }) {
                foreach (var index in new [] { -1, 5 }) {
                    var exn = Assert.Throws<global::KRPC.Service.KRPC.IndexOutOfRangeException> (
                        () => Eval<int> (Expression.ElementAt (collection, Expression.ConstantInt (index))));
                    Assert.AreEqual ("The index " + index + " is out of range for 5 elements", exn.Message);
                }
            }
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
        public void GetWithoutAnIndex ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.Get (tuple, null));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (
                () => Expression.Get (list, null));
        }

        [Test]
        public void BuildDictionary ()
        {
            var param = Expression.Parameter ("x", Type.Int ());
            var keyFunc = Expression.Lambda (
                new List<Expression> { param },
                Expression.Multiply (param, Expression.ConstantInt (10)));
            var valueFunc = Expression.Lambda (
                new List<Expression> { param },
                Expression.Multiply (param, Expression.ConstantInt (2)));
            var expr = Expression.BuildDictionary (list, keyFunc, valueFunc);
            var result = Eval<IDictionary<int, int>> (expr);
            Assert.AreEqual (Type.DictionaryType (Type.Int (), Type.Int ()), expr.ReturnType);
            Assert.AreEqual (5, result.Count);
            Assert.AreEqual (2, result [10]);
            Assert.AreEqual (10, result [50]);
        }

        [Test]
        public void BytesIsNotACollection ()
        {
            var data = Expression.ConstantBytes (new byte [] { 1, 2 });
            foreach (var build in new List<TestDelegate> {
                () => Expression.Count (data),
                () => Expression.ToList (data),
                () => Expression.First (data),
                () => Expression.Get (data, Expression.ConstantInt (0)),
                () => Expression.Contains (data, Expression.ConstantInt (0))
            }) {
                var exn = Assert.Catch (build);
                StringAssert.Contains ("A bytes value is not a collection", exn.Message);
            }
        }

        [Test]
        public void DictionariesAreNotCollectionsOfValues ()
        {
            var parameter = Expression.Parameter ("x", Type.String ());
            foreach (var build in new List<TestDelegate> {
                () => Expression.ToList (dictionary),
                () => Expression.Select (dictionary, Expression.Lambda (
                    new List<Expression> { parameter }, parameter))
            }) {
                var exn = Assert.Catch (build);
                StringAssert.Contains (
                    "dictionary cannot be used as a collection of values", exn.Message);
            }
            // Reading the keys or the values gives a list, and counting works directly
            Assert.AreEqual (3, Eval<int> (Expression.Count (dictionary)));
            Assert.AreEqual (
                new List<string> { "a", "b", "c" },
                Eval<IList<string>> (Expression.DictionaryKeys (dictionary)));
        }

        [Test]
        public void IndexesMustBeIntegers ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Get (list, Expression.ConstantDouble (0)));
            StringAssert.Contains ("Expected an integer", exn.Message);
        }

        [Test]
        public void CountOfACollectionAProcedureReturns ()
        {
            // A procedure returns a collection interface type
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ()))
                .Returns ((IList<string> x) => x);
            mock.Setup (x => x.EchoDictionary (It.IsAny<IDictionary<int, string>> ()))
                .Returns ((IDictionary<int, string> x) => x);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var values = Expression.Call (BuildProcedureCall (
                "EchoList", new Argument (0, new List<string> { "a", "b" })));
            Assert.AreEqual (2, Eval<int> (Expression.Count (values)));
            var entries = Expression.Call (BuildProcedureCall (
                "EchoDictionary", new Argument (0, new Dictionary<int, string> { { 1, "a" } })));
            Assert.AreEqual (1, Eval<int> (Expression.Count (entries)));
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
        public void EqualityOfCollectionsComparesContents ()
        {
            System.Func<Expression> oneTwo = () => Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (1), Expression.ConstantInt (2)
            });
            Assert.IsTrue (Eval<bool> (Expression.Equal (oneTwo (), oneTwo ())));
            Assert.IsFalse (Eval<bool> (Expression.NotEqual (oneTwo (), oneTwo ())));
            var twoOne = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (2), Expression.ConstantInt (1)
            });
            Assert.IsFalse (Eval<bool> (Expression.Equal (oneTwo (), twoOne)));
            System.Func<Expression> set = () => Expression.CreateSet (new HashSet<Expression> {
                Expression.ConstantInt (1), Expression.ConstantInt (2)
            });
            Assert.IsTrue (Eval<bool> (Expression.Equal (set (), set ())));
            System.Func<Expression> dict = () => Expression.CreateDictionary (
                new List<Expression> { Expression.ConstantString ("a") },
                new List<Expression> { oneTwo () });
            Assert.IsTrue (Eval<bool> (Expression.Equal (dict (), dict ())));
            System.Func<Expression> pair = () => Expression.CreateTuple (new List<Expression> {
                Expression.ConstantInt (1), oneTwo ()
            });
            Assert.IsTrue (Eval<bool> (Expression.Equal (pair (), pair ())));
            var lists = Expression.CreateList (new List<Expression> { twoOne, oneTwo () });
            Assert.IsTrue (Eval<bool> (Expression.Contains (lists, oneTwo ())));
            var pairs = Expression.CreateList (new List<Expression> { pair () });
            Assert.IsTrue (Eval<bool> (Expression.Contains (pairs, pair ())));
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
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateList (mixed));
            StringAssert.Contains ("values of a list", exn.Message);
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateSet (new HashSet<Expression> (mixed)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CreateDictionary (
                    mixed, new List<Expression> {
                        Expression.ConstantInt (1),
                        Expression.ConstantInt (2)
                    }));
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
        public void AverageOfLongsDoesNotOverflow ()
        {
            var longs = Expression.CreateList (new List<Expression> {
                Expression.ConstantLong (long.MaxValue), Expression.ConstantLong (long.MaxValue)
            });
            Assert.AreEqual ((double)long.MaxValue, Eval<double> (Expression.Average (longs)));
        }

        [Test]
        public void ToList ()
        {
            Assert.AreEqual (
                new List<int> { 1, 2, 3, 4, 5 },
                Eval<IList<int>> (Expression.ToList (list)));
            Assert.AreEqual (Type.ListType (Type.Int ()), Expression.ToList (list).ReturnType);
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
            foreach (var index in new [] { 2, -1 }) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.IndexOutOfRangeException> (
                    () => Expression.Get (tuple, Expression.ConstantInt (index)));
                Assert.AreEqual ("The index " + index + " is out of range for 2 elements", exn.Message);
            }
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
        public void GetDictionaryWithANullableKey ()
        {
            var numbers = Expression.CreateDictionary (
                new List<Expression> { Expression.ConstantInt (1) },
                new List<Expression> { Expression.ConstantString ("one") });
            var key = Expression.Cast (Expression.ConstantInt (1), Type.NullableType (Type.Int ()));
            Assert.AreEqual ("one", Eval<string> (Expression.Get (numbers, key)));
        }

        [Test]
        public void GetDictionaryMissingKey ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.KeyNotFoundException> (
                () => Eval<int> (Expression.Get (dictionary, Expression.ConstantString ("z"))));
        }

        [Test]
        public void GetDictionaryMissingKeyIgnoresTheCulture ()
        {
            var numbers = Expression.CreateDictionary (
                new List<Expression> { Expression.ConstantInt (1) },
                new List<Expression> { Expression.ConstantString ("one") });
            var culture = CultureInfo.CurrentCulture;
            try {
                // Swedish writes a negative number with a minus sign, not a hyphen
                CultureInfo.CurrentCulture = new CultureInfo ("sv-SE");
                var exn = Assert.Throws<global::KRPC.Service.KRPC.KeyNotFoundException> (
                    () => Eval<string> (Expression.Get (numbers, Expression.ConstantInt (-1))));
                StringAssert.Contains ("The key -1 is not", exn.Message);
            } finally {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Test]
        public void GetListIndexOutOfRange ()
        {
            var values = Expression.CreateList (new List<Expression> { Expression.ConstantInt (1) });
            foreach (var index in new [] { -1, 1 })
                Assert.Throws<global::KRPC.Service.KRPC.IndexOutOfRangeException> (
                    () => Eval<int> (Expression.Get (values, Expression.ConstantInt (index))));
        }

        [Test]
        public void ContainsKey ()
        {
            Assert.IsTrue (Eval<bool> (Expression.ContainsKey (dictionary, Expression.ConstantString ("a"))));
            Assert.IsFalse (Eval<bool> (Expression.ContainsKey (dictionary, Expression.ConstantString ("z"))));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.ContainsKey (list, Expression.ConstantInt (1)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.ContainsKey (tuple, Expression.ConstantInt (1)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Contains (dictionary, Expression.ConstantString ("a")));
        }

        [Test]
        public void SkipAndTakeCountIsAnInt ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Skip (list, Expression.ConstantDouble (1)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Take (list, Expression.ConstantLong (1)));
        }

        [Test]
        public void Count ()
        {
            Assert.AreEqual (5, Eval<int> (Expression.Count (list)));
            Assert.AreEqual (4, Eval<int> (Expression.Count (set)));
            Assert.AreEqual (3, Eval<int> (Expression.Count (dictionary)));
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
        public void SumWrapsOnOverflow ()
        {
            var ints = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (int.MaxValue),
                Expression.ConstantInt (1)
            });
            Assert.AreEqual (int.MinValue, Eval<int> (Expression.Sum (ints)));
            var longs = Expression.CreateList (new List<Expression> {
                Expression.ConstantLong (long.MaxValue),
                Expression.ConstantLong (2)
            });
            Assert.AreEqual (long.MinValue + 1, Eval<long> (Expression.Sum (longs)));
        }

        [Test]
        public void AggregatesOfUnsignedIntegers ()
        {
            var uints = Expression.CreateList (new List<Expression> {
                Expression.ConstantUInt (uint.MaxValue),
                Expression.ConstantUInt (3)
            });
            Assert.AreEqual (2u, Eval<uint> (Expression.Sum (uints)));
            Assert.AreEqual (uint.MaxValue, Eval<uint> (Expression.Max (uints)));
            Assert.AreEqual (3u, Eval<uint> (Expression.Min (uints)));
            Assert.AreEqual ((uint.MaxValue + 3.0) / 2, Eval<double> (Expression.Average (uints)));
            var ulongs = Expression.CreateList (new List<Expression> {
                Expression.ConstantULong (ulong.MaxValue),
                Expression.ConstantULong (3)
            });
            Assert.AreEqual (2ul, Eval<ulong> (Expression.Sum (ulongs)));
            Assert.AreEqual (ulong.MaxValue, Eval<ulong> (Expression.Max (ulongs)));
            Assert.AreEqual (3ul, Eval<ulong> (Expression.Min (ulongs)));
            Assert.AreEqual (
                ((double)ulong.MaxValue + 3) / 2, Eval<double> (Expression.Average (ulongs)));
        }

        [Test]
        public void AggregatesOfNullableIntegers ()
        {
            Expression ints = LinqExpression.Constant (
                new List<int?> { int.MaxValue, null, 1 }, typeof (IList<int?>));
            Assert.AreEqual (int.MinValue, Eval<int?> (Expression.Sum (ints)));
            Expression longs = LinqExpression.Constant (
                new List<long?> { long.MaxValue, null, long.MaxValue }, typeof (IList<long?>));
            Assert.AreEqual ((double)long.MaxValue, Eval<double?> (Expression.Average (longs)));
            Expression uints = LinqExpression.Constant (
                new List<uint?> { 3, null, 1 }, typeof (IList<uint?>));
            Assert.AreEqual (4u, Eval<uint?> (Expression.Sum (uints)));
            Assert.AreEqual (3u, Eval<uint?> (Expression.Max (uints)));
            Assert.AreEqual (1u, Eval<uint?> (Expression.Min (uints)));
            Expression ulongs = LinqExpression.Constant (
                new List<ulong?> { ulong.MaxValue, null, 2 }, typeof (IList<ulong?>));
            Assert.AreEqual (1ul, Eval<ulong?> (Expression.Sum (ulongs)));
            Assert.AreEqual (ulong.MaxValue, Eval<ulong?> (Expression.Max (ulongs)));
            Assert.AreEqual (2ul, Eval<ulong?> (Expression.Min (ulongs)));
        }

        [Test]
        public void AggregatesOfAnEmptyCollection ()
        {
            Expression ints = LinqExpression.Constant (new List<int> (), typeof (IList<int>));
            Expression nullableInts = LinqExpression.Constant (new List<int?> (), typeof (IList<int?>));
            Expression nullableLongs = LinqExpression.Constant (new List<long?> (), typeof (IList<long?>));
            foreach (var aggregate in new System.Func<Expression, Expression> [] {
                Expression.Max, Expression.Min, Expression.Average
            }) {
                Assert.Throws<System.InvalidOperationException> (() => aggregate (ints).Evaluate ());
                Assert.IsNull (aggregate (nullableInts).Evaluate ());
                Assert.IsNull (aggregate (nullableLongs).Evaluate ());
            }
            var x = Expression.Parameter ("x", Type.NullableType (Type.Int ()));
            var y = Expression.Parameter ("y", Type.NullableType (Type.Int ()));
            var func = Expression.Lambda (new List<Expression> { x, y }, x);
            Assert.Throws<System.InvalidOperationException> (
                () => Expression.Aggregate (nullableInts, func).Evaluate ());
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
        public void RunFunctionOfALazyCollectionHasNoEffects ()
        {
            var calls = 0;
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ListDefault (It.IsAny<IList<int>> ()))
                .Returns ((IList<int> x) => {
                    calls++;
                    return x;
                });
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var source = Expression.Call (BuildProcedureCall (
                "ListDefault", new Argument (0, new List<int> { 1, 2 })));
            var value = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (new List<Expression> { value }, Expression.ConstantBool (true));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (Expression.Where (source, func)));
            StringAssert.Contains ("cannot be sent to a client", exn.Message);
            Assert.AreEqual (0, calls);
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
        public void ContainsPlainValueInANullableList ()
        {
            Expression nullables = LinqExpression.Constant (
                new List<int?> { 1, null }, typeof (IList<int?>));
            Assert.IsTrue (Eval<bool> (Expression.Contains (nullables, Expression.ConstantInt (1))));
            Assert.IsFalse (Eval<bool> (Expression.Contains (nullables, Expression.ConstantInt (2))));
        }

        [Test]
        public void LookUpOfANullFindsNoMatch ()
        {
            Expression none = LinqExpression.Constant (null, typeof (int?));
            Expression numbers = LinqExpression.Constant (new List<int> { 1 }, typeof (IList<int>));
            Expression nullables = LinqExpression.Constant (new List<int?> { 1, null }, typeof (IList<int?>));
            var set = Expression.CreateSet (new HashSet<Expression> { Expression.ConstantInt (1) });
            var strings = Expression.CreateList (new List<Expression> { Expression.ConstantString ("a") });
            Assert.IsFalse (Eval<bool> (Expression.Contains (numbers, none)));
            Assert.IsFalse (Eval<bool> (Expression.Contains (set, none)));
            Assert.IsFalse (Eval<bool> (Expression.Contains (strings, NullableString (null))));
            Assert.IsFalse (Eval<bool> (Expression.ContainsKey (dictionary, NullableString (null))));
            Assert.IsTrue (Eval<bool> (Expression.Contains (nullables, none)));
            Assert.IsTrue (Eval<bool> (Expression.Contains (numbers, LinqExpression.Constant (1, typeof (int?)))));
            Assert.IsTrue (Eval<bool> (Expression.ContainsKey (dictionary, NullableString ("a"))));
        }

        [Test]
        public void ContainsStructure ()
        {
            Expression structures = LinqExpression.Constant (
                new List<global::KRPC.Test.Service.TestService.TestStruct> { TestStruct (1) },
                typeof (IList<global::KRPC.Test.Service.TestService.TestStruct>));
            Assert.IsTrue (Eval<bool> (Expression.Contains (
                structures, LinqExpression.Constant (TestStruct (1)))));
            Assert.IsFalse (Eval<bool> (Expression.Contains (
                structures, LinqExpression.Constant (TestStruct (2)))));
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
        public void AggregateWithSeedWidensTheSeed ()
        {
            var total = Expression.Parameter ("total", Type.Double ());
            var x = Expression.Parameter ("x", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { total, x },
                Expression.Add (total, Expression.Divide (x, Expression.ConstantDouble (2))));
            Assert.AreEqual (1 + 7.5, Eval<double> (
                Expression.AggregateWithSeed (list, Expression.ConstantInt (1), func)));
            // A seed narrower than the accumulated value needs a cast
            var count = Expression.Parameter ("count", Type.Int ());
            var counter = Expression.Lambda (new List<Expression> { count, x }, count);
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.AggregateWithSeed (list, Expression.ConstantDouble (1), counter));
        }

        [Test]
        public void ValuelessArgumentsAreRejected ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            var func = Expression.Lambda (new List<Expression> { x, y }, x);
            var statement = Expression.Call (BuildProcedureCall ("ProcedureNoArgsNoReturn"));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.AggregateWithSeed (list, statement, func));
            Assert.AreEqual ("The seed given to AggregateWithSeed is an expression with no value", exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Contains (list, statement));
            Assert.AreEqual ("The value given to Contains is an expression with no value", exn.Message);
        }

        [Test]
        public void Concat ()
        {
            Assert.AreEqual (
                new List<int> { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 },
                Eval<IList<int>> (Expression.ToList (Expression.Concat (list, list))));
        }

        [Test]
        public void CombinedCollectionsWidenTheirValues ()
        {
            Expression ints = LinqExpression.Constant (new List<int> { 1, 2 }, typeof (IList<int>));
            Expression doubles = LinqExpression.Constant (
                new List<double> { 2, 2.5 }, typeof (IList<double>));
            Expression nullableInts = LinqExpression.Constant (
                new List<int?> { 2, null }, typeof (IList<int?>));
            Assert.AreEqual (new List<double> { 1, 2, 2, 2.5 },
                Eval<IList<double>> (Expression.ToList (Expression.Concat (ints, doubles))));
            Assert.AreEqual (new List<int?> { 1, 2, 2, null },
                Eval<IList<int?>> (Expression.ToList (Expression.Concat (ints, nullableInts))));
            Assert.IsTrue (Expression.ToList (Expression.Concat (ints, nullableInts)).ReturnType.Types [0].Nullable);
            Assert.AreEqual (new List<double> { 1, 2, 2.5 },
                Eval<IList<double>> (Expression.ToList (Expression.Union (ints, doubles))));
            Assert.AreEqual (new List<double> { 2 },
                Eval<IList<double>> (Expression.ToList (Expression.Intersect (doubles, ints))));
            Assert.AreEqual (new List<int?> { null },
                Eval<IList<int?>> (Expression.ToList (Expression.Except (nullableInts, ints))));
            Expression strings = LinqExpression.Constant (new List<string> { "a" }, typeof (IList<string>));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Concat (ints, strings));
            StringAssert.Contains (
                "The values of the collections given to Concat must all be of the same type", exn.Message);
            StringAssert.Contains ("got System.Int32, System.String", exn.Message);
            exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Union (ints, strings));
            StringAssert.Contains ("given to Union", exn.Message);
        }

        [Test]
        public void KeysMustHaveAnOrder ()
        {
            Expression lists = LinqExpression.Constant (
                new List<IList<int>> { new List<int> { 1 } }, typeof (IList<IList<int>>));
            var param = Expression.Parameter ("x", Type.ListType (Type.Int ()));
            var identity = Expression.Lambda (new List<Expression> { param }, param);
            foreach (var build in new System.Func<Expression> [] {
                    () => Expression.OrderBy (lists, identity),
                    () => Expression.MinBy (lists, identity),
                    () => Expression.MaxBy (lists, identity) }) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (() => build ());
                StringAssert.Contains ("have no order", exn.Message);
            }
            var tuple = Expression.Parameter ("x", Type.Int ());
            var key = Expression.Lambda (new List<Expression> { tuple }, Expression.CreateTuple (
                new List<Expression> { Expression.ConstantString ("a"), Expression.Cast (tuple, Type.NullableType (Type.Int ())) }));
            Assert.DoesNotThrow (() => Expression.OrderBy (list, key));
        }

        [Test]
        public void BuildDictionaryWithARepeatedKeyTakesTheLastValue ()
        {
            var param = Expression.Parameter ("x", Type.Int ());
            var keyFunc = Expression.Lambda (
                new List<Expression> { param }, Expression.Modulo (param, Expression.ConstantInt (2)));
            var valueFunc = Expression.Lambda (new List<Expression> { param }, param);
            var result = Eval<IDictionary<int, int>> (Expression.BuildDictionary (list, keyFunc, valueFunc));
            Assert.AreEqual (2, result.Count);
            Assert.AreEqual (4, result [0]);
            Assert.AreEqual (5, result [1]);
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
                Eval<IList<int>> (Expression.ToList (Expression.OrderBy (list, func))));
        }

        [Test]
        public void StringKeysOrderByCharacterCode ()
        {
            // A culture sensitive comparison orders "a" before "B"
            var strings = Expression.CreateList (new List<Expression> {
                Expression.ConstantString ("b"),
                Expression.ConstantString ("a"),
                Expression.ConstantString ("B")
            });
            var x = Expression.Parameter ("x", Type.String ());
            var identity = Expression.Lambda (new List<Expression> { x }, x);
            Assert.AreEqual (
                new List<string> { "B", "a", "b" },
                Eval<IList<string>> (Expression.ToList (Expression.OrderBy (strings, identity))));
            Assert.AreEqual ("B", Eval<string> (Expression.MinBy (strings, identity)));
            Assert.AreEqual ("b", Eval<string> (Expression.MaxBy (strings, identity)));
            // Including a string inside a tuple key
            var pair = Expression.Lambda (
                new List<Expression> { x },
                Expression.CreateTuple (new List<Expression> {
                    Expression.CreateTuple (new List<Expression> { x }),
                    Expression.ConstantInt (0)
                }));
            Assert.AreEqual (
                new List<string> { "B", "a", "b" },
                Eval<IList<string>> (Expression.ToList (Expression.OrderBy (strings, pair))));
            Assert.AreEqual ("B", Eval<string> (Expression.MinBy (strings, pair)));
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

        [Test]
        public void ValuesOfACollectionKeepTheirNullability ()
        {
            var list = Expression.CreateList (new List<Expression> {
                Expression.ConstantString ("a"), NullableString (null) });
            Assert.IsTrue (Expression.First (list).ReturnType.Nullable);
            Assert.IsTrue (Expression.Get (list, Expression.ConstantInt (1)).ReturnType.Nullable);
            Assert.IsTrue (Expression.ToList (Expression.Reverse (list)).ReturnType.Types [0].Nullable);
            var plain = Expression.CreateList (new List<Expression> { Expression.ConstantString ("a") });
            Assert.IsFalse (Expression.First (plain).ReturnType.Nullable);
            Assert.IsTrue (Expression.ToList (Expression.Concat (plain, list)).ReturnType.Types [0].Nullable);
        }

        [Test]
        public void NullableValuesGivenToAFunction ()
        {
            var list = Expression.CreateList (new List<Expression> {
                Expression.ConstantString ("a"), NullableString (null) });
            var parameter = Expression.Parameter ("x", Type.String ());
            var isA = Expression.Lambda (
                new List<Expression> { parameter }, Expression.Equal (parameter, Expression.ConstantString ("a")));
            var select = Expression.ToList (Expression.Select (list, isA));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<IList<bool>> (select));
            StringAssert.Contains ("is null", exn.Message);
            var nullableParameter = Expression.Parameter ("y", Type.NullableType (Type.String ()));
            var identity = Expression.Lambda (new List<Expression> { nullableParameter }, nullableParameter);
            var selected = Expression.ToList (Expression.Select (list, identity));
            Assert.IsTrue (selected.ReturnType.Types [0].Nullable);
            CollectionAssert.AreEqual (new List<string> { "a", null }, Eval<IList<string>> (selected));
        }

        [Test]
        public void NullCollectionIsNamed ()
        {
            var count = Expression.Count (Expression.ConstantNull (Type.ListType (Type.Int ())));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (() => Eval<int> (count));
            Assert.AreEqual ("The collection given to Count is null, which its type does not allow", exn.Message);
        }

        [Test]
        public void SetOfNullableValues ()
        {
            var list = Expression.CreateList (new List<Expression> {
                Expression.ConstantString ("a"), NullableString (null) });
            var set = Expression.ToSet (list);
            Assert.IsFalse (set.ReturnType.Types [0].Nullable);
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<ISet<string>> (set));
        }

        [Test]
        public void SetValuesAndDictionaryKeysOfNullableNumbers ()
        {
            Expression none = LinqExpression.Constant (null, typeof (int?));
            Expression three = LinqExpression.Constant (3, typeof (int?));
            var values = Expression.CreateList (new List<Expression> { three, Expression.ConstantInt (1) });
            var withNull = Expression.CreateList (new List<Expression> { none, Expression.ConstantInt (1) });
            var x = Expression.Parameter ("x", Type.NullableType (Type.Int ()));
            var identity = Expression.Lambda (new List<Expression> { x }, x);

            var set = Expression.ToSet (values);
            Assert.IsFalse (set.ReturnType.Types [0].Nullable);
            Assert.AreEqual (new HashSet<int> { 3, 1 }, Eval<HashSet<int>> (set));
            Assert.AreEqual (
                new List<int> { 1 },
                Eval<IDictionary<int, IList<int?>>> (Expression.GroupBy (values, identity)) [1]);
            Assert.AreEqual (
                (int?)3,
                Eval<IDictionary<int, int?>> (Expression.BuildDictionary (values, identity, identity)) [3]);

            foreach (var build in new List<System.Tuple<string, TestDelegate>> {
                System.Tuple.Create<string, TestDelegate> (
                    "A value of a set", () => Eval<HashSet<int>> (Expression.ToSet (withNull))),
                System.Tuple.Create<string, TestDelegate> (
                    "A key of a dictionary", () => Eval<object> (Expression.GroupBy (withNull, identity))),
                System.Tuple.Create<string, TestDelegate> (
                    "A key of a dictionary", () => Eval<object> (Expression.BuildDictionary (withNull, identity, identity)))
            }) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (build.Item2);
                StringAssert.Contains (build.Item1 + " is null", exn.Message);
            }
        }

        [Test]
        public void BuildDictionaryChecksTheKeyType ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var half = Expression.Lambda (
                new List<Expression> { x }, Expression.Divide (Expression.Cast (x, Type.Double ()), Expression.ConstantDouble (2)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.BuildDictionary (list, half, half));
        }

        [Test]
        public void SetOperationsCompareByValue ()
        {
            Expression List (int value)
            {
                return Expression.CreateList (new List<Expression> { Expression.ConstantInt (value) });
            }
            var lists = Expression.CreateList (new List<Expression> { List (1), List (1), List (2) });
            var others = Expression.CreateList (new List<Expression> { List (1) });
            Assert.AreEqual (2, Eval<int> (Expression.Count (Expression.ToSet (lists))));
            Assert.AreEqual (2, Eval<int> (Expression.Count (Expression.ToList (Expression.Distinct (lists)))));
            Assert.AreEqual (2, Eval<int> (Expression.Count (Expression.ToList (Expression.Union (lists, others)))));
            Assert.AreEqual (1, Eval<int> (Expression.Count (Expression.ToList (Expression.Intersect (lists, others)))));
            CollectionAssert.AreEqual (
                new [] { new List<int> { 2 } },
                Eval<IList<IList<int>>> (Expression.ToList (Expression.Except (lists, others))));
            Assert.IsTrue (Eval<bool> (Expression.Contains (Expression.ToSet (lists), List (1))));

            var data = Expression.Parameter ("data", Type.ListType (Type.Bytes ()));
            var function = Expression.Lambda (
                new List<Expression> { data }, Expression.Count (Expression.ToSet (data)));
            var count = ((System.Linq.Expressions.LambdaExpression)(System.Linq.Expressions.Expression)function)
                .Compile ().DynamicInvoke (new List<byte[]> { new byte [] { 1 }, new byte [] { 1 } });
            Assert.AreEqual (1, count);
        }
    }
}
