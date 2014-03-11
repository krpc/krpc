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
            public string value;

            public TestClass (string value)
            {
                this.value = value;
            }

            [KRPCMethod]
            public string FloatToString (float x)
            {
                return value + x.ToString ();
            }

            [KRPCMethod]
            public string ObjectToString (TestClass other)
            {
                return value + other.value;
            }

            [KRPCProperty]
            public int IntProperty { get; set; }

            [KRPCProperty]
            public TestClass ObjectProperty { get; set; }
        }
    }
}

