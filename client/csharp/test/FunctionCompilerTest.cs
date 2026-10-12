using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

using Expr = KRPC.Client.Services.KRPC.Expression;
using KType = KRPC.Client.Services.KRPC.Type;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class FunctionCompilerTest : ServerTestCase
    {
        // Compiles the lambda, including a single call that AddStream streams as that call
        T Evaluate<T> (Expression<Func<T>> expression)
        {
            return Connection.Run (expression);
        }

        [Test]
        public void TestConstantFolding ()
        {
            int threshold = 5;
            Assert.AreEqual (11, Connection.Run (() => threshold * 2 + 1));
            Assert.AreEqual ("foo", Connection.Run (() => "fo" + "o"));
        }

        [Test]
        public void TestRemoteProperty ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "foo";
            Assert.IsTrue (Evaluate (() => testService.StringProperty == "foo"));
            Assert.IsFalse (Evaluate (() => testService.StringProperty != "foo"));
        }

        [Test]
        public void TestMixedTypeArithmetic ()
        {
            var testService = Connection.TestService ();
            var value = Evaluate (() => testService.Counter ("CSharpCompiler.Mixed", 1) * 0.5 + 1);
            Assert.Greater (value, 1);
        }

        [Test]
        public void TestRemoteMethodWithArguments ()
        {
            var obj = Connection.TestService ().CreateTestObject ("compiled");
            var expected = obj.FloatToString (0.5f);
            Assert.AreEqual (expected, Evaluate (() => obj.FloatToString (0.5f)));
        }

        [Test]
        public void TestMethodWithMultipleArguments ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("args");
            var other = testService.CreateTestObject ("other");
            var expected = obj.OptionalArguments ("1", "2", "3", other);
            Assert.AreEqual (expected, Evaluate (() => obj.OptionalArguments ("1", "2", "3", other)));
        }

        [Test]
        public void TestNullArgumentForANullDefault ()
        {
            var obj = Connection.TestService ().CreateTestObject ("args");
            var expected = obj.OptionalArguments ("1", "2", "3", null).Length;
            Assert.AreEqual (expected, Evaluate (() => obj.OptionalArguments ("1", "2", "3", null).Length));
        }

        [Test]
        public void TestNullArgument ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("null");
            Assert.IsTrue (Evaluate (() => obj.EchoNullableObject (null) == null));
            Assert.Throws<FunctionCompilationException> (
                () => Evaluate (() => testService.NotNullableObject (null)));
        }

        [Test]
        public void TestStaticMethod ()
        {
            var expected = TestClass.StaticMethod (Connection, "bob", "");
            Assert.AreEqual (expected, Evaluate (() => TestClass.StaticMethod (Connection, "bob", "")));
        }

        [Test]
        public void TestChainedCalls ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("outer");
            var inner = testService.CreateTestObject ("inner");
            obj.ObjectProperty = inner;
            inner.IntProperty = 7;
            Assert.IsTrue (Evaluate (() => obj.ObjectProperty.IntProperty == 7));
            Assert.AreEqual ("value=inner", Evaluate (() => obj.ObjectProperty.GetValue ()));
        }

        [Test]
        public void TestObjectEquality ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("eq");
            var other = testService.CreateTestObject ("eq2");
            testService.ObjectProperty = obj;
            Assert.IsTrue (Evaluate (() => testService.ObjectProperty == obj));
            Assert.IsFalse (Evaluate (() => testService.ObjectProperty == other));
        }

        [Test]
        public void TestTupleAndCollectionEqualityByContent ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("contentequality");
            obj.IntProperty = 2;
            var list = new List<int> { 1, 2 };
            Assert.IsTrue (Evaluate (() => Tuple.Create (obj.IntProperty, 1) == Tuple.Create (2, 1)));
            Assert.IsTrue (Evaluate (
                () => testService.IncrementList (list) == new List<int> { 2, obj.IntProperty + 1 }));
            Assert.IsFalse (Evaluate (
                () => testService.IncrementList (list) != new List<int> { 2, obj.IntProperty + 1 }));
        }

        [Test]
        public void TestEnumValues ()
        {
            var testService = Connection.TestService ();
            Assert.IsTrue (Evaluate (() => testService.EnumEcho (TestEnum.ValueB) == TestEnum.ValueB));
        }

        [Test]
        public void TestTernary ()
        {
            var obj = Connection.TestService ().CreateTestObject ("ternary");
            obj.IntProperty = 1;
            Assert.AreEqual ("one", Evaluate (() => obj.IntProperty == 1 ? "one" : "other"));
            Assert.AreEqual (2.5, Evaluate (() => obj.IntProperty == 1 ? 2.5 : 2));
        }

        [Test]
        public void TestShortCircuitOfAClientSideOperand ()
        {
            var obj = Connection.TestService ().CreateTestObject ("shortcircuit");
            obj.IntProperty = 5;
            TestClass target = null;
            Assert.IsFalse (Evaluate (() => target != null && target.IntProperty > 1));
            Assert.IsTrue (Evaluate (() => target == null || target.IntProperty > 1));
            Assert.IsTrue (Evaluate (() => target == null && obj.IntProperty > 1));
        }

        [Test]
        public void TestClientSideNodesAreFolded ()
        {
            var obj = Connection.TestService ().CreateTestObject ("folded");
            obj.IntProperty = 5;
            var array = new [] { 1, 2 };
            int? missing = null;
            Assert.AreEqual (7, Evaluate (() => obj.IntProperty + array.Length));
            Assert.AreEqual (7, Evaluate (() => obj.IntProperty + (missing ?? 2)));
            Assert.AreEqual (8, Evaluate (() => obj.IntProperty + new [] { 1, 2 }.Sum ()));
        }

        [Test]
        public void TestContainsAndFormatWithAServerArgument ()
        {
            var obj = Connection.TestService ().CreateTestObject ("containsformat");
            obj.IntProperty = 2;
            var list = new List<int> { 1, 2 };
            var set = new HashSet<int> { 3 };
            Assert.IsTrue (Evaluate (() => list.Contains (obj.IntProperty)));
            Assert.IsFalse (Evaluate (() => set.Contains (obj.IntProperty)));
            Assert.AreEqual ("v=2 {x}", Evaluate (
                () => FormattableString.Invariant ($"v={obj.IntProperty} {{x}}")));
            Assert.AreEqual ("2 and 3", Evaluate (
                () => string.Format (
                    CultureInfo.InvariantCulture, "{0} and {1}", obj.IntProperty, obj.IntProperty + 1)));
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Evaluate (() => $"v={obj.IntProperty:F2}"));
            StringAssert.Contains ("format specifiers are not supported", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Evaluate (() => string.Format (
                    CultureInfo.InvariantCulture, "a}b{0}", obj.IntProperty)));
            StringAssert.Contains ("must be doubled", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Evaluate (() => string.Format (
                    CultureInfo.InvariantCulture, "{0} {1}", obj.IntProperty)));
            StringAssert.Contains ("format item {1} is out of range", exn.Message);
            // An array of integers is one argument, as C# binds it, and not the arguments
            Assert.Throws<FunctionCompilationException> (
                () => Evaluate (() => string.Format (
                    CultureInfo.InvariantCulture, "{0}", new [] { obj.IntProperty, 2 })));
        }

        [Test]
        public void TestEmptyCapturedCollections ()
        {
            var obj = Connection.TestService ().CreateTestObject ("emptycaptured");
            obj.IntProperty = 2;
            var list = new List<int> ();
            var set = new HashSet<int> ();
            var dictionary = new Dictionary<int, string> ();
            Assert.IsFalse (Evaluate (() => list.Contains (obj.IntProperty)));
            Assert.IsFalse (Evaluate (() => set.Contains (obj.IntProperty)));
            Assert.IsFalse (Evaluate (() => dictionary.ContainsKey (obj.IntProperty)));
        }

        [Test]
        public void TestCapturedSetDeclaredAsAnInterface ()
        {
            var testService = Connection.TestService ();
            ISet<int> set = new HashSet<int> { 1, 2 };
            ISet<int> empty = new HashSet<int> ();
            var hashSet = new HashSet<int> { 3 };
            CollectionAssert.AreEquivalent (
                new [] { 1, 2 },
                Connection.Run (
                    () => testService.StringToInt32 ("1") > 0
                        ? set : testService.IncrementSet (new HashSet<int> { 0 })));
            CollectionAssert.IsEmpty (
                Connection.Run (
                    () => testService.StringToInt32 ("1") > 0
                        ? empty : testService.IncrementSet (new HashSet<int> { 0 })));
            CollectionAssert.AreEquivalent (
                new [] { 3 },
                Connection.Run (
                    () => testService.StringToInt32 ("1") > 0
                        ? hashSet : testService.IncrementSet (new HashSet<int> { 0 })));
        }

        [Test]
        public void TestSetViewedAsASequence ()
        {
            var testService = Connection.TestService ();
            var set = new HashSet<int> { 1, 2 };
            Assert.AreEqual (2, Evaluate (() => ((IEnumerable<int>)testService.IncrementSet (set)).Count ()));
            Assert.AreEqual (2, Evaluate (() => ((ICollection<int>)testService.IncrementSet (set)).Count));
            Assert.AreEqual (2, Evaluate (
                () => (testService.StringToInt32 ("1") > 0
                       ? (IEnumerable<int>)testService.IncrementSet (set)
                       : testService.IncrementList (new List<int> { 1 })).Count ()));
            Assert.AreEqual (1, Evaluate (
                () => (testService.StringToInt32 ("1") < 0
                       ? (IEnumerable<int>)testService.IncrementSet (set)
                       : testService.IncrementList (new List<int> { 1 })).Count ()));
            Assert.AreEqual (0, Evaluate (
                () => (testService.StringToInt32 ("1") < 0
                       ? (IEnumerable<int>)testService.IncrementSet (set) : new List<int> ()).Count ()));
        }

        [Test]
        public void TestIntegerMathIsExact ()
        {
            var testService = Connection.TestService ();
            long big = long.MaxValue - 1;
            long small = long.MinValue + 1;
            Assert.AreEqual (big, Evaluate (() => Math.Min (
                testService.Int64SpecialDefaults (long.MaxValue, long.MinValue) [0], big)));
            Assert.AreEqual (small, Evaluate (() => Math.Max (
                testService.Int64SpecialDefaults (long.MaxValue, long.MinValue) [1], small)));
            Assert.AreEqual (long.MaxValue, Evaluate (() => Math.Abs (
                testService.Int64SpecialDefaults (long.MaxValue, long.MinValue) [1] + 1)));
#if NET
            Assert.AreEqual (big, Evaluate (() => Math.Clamp (
                testService.Int64SpecialDefaults (long.MaxValue, long.MinValue) [0], 0L, big)));
#endif
        }

