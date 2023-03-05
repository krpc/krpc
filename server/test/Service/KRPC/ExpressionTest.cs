using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.KRPC;
using NUnit.Framework;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Test.Service.KRPC
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class ExpressionTest
    {
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidMethodWithUnusedGenericTypeRule")]
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

        [Test]
        public void Call ()
        {
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
