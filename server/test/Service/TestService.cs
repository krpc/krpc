using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;

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
        internal static ITestService Service;

        public static void ProcedureWithoutAttribute ()
        {
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
        public static void ProcedureSingleArgNoReturn (Response data)
        {
            Service.ProcedureSingleArgNoReturn (data);
        }

        [KRPCProcedure]
        public static void ProcedureThreeArgsNoReturn (Response x, Request y, Response z)
        {
            Service.ProcedureThreeArgsNoReturn (x, y, z);
        }

        [KRPCProcedure]
        public static Response ProcedureNoArgsReturns ()
        {
            return Service.ProcedureNoArgsReturns ();
        }

        [KRPCProcedure]
        public static Response ProcedureSingleArgReturns (Response data)
        {
            return Service.ProcedureSingleArgReturns (data);
        }

        [KRPCProcedure]
        public static int ProcedureWithValueTypes (float x, string y, byte[] z)
        {
            return Service.ProcedureWithValueTypes (x, y, z);
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

        [KRPCProcedure]
        public static TestClass EchoTestObject (TestClass obj)
        {
            return Service.EchoTestObject (obj);
        }

        [KRPCClass]
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

            [KRPCProperty]
            public TestClass ObjectProperty { get; set; }

            [KRPCMethod]
            public static string StaticMethod (string a = "")
            {
                return "jeb" + a;
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
        public static KRPC.Utils.Tuple<int,bool> EchoTuple (KRPC.Utils.Tuple<int,bool> t)
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
                return new KRPC.Utils.Tuple<int,bool> (1, false);
            }
        }

        [KRPCProcedure]
        [KRPCDefaultValue ("x", typeof(CreateTupleDefault))]
        public static KRPC.Utils.Tuple<int,bool> TupleDefault (KRPC.Utils.Tuple<int,bool> x)
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
        [KRPCDefaultValue ("x", typeof(CreateListDefault))]
        public static IList<int> ListDefault (IList<int> x)
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
        [KRPCDefaultValue ("x", typeof(CreateSetDefault))]
        public static HashSet<int> SetDefault (HashSet<int> x)
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
        [KRPCDefaultValue ("x", typeof(CreateDictionaryDefault))]
        public static IDictionary<int,bool> DictionaryDefault (IDictionary<int,bool> x)
        {
            return Service.DictionaryDefault (x);
        }
    }
}
