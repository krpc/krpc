using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    /// <summary>
    /// Test service documentation.
    /// </summary>
    [KRPCService (GameScene = GameScene.Flight)]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidTypeInterfaceInconsistencyRule")]
    public static class TestService
    {
        [KRPCException]
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Exceptions", "MissingExceptionConstructorsRule")]
        [SuppressMessage ("Gendarme.Rules.Serialization", "MissingSerializableAttributeOnISerializableTypeRule")]
        [SuppressMessage ("Gendarme.Rules.Serialization", "MissingSerializationConstructorRule")]
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
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public static string PropertyWithSet {
            set { Service.PropertyWithSet = value; }
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
        public static TestClass EchoTestObject (TestClass obj)
        {
            return Service.EchoTestObject (obj);
        }

        [KRPCProcedure (Nullable = false)]
        public static TestClass ReturnNullWhenNotAllowed ()
        {
            return Service.ReturnNullWhenNotAllowed ();
        }

        [KRPCClass (GameScene = GameScene.Flight | GameScene.SpaceCenter)]
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
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
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
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
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
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
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public static IDictionary<int,IList<string>> EchoNestedCollection (IDictionary<int,IList<string>> c)
        {
            return Service.EchoNestedCollection (c);
        }

        [KRPCProcedure]
        public static IList<TestClass> EchoListOfObjects (IList<TestClass> l)
        {
            return Service.EchoListOfObjects (l);
        }

        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
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

        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
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

        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
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

        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
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
    }
}
