using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Service.KRPC;
using NUnit.Framework;
using Type = KRPC.Service.KRPC.Type;

namespace KRPC.Test.Service.KRPC
{
    public partial class ExpressionTest
    {
        // A value a parameter can take, with a null of its type and with a null nested in it
        sealed class Sample
        {
            public string Name;
            public Func<Expression> Make;
            public Func<Expression> NullOf;
            public Func<Expression> Nested;
        }

        static List<Expression> Values (params Expression[] values)
        {
            return values.ToList ();
        }

        static List<Sample> Samples ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            var s = Expression.Parameter ("s", Type.String ());
            Func<int, Expression> i = Expression.ConstantInt;
            Func<string, Expression> str = Expression.ConstantString;
            return new List<Sample> {
                new Sample { Name = "int", Make = () => i (2), NullOf = () => Expression.ConstantNull (Type.Int ()) },
                new Sample { Name = "zero", Make = () => i (0), NullOf = () => Expression.ConstantNull (Type.Int ()) },
                new Sample { Name = "int?", Make = () => Expression.Cast (i (1), Type.NullableType (Type.Int ())) },
                new Sample { Name = "double", Make = () => Expression.ConstantDouble (1.5),
                    NullOf = () => Expression.ConstantNull (Type.Double ()) },
                new Sample { Name = "bool", Make = () => Expression.ConstantBool (true),
                    NullOf = () => Expression.ConstantNull (Type.Bool ()) },
                new Sample { Name = "string", Make = () => str ("ab"), NullOf = () => Expression.ConstantNull (Type.String ()) },
                new Sample { Name = "list<int>", Make = () => Expression.CreateList (Values (i (1), i (2))),
                    NullOf = () => Expression.ConstantNull (Type.ListType (Type.Int ())),
                    Nested = () => Expression.CreateList (Values (i (1), Expression.ConstantNull (Type.Int ()))) },
                new Sample { Name = "list<string>", Make = () => Expression.CreateList (Values (str ("a"), str ("b"))),
                    NullOf = () => Expression.ConstantNull (Type.ListType (Type.String ())),
                    Nested = () => Expression.CreateList (Values (str ("a"), Expression.ConstantNull (Type.String ()))) },
                new Sample { Name = "set<int>", Make = () => Expression.CreateSet (new HashSet<Expression> { i (1), i (2) }),
                    NullOf = () => Expression.ConstantNull (Type.SetType (Type.Int ())) },
                new Sample { Name = "dict<string,int>", Make = () => Expression.CreateDictionary (Values (str ("a")), Values (i (1))),
                    NullOf = () => Expression.ConstantNull (Type.DictionaryType (Type.String (), Type.Int ())),
                    Nested = () => Expression.CreateDictionary (Values (str ("a")), Values (Expression.ConstantNull (Type.Int ()))) },
                new Sample { Name = "tuple<int,string>", Make = () => Expression.CreateTuple (Values (i (1), str ("a"))),
                    NullOf = () => Expression.ConstantNull (Type.TupleType (new List<Type> { Type.Int (), Type.String () })),
                    Nested = () => Expression.CreateTuple (Values (
                        Expression.ConstantNull (Type.Int ()), Expression.ConstantNull (Type.String ()))) },
                new Sample { Name = "bytes", Make = () => Expression.ConstantBytes (new byte [] { 1 }),
                    NullOf = () => Expression.ConstantNull (Type.Bytes ()) },
                new Sample { Name = "x => x", Make = () => Expression.Lambda (Values (x), x) },
                new Sample { Name = "x => x > 0", Make = () => Expression.Lambda (Values (x), Expression.GreaterThan (x, i (0))) },
                new Sample { Name = "(x, y) => x + y", Make = () => Expression.Lambda (Values (x, y), Expression.Add (x, y)) },
                new Sample { Name = "x => [x]", Make = () => Expression.Lambda (Values (x), Expression.CreateList (Values (x))) },
                new Sample { Name = "s => s", Make = () => Expression.Lambda (Values (s), s) },
            };
        }

        // The values tried for each type of parameter. An Expression parameter takes a sample
        static List<object> Choices (System.Type type, List<Sample> samples)
        {
            Func<Func<object>, object> lazy = x => x;
            if (type == typeof (Expression))
                return samples.Cast<object> ().ToList ();
            if (type == typeof (IList<Expression>))
                return new List<object> {
                    lazy (() => Values (Expression.ConstantInt (1), Expression.ConstantInt (2))),
                    lazy (() => Values ()), lazy (() => Values (Expression.ConstantString ("a"))) };
            if (type == typeof (HashSet<Expression>))
                return new List<object> { lazy (() => new HashSet<Expression> { Expression.ConstantInt (1) }) };
            if (type == typeof (IDictionary<string, Expression>))
                return new List<object> {
                    lazy (() => new Dictionary<string, Expression> ()),
                    lazy (() => new Dictionary<string, Expression> { { "x", Expression.ConstantInt (2) } }),
                    lazy (() => new Dictionary<string, Expression> { { "s", Expression.ConstantString ("a") } }) };
            if (type == typeof (string))
                return new List<object> { "KRPC", "InvalidOperationException", "x" };
            if (type == typeof (Type))
                return new List<object> {
                    lazy (Type.Int), lazy (Type.String), lazy (() => Type.ListType (Type.Int ())) };
            if (type == typeof (bool))
                return new List<object> { true };
            if (type == typeof (int))
                return new List<object> { 1 };
            if (type == typeof (long))
                return new List<object> { 1L };
            if (type == typeof (uint))
                return new List<object> { 1u };
            if (type == typeof (ulong))
                return new List<object> { 1ul };
            if (type == typeof (float))
                return new List<object> { 1f };
            if (type == typeof (double))
                return new List<object> { 1.0 };
            if (type == typeof (byte[]))
                return new List<object> { new byte [] { 1 } };
            return null;
        }

