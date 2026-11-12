using System.Collections.Generic;
using System.Linq;
using KRPC.Service.KRPC;
using NUnit.Framework;

namespace KRPC.Test.Service.KRPC
{
    public partial class ExpressionTest
    {
        delegate Expression CaseStatement (
            Expression value, IList<IList<Expression>> cases, IList<Expression> bodies);

        static readonly CaseStatement[] caseStatements = {
            Expression.Match, Expression.Switch
        };

        static IList<Expression> TestValues (params int[] values)
        {
            return values.Select (x => Expression.ConstantInt (x)).ToList ();
        }

        // Appends a string to the trace variable
        static Expression Append (Expression trace, string value)
        {
            return Expression.Assign (trace, Expression.StringConcat (
                new List<Expression> { trace, Expression.ConstantString (value) }));
        }

        // Evaluates statements in a block with a trace variable, and returns the trace
        static string Trace (System.Func<Expression, IList<Expression>> statements)
        {
            var trace = Expression.Variable ("trace", Type.String ());
            var all = new List<Expression> { Expression.Assign (trace, Expression.ConstantString ("")) };
            all.AddRange (statements (trace));
            all.Add (trace);
            return Eval<string> (Expression.BlockWithVariables (new List<Expression> { trace }, all));
        }

        // A match or switch over the value with cases 1, the default, 2 and 1 again. Each body
        // appends its letter
        static string TraceFourCases (CaseStatement factory, int value)
        {
            return Trace (trace => new List<Expression> {
                factory (
                    Expression.ConstantInt (value),
                    new List<IList<Expression>> { TestValues (1), TestValues (), TestValues (2, 3), TestValues (1) },
                    new List<Expression> {
                        Append (trace, "a"), Append (trace, "d"), Append (trace, "b"), Append (trace, "c")
                    })
            });
        }

        [Test]
        public void MatchRunsTheFirstMatchingCase ()
        {
            Assert.AreEqual ("a", TraceFourCases (Expression.Match, 1));
            Assert.AreEqual ("b", TraceFourCases (Expression.Match, 3));
            Assert.AreEqual ("d", TraceFourCases (Expression.Match, 4));
        }

        [Test]
        public void SwitchRunsFromTheFirstMatchingCase ()
        {
            Assert.AreEqual ("adbc", TraceFourCases (Expression.Switch, 1));
            Assert.AreEqual ("bc", TraceFourCases (Expression.Switch, 3));
            Assert.AreEqual ("dbc", TraceFourCases (Expression.Switch, 4));
        }

        // A match or switch over the value with the given cases, whose bodies append "a", "b"
        // and "c"
        static string TraceCases (CaseStatement factory, int value, params IList<Expression>[] cases)
        {
            return Trace (trace => new List<Expression> {
                factory (
                    Expression.ConstantInt (value), cases,
                    new List<Expression> { Append (trace, "a"), Append (trace, "b"), Append (trace, "c") })
            });
        }

        [Test]
        public void MatchAndSwitchWithTheDefaultFirst ()
        {
            Assert.AreEqual ("b", TraceCases (Expression.Match, 1, TestValues (), TestValues (1), TestValues (2)));
            Assert.AreEqual ("a", TraceCases (Expression.Match, 5, TestValues (), TestValues (1), TestValues (2)));
            Assert.AreEqual ("bc", TraceCases (
                Expression.Switch, 1, TestValues (), TestValues (1), TestValues (2)));
            Assert.AreEqual ("abc", TraceCases (
                Expression.Switch, 5, TestValues (), TestValues (1), TestValues (2)));
        }

        [Test]
        public void MatchAndSwitchWithTheDefaultLast ()
        {
            Assert.AreEqual ("b", TraceCases (Expression.Match, 2, TestValues (1), TestValues (2), TestValues ()));
            Assert.AreEqual ("c", TraceCases (Expression.Match, 5, TestValues (1), TestValues (2), TestValues ()));
            Assert.AreEqual ("abc", TraceCases (
                Expression.Switch, 1, TestValues (1), TestValues (2), TestValues ()));
            Assert.AreEqual ("c", TraceCases (
                Expression.Switch, 5, TestValues (1), TestValues (2), TestValues ()));
        }