#if NET
        [Test]
        public void TestClampWithMinimumAboveMaximum ()
        {
            var testService = Connection.TestService ();
            Assert.AreEqual (5, Connection.Run (
                () => Math.Clamp (testService.StringToInt32 ("5"), 1, 10)));
            var exn = Assert.Throws<ArgumentException> (
                () => Connection.Run (
                    () => Math.Clamp (testService.StringToInt32 ("5"), 10, 1)));
            StringAssert.Contains ("'10' cannot be greater than 1.", exn.Message);
            Assert.AreEqual (5.0, Connection.Run (
                () => Math.Clamp (testService.StringToInt32 ("5") * 1.0, 1.0, 10.0)));
            exn = Assert.Throws<ArgumentException> (
                () => Connection.Run (
                    () => Math.Clamp (testService.StringToInt32 ("5") * 1.0, 10.0, 1.0)));
            StringAssert.Contains ("cannot be greater than", exn.Message);
        }
#endif

        [Test]
        public void TestCheckedArithmeticRejected ()
        {
            var obj = Connection.TestService ().CreateTestObject ("checked");
            obj.IntProperty = 2;
            foreach (var function in new Expression<Func<long>> [] {
                () => checked (obj.IntProperty + 1),
                () => checked (-obj.IntProperty),
                () => checked ((int)(obj.IntProperty * 2L))
            }) {
                var exn = Assert.Throws<FunctionCompilationException> (() => Evaluate (function));
                StringAssert.Contains ("checked arithmetic is not supported", exn.Message);
            }
            long widened = 0;
            Assert.AreEqual (2L, Evaluate (() => checked ((long)obj.IntProperty) + widened));
            int five = 5;
            Assert.AreEqual (7, Evaluate (() => obj.IntProperty + checked (five + 0)));
            // An enumeration converts to its underlying type, and wider, without overflow
            var testService = Connection.TestService ();
            Assert.AreEqual (1, Evaluate (() => checked ((int)testService.EnumEcho (TestEnum.ValueB))));
            Assert.AreEqual (1L, Evaluate (() => checked ((long)testService.EnumEcho (TestEnum.ValueB))));
        }

        [Test]
        public void TestMissingKey ()
        {
            var obj = Connection.TestService ().CreateTestObject ("missingkey");
            obj.IntProperty = 9;
            var values = new Dictionary<int, int> { { 1, 2 } };
            var exn = Assert.Throws<KeyNotFoundException> (
                () => Evaluate (() => values [obj.IntProperty]));
            StringAssert.Contains ("not in the dictionary", exn.Message);
        }

        [Test]
        public void TestIndexOutOfRangeAndDivideByZero ()
        {
            var obj = Connection.TestService ().CreateTestObject ("outofrange");
            obj.IntProperty = 0;
            var values = new List<int> { 1 };
            Assert.Throws<IndexOutOfRangeException> (
                () => Evaluate (() => values [obj.IntProperty + 1]));
            Assert.Throws<DivideByZeroException> (
                () => Evaluate (() => 1 / obj.IntProperty));
        }

        [Test]
        public void TestLiftedComparisonAndConversion ()
        {
            var testService = Connection.TestService ();
            int? missing = null;
            Assert.IsFalse (Evaluate (() => testService.EchoNullableInt (null) > 5));
            Assert.IsFalse (Evaluate (() => 5 <= testService.EchoNullableInt (null)));
            Assert.IsTrue (Evaluate (() => testService.EchoNullableInt (7) > 5));
            Assert.IsFalse (Evaluate (() => testService.EchoNullableInt (7) > missing));
            Assert.IsTrue (Evaluate (() => (double?)testService.EchoNullableInt (7) > 6.5));
            Assert.IsFalse (Evaluate (() => (double?)testService.EchoNullableInt (null) > 6.5));
        }

        [Test]
        public void TestTernaryBranchThatFailsOnlyWhenChosen ()
        {
            var obj = Connection.TestService ().CreateTestObject ("ternarylazy");
            obj.IntProperty = 5;
            var empty = new List<int> ();
            Assert.AreEqual (5, Evaluate (() => obj.IntProperty > 0 ? obj.IntProperty : empty [0]));
            obj.IntProperty = -1;
            var exn = Assert.Throws<InvalidOperationException> (
                () => Evaluate (() => obj.IntProperty > 0 ? obj.IntProperty : empty [0]));
            StringAssert.Contains ("Error evaluating client side sub-expression", exn.Message);
        }

        [Test]
        public void TestBooleanOperators ()
        {
            var obj = Connection.TestService ().CreateTestObject ("bool");
            obj.IntProperty = 5;
            Assert.IsTrue (Evaluate (() => obj.IntProperty > 1 && obj.IntProperty < 10));
            Assert.IsTrue (Evaluate (() => obj.IntProperty > 10 || !(obj.IntProperty == 4)));
        }

        [Test]
        public void TestMathPow ()
        {
            var obj = Connection.TestService ().CreateTestObject ("pow");
            obj.IntProperty = 3;
            Assert.AreEqual (9.0, Evaluate (() => Math.Pow (obj.IntProperty, 2)));
        }

        IList<TestClass> MakeObjects (string prefix)
        {
            var testService = Connection.TestService ();
            var objs = new List<TestClass> ();
            for (int i = 0; i < 3; i++) {
                var obj = testService.CreateTestObject (prefix + i);
                obj.IntProperty = i + 1;
                objs.Add (obj);
            }
            return objs;
        }

        [Test]
        public void TestSelect ()
        {
            var objs = MakeObjects ("select");
            CollectionAssert.AreEqual (
                new [] { 2, 4, 6 },
                Evaluate (() => objs.Select (o => o.IntProperty * 2).ToList ()));
        }

        [Test]
        public void TestSelectWithoutToList ()
        {
            var objs = MakeObjects ("lazy");
            // The lazily evaluated sequence is implicitly converted to a list
            var expression = Connection.Compile (() => objs.Select (o => o.IntProperty));
            var stream = Connection.AddStream<IList<int>> (expression);
            CollectionAssert.AreEqual (new [] { 1, 2, 3 }, stream.Get ());
            stream.Remove ();
            // A typed lambda decodes the list as the lazily evaluated sequence it declares
            IEnumerable<int> values = Connection.Run (() => objs.Select (o => o.IntProperty));
            CollectionAssert.AreEqual (new [] { 1, 2, 3 }, values);
            var lazyStream = Connection.AddStream (() => objs.Select (o => o.IntProperty));
            CollectionAssert.AreEqual (new [] { 1, 2, 3 }, lazyStream.Get ());
            lazyStream.Remove ();
            // An ordered sequence is not decoded as a list
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Run (() => objs.OrderBy (o => o.IntProperty)));
            StringAssert.Contains ("ToList", exn.Message);
            Assert.Throws<FunctionCompilationException> (
                () => Connection.AddStream (() => objs.OrderBy (o => o.IntProperty)));
            CollectionAssert.AreEqual (objs, Connection.Run (
                () => objs.OrderBy (o => o.IntProperty).ToList ()));
        }

        [Test]
        public void TestWhere ()
        {
            var objs = MakeObjects ("where");
            CollectionAssert.AreEqual (
                new [] { 2, 3 },
                Evaluate (() => objs.Where (o => o.IntProperty > 1).Select (o => o.IntProperty).ToList ()));
        }

        [Test]
        public void TestAggregations ()
        {
            var objs = MakeObjects ("agg");
            Assert.AreEqual (6, Evaluate (() => objs.Select (o => o.IntProperty).Sum ()));
            Assert.AreEqual (1, Evaluate (() => objs.Select (o => o.IntProperty).Min ()));
            Assert.AreEqual (3, Evaluate (() => objs.Select (o => o.IntProperty).Max ()));
            Assert.AreEqual (3, Evaluate (() => objs.Select (o => o.IntProperty).Count ()));
        }

        [Test]
        public void TestAnyAll ()
        {
            var objs = MakeObjects ("anyall");
            Assert.IsTrue (Evaluate (() => objs.Any (o => o.IntProperty == 2)));
            Assert.IsFalse (Evaluate (() => objs.Any (o => o.IntProperty == 4)));
            Assert.IsTrue (Evaluate (() => objs.All (o => o.IntProperty < 4)));
            Assert.IsFalse (Evaluate (() => objs.All (o => o.IntProperty > 1)));
        }

        [Test]
        public void TestContains ()
        {
            var objs = MakeObjects ("contains");
            Assert.IsTrue (Evaluate (() => objs.Select (o => o.IntProperty).Contains (2)));
            Assert.IsFalse (Evaluate (() => objs.Select (o => o.IntProperty).Contains (4)));
        }

        [Test]
        public void TestOrderBy ()
        {
            var objs = MakeObjects ("orderby");
            var expression = Connection.Compile (
                () => objs.Select (o => o.IntProperty).OrderBy (x => -x));
            var stream = Connection.AddStream<IList<int>> (expression);
            CollectionAssert.AreEqual (new [] { 3, 2, 1 }, stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestIndexing ()
        {
            var objs = MakeObjects ("index");
            Assert.AreEqual (1, Evaluate (() => objs.Select (o => o.IntProperty).ToList () [0]));
        }

        [Test]
        public void TestEvent ()
        {
            var testService = Connection.TestService ();
            var evnt = Connection.AddEvent (() => testService.Counter ("CSharpCompiler.Event", 1) > 5);
            lock (evnt.Condition) {
                evnt.Wait (5);
                Assert.IsTrue (evnt.Stream.Get ());
            }
        }

        [Test]
        public void TestSingleCallStreamStillWorks ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "plain";
            var stream = Connection.AddStream (() => testService.StringProperty);
            Assert.AreEqual ("plain", stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestSingleCallWithRemoteArgumentsReevaluatesThem ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "a";
            var stream = Connection.AddStream (
                () => testService.OptionalArguments (testService.StringProperty, "", "", null));
            Assert.AreEqual ("anull", stream.Get ());
            testService.StringProperty = "b";
            lock (stream.Condition) {
                for (int i = 0; i < 10 && stream.Get () != "bnull"; i++)
                    stream.Wait (0.5);
            }
            Assert.AreEqual ("bnull", stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestCompiledStreamSkipsReturnTypeCheck ()
        {
            var obj = Connection.TestService ().CreateTestObject ("streamnocheck");
            obj.IntProperty = 2;
            var checkedFunctions = Connection.functionReturnTypes.Count;
            var stream = Connection.AddStream (() => obj.IntProperty + 1);
            Assert.AreEqual (3, stream.Get ());
            stream.Remove ();
            Assert.AreEqual (checkedFunctions, Connection.functionReturnTypes.Count);
        }

        [Test]
        public void TestRunningALambdaDoesNotCacheItsReturnType ()
        {
            var objects = MakeObjects ("runnocache");
            var cachedFunctions = Connection.functionReturnTypes.Count;
            var values = Connection.Run (
                () => objects.Select (x => x.FloatToString (0.5f)).ToList ());
            Assert.AreEqual (3, values.Count);
            Assert.AreEqual (cachedFunctions, Connection.functionReturnTypes.Count);
        }

        [Test]
        public void TestStreamOfALambdaChecksItsResultType ()
        {
            var obj = Connection.TestService ().CreateTestObject ("streamtype");
            obj.IntProperty = 2;
            Expression<Func<int>> function = () => obj.IntProperty + 1;
            Assert.Throws<ArgumentException> (
                () => Connection.AddStream<double> ((LambdaExpression)function));
        }

        [Test]
        public void TestLambdaOfAWiderResultType ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("wider");
            obj.IntProperty = 2;
            var stream = Connection.AddStream<object> (() => obj.IntProperty * 0.5);
            Assert.AreEqual (1.0, stream.Get ());
            stream.Remove ();
            Expression<Func<int>> function = () => obj.IntProperty + 1;
            var lambdaStream = Connection.AddStream<object> ((LambdaExpression)function);
            Assert.AreEqual (3, lambdaStream.Get ());
            lambdaStream.Remove ();
            Assert.AreEqual (3, Connection.Run<object> (() => obj.IntProperty + 1));
            CollectionAssert.AreEqual (
                new [] { 3 },
                Connection.Run<ICollection<int>> (
                    () => testService.IncrementList (new List<int> { obj.IntProperty })));
            var collectionStream = Connection.AddStream<ICollection<int>> (
                () => testService.IncrementList (new List<int> { obj.IntProperty }));
            CollectionAssert.AreEqual (new [] { 3 }, collectionStream.Get ());
            collectionStream.Remove ();
        }

        [Test]
        public void TestUndecodableResultThrowsBeforeRunning ()
        {
            var testService = Connection.TestService ();
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Run (
                    () => new [] { testService.Counter ("CSharpCompiler.Undecodable", 1) }));
            Assert.AreEqual (1, testService.Counter ("CSharpCompiler.Undecodable", 1));
        }

        [Test]
        public void TestTupleItemsAndTupleCreate ()
        {
            var obj = Connection.TestService ().CreateTestObject ("tupleitems");
            obj.IntProperty = 4;
            Assert.AreEqual (5, Evaluate (() => Tuple.Create (obj.IntProperty, 1).Item1 + 1));
            Assert.AreEqual (
                Tuple.Create (4, "a"), Evaluate (() => Tuple.Create (obj.IntProperty, "a")));
        }

        [Test]
        public void TestDictionaryInitializer ()
        {
            var obj = Connection.TestService ().CreateTestObject ("dictinit");
            obj.IntProperty = 4;
            var result = Evaluate (() => new Dictionary<string, int> {
                { "a", obj.IntProperty }, { "b", 2 }
            });
            CollectionAssert.AreEquivalent (
                new Dictionary<string, int> { { "a", 4 }, { "b", 2 } }, result);
        }

        [Test]
        public void TestServerBuildErrorIsACompilationError ()
        {
            var obj = Connection.TestService ().CreateTestObject ("builderror");
            var krpc = KRPC.Client.Services.KRPC.ExtensionMethods.KRPC (Connection);
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Run (
                    () => obj.IntProperty + (int)krpc.GameScene));
        }

        int sideEffects;

        int SideEffect ()
        {
            sideEffects++;
            return 1;
        }

        [Test]
        public void TestClientSideSubexpressionIsEvaluatedOnce ()
        {
            var obj = Connection.TestService ().CreateTestObject ("sideeffect");
            obj.IntProperty = 1;
            // Each dequeue is an effect on the client
            var values = new Queue<int> (new [] { 1, 2, 3 });
            Assert.AreEqual (
                5, Connection.Run (() => obj.IntProperty + (values.Dequeue () + 1) * 2));
            Assert.AreEqual (2, values.Count);
            Assert.AreEqual (
                Tuple.Create (1, Tuple.Create (2, 2)),
                Connection.Run (
                    () => Tuple.Create (obj.IntProperty, Tuple.Create (values.Dequeue (), 2))));
            Assert.AreEqual (1, values.Count);
        }

        [Test]
        public void TestOperandsThatReduceToValues ()
        {
            var testService = Connection.TestService ();
            var flag = true;
            CollectionAssert.AreEqual (
                new [] { 2, 2 },
                Connection.Run (
                    () => testService.IncrementList (new List<int> { 0, 1 })
                        .Select (p => (flag ? 1 : p) * 2).ToList ()));
            CollectionAssert.IsEmpty (
                Connection.Run (
                    () => testService.IncrementList (new List<int> { 0, 1 })
                        .Where (p => !(flag || p > 1)).ToList ()));
            CollectionAssert.AreEqual (
                new [] { Tuple.Create (1, 2), Tuple.Create (1, 2) },
                Connection.Run (
                    () => testService.IncrementList (new List<int> { 0, 1 })
                        .Select (p => Tuple.Create (flag ? 1 : p, 2)).ToList ()));
            var values = new Queue<int> (new [] { 1, 2 });
            Assert.AreEqual (
                2, Connection.Run (
                    () => (flag ? values.Dequeue () : testService.StringToInt32 ("7")) + 1));
            Assert.AreEqual (1, values.Count);
            int? missing = null;
            Assert.IsFalse (Connection.Run (
                () => (flag ? missing : testService.EchoNullableInt (1)) > 0));
        }

        int argumentEvaluations;

        string ArgumentThatFailsOnce ()
        {
            argumentEvaluations++;
            if (argumentEvaluations == 1)
                throw new ArgumentException ("client side failure");
            return "value";
        }

        [Test]
        public void TestClientSideErrorInASingleCallStreamIsRaised ()
        {
            var testService = Connection.TestService ();
            argumentEvaluations = 0;
            var exn = Assert.Throws<ArgumentException> (
                () => Connection.AddStream (() => testService.OptionalArguments (ArgumentThatFailsOnce (), "y", "z", null)));
            Assert.AreEqual ("client side failure", exn.Message);
            Assert.AreEqual (1, argumentEvaluations);
        }

        static string PropertyOf (Services.TestService.Service testService)
        {
            return testService.StringProperty;
        }

        static int Twice (int x)
        {
            return 2 * x;
        }

        static IEnumerable<int> Generate (Services.TestService.Service testService)
        {
            yield return testService.StringProperty.Length;
        }

        sealed class RemoteText
        {
            readonly Services.TestService.Service testService;

            public RemoteText (Services.TestService.Service service)
            {
                testService = service;
            }

            public override string ToString ()
            {
                return testService.StringProperty;
            }
        }

        sealed class RemoteSequence : IEnumerable<int>, IComparer<int>
        {
            readonly Services.TestService.Service testService;

            public RemoteSequence (Services.TestService.Service service)
            {
                testService = service;
            }

            public IEnumerator<int> GetEnumerator ()
            {
                yield return testService.StringProperty.Length;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
            {
                return GetEnumerator ();
            }

            public int Compare (int x, int y)
            {
                return testService.StringProperty.Length + x.CompareTo (y);
            }
        }

        [Test]
        public void TestHiddenCallIntoClientCodeIsRejected ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "hidden";
            Func<string> property = () => testService.StringProperty;
            Func<int, int> twice = x => 2 * x;
            var list = new List<int> { 1, 2 };
            var text = new RemoteText (testService);
            var sequence = new RemoteSequence (testService);
            var generated = Generate (testService);
            var lazy = new Lazy<string> (() => testService.StringProperty);
            // Each runs client code that could make a remote call, once, when compiling
            var rejected = new List<Expression<Func<object>>> {
                () => property.Invoke () + testService.StringProperty,
                () => list.Select (twice).Sum () + testService.StringProperty.Length,
                () => list.Select (Twice).Sum () + testService.StringProperty.Length,
                () => new ClientValue (testService).Limit + testService.StringProperty.Length,
                () => text.ToString () + testService.StringProperty,
                () => "a" + text + testService.StringProperty,
                () => $"{text}{testService.StringProperty}",
                () => ("a" + text).Length + testService.StringProperty.Length,
                () => sequence.Sum () + testService.StringProperty.Length,
                () => list.OrderBy (x => x, sequence).First () + testService.StringProperty.Length,
                () => generated.Sum () + testService.StringProperty.Length,
                () => lazy.Value + testService.StringProperty
            };
            foreach (var expression in rejected)
                Assert.Throws<FunctionCompilationException> (
                    () => Connection.Compile (expression), expression.ToString ());
            // A lambda, a method group of the base library and a value with no methods of
            // its own are evaluated on the client
            var value = new ClientValue (testService) { Limit = 3 };
            Assert.AreEqual (15, Connection.Run (
                () => list.Select (x => 2 * x).Sum () + list.Select (Math.Abs).Sum () +
                testService.StringProperty.Length));
            Assert.AreEqual (4, Connection.Run (
                () => new [] { value }.Count () + value.Limit));
        }

        sealed class ClientValue
        {
            readonly Services.TestService.Service testService;

            public ClientValue (Services.TestService.Service service)
            {
                testService = service;
            }

            public int Limit { get; set; }

            public string Remote {
                get { return testService.StringProperty; }
            }

            public static int operator + (ClientValue value, int x)
            {
                return value.testService.StringProperty.Length + x;
            }

            public static explicit operator string (ClientValue value)
            {
                return value.testService.StringProperty;
            }
        }

        [Test]
        public void TestNullStringArguments ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = " axb ";
            string none = null;
            char[] noCharacters = null;
            // Null is an empty string for the new value and the separator, as in C#
            Assert.AreEqual (" b ", Connection.Run (
                () => testService.StringProperty.Replace ("ax", none)));
            Assert.AreEqual ("ab", Connection.Run (
                () => string.Join (none, new List<string> { testService.StringProperty.Trim ().Substring (0, 1), "b" })));
            Assert.AreEqual ("axb", Connection.Run (
                () => testService.StringProperty.Trim (noCharacters)));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => string.Format (none, testService.StringProperty)));
        }

        [Test]
        public void TestClientValuesInAConcatenation ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "x";
            short small = 3;
            var pair = Tuple.Create (1, 2);
            Assert.AreEqual ("x3", Connection.Run (() => testService.StringProperty + small));
            Assert.AreEqual ("x" + pair, Connection.Run (() => testService.StringProperty + pair));
        }

        [Test]
        public void TestUnaryPlus ()
        {
            var obj = Connection.TestService ().CreateTestObject ("unaryplus");
            obj.IntProperty = 4;
            Assert.AreEqual (4, Connection.Run (() => +obj.IntProperty));
        }

        [Test]
        public void TestSignOfANaNThrows ()
        {
            var obj = Connection.TestService ().CreateTestObject ("signnan");
            obj.IntProperty = 2;
            Assert.AreEqual (-1, Connection.Run (() => Math.Sign (obj.IntProperty * -2.5)));
            var exn = Assert.Catch (
                () => Connection.Run (() => Math.Sign (obj.IntProperty * double.NaN)));
            StringAssert.Contains ("Not-a-Number", exn.Message);
        }

        [Test]
        public void TestCallIntoClientCodeIsRejected ()
        {
            var testService = Connection.TestService ();
            Func<string> property = () => testService.StringProperty;
            // A remote call the method or delegate makes would run once, when compiling
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => property () + testService.StringProperty));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => PropertyOf (testService) + testService.StringProperty));
            var value = new ClientValue (testService) { Limit = 3 };
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => value.Remote + testService.StringProperty));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => (value + 1) + testService.StringProperty.Length));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => (string)value + testService.StringProperty));
            // An auto-property is read on the client
            var obj = testService.CreateTestObject ("clientvalue");
            obj.IntProperty = 1;
            Assert.IsTrue (Connection.Run (() => obj.IntProperty < value.Limit));
        }

        [Test]
        public void TestClientSideLambdaIsRejected ()
        {
            var testService = Connection.TestService ();
            var limit = 3;
            Assert.Throws<FunctionCompilationException> (
                () => Connection.AddEvent (() => limit > 1));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.AddStream (() => limit + 1));
            testService.StringProperty = "stream";
            var stream = Connection.AddStream (() => testService.StringProperty);
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.AddEvent (() => stream.Get () == "stream"));
            StringAssert.Contains ("Stream", exn.Message);
            stream.Remove ();
        }

        [Test]
        public void TestMathFunctions ()
        {
            var obj = Connection.TestService ().CreateTestObject ("mathfns");
            obj.IntProperty = 16;
            Assert.AreEqual (4.0, Evaluate (() => Math.Sqrt (obj.IntProperty)));
            Assert.AreEqual (16, Evaluate (() => Math.Abs (-obj.IntProperty)));
            Assert.AreEqual (16.0, Evaluate (() => Math.Max ((double)obj.IntProperty, 3.0)));
            // Client side arguments are still evaluated on the client
            Assert.AreEqual (3.0, Connection.Run (() => Math.Sqrt (9) * 1.0));
        }

        [Test]
        public void TestMathRoundingAndLogarithms ()
        {
            var obj = Connection.TestService ().CreateTestObject ("rounding");
            obj.IntProperty = 5;
            Assert.AreEqual (2.0, Evaluate (() => Math.Round (obj.IntProperty / 2.0)));
            Assert.AreEqual (3.0, Evaluate (
                () => Math.Round (obj.IntProperty / 2.0, MidpointRounding.AwayFromZero)));
            Assert.AreEqual (
                0.62, Evaluate (() => Math.Round (obj.IntProperty / 8.0, 2)), 1e-10);
            Assert.AreEqual (0.63, Evaluate (
                () => Math.Round (
                    obj.IntProperty / 8.0, 2, MidpointRounding.AwayFromZero)), 1e-10);
            var mode = MidpointRounding.AwayFromZero;
            Assert.AreEqual (3.0, Evaluate (() => Math.Round (obj.IntProperty / 2.0, mode)));
            Assert.AreEqual (3.0, Evaluate (() => Math.Log (obj.IntProperty * 1.6, 2)), 1e-10);
            Assert.AreEqual (
                Math.Log (5.0), Evaluate (() => Math.Log ((double)obj.IntProperty)), 1e-10);
#if NET
            Assert.AreEqual (3.0, Evaluate (() => Math.Clamp (obj.IntProperty / 1.0, 0.0, 3.0)));
            // Directed rounding, which no StdLib procedure performs
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (
                    () => Math.Round (obj.IntProperty / 2.0, MidpointRounding.ToPositiveInfinity)));
