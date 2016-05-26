using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;

namespace KRPC.Test.Service
{
    /// <summary>
    /// Test service documentation.
    /// </summary>
    [KRPCService (GameScene = GameScene.Flight)]
    public static class TestService
    {
        public static ITestService Service;

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
        public class TestClass
        {
            public readonly string value;

            public TestClass (string value)
            {
                this.value = value;
            }

            [KRPCMethod]
            public string FloatToString (float x)
            {
                return value + x;
            }

            [KRPCMethod]
            public string ObjectToString (TestClass other)
            {
                return value + other.value;
            }

            [KRPCMethod]
            public string IntToString (int x = 42)
            {
                return value + x;
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
        public enum TestEnum
        {
            /// <summary>
            /// Documented enum field
            /// </summary>
            x,
            y,
            z
        }

        public enum TestEnumWithoutAttribute
        {
            foo,
            bar,
            baz
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
        public static IDictionary<int,IList<string>> EchoNestedCollection (IDictionary<int,IList<string>> c)
        {
            return Service.EchoNestedCollection (c);
        }

        [KRPCProcedure]
        public static IList<TestClass> EchoListOfObjects (IList<TestClass> l)
        {
            return Service.EchoListOfObjects (l);
        }
    }
}

