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
    }
}

