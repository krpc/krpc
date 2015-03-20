using System;
using System.Linq;
using System.Reflection;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPCTest.Utils
{
    /// <summary>Class docs</summary>
    public class TestDocumentedClass
    {
        /// <summary>Method docs</summary>
        public void method () {}

        /// <summary>Static method docs</summary>
        public static void staticMethod () {}

        /// <summary>Property docs</summary>
        public int Property { get; set; }

        /// <summary>Static property docs</summary>
        public static int StaticProperty { get; set; }

        /// <summary>Method arguments docs</summary>
        public void MethodArguments (int one, string two, KRPC.Utils.Tuple<int,float,string> three, KRPC.Schema.KRPC.Response four, TestDocumentedClass.NestedClass five) {}

        /// <summary>Nested class docs</summary>
        public class NestedClass {
            /// <summary>Nested class method docs</summary>
            public void method () {}
        }

        public void notDocumented () {}
    }

    /// <summary>Static class docs</summary>
    public static class TestDocumentedStaticClass { }

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
        }

        [Test]
        public void TestGetDocumentationName ()
        {
            Assert.AreEqual ("T:KRPCTest.Utils.TestDocumentedClass", DocumentationExtensions.GetDocumentationName (cls));
            Assert.AreEqual ("T:KRPCTest.Utils.TestDocumentedStaticClass", DocumentationExtensions.GetDocumentationName (staticClass));
            Assert.AreEqual ("M:KRPCTest.Utils.TestDocumentedClass.method", DocumentationExtensions.GetDocumentationName (method));
            Assert.AreEqual ("M:KRPCTest.Utils.TestDocumentedClass.staticMethod", DocumentationExtensions.GetDocumentationName (staticMethod));
            Assert.AreEqual ("P:KRPCTest.Utils.TestDocumentedClass.Property", DocumentationExtensions.GetDocumentationName (property));
            Assert.AreEqual (
                "M:KRPCTest.Utils.TestDocumentedClass.MethodArguments(" +
                "System.Int32,System.String,KRPC.Utils.Tuple{System.Int32,System.Single,System.String},KRPC.Schema.KRPC.Response,KRPCTest.Utils.TestDocumentedClass.NestedClass" +
                ")", DocumentationExtensions.GetDocumentationName (methodArguments));
            Assert.AreEqual ("T:KRPCTest.Utils.TestDocumentedClass.NestedClass", DocumentationExtensions.GetDocumentationName (nestedClass));
            Assert.AreEqual ("M:KRPCTest.Utils.TestDocumentedClass.NestedClass.method", DocumentationExtensions.GetDocumentationName (nestedClassMethod));
            Assert.AreEqual ("P:KRPCTest.Utils.TestDocumentedClass.StaticProperty", DocumentationExtensions.GetDocumentationName (staticProperty));
        }

        [Test]
        public void TestGetDocumentation ()
        {
            Assert.AreEqual ("Class docs", cls.GetDocumentation ());
            Assert.AreEqual ("Static class docs", staticClass.GetDocumentation ());
            Assert.AreEqual ("Method docs", method.GetDocumentation ());
            Assert.AreEqual ("Static method docs", staticMethod.GetDocumentation ());
            Assert.AreEqual ("Property docs", property.GetDocumentation ());
            Assert.AreEqual ("Static property docs", staticProperty.GetDocumentation ());
            Assert.AreEqual ("Method arguments docs", methodArguments.GetDocumentation ());
            Assert.AreEqual ("Nested class docs", nestedClass.GetDocumentation ());
            Assert.AreEqual ("Nested class method docs", nestedClassMethod.GetDocumentation ());
        }

        [Test]
        public void TestNoDocumentation ()
        {
            Assert.AreEqual ("", notDocumented.GetDocumentation ());
        }
    }
}
