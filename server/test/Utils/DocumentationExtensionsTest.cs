using System;
using System.Linq;
using System.Reflection;
using KRPC.Utils;
using KRPC.Schema.KRPC;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    /// <summary>Class docs</summary>
    public class TestDocumentedClass
    {
        /// <summary>Method docs</summary>
        public void method ()
        {
        }

        /// <summary>Static method docs</summary>
        public static void staticMethod ()
        {
        }

        /// <summary>Property docs</summary>
        public int Property { get; set; }

        /// <summary>Static property docs</summary>
        public static int StaticProperty { get; set; }

        /// <summary>Method arguments docs</summary>
        public void MethodArguments (int one, string two, KRPC.Utils.Tuple<int,float,string> three, Response four, TestDocumentedClass.NestedClass five)
        {
        }

        /// <summary>Nested class docs</summary>
        public class NestedClass
        {
            /// <summary>Nested class method docs</summary>
            public void method ()
            {
            }
        }

        public void notDocumented ()
        {
        }

        /// <summary>
        /// This is the first line.
        /// And the second.
        ///
        /// And the third after a line break.
        /// </summary>
        /// <param name="param1">Param1.</param>
        /// <param name="param2">Param2 <paramref name="param1"/>.</param>
        /// <returns>Nothing....</returns>
        public void multiLineDocumentation (string param1, int param2)
        {
        }

        /// <summary>Foo <see cref="TestDocumentedClass.NestedClass"/> bar.</summary>
        public void crefDocumentation ()
        {
        }
    }

    /// <summary>Static class docs</summary>
    public static class TestDocumentedStaticClass
    {

    }

    [TestFixture]
    public class DocumentationExtentionsTest
    {
        Type cls;
        Type staticClass;
        MethodInfo method;
        MethodInfo staticMethod;
        PropertyInfo property;
        PropertyInfo staticProperty;
        MethodInfo methodArguments;
        Type nestedClass;
        MethodInfo nestedClassMethod;
        MethodInfo notDocumented;
        MethodInfo multiLineDocumentation;
        MethodInfo crefDocumentation;

        [SetUp]
        public void SetUp ()
        {
            cls = typeof(TestDocumentedClass);
            staticClass = typeof(TestDocumentedStaticClass);
            method = typeof(TestDocumentedClass).GetMethod ("method", BindingFlags.Public | BindingFlags.Instance);
            staticMethod = typeof(TestDocumentedClass).GetMethod ("staticMethod", BindingFlags.Public | BindingFlags.Static);
            property = typeof(TestDocumentedClass).GetProperty ("Property", BindingFlags.Public | BindingFlags.Instance);
            staticProperty = typeof(TestDocumentedClass).GetProperty ("StaticProperty", BindingFlags.Public | BindingFlags.Static);
            methodArguments = typeof(TestDocumentedClass).GetMethods ().Single (m => m.Name == "MethodArguments");
            nestedClass = typeof(TestDocumentedClass.NestedClass);
            nestedClassMethod = typeof(TestDocumentedClass.NestedClass).GetMethod ("method", BindingFlags.Public | BindingFlags.Instance);
            notDocumented = typeof(TestDocumentedClass).GetMethod ("notDocumented", BindingFlags.Public | BindingFlags.Instance);
            multiLineDocumentation = typeof(TestDocumentedClass).GetMethod ("multiLineDocumentation", BindingFlags.Public | BindingFlags.Instance);
            crefDocumentation = typeof(TestDocumentedClass).GetMethod ("crefDocumentation", BindingFlags.Public | BindingFlags.Instance);
        }

        [Test]
        public void TestGetDocumentationName ()
        {
            Assert.AreEqual ("T:KRPC.Test.Utils.TestDocumentedClass", DocumentationExtensions.GetDocumentationName (cls));
            Assert.AreEqual ("T:KRPC.Test.Utils.TestDocumentedStaticClass", DocumentationExtensions.GetDocumentationName (staticClass));
            Assert.AreEqual ("M:KRPC.Test.Utils.TestDocumentedClass.method", DocumentationExtensions.GetDocumentationName (method));
            Assert.AreEqual ("M:KRPC.Test.Utils.TestDocumentedClass.staticMethod", DocumentationExtensions.GetDocumentationName (staticMethod));
            Assert.AreEqual ("P:KRPC.Test.Utils.TestDocumentedClass.Property", DocumentationExtensions.GetDocumentationName (property));
            Assert.AreEqual (
                "M:KRPC.Test.Utils.TestDocumentedClass.MethodArguments(" +
                "System.Int32,System.String,KRPC.Utils.Tuple{System.Int32,System.Single,System.String},KRPC.Schema.KRPC.Response,KRPC.Test.Utils.TestDocumentedClass.NestedClass" +
                ")", DocumentationExtensions.GetDocumentationName (methodArguments));
            Assert.AreEqual ("T:KRPC.Test.Utils.TestDocumentedClass.NestedClass", DocumentationExtensions.GetDocumentationName (nestedClass));
            Assert.AreEqual ("M:KRPC.Test.Utils.TestDocumentedClass.NestedClass.method", DocumentationExtensions.GetDocumentationName (nestedClassMethod));
            Assert.AreEqual ("P:KRPC.Test.Utils.TestDocumentedClass.StaticProperty", DocumentationExtensions.GetDocumentationName (staticProperty));
        }

        [Test]
        public void TestGetDocumentation ()
        {
            Assert.AreEqual ("<doc>\n<summary>Class docs</summary>\n</doc>", cls.GetDocumentation ());
            Assert.AreEqual ("<doc>\n<summary>Static class docs</summary>\n</doc>", staticClass.GetDocumentation ());
            Assert.AreEqual ("<doc>\n<summary>Method docs</summary>\n</doc>", method.GetDocumentation ());
            Assert.AreEqual ("<doc>\n<summary>Static method docs</summary>\n</doc>", staticMethod.GetDocumentation ());
            Assert.AreEqual ("<doc>\n<summary>Property docs</summary>\n</doc>", property.GetDocumentation ());
            Assert.AreEqual ("<doc>\n<summary>Static property docs</summary>\n</doc>", staticProperty.GetDocumentation ());
            Assert.AreEqual ("<doc>\n<summary>Method arguments docs</summary>\n</doc>", methodArguments.GetDocumentation ());
            Assert.AreEqual ("<doc>\n<summary>Nested class docs</summary>\n</doc>", nestedClass.GetDocumentation ());
            Assert.AreEqual ("<doc>\n<summary>Nested class method docs</summary>\n</doc>", nestedClassMethod.GetDocumentation ());
        }

        [Test]
        [Ignore]
        public void TestGetMultiLineDocumentation ()
        {
            Assert.AreEqual (
                "<doc>\n" +
                "<summary>\n" +
                "This is the first line.\n" +
                "And the second.\n\n" +
                "And the third after a line break.\n" +
                "</summary>\n" +
                "<param name=\"param1\">Param1.</param>\n" +
                "<param name=\"param2\">Param2 <paramref name=\"param1\" />.</param>\n" +
                "<returns>Nothing....</returns>\n" +
                "</doc>",
                multiLineDocumentation.GetDocumentation ());
        }

        [Test]
        public void TestCrefDocumentation ()
        {
            Assert.AreEqual (
                "<doc>\n<summary>Foo <see cref=\"T:KRPC.Test.Utils.TestDocumentedClass.NestedClass\" /> bar.</summary>\n</doc>",
                crefDocumentation.GetDocumentation ());
        }

        [Test]
        public void TestNoDocumentation ()
        {
            Assert.AreEqual ("", notDocumented.GetDocumentation ());
        }
    }
}