        static object Realize (object choice)
        {
            var sample = choice as Sample;
            if (sample != null)
                return sample.Make ();
            var make = choice as Func<object>;
            return make != null ? make () : choice;
        }

        static IEnumerable<object[]> Combinations (IList<List<object>> choices, int index, object[] current)
        {
            if (index == choices.Count) {
                yield return (object[])current.Clone ();
                yield break;
            }
            foreach (var choice in choices [index]) {
                current [index] = choice;
                foreach (var combination in Combinations (choices, index + 1, current))
                    yield return combination;
            }
        }

        // Builds the expression, and evaluates it when asked. Gives the exception thrown, if any
        static System.Exception Attempt (MethodInfo method, object[] args, bool evaluate)
        {
            try {
                var expression = (Expression)method.Invoke (null, args);
                if (evaluate) {
                    expression.Check ();
                    expression.Compile ();
                    expression.Evaluate ();
                }
                return null;
            } catch (TargetInvocationException e) {
                return e.InnerException;
            } catch (System.Exception e) {
                return e;
            }
        }

        // Valid arguments for the RPCs the samples cannot build, which the RPCs are built
        // with but not evaluated
        static readonly IDictionary<string, Func<object[]>> explicitArguments = new Dictionary<string, Func<object[]>> {
            { "Assign", () => new object [] { Expression.Variable ("v", Type.Int ()), Expression.ConstantInt (1) } },
            { "ForEach", () => new object [] {
                Expression.Variable ("v", Type.Int ()),
                Expression.CreateList (Values (Expression.ConstantInt (1))), Expression.Continue () } },
            { "TryCatch", () => new object [] {
                Expression.ConstantInt (1), "KRPC", "InvalidOperationException", null, Expression.ConstantInt (2) } },
            { "TryCatchAll", () => new object [] { Expression.ConstantInt (1), null, Expression.ConstantInt (2) } },
        };

        // RPCs whose valid arguments name a procedure or a structure type, covered by their own tests
        static readonly ISet<string> notSampled = new HashSet<string> {
            "Call", "CallWithArguments", "ConstantEnum", "CreateStruct", "GetField"
        };

        [Test]
        public void NullsGiveKRPCExceptions ()
        {
            var samples = Samples ();
            var failures = new List<string> ();
            var methods = typeof (Expression).GetMethods (BindingFlags.Public | BindingFlags.Static)
                .Where (x => x.GetCustomAttributes (typeof (KRPCMethodAttribute), false).Any ())
                .Where (x => !notSampled.Contains (x.Name));
            foreach (var method in methods) {
                var parameters = method.GetParameters ();
                object[] found = null;
                var evaluate = !explicitArguments.ContainsKey (method.Name) && method.Name != "While";
                if (explicitArguments.ContainsKey (method.Name)) {
                    found = explicitArguments [method.Name] ();
                } else {
                    var choices = parameters.Select (x => Choices (x.ParameterType, samples)).ToList ();
                    Assert.IsFalse (choices.Contains (null), method.Name + " takes a parameter with no choices");
                    foreach (var pass in new [] { evaluate, false }) {
                        found = Combinations (choices, 0, new object [choices.Count])
                            .FirstOrDefault (x => Attempt (method, x.Select (Realize).ToArray (), pass) == null);
                        if (found != null) {
                            evaluate = pass;
                            break;
                        }
                    }
                    Assert.IsNotNull (found, "No valid arguments found for " + method.Name);
                }
                Func<int, object, string, string> check = (index, value, variant) => {
                    var args = found.Select (Realize).ToArray ();
                    args [index] = value;
                    var thrown = Attempt (method, args, evaluate);
                    if (thrown == null)
                        return variant == "null" && !parameters [index].IsDefined (typeof (KRPCNullableAttribute))
                            ? "accepted a null" : null;
                    return thrown.GetType ().Namespace == typeof (Expression).Namespace
                        ? null : thrown.GetType () + ": " + thrown.Message;
                };
                for (int i = 0; i < parameters.Length; i++) {
                    var results = new List<Tuple<string, string>> ();
                    if (!parameters [i].ParameterType.IsValueType)
                        results.Add (System.Tuple.Create ("null", check (i, null, "null")));
                    var sample = found [i] as Sample;
                    if (sample != null && sample.NullOf != null)
                        results.Add (System.Tuple.Create ("a null " + sample.Name, check (i, sample.NullOf (), "value")));
                    if (sample != null && sample.Nested != null)
                        results.Add (System.Tuple.Create ("a nested null", check (i, sample.Nested (), "value")));
                    failures.AddRange (results.Where (x => x.Item2 != null).Select (
                        x => method.Name + " given " + x.Item1 + " for " + parameters [i].Name + ": " + x.Item2));
                }
            }
            CollectionAssert.IsEmpty (failures);
        }
    }
}