        [Test]
        public void MatchAndSwitchWithNoMatchAndNoDefault ()
        {
            foreach (var factory in caseStatements)
                Assert.AreEqual ("", Trace (trace => new List<Expression> {
                    factory (
                        Expression.ConstantInt (5),
                        new List<IList<Expression>> { TestValues (1), TestValues (2) },
                        new List<Expression> { Append (trace, "a"), Append (trace, "b") })
                }));
        }

        [Test]
        public void MatchAndSwitchWithNoCases ()
        {
            foreach (var factory in caseStatements)
                Assert.AreEqual ("v", Trace (trace => new List<Expression> {
                    factory (
                        Expression.Block (new List<Expression> { Append (trace, "v"), Expression.ConstantInt (1) }),
                        new List<IList<Expression>> (), new List<Expression> ())
                }));
        }

        [Test]
        public void MatchAndSwitchEvaluateTheValueOnceAndTestValuesLazily ()
        {
            // Each value appends its name before giving its number
            var expected = new [] { "v12a", "v12ab" };
            for (int i = 0; i < 2; i++)
                Assert.AreEqual (expected [i], Trace (trace => {
                    System.Func<string, int, Expression> traced = (name, number) =>
                        Expression.Block (new List<Expression> {
                            Append (trace, name), Expression.ConstantInt (number)
                        });
                    return new List<Expression> {
                        caseStatements [i] (
                            traced ("v", 2),
                            new List<IList<Expression>> {
                                new List<Expression> { traced ("1", 1), traced ("2", 2), traced ("3", 3) },
                                new List<Expression> { traced ("4", 2) }
                            },
                            new List<Expression> { Append (trace, "a"), Append (trace, "b") })
                    };
                }));
        }

        [Test]
        public void MatchAndSwitchCompareAsEqual ()
        {
            // An integer value matches a double test value, and strings compare by value
            foreach (var factory in caseStatements) {
                Assert.AreEqual ("a", Trace (trace => new List<Expression> {
                    factory (
                        Expression.ConstantInt (2),
                        new List<IList<Expression>> { new List<Expression> { Expression.ConstantDouble (2) } },
                        new List<Expression> { Append (trace, "a") })
                }));
                Assert.AreEqual ("a", Trace (trace => new List<Expression> {
                    factory (
                        Expression.StringConcat (new List<Expression> {
                            Expression.ConstantString ("f"), Expression.ConstantString ("oo")
                        }),
                        new List<IList<Expression>> { new List<Expression> { Expression.ConstantString ("foo") } },
                        new List<Expression> { Append (trace, "a") })
                }));
            }
        }

        [Test]
        public void MatchAndSwitchMatchANullValue ()
        {
            foreach (var factory in caseStatements)
                Assert.AreEqual ("n", Trace (trace => new List<Expression> {
                    factory (
                        Expression.ConstantNull (Type.Int ()),
                        new List<IList<Expression>> {
                            TestValues (1), new List<Expression> { Expression.ConstantNull (Type.Int ()) }
                        },
                        new List<Expression> { Append (trace, "a"), Append (trace, "n") })
                }));
        }

        // A loop over 1 to 3 holding a match or switch on the loop variable, with a body for 2
        // that runs the given statement, then appends "x". The loop appends each value after
        // the match or switch
        static string TraceLoopWithCases (CaseStatement factory, System.Func<Expression, Expression> statement)
        {
            var x = Expression.Variable ("x", Type.Int ());
            var trace = Expression.Variable ("trace", Type.String ());
            var expr = Expression.BlockWithVariables (
                new List<Expression> { trace, x },
                new List<Expression> {
                    Expression.Assign (trace, Expression.ConstantString ("")),
                    Expression.ForEach (x, Expression.CreateList (TestValues (1, 2, 3)), Expression.Block (new List<Expression> {
                        factory (
                            x,
                            new List<IList<Expression>> { TestValues (2), TestValues () },
                            new List<Expression> {
                                Expression.Block (new List<Expression> { statement (trace), Append (trace, "x") }),
                                Append (trace, "d")
                            }),
                        Expression.Assign (trace, Expression.StringConcat (
                            new List<Expression> { trace, Expression.ConvertToString (x) }))
                    })),
                    trace
                });
            return Eval<string> (expr);
        }

