using System.Collections.Generic;
using KRPC.Service.Attributes;

namespace KRPCTest.Service
{
    [KRPCService]
    public static class TestService
    {
        public static ITestService Service;

        public static void ProcedureWithoutAttribute ()
        {
        }

        [KRPCProcedure]
        public static void ProcedureNoArgsNoReturn ()
        {
            Service.ProcedureNoArgsNoReturn ();
        }

        [KRPCProcedure]
        public static void ProcedureSingleArgNoReturn (KRPC.Schema.KRPC.Response data)
        {
            Service.ProcedureSingleArgNoReturn (data);
        }

        [KRPCProcedure]
        public static void ProcedureThreeArgsNoReturn (KRPC.Schema.KRPC.Response x,
                                                       KRPC.Schema.KRPC.Request y, KRPC.Schema.KRPC.Response z)
        {
            Service.ProcedureThreeArgsNoReturn (x, y, z);
        }

        [KRPCProcedure]
        public static KRPC.Schema.KRPC.Response ProcedureNoArgsReturns ()
        {
            return Service.ProcedureNoArgsReturns ();
        }

        [KRPCProcedure]
        public static KRPC.Schema.KRPC.Response ProcedureSingleArgReturns (KRPC.Schema.KRPC.Response data)
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

        [KRPCProcedure]
        public static void ProcedureEnumArg (KRPC.Schema.Test.TestEnum x)
        {
            Service.ProcedureEnumArg (x);
        }

        [KRPCProcedure]
        public static KRPC.Schema.Test.TestEnum ProcedureEnumReturn ()
        {
            return Service.ProcedureEnumReturn ();
        }

        [KRPCEnum]
        public enum CSharpEnum
        {
            x,
            y,
            z}
        ;

        public enum CSharpEnumWithoutAttribute
        {
            foo,
            bar,
            baz}
        ;

        [KRPCProcedure]
        public static void ProcedureCSharpEnumArg (CSharpEnum x)
        {
            Service.ProcedureCSharpEnumArg (x);
        }

        [KRPCProcedure]
        public static CSharpEnum ProcedureCSharpEnumReturn ()
        {
            return Service.ProcedureCSharpEnumReturn ();
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

