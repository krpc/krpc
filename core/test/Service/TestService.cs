using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    /// <summary>
    /// Test service documentation.
    /// </summary>
    [KRPCService (GameScene = GameScene.Flight)]
    public static class TestService
    {
        [KRPCException]
        public class MyException : Exception {
        };

        internal static ITestService Service;

        public static void ProcedureWithoutAttribute ()
        {
            Service.ProcedureWithoutAttribute ();
        }

        /// <summary>
        /// Procedure with no return arguments.
        /// </summary>
        [KRPCProcedure]
        public static void ProcedureNoArgsNoReturn ()
        {
            Service.ProcedureNoArgsNoReturn ();
        }

        /// <summary>
        /// Procedure with a single return argument.
        /// </summary>
        [KRPCProcedure]
        public static void ProcedureSingleArgNoReturn (string x)
        {
            Service.ProcedureSingleArgNoReturn (x);
        }

        [KRPCProcedure]
        public static void ProcedureThreeArgsNoReturn (string x, int y, string z)
        {
            Service.ProcedureThreeArgsNoReturn (x, y, z);
        }

        [KRPCProcedure]
        public static string ProcedureNoArgsReturns ()
        {
            return Service.ProcedureNoArgsReturns ();
        }

        [KRPCProcedure]
        public static string ProcedureSingleArgReturns (string x)
        {
            return Service.ProcedureSingleArgReturns (x);
        }

        [KRPCProperty]
        public static string PropertyWithGetAndSet {
            get { return Service.PropertyWithGetAndSet; }
            set { Service.PropertyWithGetAndSet = value; }
        }

        [KRPCProperty]
        public static string PropertyWithGet {
            get { return Service.PropertyWithGet; }
        }

        [KRPCProperty]
        public static string PropertyWithSet {
            set { Service.PropertyWithSet = value; }
        }

        [KRPCProperty (Nullable = true)]
        public static string NullableProperty {
            get { return Service.NullableProperty; }
            set { Service.NullableProperty = value; }
        }

        [KRPCProperty]
        [KRPCNullable]
        public static string NullableByAttributeProperty {
            get { return Service.NullableByAttributeProperty; }
            set { Service.NullableByAttributeProperty = value; }
        }

        [KRPCProcedure]
        public static TestClass CreateTestObject (string value)
        {
            return Service.CreateTestObject (value);
        }

        [KRPCProcedure]
        public static void DeleteTestObject (TestClass obj)
        {
            Service.DeleteTestObject (obj);
        }

        [KRPCProcedure (Nullable = true)]
        public static TestClass EchoTestObject ([KRPCNullable] TestClass obj)
        {
            return Service.EchoTestObject (obj);
        }

        [KRPCProcedure (Nullable = false)]
        public static TestClass ReturnNullWhenNotAllowed ()
        {
            return Service.ReturnNullWhenNotAllowed ();
        }

        [KRPCProcedure (Nullable = true)]
        public static string EchoNullableString ([KRPCNullable] string x)
        {
            return Service.EchoNullableString (x);
        }

        [KRPCProcedure]
        public static int? EchoNullableInt (int? x)
        {
            return Service.EchoNullableInt (x);
        }

        [KRPCProcedure]
        public static TestEnum? EchoNullableEnum (TestEnum? x)
        {
            return Service.EchoNullableEnum (x);
        }

        [KRPCProcedure (Nullable = true)]
        public static IList<string> EchoNullableList ([KRPCNullable] IList<string> l)
        {
            return Service.EchoNullableList (l);
        }

        [KRPCProcedure]
        public static IList<int?> EchoListOfNullableInts (IList<int?> l)
        {
            return Service.EchoListOfNullableInts (l);
        }

        [KRPCProcedure]
        [KRPCNullable (Position.Element)]
        public static IList<TestClass> EchoListOfNullableObjects (
            [KRPCNullable (Position.Element)] IList<TestClass> l)
        {
            return Service.EchoListOfNullableObjects (l);
        }

        [KRPCProcedure]
        [KRPCNullable (Position.Value)]
        public static IDictionary<string,TestClass> EchoDictionaryOfNullableObjects (
            [KRPCNullable (Position.Value)] IDictionary<string,TestClass> d)
        {
            return Service.EchoDictionaryOfNullableObjects (d);
        }

        [KRPCProcedure]
        [KRPCNullable (Position.Item2)]
        public static Tuple<int,TestClass> EchoTupleWithANullableObject (
            [KRPCNullable (Position.Item2)] Tuple<int,TestClass> t)
        {
            return Service.EchoTupleWithANullableObject (t);
        }

        [KRPCProcedure]
        [KRPCNullable (Position.Element, Position.Element)]
        public static IList<IList<TestClass>> EchoNestedListOfNullableObjects (
            [KRPCNullable (Position.Element, Position.Element)] IList<IList<TestClass>> l)
        {
            return Service.EchoNestedListOfNullableObjects (l);
        }

        [KRPCClass (GameScene = GameScene.Flight | GameScene.SpaceCenter)]
        public class TestClass
        {
            public readonly string Value;

            public TestClass (string value)
            {
                Value = value;
            }

            [KRPCMethod]
            public string FloatToString (float x)
            {
                return Value + x;
            }

            [KRPCMethod]
            public string ObjectToString (TestClass other)
            {
                return Value + other.Value;
            }

            [KRPCMethod (Nullable = true)]
            public TestClass EchoNullableObject ([KRPCNullable] TestClass other)
            {
                return other;
            }

            [KRPCMethod]
            public string IntToString (int x = 42)
            {
                return Value + x;
            }

            [KRPCProperty]
            public int IntProperty { get; set; }

            [KRPCProperty (Nullable = true)]
            public TestClass ObjectProperty { get; set; }

            [KRPCMethod]
            public static string StaticMethod (string a = "")
            {
                return "jeb" + a;
            }

            [KRPCMethod (Nullable = true)]
            public static string StaticNullableMethod ([KRPCNullable] string x)
            {
                return x;
            }

            [KRPCMethod]
            public string MethodAvailableInInheritedGameScene()
            {
                return "foo";
            }

            [KRPCMethod (GameScene = GameScene.EditorVAB)]
            public string MethodAvailableInSpecifiedGameScene()
            {
                return "foo";
            }

            [KRPCProperty]
            public string ClassPropertyAvailableInInheritedGameScene
            {
                get { return "foo"; }
            }

            [KRPCProperty (GameScene = GameScene.EditorVAB)]
            public string ClassPropertyAvailableInSpecifiedGameScene
            {
                get { return "foo"; }
            }
        }

        [KRPCProcedure]
        public static void ProcedureSingleOptionalArgNoReturn (string x = "foo")
        {
            Service.ProcedureSingleOptionalArgNoReturn (x);
        }

        [KRPCProcedure]
        public static void ProcedureThreeOptionalArgsNoReturn (float x, string y = "jeb", int z = 42)
        {
            Service.ProcedureThreeOptionalArgsNoReturn (x, y, z);
        }

        [KRPCProcedure]
        public static void ProcedureOptionalNullArg (TestClass x = null)
        {
            Service.ProcedureOptionalNullArg (x);
        }

        /// <summary>
        /// Documentation string for TestEnum.
        /// </summary>
        [KRPCEnum]
        [Serializable]
        public enum TestEnum
        {
            /// <summary>
            /// Documented enum field
            /// </summary>
            X,
            Y,
            Z
        }

        [Serializable]
        public enum TestEnumWithoutAttribute
        {
            Foo,
            Bar,
            Baz
        }

        [KRPCProcedure]
        public static void ProcedureEnumArg (TestEnum x)
        {
            Service.ProcedureEnumArg (x);
        }

        [KRPCProcedure]
        public static TestEnum ProcedureEnumReturn ()
        {
            return Service.ProcedureEnumReturn ();
        }

        [KRPCProcedure]
        public static void BlockingProcedureNoReturn (int n)
        {
            Service.BlockingProcedureNoReturn (n);
        }

        [KRPCProcedure]
        public static int BlockingProcedureReturns (int n, int sum = 0)
        {
            return Service.BlockingProcedureReturns (n, sum);
        }

        [KRPCProcedure]
        public static IList<string> EchoList (IList<string> l)
        {
            return Service.EchoList (l);
        }

        [KRPCProcedure]
        public static IDictionary<int,string> EchoDictionary (IDictionary<int,string> d)
        {
            return Service.EchoDictionary (d);
        }

        [KRPCProcedure]
        public static HashSet<int> EchoSet (HashSet<int> h)
        {
            return Service.EchoSet (h);
        }

        [KRPCProcedure]
        public static Tuple<int,bool> EchoTuple (Tuple<int,bool> t)
        {
            return Service.EchoTuple (t);
        }

        [KRPCProcedure]
        public static IDictionary<int,IList<string>> EchoNestedCollection (IDictionary<int,IList<string>> c)
        {
            return Service.EchoNestedCollection (c);
        }

        [KRPCProcedure]
        public static IList<TestClass> EchoListOfObjects (IList<TestClass> l)
        {
            return Service.EchoListOfObjects (l);
        }

        public static class CreateTupleDefault
        {
            public static object Create ()
            {
                return new Tuple<int,bool> (1, false);
            }
        }

        [KRPCProcedure]
        public static Tuple<int,bool> TupleDefault (
            [KRPCDefaultValue (typeof(CreateTupleDefault))] Tuple<int,bool> x)
        {
            return Service.TupleDefault (x);
        }

        public static class CreateListDefault
        {
            public static object Create ()
            {
                return new List<int> { 1, 2, 3 };
            }
        }

        [KRPCProcedure]
        public static IList<int> ListDefault (
            [KRPCDefaultValue (typeof(CreateListDefault))] IList<int> x)
        {
            return Service.ListDefault (x);
        }

        public static class CreateSetDefault
        {
            public static object Create ()
            {
                return new HashSet<int> { 1, 2, 3 };
            }
        }

        [KRPCProcedure]
        public static HashSet<int> SetDefault (
            [KRPCDefaultValue (typeof(CreateSetDefault))] HashSet<int> x)
        {
            return Service.SetDefault (x);
        }

        public static class CreateDictionaryDefault
        {
            public static object Create ()
            {
                return new Dictionary<int,bool> {
                    { 1, false },
                    { 2, true }
                };
            }
        }

        [KRPCProcedure]
        public static IDictionary<int,bool> DictionaryDefault (
            [KRPCDefaultValue (typeof(CreateDictionaryDefault))] IDictionary<int,bool> x)
        {
            return Service.DictionaryDefault (x);
        }

        /// <summary>
        /// Documentation string for TestStruct.
        /// </summary>
        [KRPCStruct]
        public struct TestStruct
        {
            /// <summary>
            /// Documented struct field
            /// </summary>
            [KRPCProperty]
            public int IntField { get; set; }

            [KRPCProperty]
            public string StringField { get; set; }

            [KRPCProperty]
            public TestEnum EnumField { get; set; }

            [KRPCProperty]
            public TestClass ObjectField { get; set; }

            [KRPCProperty]
            public IList<string> ListField { get; set; }

            /// <summary>
            /// A property that is not a field of the structure, as it is not marked as one.
            /// </summary>
            public int NotAField {
                get { return IntField + 1; }
            }
        }

        [KRPCStruct]
        public struct TestNestedStruct
        {
            [KRPCProperty]
            public TestStruct StructField { get; set; }

            [KRPCProperty]
            public int IntField { get; set; }
        }

        /// <summary>
        /// Documentation string for TestNullableStruct.
        /// </summary>
        [KRPCStruct]
        public struct TestNullableStruct
        {
            [KRPCProperty]
            public int IntField { get; set; }

            [KRPCProperty]
            public int? NullableIntField { get; set; }

            [KRPCProperty]
            public TestEnum? NullableEnumField { get; set; }

            [KRPCProperty (Nullable = true)]
            public string NullableStringField { get; set; }

            [KRPCProperty (Nullable = true)]
            public TestClass NullableObjectField { get; set; }
        }

        [KRPCProcedure]
        public static TestStruct EchoStruct (TestStruct x)
        {
            return Service.EchoStruct (x);
        }

        [KRPCProcedure]
        public static IList<TestStruct> EchoListOfStructs (IList<TestStruct> l)
        {
            return Service.EchoListOfStructs (l);
        }

        [KRPCProcedure]
        public static TestNestedStruct EchoNestedStruct (TestNestedStruct x)
        {
            return Service.EchoNestedStruct (x);
        }

        public static class CreateStructDefault
        {
            public static object Create ()
            {
                return new TestStruct {
                    IntField = 42,
                    StringField = "jeb",
                    EnumField = TestEnum.Y,
                    ObjectField = new TestClass ("kerbin"),
                    ListField = new List<string> { "a", "b" }
                };
            }
        }

        [KRPCProcedure]
        public static TestStruct StructDefault (
            [KRPCDefaultValue (typeof(CreateStructDefault))] TestStruct x)
        {
            return Service.StructDefault (x);
        }

        [KRPCProcedure]
        public static void ProcedureAvailableInInheritedGameScene()
        {
            Service.ProcedureAvailableInInheritedGameScene();
        }

        [KRPCProcedure (GameScene = GameScene.EditorVAB)]
        public static void ProcedureAvailableInSpecifiedGameScene()
        {
            Service.ProcedureAvailableInSpecifiedGameScene();
        }

        [KRPCProperty]
        public static string PropertyAvailableInInheritedGameScene
        {
            get { return Service.PropertyAvailableInInheritedGameScene; }
        }

        [KRPCProperty (GameScene = GameScene.EditorVAB)]
        public static string PropertyAvailableInSpecifiedGameScene
        {
            get { return Service.PropertyAvailableInSpecifiedGameScene; }
        }

        /// <summary>
        /// A deprecated procedure, annotated with a reason.
        /// </summary>
        [KRPCProcedure]
        [Obsolete ("Use <see cref='ProcedureNoArgsNoReturn'/> instead.")]
        public static void DeprecatedProcedure ()
        {
        }

        /// <summary>
        /// A deprecated procedure, annotated without a reason.
        /// </summary>
        [KRPCProcedure]
        [Obsolete]
        public static void DeprecatedProcedureNoMessage ()
        {
        }

        /// <summary>
        /// A deprecated property, annotated with a reason.
        /// </summary>
        [KRPCProperty]
        [Obsolete ("Use <see cref='PropertyWithGet'/> instead.")]
        public static string DeprecatedProperty {
            get { return string.Empty; }
        }

        /// <summary>
        /// A deprecated class, annotated with a reason.
        /// </summary>
        [KRPCClass]
        [Obsolete ("Use <see cref='TestClass'/> instead.")]
        public class DeprecatedClass
        {
            /// <summary>
            /// A deprecated class method, annotated with a reason.
            /// </summary>
            [KRPCMethod]
            [Obsolete ("Use <see cref='TestClass.FloatToString'/> instead.")]
            public string DeprecatedMethod ()
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// A deprecated enumeration, annotated with a reason.
        /// </summary>
        [KRPCEnum]
        [Serializable]
        [Obsolete ("Use <see cref='TestEnum'/> instead.")]
        public enum DeprecatedEnum
        {
            /// <summary>
            /// A value that is not deprecated.
            /// </summary>
            A,

            /// <summary>
            /// A deprecated enumeration value, annotated with a reason.
            /// </summary>
            [Obsolete ("Use <see cref='A'/> instead.")]
            B
        }

        /// <summary>
        /// A deprecated exception, annotated with a reason.
        /// </summary>
        [KRPCException]
        [Obsolete ("Use MyException instead.")]
        public class DeprecatedException : Exception
        {
        }
    }
}
