using System.Collections.Generic;
using System.Globalization;
using KRPC.Service.KRPC;
using NUnit.Framework;

namespace KRPC.Test.Service.KRPC
{
    public partial class ExpressionTest
    {
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
            Assert.Throws<System.ArgumentException> (() => Eval<string> (Expression.StringReplace (
                s, Expression.ConstantString (string.Empty), Expression.ConstantString ("Mars"))));
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
        public void StringGetIndexOutOfRange ()
        {
            foreach (var index in new [] { -1, 1 })
                Assert.Throws<global::KRPC.Service.KRPC.IndexOutOfRangeException> (
                    () => Eval<string> (Expression.StringGet (
                        Expression.ConstantString ("a"), Expression.ConstantInt (index))));
        }

        [Test]
        public void StringSubstringOutOfRange ()
        {
            Assert.AreEqual ("", Eval<string> (Expression.StringSubstring (
                Expression.ConstantString ("ab"), Expression.ConstantInt (2), Expression.ConstantInt (0))));
            foreach (var range in new [] { new [] { 1, 2 }, new [] { -1, 1 }, new [] { 0, -1 }, new [] { 3, 0 } }) {
                var exn = Assert.Throws<global::KRPC.Service.KRPC.IndexOutOfRangeException> (
                    () => Eval<string> (Expression.StringSubstring (
                        Expression.ConstantString ("ab"), Expression.ConstantInt (range [0]),
                        Expression.ConstantInt (range [1]))));
                StringAssert.Contains ("out of range for a string of 2 characters", exn.Message);
            }
        }

        [Test]
        public void ConvertToStringOfAStatement ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConvertToString (Expression.Call (BuildProcedureCall ("ProcedureNoArgsNoReturn"))));
            Assert.AreEqual ("The argument of ConvertToString is an expression with no value", exn.Message);
        }

        [Test]
        public void ConvertToStringOfANull ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (Expression.ConvertToString (Expression.ConstantNull (Type.Int ()))));
            StringAssert.Contains ("The argument of ConvertToString is null", exn.Message);
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (Expression.ConvertToString (NullableString (null))));
            Assert.AreEqual ("1", Eval<string> (Expression.ConvertToString (
                Expression.Cast (Expression.ConstantInt (1), Type.NullableType (Type.Int ())))));
            Assert.AreEqual ("a", Eval<string> (Expression.ConvertToString (NullableString ("a"))));
        }

        [Test]
        public void StringJoinOfALazySequenceHoldingANull ()
        {
            var values = Expression.CreateList (new List<Expression> {
                Expression.ConstantString ("a"), Expression.ConstantNull (Type.String ()) });
            var x = Expression.Parameter ("x", Type.NullableType (Type.String ()));
            var all = Expression.Where (values, Expression.Lambda (
                new List<Expression> { x }, Expression.ConstantBool (true)));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (Expression.StringJoin (Expression.ConstantString (","), all)));
            StringAssert.Contains ("The second argument of StringJoin holds a null", exn.Message);
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
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.StringReplace (
                    Expression.ConstantString ("a"), Expression.ConstantString ("a"), Expression.ConstantInt (1)));
            StringAssert.Contains ("The third argument of StringReplace is not a string", exn.Message);
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
                    Expression.ConstantString ("\u200d"))));
            } finally {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Test]
        public void ConvertToStringDoesNotDependOnTheCulture ()
        {
            // German writes a decimal comma
            var culture = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = new CultureInfo ("de-DE");
                Assert.AreEqual ("1.5", Eval<string> (
                    Expression.ConvertToString (Expression.ConstantDouble (1.5))));
                Assert.AreEqual ("2.5", Eval<string> (
                    Expression.ConvertToString (Expression.ConstantFloat (2.5f))));
            } finally {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Test]
        public void StringConcatGivenANullElement ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.StringConcat (
                    new List<Expression> { Expression.ConstantString ("a"), null }));
            StringAssert.Contains ("cannot be null", exn.Message);
        }

        [Test]
        public void AppendToASplitString ()
        {
            var parts = Expression.Variable ("parts", Type.ListType (Type.String ()));
            var expr = Expression.BlockWithVariables (
                new List<Expression> { parts },
                new List<Expression> {
                    Expression.Assign (parts, Expression.StringSplit (
                        Expression.ConstantString ("a,b"), Expression.ConstantString (","))),
                    Expression.Append (parts, Expression.ConstantString ("c")),
                    parts
                });
            CollectionAssert.AreEqual (new [] { "a", "b", "c" }, Eval<IList<string>> (expr));
        }

        [Test]
        public void NullableStringOperand ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<int> (Expression.StringLength (NullableString (null))));
            StringAssert.Contains ("The argument of StringLength is null", exn.Message);
            Assert.AreEqual (1, Eval<int> (Expression.StringLength (NullableString ("a"))));
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (Expression.StringConcat (new List<Expression> {
                    Expression.ConstantString ("a"), NullableString (null) })));
        }
    }
}