#endif
        }

        [Test]
        public void TestWriteLine ()
        {
            var obj = Connection.TestService ().CreateTestObject ("writeline");
            obj.IntProperty = 3;
            Connection.Run (() => Console.WriteLine ("hello"));
            Connection.Run (() => Console.WriteLine (obj.GetValue ()));
            Connection.Run (() => Console.WriteLine ("{0} = {1}", "value", obj.GetValue ()));
            Connection.Run (
                () => Console.WriteLine (FormattableString.Invariant ($"value = {obj.IntProperty}")));
            Connection.Run (() => Console.WriteLine ());
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => Console.WriteLine (obj.IntProperty)));
            StringAssert.Contains ("culture", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => Console.WriteLine (new [] { 'a' })));
            StringAssert.Contains ("character array", exn.Message);
        }

        [Test]
        public void TestStdLibService ()
        {
            var stdlib = KRPC.Client.Services.StdLib.ExtensionMethods.StdLib (Connection);
            Assert.AreEqual (3.0, stdlib.Sqrt (9));
            Assert.AreEqual (
                Tuple.Create (0.0, 0.0, 1.0),
                stdlib.VectorCross (
                    Tuple.Create (1.0, 0.0, 0.0), Tuple.Create (0.0, 1.0, 0.0)));
        }

        [Test]
        public void TestStringConcatenation ()
        {
            var obj = Connection.TestService ().CreateTestObject ("strconcat");
            obj.IntProperty = 42;
            Assert.AreEqual ("value is 42!", Evaluate (
                () => "value is " + obj.IntProperty.ToString (CultureInfo.InvariantCulture) + "!"));
            Assert.AreEqual ("42", Evaluate (
                () => obj.IntProperty.ToString (CultureInfo.InvariantCulture) + ""));
        }

        [Test]
        public void TestStringConcatenationOfANull ()
        {
            var testService = Connection.TestService ();
            Assert.AreEqual ("<>", Evaluate (() => "<" + testService.EchoNullableString (null) + ">"));
            Assert.AreEqual ("<a>", Evaluate (() => "<" + testService.EchoNullableString ("a") + ">"));
            Assert.AreEqual ("<>", Evaluate (
                () => FormattableString.Invariant ($"<{testService.EchoNullableInt (null)}>")));
            Assert.AreEqual ("<1>", Evaluate (
                () => FormattableString.Invariant ($"<{testService.EchoNullableInt (1)}>")));
        }

        [Test]
        public void TestStringConcatenationOfACapturedValue ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "count ";
            int n = 3;
            Assert.AreEqual ("count 3", Evaluate (() => testService.StringProperty + n));
            Assert.AreEqual ("count 1", Evaluate (() => testService.StringProperty + 1));
            Assert.AreEqual ("3count ", Evaluate (() => n + testService.StringProperty));
        }

        [Test]
        public void TestStringConcatenationOfACharacter ()
        {
            var testService = Connection.TestService ();
            var c = 'q';
            Assert.AreEqual ("5!", Connection.Run (() => testService.Int32ToString (5) + '!'));
            Assert.AreEqual ("!5", Connection.Run (() => '!' + testService.Int32ToString (5)));
            Assert.AreEqual ("5q", Connection.Run (() => testService.Int32ToString (5) + c));
            Assert.AreEqual ("5c", Connection.Run (() => $"{testService.Int32ToString (5)}{'c'}"));
        }

        [Test]
        public void TestBitwiseComplement ()
        {
            var obj = Connection.TestService ().CreateTestObject ("bitwise");
            obj.IntProperty = 12;
            Assert.AreEqual (-13, Evaluate (() => ~obj.IntProperty));
            Assert.AreEqual (8, Evaluate (() => obj.IntProperty & 10));
        }

        [Test]
        public void TestSkipAndTake ()
        {
            var objs = MakeObjects ("skiptake");
            CollectionAssert.AreEqual (
                new [] { 2, 3 },
                Evaluate (() => objs.Select (o => o.IntProperty).Skip (1).Take (2).ToList ()));
        }

        [Test]
        public void TestToDictionary ()
        {
            var objs = MakeObjects ("todict");
            var result = Evaluate (() => objs.ToDictionary (
                o => o.IntProperty.ToString (CultureInfo.InvariantCulture), o => o.IntProperty * 2));
            Assert.AreEqual (3, result.Count);
            Assert.AreEqual (2, result ["1"]);
            Assert.AreEqual (6, result ["3"]);
        }

        [Test]
        public void TestDictionaryKeysAndValues ()
        {
            var testService = Connection.TestService ();
            var dictionary = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            CollectionAssert.AreEquivalent (new [] { "a", "b" }, Evaluate (
                () => testService.IncrementDictionary (dictionary).Keys.ToList ()));
            CollectionAssert.AreEquivalent (new [] { 20, 30 }, Evaluate (
                () => testService.IncrementDictionary (dictionary).Values.Select (x => x * 10).ToList ()));
            Assert.AreEqual (2, Evaluate (() => testService.IncrementDictionary (dictionary).Keys.Count));
            Assert.IsTrue (Evaluate (() => testService.IncrementDictionary (dictionary).Values.Contains (3)));
        }

        [Test]
        public void TestWideIntegerConstants ()
        {
            var obj = Connection.TestService ().CreateTestObject ("wide");
            obj.IntProperty = 3;
            Assert.AreEqual (9000000000L, Evaluate (() => obj.IntProperty * 3000000000L));
            Assert.AreEqual (4000000003u, Evaluate (() => (uint)obj.IntProperty + 4000000000u));
        }

        [Test]
        public void TestNegate ()
        {
            var obj = Connection.TestService ().CreateTestObject ("negate");
            obj.IntProperty = 3;
            Assert.AreEqual (-3, Evaluate (() => -obj.IntProperty));
            Assert.AreEqual (-1.5, Evaluate (() => -(obj.IntProperty * 0.5)));
        }

        [Test]
        public void TestContainsKey ()
        {
            var objs = MakeObjects ("containskey");
            Assert.IsTrue (Evaluate (() => objs.ToDictionary (
                o => o.IntProperty.ToString (CultureInfo.InvariantCulture), o => o.IntProperty).ContainsKey ("2")));
            Assert.IsFalse (Evaluate (() => objs.ToDictionary (
                o => o.IntProperty.ToString (CultureInfo.InvariantCulture), o => o.IntProperty).ContainsKey ("7")));
        }

        [Test]
        public void TestRunFunction ()
        {
            var obj = Connection.TestService ().CreateTestObject ("run");
            obj.IntProperty = 20;
            Assert.AreEqual (41, Connection.Run (() => obj.IntProperty * 2 + 1));
        }

        [Test]
        public void TestRunFunctionNullResult ()
        {
            var obj = Connection.TestService ().CreateTestObject ("runnull");
            // The property is nullable and unset, so the function's value is null
            Assert.IsNull (Connection.Run (() => obj.ObjectProperty));
            // The function's type is int, so the null reaches the check for one
            var testService = Connection.TestService ();
            var exn = Assert.Throws<InvalidOperationException> (
                () => Connection.Run<int> (
                    Connection.Compile (() => testService.EchoNullableInt (null))));
            StringAssert.Contains ("produced a null value", exn.Message);
            var other = Connection.TestService ().CreateTestObject ("runnull2");
            obj.ObjectProperty = other;
            Assert.AreEqual (other, Connection.Run (() => obj.ObjectProperty));
        }

        [Test]
        public void TestNullComparison ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("nullcompare");
            Assert.IsTrue (Connection.Run (() => obj.ObjectProperty == null));
            Assert.IsFalse (Connection.Run (() => null != obj.ObjectProperty));
            obj.ObjectProperty = obj;
            Assert.IsFalse (Connection.Run (() => obj.ObjectProperty == null));
            obj.IntProperty = 3;
            Assert.IsTrue (Connection.Run (
                () => testService.EchoNullableInt (obj.IntProperty) != null));
            Assert.AreEqual (4, Connection.Run (
                () => testService.EchoNullableInt (obj.IntProperty) + 1));
        }

        [Test]
        public void TestNullComparisonOfANonNullableValue ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "foo";
            Assert.IsFalse (Connection.Run (() => testService.StringProperty == null));
            Assert.IsTrue (Connection.Run (() => null != testService.StringProperty));
            Assert.AreEqual ("foo", Connection.Run (() => testService.StringProperty ?? "bar"));
            // The value compared is still evaluated
            var count = testService.Counter ("CSharpCompiler.NonNullableCompare", 1);
            Assert.IsFalse (Connection.Run (
                () => testService.Counter ("CSharpCompiler.NonNullableCompare", 1)
                    .ToString (CultureInfo.InvariantCulture) == null));
            Assert.AreEqual (count + 2, testService.Counter ("CSharpCompiler.NonNullableCompare", 1));
            Assert.IsTrue (Connection.Run (() => testService.EchoNullableString (null) == null));
            Assert.AreEqual ("bar", Connection.Run (() => testService.EchoNullableString (null) ?? "bar"));
        }

        [Test]
        public void TestNullableObjectElements ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("compiled-nullable-elements");
            obj.IntProperty = 1;
            CollectionAssert.AreEqual (
                new [] { obj, null },
                Connection.Run (() => testService.EchoListOfNullableObjects (
                    new List<TestClass> { obj, testService.EchoTestObject (null) })));
            CollectionAssert.AreEqual (
                new [] { obj, null },
                Connection.Run (() => testService.EchoListOfNullableObjects (
                    new List<TestClass> { obj, testService.EchoTestObject (null) }).Select (x => x)));
            CollectionAssert.AreEqual (
                new [] { obj },
                Connection.Run (() => testService.EchoListOfNullableObjects (
                    new List<TestClass> { obj, testService.EchoTestObject (null) })
                    .Where (x => x != null && x.IntProperty > 0).ToList ()));
            var stream = Connection.AddStream (() => testService.EchoListOfNullableObjects (
                new List<TestClass> { obj, testService.EchoTestObject (null) }).Select (x => x).ToList ());
            CollectionAssert.AreEqual (new [] { obj, null }, stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestNullConstants ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("null-constants");
            obj.IntProperty = 0;
            Assert.IsNull (Connection.Run (() => (string)null));
            Assert.IsNull (Connection.Run (() => obj.IntProperty > 0 ? "a" : null));
            Assert.IsNull (Connection.Run (() => obj.IntProperty > 0 ? obj.IntProperty : (int?)null));
            obj.IntProperty = 2;
            Assert.AreEqual ("a", Connection.Run (() => obj.IntProperty > 0 ? "a" : null));
            Assert.AreEqual (2, Connection.Run (() => obj.IntProperty > 0 ? obj.IntProperty : (int?)null));
            CollectionAssert.AreEqual (
                new [] { obj, null },
                Connection.Run (() => testService.EchoListOfNullableObjects (
                    new List<TestClass> { obj, null })));
            CollectionAssert.AreEqual (
                new [] { "value=null-constants", null },
                Connection.Run (() => new List<string> { obj.GetValue (), null }));
        }

        [Test]
        public void TestCapturedCollectionWithNull ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("captured-null");
            obj.IntProperty = 0;
            var values = new List<int?> { 5, null };
            var objs = new List<TestClass> { obj, null };
            var named = new Dictionary<string, int?> { { "a", null }, { "b", 1 } };
            var strings = new List<string> { "a", null };
            var pairs = new List<Tuple<string, int?>> { Tuple.Create ("a", (int?)null), Tuple.Create ("b", (int?)1) };
            CollectionAssert.AreEqual (
                new int? [] { 5, null },
                Connection.Run (() => testService.EchoListOfNullableInts (values)));
            CollectionAssert.AreEqual (
                new [] { obj, null },
                Connection.Run (() => testService.EchoListOfNullableObjects (objs)));
            Assert.AreEqual (
                1, Connection.Run (() => named ["b"] + obj.IntProperty));
            Assert.IsTrue (
                Connection.Run (() => !named ["a"].HasValue && obj.IntProperty == 0));
            Assert.AreEqual (
                1, Connection.Run (() => strings.Count (x => x == null) + obj.IntProperty));
            Assert.AreEqual (
                1, Connection.Run (() => pairs.Count (x => x.Item2 == null) + obj.IntProperty));
        }

        [Test]
        public void TestNullIntoANonNullablePosition ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("non-nullable-position");
            var exn = Assert.Throws<NullReferenceException> (
                () => Connection.Run (
                    () => testService.NotNullableObject (testService.EchoTestObject (null))));
            StringAssert.Contains ("null", exn.Message);
            exn = Assert.Throws<NullReferenceException> (
                () => Connection.Run (() => testService.EchoListOfNullableObjects (
                    new List<TestClass> { obj, testService.EchoTestObject (null) })
                    .Select (x => testService.NotNullableObject (x)).ToList ()));
            StringAssert.Contains ("null", exn.Message);
        }

        [Test]
        public void TestNullableMembers ()
        {
            var testService = Connection.TestService ();
            Assert.IsFalse (Connection.Run (() => testService.EchoNullableInt (null).HasValue));
            Assert.IsTrue (Connection.Run (() => testService.EchoNullableInt (3).HasValue));
            Assert.AreEqual (3, Connection.Run (() => testService.EchoNullableInt (3).Value));
            Assert.AreEqual (4, Connection.Run (() => testService.EchoNullableInt (3).Value + 1));
            Assert.Throws<NullReferenceException> (
                () => Connection.Run (() => testService.EchoNullableInt (null).Value));
            Assert.AreEqual (0, Connection.Run (
                () => testService.EchoNullableInt (null).GetValueOrDefault ()));
            Assert.AreEqual (3, Connection.Run (
                () => testService.EchoNullableInt (3).GetValueOrDefault ()));
            Assert.AreEqual (7, Connection.Run (
                () => testService.EchoNullableInt (null).GetValueOrDefault (7)));
            Assert.AreEqual (7, Connection.Run (
                () => testService.EchoNullableInt (null).GetValueOrDefault (
                    testService.StringToInt32 ("7"))));
            Assert.AreEqual (3, Connection.Run (
                () => testService.EchoNullableInt (3).GetValueOrDefault (
                    testService.StringToInt32 ("7"))));
        }

        [Test]
        public void TestNullCoalescing ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("coalesce");
            var other = testService.CreateTestObject ("coalesceother");
            Assert.AreEqual (5, Connection.Run (() => testService.EchoNullableInt (null) ?? 5));
            Assert.AreEqual (3, Connection.Run (() => testService.EchoNullableInt (3) ?? 5));
            Assert.AreEqual (3, Connection.Run (
                () => testService.EchoNullableInt (null) ?? testService.EchoNullableInt (3)));
            Assert.AreEqual (other, Connection.Run (() => obj.ObjectProperty ?? other));
            // The left operand is evaluated once
            var count = testService.Counter ("CSharpCompiler.CoalesceLeft", 1);
            Assert.AreEqual (count + 1, Connection.Run (
                () => testService.EchoNullableInt (
                    testService.Counter ("CSharpCompiler.CoalesceLeft", 1)) ?? 0));
            Assert.AreEqual (count + 2, testService.Counter ("CSharpCompiler.CoalesceLeft", 1));
            // The right operand is evaluated only when the left one is null
            Assert.AreEqual (1, Connection.Run (
                () => testService.EchoNullableInt (1) ??
                    testService.Counter ("CSharpCompiler.CoalesceRight", 1)));
            Assert.AreEqual (1, testService.Counter ("CSharpCompiler.CoalesceRight", 1));
        }

        [Test]
        public void TestRunFunctionActionLambda ()
        {
            var testService = Connection.TestService ();
            // The procedure throws once it has been called more than 100 times since a reset
            Assert.Throws<CustomException> (() => {
                for (int i = 0; i < 102; i++)
                    testService.ThrowCustomExceptionLater ();
            });
            Connection.Run (() => testService.ResetCustomExceptionLater ());
            Assert.AreEqual (0, testService.ThrowCustomExceptionLater ());
        }

        [Test]
        public void TestClientSideActionIsRejected ()
        {
            sideEffects = 0;
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Run ((Expression<Action>)(() => SideEffect ())));
            StringAssert.Contains ("makes no remote procedure call", exn.Message);
            Assert.AreEqual (0, sideEffects);
        }

        [Test]
        public void TestRunFunctionSideEffects ()
        {
            var obj = Connection.TestService ().CreateTestObject ("runeffect");
            obj.IntProperty = 1;
            var call = new KRPC.Schema.KRPC.ProcedureCall ();
            call.Service = "TestService";
            call.Procedure = "TestClass_set_IntProperty";
            var expression = Expr.CallWithArguments (
                Connection, call,
                new Dictionary<int, Expr> {
                    { 0, Expr.ConstantObject (Connection, obj.id) },
                    { 1, Expr.ConstantInt (Connection, 42) }
                });
            Connection.Run (expression);
            Assert.AreEqual (42, obj.IntProperty);
        }

        [Test]
        public void TestRunFunctionWithStatements ()
        {
            // Built with the factory API: total = 0; foreach x in [1, 2, 3]: total += x
            var total = Expr.Variable (Connection, "total", KType.Int (Connection));
            var x = Expr.Variable (Connection, "x", KType.Int (Connection));
            var values = Expr.CreateList (
                Connection,
                new List<Expr> {
                    Expr.ConstantInt (Connection, 1),
                    Expr.ConstantInt (Connection, 2),
                    Expr.ConstantInt (Connection, 3)
                });
            var program = Expr.BlockWithVariables (
                Connection,
                new List<Expr> { total, x },
                new List<Expr> {
                    Expr.Assign (Connection, total, Expr.ConstantInt (Connection, 0)),
                    Expr.ForEach (
                        Connection, x, values,
                        Expr.Assign (Connection, total, Expr.Add (Connection, total, x))),
                    total
                });
            Assert.AreEqual (6, Connection.Run<int> (program));
        }

        [Test]
        public void TestCallingAProcedureThatPauses ()
        {
            // The server reports the pause as KRPC.InvalidOperationException
            var testService = Connection.TestService ();
            var exn = Assert.Throws<InvalidOperationException> (
                () => Connection.Run (() => testService.BlockingProcedure (3, 0)));
            StringAssert.Contains ("paused execution", exn.Message);
        }

        [Test]
        public void TestStructField ()
        {
            var testService = Connection.TestService ();
            Assert.AreEqual (
                "CSharpCompiler.Field",
                Evaluate (() => testService.CounterStruct ("CSharpCompiler.Field").StringField));
            Assert.IsTrue (
                Evaluate (() => testService.CounterStruct ("CSharpCompiler.Field2").IntField >= 0));
        }

        [Test]
        public void TestNestedStructField ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("nested");
            var nested = new TestNestedStruct (
                new TestStruct (1, "inner", TestEnum.ValueA, new List<int> { 2 }), obj, "outer");
            Assert.AreEqual (
                "inner",
                Evaluate (() => testService.NestedStructEcho (nested).StructField.StringField));
        }

        [Test]
        public void TestStructOrderingRejected ()
        {
            var testService = Connection.TestService ();
            var value = new TestStruct (1, "order", TestEnum.ValueA, new List<int> ());
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => testService.StructEcho (value) < value));
            StringAssert.Contains ("structures cannot be ordered", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => testService.StructEcho (value) >= value));
            StringAssert.Contains ("structures cannot be ordered", exn.Message);
        }

        [Test]
        public void TestCreateStruct ()
        {
            var testService = Connection.TestService ();
            var value = Evaluate (
                () => testService.StructEcho (
                    new TestStruct (
                        testService.Counter ("CSharpCompiler.Struct", 1),
                        "built", TestEnum.ValueC, new List<int> { 1, 2 })));
            Assert.AreEqual ("built", value.StringField);
            Assert.AreEqual (TestEnum.ValueC, value.EnumField);
            Assert.AreEqual (new List<int> { 1, 2 }, value.ListField);
        }

        [Test]
        public void TestUnsupportedConstructs ()
        {
            var testService = Connection.TestService ();
            // There is no character type in the algebra, so indexing a string in C#,
            // which produces a char, has no equivalent
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => testService.StringProperty [0]));
            StringAssert.Contains ("a string cannot be indexed", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => testService.StringProperty.PadLeft (5)));
            StringAssert.Contains ("'PadLeft' has no server side equivalent", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => Math.Truncate (testService.Counter ("Truncate", 1) * 1.5)));
            StringAssert.Contains ("Math.Truncate has no server side equivalent", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => testService.IncrementList (new List<int> { 1 }).Any ()));
            StringAssert.Contains ("unsupported overload of the collection operation 'Any'", exn.Message);
        }

        [Test]
        public void TestElementSelection ()
        {
            var objects = MakeObjects ("CSharpSelection");
            Assert.AreEqual (1, Evaluate (
                () => objects.Select (x => x.IntProperty).First ()));
            Assert.AreEqual (3, Evaluate (
                () => objects.Select (x => x.IntProperty).Last ()));
            Assert.AreEqual (2, Evaluate (
                () => objects.Select (x => x.IntProperty).ElementAt (1)));
        }

        [Test]
        public void TestReshaping ()
        {
            var objects = MakeObjects ("CSharpReshape");
            CollectionAssert.AreEqual (new List<int> { 3, 2, 1 }, Evaluate (
                () => objects.Select (x => x.IntProperty).Reverse ().ToList ()));
            CollectionAssert.AreEqual (new List<int> { 1, 2, 3 }, Evaluate (
                () => objects.Select (x => x.IntProperty).Distinct ().ToList ()));
            CollectionAssert.AreEqual (new List<int> { 1, 2, 3, 4 }, Evaluate (
                () => objects.Select (x => x.IntProperty)
                    .Union (new List<int> { 3, 4 }).ToList ()));
            CollectionAssert.AreEqual (new List<int> { 3 }, Evaluate (
                () => objects.Select (x => x.IntProperty)
                    .Intersect (new List<int> { 3, 4 }).ToList ()));
            CollectionAssert.AreEqual (new List<int> { 1, 2 }, Evaluate (
                () => objects.Select (x => x.IntProperty)
                    .Except (new List<int> { 3, 4 }).ToList ()));
            CollectionAssert.AreEqual (new List<int> { 11, 22, 33 }, Evaluate (
                () => objects.Select (x => x.IntProperty)
                    .Zip (new List<int> { 10, 20, 30 }, (a, b) => a + b).ToList ()));
        }

        [Test]
        public void TestStringOperations ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "Hello, World";
            Assert.AreEqual (12, Evaluate (() => testService.StringProperty.Length));
            Assert.AreEqual ("World", Evaluate (
                () => testService.StringProperty.Substring (7, 5)));
            Assert.AreEqual ("HELLO, WORLD", Evaluate (
                () => testService.StringProperty.ToUpperInvariant ()));
            Assert.AreEqual ("hello, world", Evaluate (
                () => testService.StringProperty.ToLower (CultureInfo.InvariantCulture)));
            Assert.AreEqual ("Hello, Mars", Evaluate (
                () => testService.StringProperty.Replace ("World", "Mars")));
            Assert.AreEqual (7, Evaluate (
                () => testService.StringProperty.IndexOf ("World", StringComparison.Ordinal)));
            Assert.IsTrue (Evaluate (
                () => testService.StringProperty.Contains ("o, W")));
            Assert.IsTrue (Evaluate (
                () => testService.StringProperty.StartsWith ("Hello", StringComparison.Ordinal)));
            Assert.IsTrue (Evaluate (
                () => testService.StringProperty.EndsWith ("World", StringComparison.Ordinal)));
        }

        [Test]
        public void TestStringOperationsTakingACharacter ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "Hello, World";
            Assert.AreEqual (5, Evaluate (() => testService.StringProperty.IndexOf (',')));
            Assert.AreEqual ("Hello; World", Evaluate (
                () => testService.StringProperty.Replace (',', ';')));
            Assert.IsTrue (Evaluate (() => testService.StringProperty.Contains ('W')));
