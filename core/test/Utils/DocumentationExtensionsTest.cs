using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using KRPC.Schema.KRPC;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
    public class DocumentationExtentionsTest
    {
        static readonly System.Type cls = typeof(TestDocumentedClass);
        static readonly System.Type staticClass = typeof(TestDocumentedStaticClass);
        static readonly MethodInfo method = typeof(TestDocumentedClass).GetMethod ("Method", BindingFlags.Public | BindingFlags.Instance);
        static readonly MethodInfo staticMethod = typeof(TestDocumentedClass).GetMethod ("StaticMethod", BindingFlags.Public | BindingFlags.Static);
        static readonly PropertyInfo property = typeof(TestDocumentedClass).GetProperty ("Property", BindingFlags.Public | BindingFlags.Instance);
        static readonly PropertyInfo staticProperty = typeof(TestDocumentedClass).GetProperty ("StaticProperty", BindingFlags.Public | BindingFlags.Static);
        static readonly MethodInfo methodArguments = typeof(TestDocumentedClass).GetMethods ().Single (m => m.Name == "MethodArguments");
        static readonly System.Type nestedClass = typeof(TestDocumentedClass.NestedClass);
        static readonly MethodInfo nestedClassMethod = typeof(TestDocumentedClass.NestedClass).GetMethod ("Method", BindingFlags.Public | BindingFlags.Instance);

        static readonly MethodInfo childMethod = typeof(TestDocumentedParentClass).GetMethod ("ChildMethod", BindingFlags.Public | BindingFlags.Instance);
        static readonly MethodInfo childStaticMethod = typeof(TestDocumentedParentClass).GetMethod ("ChildStaticMethod", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        static readonly PropertyInfo childProperty = typeof(TestDocumentedParentClass).GetProperty ("ChildProperty", BindingFlags.Public | BindingFlags.Instance);
        static readonly PropertyInfo childStaticProperty = typeof(TestDocumentedParentClass).GetProperty ("ChildStaticProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        static readonly MethodInfo childGenericMethod = typeof(TestDocumentedParentGenericClass).GetMethod ("ChildGenericMethod", BindingFlags.Public | BindingFlags.Instance);
        static readonly MethodInfo childGenericStaticMethod = typeof(TestDocumentedParentGenericClass).GetMethod ("ChildGenericStaticMethod", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        static readonly PropertyInfo childGenericProperty = typeof(TestDocumentedParentGenericClass).GetProperty ("ChildGenericProperty", BindingFlags.Public | BindingFlags.Instance);
        static readonly PropertyInfo childGenericStaticProperty = typeof(TestDocumentedParentGenericClass).GetProperty ("ChildGenericStaticProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        static readonly MethodInfo notDocumented = typeof(TestDocumentedClass).GetMethod ("NotDocumented", BindingFlags.Public | BindingFlags.Instance);
        static readonly MethodInfo multiLineDocumentation = typeof(TestDocumentedClass).GetMethod ("MultiLineDocumentation", BindingFlags.Public | BindingFlags.Instance);
        static readonly MethodInfo crefDocumentation = typeof(TestDocumentedClass).GetMethod ("CrefDocumentation", BindingFlags.Public | BindingFlags.Instance);

        #pragma warning disable 0414
        static object[] GetDocumentationNameCases = {
            new object[] {
                cls,
                "T:KRPC.Test.Utils.TestDocumentedClass"
            },
            new object[] {
                staticClass,
                "T:KRPC.Test.Utils.TestDocumentedStaticClass"
            },
            new object[] {
                method,
                "M:KRPC.Test.Utils.TestDocumentedClass.Method"
            },
            new object[] {
                staticMethod,
                "M:KRPC.Test.Utils.TestDocumentedClass.StaticMethod"
            },
            new object[] {
                property,
                "P:KRPC.Test.Utils.TestDocumentedClass.Property"
            },
            new object[] {
                staticProperty,
                "P:KRPC.Test.Utils.TestDocumentedClass.StaticProperty"
            },
            new object[] {
                methodArguments,
                "M:KRPC.Test.Utils.TestDocumentedClass.MethodArguments(" +
                "System.Int32,System.String,System.Tuple{System.Int32,System.Single,System.String}," +
                "KRPC.Schema.KRPC.Response,KRPC.Test.Utils.TestDocumentedClass.NestedClass)"
            },
            new object[] {
                nestedClass,
                "T:KRPC.Test.Utils.TestDocumentedClass.NestedClass"
            },
            new object[] {
                nestedClassMethod,
                "M:KRPC.Test.Utils.TestDocumentedClass.NestedClass.Method"
            },
            new object[] {
                childMethod,
                "M:KRPC.Test.Utils.TestDocumentedChildClass.ChildMethod"
            },
            new object[] {
                childStaticMethod,
                "M:KRPC.Test.Utils.TestDocumentedChildClass.ChildStaticMethod"
            },
            new object[] {
                childProperty,
                "P:KRPC.Test.Utils.TestDocumentedChildClass.ChildProperty"
            },
            new object[] {
                childStaticProperty,
                "P:KRPC.Test.Utils.TestDocumentedChildClass.ChildStaticProperty"
            },
            new object[] {
                childGenericMethod,
                "M:KRPC.Test.Utils.TestDocumentedChildGenericClass`1.ChildGenericMethod"
            },
            new object[] {
                childGenericStaticMethod,
                "M:KRPC.Test.Utils.TestDocumentedChildGenericClass`1.ChildGenericStaticMethod"
            },
            new object[] {
                childGenericProperty,
                "P:KRPC.Test.Utils.TestDocumentedChildGenericClass`1.ChildGenericProperty"
            },
            new object[] {
                childGenericStaticProperty,
                "P:KRPC.Test.Utils.TestDocumentedChildGenericClass`1.ChildGenericStaticProperty"
            }
        };
        #pragma warning restore 0414

        [Test, TestCaseSource ("GetDocumentationNameCases")]
        public void GetDocumentationName (MemberInfo member, string name)
        {
            Assert.AreEqual (name, DocumentationExtensions.GetDocumentationName (member));
        }

        #pragma warning disable 0414
        static object[] GetDocumentationCases = {
            new object[] {
                cls,
                "<doc>\n<summary>Class docs</summary>\n</doc>"
            },
            new object[] {
                staticClass,
                "<doc>\n<summary>Static class docs</summary>\n</doc>"
            },
            new object[] {
                method,
                "<doc>\n<summary>Method docs</summary>\n</doc>"
            },
            new object[] {
                staticMethod,
                "<doc>\n<summary>Static method docs</summary>\n</doc>"
            },
            new object[] {
                property,
                "<doc>\n<summary>Property docs</summary>\n</doc>"
            },
            new object[] {
                staticProperty,
                "<doc>\n<summary>Static property docs</summary>\n</doc>"
            },
            new object[] {
                methodArguments,
                "<doc>\n<summary>Method arguments docs</summary>\n</doc>"
            },
            new object[] {
                nestedClass,
                "<doc>\n<summary>Nested class docs</summary>\n</doc>"
            },
            new object[] {
                nestedClassMethod,
                "<doc>\n<summary>Nested class method docs</summary>\n</doc>"
            },
            new object[] {
                childMethod,
                "<doc>\n<summary>Inherited method docs</summary>\n</doc>"
            },
            new object[] {
                childStaticMethod,
                "<doc>\n<summary>Inherited static method docs</summary>\n</doc>"
            },
            new object[] {
                childProperty,
                "<doc>\n<summary>Inherited property docs</summary>\n</doc>"
            },
            new object[] {
                childStaticProperty,
                "<doc>\n<summary>Inherited static property docs</summary>\n</doc>"
            },
            new object[] {
                childGenericMethod,
                "<doc>\n<summary>Inherited generic method docs</summary>\n</doc>"
            },
            new object[] {
                childGenericStaticMethod,
                "<doc>\n<summary>Inherited generic static method docs</summary>\n</doc>"
            },
            new object[] {
                childGenericProperty,
                "<doc>\n<summary>Inherited generic property docs</summary>\n</doc>"
            },
            new object[] {
                childGenericStaticProperty,
                "<doc>\n<summary>Inherited generic static property docs</summary>\n</doc>"
            },
            new object[] {
                notDocumented,
                string.Empty
            },
            new object[] {
                crefDocumentation,
                "<doc>\n<summary>Foo <see cref=\"T:KRPC.Test.Utils.TestDocumentedClass.NestedClass\" /> bar.</summary>\n</doc>"
            }
        };
        #pragma warning restore 0414

        [Test, TestCaseSource ("GetDocumentationCases")]
        public void TestGetDocumentation (MemberInfo member, string name)
        {
            Assert.AreEqual (name, member.GetDocumentation ());
        }

        [Test]
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
    }

    /// <summary>Class docs</summary>
    [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
    sealed class TestDocumentedClass
    {
        /// <summary>Method docs</summary>
        public void Method ()
        {
        }

        /// <summary>Static method docs</summary>
        public static void StaticMethod ()
        {
        }

        /// <summary>Property docs</summary>
        public int Property { get; set; }

        /// <summary>Static property docs</summary>
        public static int StaticProperty { get; set; }

        /// <summary>Method arguments docs</summary>
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnusedParametersRule")]
        public void MethodArguments (int one, string two, Tuple<int,float,string> three, Response four, NestedClass five)
        {
        }

        /// <summary>Nested class docs</summary>
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public sealed class NestedClass
        {
            /// <summary>Nested class method docs</summary>
            public void Method ()
            {
            }
        }

        public void NotDocumented ()
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
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnusedParametersRule")]
        public void MultiLineDocumentation (string param1, int param2)
        {
        }

        /// <summary>Foo <see cref="NestedClass"/> bar.</summary>
        public void CrefDocumentation ()
        {
        }
    }

    /// <summary>Static class docs</summary>
    static class TestDocumentedStaticClass
    {
    }

    sealed class TestDocumentedParentClass : TestDocumentedChildClass
    {
    }

    [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
    class TestDocumentedChildClass
    {
        /// <summary>Inherited method docs</summary>
        public void ChildMethod ()
        {
        }

        /// <summary>Inherited static method docs</summary>
        public static void ChildStaticMethod ()
        {
        }

        /// <summary>Inherited property docs</summary>
        public int ChildProperty { get; set; }

        /// <summary>Inherited static property docs</summary>
        public static int ChildStaticProperty { get; set; }
    }

    sealed class TestDocumentedParentGenericClass : TestDocumentedChildGenericClass<ICollection<string>>
    {
    }

    [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
    class TestDocumentedChildGenericClass<T>
    {
        /// <summary>Inherited generic method docs</summary>
        public void ChildGenericMethod ()
        {
        }

        /// <summary>Inherited generic static method docs</summary>
        public static void ChildGenericStaticMethod ()
        {
        }

        /// <summary>Inherited generic property docs</summary>
        public int ChildGenericProperty { get; set; }

        /// <summary>Inherited generic static property docs</summary>
        public static int ChildGenericStaticProperty { get; set; }
    }
}
