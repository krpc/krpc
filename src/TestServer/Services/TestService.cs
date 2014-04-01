using System;
using KRPC.Service.Attributes;

namespace TestServer.Services
{
    [KRPCService]
    public static class TestService
    {
        [KRPCProcedure]
        public static string FloatToString (float value)
        {
            return value.ToString ();
        }

        [KRPCProcedure]
        public static string DoubleToString (double value)
        {
            return value.ToString ();
        }

        [KRPCProcedure]
        public static string Int32ToString (int value)
        {
            return value.ToString ();
        }

        [KRPCProcedure]
        public static string Int64ToString (long value)
        {
            return value.ToString ();
        }

        [KRPCProcedure]
        public static string BoolToString (bool value)
        {
            return value.ToString ();
        }

        [KRPCProcedure]
        public static int StringToInt32 (string value)
        {
            return Convert.ToInt32 (value);
        }

        [KRPCProcedure]
        public static string BytesToHexString (byte[] value)
        {

            return BitConverter.ToString (value).Replace ("-", "").ToLower ();
        }

        [KRPCProcedure]
        public static string AddMultipleValues (float x, int y, long z)
        {
            return (x + y + z).ToString ();
        }

        [KRPCProperty]
        public static string StringProperty { get; set; }

        [KRPCProperty]
        public static string StringPropertyPrivateGet { private get; set; }

        static string stringPropertyPrivateSet = "foo";

        [KRPCProperty]
        public static string StringPropertyPrivateSet {
            get { return stringPropertyPrivateSet; }
            private set { stringPropertyPrivateSet = value; }
        }

        [KRPCProcedure]
        public static TestClass CreateTestObject (string value)
        {
            return new TestClass (value);
        }

        [KRPCProcedure]
        public static TestClass EchoTestObject (TestClass value)
        {
            return value;
        }

        [KRPCProperty]
        public static TestClass ObjectProperty { get; set; }

        [KRPCClass]
        public class TestClass
        {
            string value;

            public TestClass (string value)
            {
                this.value = value;
            }

            [KRPCMethod]
            public string GetValue ()
            {
                return "value=" + value;
            }

            [KRPCMethod]
            public string FloatToString (float x)
            {
                return value + x.ToString ();
            }

            [KRPCMethod]
            public string ObjectToString (TestClass other)
            {
                return value + (other == null ? "null" : other.value);
            }

            [KRPCProperty]
            public int IntProperty { get; set; }

            [KRPCProperty]
            public TestClass ObjectProperty { get; set; }

            [KRPCMethod]
            public static string OptionalArguments (string x, string y = "foo", string z = "bar", string w = "baz")
            {
                return x + y + z + w;
            }
        }

        [KRPCProcedure]
        public static string OptionalArguments (string x, string y = "foo", string z = "bar", string w = "baz")
        {
            return x + y + z + w;
        }

        [KRPCProcedure]
        public static KRPC.Schema.Test.TestEnum EnumReturn ()
        {
            return KRPC.Schema.Test.TestEnum.a;
        }

        [KRPCProcedure]
        public static KRPC.Schema.Test.TestEnum EnumEcho (KRPC.Schema.Test.TestEnum x)
        {
            return x;
        }

        [KRPCEnum]
        public enum CSharpEnum { x, y, z };

        [KRPCProcedure]
        public static CSharpEnum CSharpEnumReturn ()
        {
            return CSharpEnum.y;
        }

        [KRPCProcedure]
        public static CSharpEnum CSharpEnumEcho (CSharpEnum x)
        {
            return x;
        }
    }
}