#if NET
            Assert.IsTrue (Evaluate (() => testService.StringProperty.StartsWith ('H')));
            Assert.IsTrue (Evaluate (() => testService.StringProperty.EndsWith ('d')));
#endif
        }

        [Test]
        public void TestIndexedOverloadsAndJoinOfNonStrings ()
        {
            var testService = Connection.TestService ();
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (
                    () => testService.IncrementList (new List<int> { 1 }).Select ((x, i) => x + i).ToList ()));
            StringAssert.Contains ("taking an element's index", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (
                    () => testService.IncrementList (new List<int> { 1 }).Where ((x, i) => i > 0).ToList ()));
            StringAssert.Contains ("taking an element's index", exn.Message);
            exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (
                    () => string.Join (",", testService.IncrementList (new List<int> { 1, 2 }))));
            StringAssert.Contains ("Select", exn.Message);
            Assert.AreEqual ("2,3", Connection.Run (
                () => string.Join (",", testService.IncrementList (new List<int> { 1, 2 })
                                   .Select (x => x.ToString (CultureInfo.InvariantCulture)))));
        }

        [Test]
        public void TestNullableConversions ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("nullableconversions");
            obj.IntProperty = 4;
            Assert.IsNull (Evaluate (() => (double?)testService.EchoNullableInt (null)));
            Assert.AreEqual (2.0, Evaluate (() => (double?)testService.EchoNullableInt (2)));
            Assert.IsFalse (Evaluate (() => testService.EchoNullableInt (null) == 2.5));
            Assert.AreEqual (5L, Evaluate (() => ((long?)testService.EchoNullableInt (null)) ?? 5L));
            Assert.IsFalse (Evaluate (() => ((long?)testService.EchoNullableInt (null)).HasValue));
            Assert.Throws<NullReferenceException> (
                () => Evaluate (() => (int)testService.EchoNullableInt (null)));
            CollectionAssert.AreEqual (
                new [] { 5 }, Evaluate (() => new List<int> { (int)testService.EchoNullableInt (5) }));
            CollectionAssert.AreEqual (
                new int? [] { 4 }, Evaluate (() => new List<int?> { obj.IntProperty }));
            Assert.AreEqual (
                Tuple.Create ((int?)4, 1), Evaluate (() => Tuple.Create ((int?)obj.IntProperty, 1)));
        }

        [Test]
        public void TestLiftedOperators ()
        {
            var testService = Connection.TestService ();
            Assert.IsTrue (Evaluate (() => testService.EchoNullableInt (7) + 1 > 5));
            Assert.IsTrue (Evaluate (() => -testService.EchoNullableInt (7) < 0));
            Assert.AreEqual (8, Evaluate (() => (testService.EchoNullableInt (7) + 1) ?? 0));
            Assert.IsTrue (Evaluate (() => (testService.EchoNullableInt (7) & 3).HasValue));
            Assert.IsFalse (Evaluate (() => (testService.EchoNullableInt (7) << 1) == null));
            Assert.AreEqual (8, Evaluate (() => (testService.EchoNullableInt (7) + 1).GetValueOrDefault ()));
            Assert.AreEqual ("8", Evaluate (
                () => FormattableString.Invariant ($"{testService.EchoNullableInt (7) + 1}")));
            CollectionAssert.AreEqual (new int? [] { 8 }, Evaluate (
                () => new List<int?> { testService.EchoNullableInt (7) + 1 }));
            Assert.Throws<NullReferenceException> (
                () => Evaluate (() => testService.EchoNullableInt (null) + 1));
        }

        [Test]
        public void TestNullCoalescingWidens ()
        {
            var testService = Connection.TestService ();
            Assert.AreEqual (7L, Evaluate (() => testService.EchoNullableInt (7) ?? 5L));
            Assert.AreEqual (5L, Evaluate (() => testService.EchoNullableInt (null) ?? 5L));
            CollectionAssert.AreEqual (new [] { 7L, 5L }, Evaluate (() => new List<long> {
                testService.EchoNullableInt (7) ?? 5L, testService.EchoNullableInt (null) ?? 5L }));
        }

        [Test]
        public void TestNullCoalescingOfAClientSideValueWidens ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("coalescewidens");
            obj.IntProperty = 2;
            int? value = 5;
            Assert.AreEqual (5L, Evaluate (() => value ?? (long)obj.IntProperty));
            Assert.AreEqual (6L, Evaluate (() => (value ?? (long)obj.IntProperty) + 1L));
            Assert.AreEqual (5.0, Evaluate (() => value ?? obj.IntProperty * 1.5));
            value = null;
            Assert.AreEqual (3.0, Evaluate (() => value ?? obj.IntProperty * 1.5));
        }

        [Test]
        public void TestNullableElementTypes ()
        {
            var testService = Connection.TestService ();
            var values = new List<int?> { 1, null, 3 };
            var noNulls = new List<int?> { 5, 6 };
            var empty = new List<int?> ();
            CollectionAssert.AreEqual (new [] { 1, 0, 3 }, Evaluate (
                () => testService.EchoListOfNullableInts (values).Select (x => x ?? 0).ToList ()));
            CollectionAssert.AreEqual (new int? [] { 5, 6 }, Evaluate (
                () => testService.EchoListOfNullableInts (noNulls).ToList ()));
            Assert.AreEqual (0, Evaluate (() => testService.EchoListOfNullableInts (empty).Count));
        }

        [Test]
        public void TestLiftedComparisonEvaluatesOnce ()
        {
            var testService = Connection.TestService ();
            var count = testService.Counter ("CSharpCompiler.Lifted", 1);
            Assert.IsTrue (Evaluate (() => (int?)testService.Counter ("CSharpCompiler.Lifted", 1) > 0));
            Assert.IsFalse (Evaluate (
                () => testService.EchoNullableInt (null) > (int?)testService.Counter ("CSharpCompiler.Lifted", 1)));
            Assert.AreEqual (count + 3, testService.Counter ("CSharpCompiler.Lifted", 1));
        }

        [Test]
        public void TestShortCircuitWithAFailingClientSideOperand ()
        {
            var obj = Connection.TestService ().CreateTestObject ("shortcircuit");
            var empty = new List<int> ();
            obj.IntProperty = -1;
            Assert.IsFalse (Evaluate (() => obj.IntProperty > 0 && empty [0] > 1));
            Assert.IsTrue (Evaluate (() => obj.IntProperty < 0 || empty [0] > 1));
            obj.IntProperty = 1;
            var exn = Assert.Throws<InvalidOperationException> (
                () => Evaluate (() => obj.IntProperty > 0 && empty [0] > 1));
            StringAssert.Contains ("Error evaluating client side sub-expression", exn.Message);
        }

        [Test]
        public void TestFailingClientSideSubexpressionOfABranch ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("nestedfailure");
            var empty = new List<int> ();
            var bad = "x";
            obj.IntProperty = -1;
            Assert.IsFalse (Evaluate (
                () => obj.IntProperty > 0 && testService.IncrementList (new List<int> { 1 }) [0] > empty [0]));
            Assert.AreEqual (1, Evaluate (() => obj.IntProperty > 0 ? obj.IntProperty + int.Parse (bad) : 1));
            obj.IntProperty = 1;
            var exn = Assert.Throws<InvalidOperationException> (() => Evaluate (
                () => obj.IntProperty > 0 && testService.IncrementList (new List<int> { 1 }) [0] > empty [0]));
            StringAssert.Contains ("Error evaluating client side sub-expression", exn.Message);
            exn = Assert.Throws<InvalidOperationException> (
                () => Evaluate (() => obj.IntProperty > 0 ? obj.IntProperty + int.Parse (bad) : 1));
            StringAssert.Contains ("Error evaluating client side sub-expression", exn.Message);
        }

        [Test]
        public void TestMemberOfAClientSideBranch ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "remote";
            var flag = true;
            var local = "ab";
            Assert.AreEqual (2, Evaluate (() => (flag ? local : testService.StringProperty).Length));
        }

        [Test]
        public void TestNullElementsInAJoinAndAConcat ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "-";
            var strings = new List<string> { "a", null, "b" };
            Assert.AreEqual ("a--b", Evaluate (() => string.Join (testService.StringProperty, strings)));
            Assert.AreEqual ("-", Evaluate (
                () => string.Concat (new List<string> { testService.StringProperty, null })));
            Assert.AreEqual ("ab-", Evaluate (
                () => string.Concat (strings) + testService.StringProperty));
        }

        [Test]
        public void TestStringConcatOfACollection ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "e";
            Assert.AreEqual ("23", Evaluate (() => string.Concat (
                testService.IncrementList (new List<int> { 1, 2 })
                    .Select (x => x.ToString (CultureInfo.InvariantCulture)))));
            Assert.AreEqual ("abcde", Evaluate (
                () => string.Concat ("a", "b", "c", "d", testService.StringProperty)));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (
                    () => string.Concat (testService.IncrementList (new List<int> { 1 }))));
        }

        [Test]
        public void TestCollectionInitializers ()
        {
            var obj = Connection.TestService ().CreateTestObject ("initializers");
            obj.IntProperty = 2;
            var captured = new List<int> { 1 };
            CollectionAssert.AreEquivalent (
                new [] { 2, 3 }, Evaluate (() => new HashSet<int> { obj.IntProperty, 3, obj.IntProperty }));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => new List<int> (captured) { obj.IntProperty }));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => new SortedSet<int> { obj.IntProperty }));
        }

        [Test]
        public void TestDictionaryInitializerOrderAndRepeatedKeys ()
        {
            var testService = Connection.TestService ();
            var id = "CSharpCompiler.DictionaryInit";
            var count = testService.Counter (id, 1);
            var result = Evaluate (() => new Dictionary<int, int> {
                { testService.Counter (id, 1), testService.Counter (id, 1) },
                { testService.Counter (id, 1), testService.Counter (id, 1) }
            });
            Assert.AreEqual (count + 2, result [count + 1]);
            Assert.AreEqual (count + 4, result [count + 3]);
            testService.StringProperty = "key";
            Assert.Throws<ArgumentException> (() => Evaluate (() => new Dictionary<string, int> {
                { testService.StringProperty, 1 }, { testService.StringProperty, 2 }
            }));
        }

        [Test]
        public void TestFormatEvaluatesArgumentsOnceInOrder ()
        {
            var testService = Connection.TestService ();
            var id = "CSharpCompiler.Format";
            var count = testService.Counter (id, 1);
            var expected = string.Format (CultureInfo.InvariantCulture, "{0} {0}", count + 1);
            Assert.AreEqual (expected, Evaluate (() => string.Format (
                CultureInfo.InvariantCulture, "{0} {0}", testService.Counter (id, 1))));
            expected = string.Format (CultureInfo.InvariantCulture, "{1} {0}", count + 2, count + 3);
            Assert.AreEqual (expected, Evaluate (() => string.Format (
                CultureInfo.InvariantCulture, "{1} {0}", testService.Counter (id, 1), testService.Counter (id, 1))));
            Assert.AreEqual ("x", Evaluate (() => string.Format (
                CultureInfo.InvariantCulture, "x", testService.Counter (id, 1))));
            Assert.AreEqual (count + 5, testService.Counter (id, 1));
        }

        [Test]
        public void TestCultureSensitiveCalls ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("culture");
            var exn = Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => $"{obj.IntProperty}"));
            StringAssert.Contains ("culture", exn.Message);
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => "n=" + obj.IntProperty));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => obj.IntProperty.ToString ()));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => testService.StringProperty.ToUpper ()));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (() => testService.StringProperty.IndexOf ("a")));
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (
                    () => testService.StringProperty.StartsWith ("a", StringComparison.CurrentCulture)));
            var values = new List<int> { 8, 9, 99 };
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (
                    () => testService.IncrementList (values).Select (x => testService.Int32ToString (x))
                        .OrderBy (x => x).ToList ()));
            CollectionAssert.AreEqual (new [] { "10", "100", "9" }, Evaluate (
                () => testService.IncrementList (values).Select (x => testService.Int32ToString (x))
                    .OrderBy (x => x, StringComparer.Ordinal).ToList ()));
            obj.IntProperty = 2;
            Assert.AreEqual ("2.5", Evaluate (
                () => FormattableString.Invariant ($"{obj.IntProperty + 0.5}")));
        }

        [Test]
        public void TestTuplesOfMoreThanSevenValues ()
        {
            var obj = Connection.TestService ().CreateTestObject ("bigtuple");
            Assert.Throws<FunctionCompilationException> (
                () => Connection.Compile (
                    () => Tuple.Create (obj.IntProperty, 2, 3, 4, 5, 6, 7, 8)));
        }

        [Test]
        public void TestMoreCollectionOperations ()
        {
            var testService = Connection.TestService ();
            var objs = MakeObjects ("morecollections");
            CollectionAssert.AreEqual (new [] { 1, 1, 2, 2, 3, 3 }, Evaluate (
                () => objs.SelectMany (o => new List<int> { o.IntProperty, o.IntProperty }).ToList ()));
            CollectionAssert.AreEquivalent (new [] { 1 }, Evaluate (
                () => objs.Select (o => o.IntProperty / 4 + 1).ToHashSet ()));
            Assert.AreEqual (2.0, Evaluate (() => objs.Select (o => o.IntProperty).Average ()));
            var obj = objs [2];
            Assert.AreEqual (12, Evaluate (() => obj.IntProperty << 2));
            Assert.AreEqual (1, Evaluate (() => obj.IntProperty >> 1));
            Assert.AreEqual (2, Evaluate (() => obj.IntProperty ^ 1));
        }

        [Test]
        public void TestStringTrimming ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "  pad  ";
            Assert.AreEqual ("pad", Evaluate (() => testService.StringProperty.Trim ()));
            Assert.AreEqual ("pad  ", Evaluate (
                () => testService.StringProperty.TrimStart ()));
            Assert.AreEqual ("  pad", Evaluate (
                () => testService.StringProperty.TrimEnd ()));
        }

        [Test]
        public void TestLambdaWithParameters ()
        {
            var obj = Connection.TestService ().CreateTestObject ("parameters");
            obj.IntProperty = 3;
            var scaled = Connection.Compile ((TestClass target, double factor) => target.IntProperty * factor);
            Assert.AreEqual (6.0, Connection.Run<double> (scaled, obj, 2.0));
            Assert.AreEqual (1.5, Connection.Run<double> (scaled, obj, 0.5));
            var summary = Connection.Compile (
                (IList<int> values, string label, int offset, bool negate) =>
                    label + (negate ? -(values.Sum () + offset) : values.Sum () + offset)
                        .ToString (CultureInfo.InvariantCulture));
            Assert.AreEqual ("total7", Connection.Run<string> (
                summary, new List<int> { 1, 2, 3 }, "total", 1, false));
            Assert.AreEqual ("x-6", Connection.Run<string> (
                summary, new List<int> { 1, 2, 3 }, "x", 0, true));
        }

        [Test]
        public void TestLambdaWithParametersAndNoValue ()
        {
            var testService = Connection.TestService ();
            var count = Connection.Compile<string> (id => testService.Counter (id, 1));
            var before = testService.Counter ("TestLambdaWithParametersAndNoValue");
            Connection.Run (count, "TestLambdaWithParametersAndNoValue");
            Connection.Run (count, "TestLambdaWithParametersAndNoValue");
            Assert.AreEqual (before + 3, testService.Counter ("TestLambdaWithParametersAndNoValue"));
        }
    }
}