        [Test]
        public void BreakInAMatchEndsTheLoop ()
        {
            Assert.AreEqual ("d1", TraceLoopWithCases (Expression.Match, trace => Expression.Break ()));
        }

        [Test]
        public void BreakInASwitchEndsTheSwitch ()
        {
            Assert.AreEqual ("d12d3", TraceLoopWithCases (
                Expression.Switch, trace => Expression.Break ()));
        }

        [Test]
        public void ContinueInAMatchOrSwitchSkipsToTheNextIteration ()
        {
            foreach (var factory in caseStatements)
                Assert.AreEqual ("d1d3", TraceLoopWithCases (factory, trace => Expression.Continue ()));
        }

        static Expression LoopThatBreaks (Expression trace)
        {
            return Expression.While (Expression.ConstantBool (true), Expression.Block (
                new List<Expression> { Append (trace, "i"), Expression.Break () }));
        }

        [Test]
        public void BreakInALoopInAMatchOrSwitchEndsThatLoop ()
        {
            Assert.AreEqual ("d1ix2d3", TraceLoopWithCases (Expression.Match, LoopThatBreaks));
            Assert.AreEqual ("d1ixd2d3", TraceLoopWithCases (Expression.Switch, LoopThatBreaks));
        }

        [Test]
        public void ReturnInAMatchOrSwitchEndsTheFunction ()
        {
            foreach (var factory in caseStatements) {
                var x = Expression.Parameter ("x", Type.Int ());
                var function = Expression.Lambda (
                    new List<Expression> { x },
                    Expression.Block (new List<Expression> {
                        factory (
                            x,
                            new List<IList<Expression>> { TestValues (1) },
                            new List<Expression> { Expression.Return (Expression.ConstantString ("one")) }),
                        Expression.ConstantString ("other")
                    }));
                foreach (var pair in new [] { System.Tuple.Create (1, "one"), System.Tuple.Create (2, "other") })
                    Assert.AreEqual (pair.Item2, Eval<string> (Expression.Invoke (
                        function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (pair.Item1) } })));
            }
        }

        [Test]
        public void BreakInAMatchOutsideALoopIsRejected ()
        {
            var inMatch = StatementWithMarker (Expression.Match (
                Expression.ConstantInt (1),
                new List<IList<Expression>> { TestValues (1) },
                new List<Expression> { Expression.Break () }));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.RunFunction (inMatch));
            Assert.AreEqual ("A break statement must be inside a loop or a switch", exn.Message);
            var inSwitch = StatementWithMarker (Expression.Switch (
                Expression.ConstantInt (1),
                new List<IList<Expression>> { TestValues (1) },
                new List<Expression> { Expression.Break () }));
            Assert.DoesNotThrow (() => global::KRPC.Service.KRPC.KRPC.RunFunction (inSwitch));
        }

        [Test]
        public void InvalidMatchesAndSwitches ()
        {
            var value = Expression.ConstantInt (1);
            var body = Expression.Block (new List<Expression> { Expression.ConstantInt (0) });
            foreach (var factory in caseStatements) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (() => factory (
                    value, new List<IList<Expression>> { TestValues (1), TestValues (2) }, new List<Expression> { body }));
                StringAssert.Contains ("one body for each case", exn.Message);
                exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (() => factory (
                    value, new List<IList<Expression>> { TestValues (), TestValues () }, new List<Expression> { body, body }));
                StringAssert.Contains ("at most one default case", exn.Message);
                Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (() => factory (
                    value,
                    new List<IList<Expression>> { new List<Expression> { Expression.ConstantString ("a") } },
                    new List<Expression> { body }));
                Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (() => factory (
                    Expression.Continue (), new List<IList<Expression>> (), new List<Expression> ()));
                Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (() => factory (
                    value, new List<IList<Expression>> { null }, new List<Expression> { body }));
                Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (() => factory (
                    value, new List<IList<Expression>> { new List<Expression> { null } }, new List<Expression> { body }));
                Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (() => factory (
                    value, new List<IList<Expression>> { TestValues (1) }, new List<Expression> { null }));
            }
        }
    }
}
