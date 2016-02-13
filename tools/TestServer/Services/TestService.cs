using System;
using System.Linq;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Continuations;

namespace TestServer.Services
{
    /// <summary>
    /// Service documentation string.
    /// </summary>
    [KRPCService]
    public static class TestService
    {
        /// <summary>
        /// Procedure documentation string.
        /// </summary>
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

        /// <summary>
        /// Property documentation string.
        /// </summary>
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

        /// <summary>
        /// Class documentation string.
        /// </summary>
        [KRPCClass]
        public class TestClass : KRPC.Utils.Equatable<TestClass>
        {
            readonly string value;

            public TestClass (string value)
            {
                this.value = value;
            }

            /// <summary>
            /// Method documentation string.
            /// </summary>
            [KRPCMethod]
            public string GetValue ()
            {
                return "value=" + value;
            }

            [KRPCMethod]
            public string FloatToString (float x)
            {
                return value + x;
            }

            [KRPCMethod]
            public string ObjectToString (TestClass other)
            {
                return value + (other == null ? "null" : other.value);
            }

            /// <summary>
            /// Property documentation string.
            /// </summary>
            [KRPCProperty]
            public int IntProperty { get; set; }

            [KRPCProperty]
            public TestClass ObjectProperty { get; set; }

            [KRPCMethod]
            public string OptionalArguments (string x, string y = "foo", string z = "bar", string anotherParameter = "baz")
            {
                return x + y + z + anotherParameter;
            }

            [KRPCMethod]
            public static string StaticMethod (string a = "", string b = "")
            {
                return "jeb" + a + b;
            }

            public override bool Equals (TestClass obj)
            {
                return value == obj.value;
            }

            public override int GetHashCode ()
            {
                return value.GetHashCode ();
            }
        }

        [KRPCProcedure]
        public static string OptionalArguments (string x, string y = "foo", string z = "bar", string anotherParameter = "baz")
        {
            return x + y + z + anotherParameter;
        }

        [KRPCEnum]
        public enum TestEnum
        {
            ValueA,
            ValueB,
            ValueC}
        ;

        [KRPCProcedure]
        public static TestEnum EnumReturn ()
        {
            return TestEnum.ValueB;
        }

        [KRPCProcedure]
        public static TestEnum EnumEcho (TestEnum x)
        {
            return x;
        }

        [KRPCProcedure]
        public static TestEnum EnumDefaultArg (TestEnum x = TestEnum.ValueC)
        {
            return x;
        }

        [KRPCProcedure]
        public static int BlockingProcedure (int n, int sum = 0)
        {
            if (n == 0)
                return sum;
            throw new YieldException (new ParameterizedContinuation<int,int,int> (BlockingProcedure, n - 1, sum + n));
        }

        [KRPCProcedure]
        public static IList<int> IncrementList (IList<int> l)
        {
            return l.Select (x => x + 1).ToList ();
        }

        [KRPCProcedure]
        public static IDictionary<string,int> IncrementDictionary (IDictionary<string,int> d)
        {
            var result = new Dictionary<string,int> ();
            foreach (var entry in d)
                result [entry.Key] = entry.Value + 1;
            return result;
        }

        [KRPCProcedure]
        public static HashSet<int> IncrementSet (HashSet<int> h)
        {
            var result = new HashSet<int> ();
            foreach (var item in h)
                result.Add (item + 1);
            return result;
        }

        [KRPCProcedure]
        public static KRPC.Utils.Tuple<int,long> IncrementTuple (KRPC.Utils.Tuple<int,long> t)
        {
            return KRPC.Utils.Tuple.Create<int,long> (t.Item1 + 1, t.Item2 + 1);
        }

        [KRPCProcedure]
        public static IDictionary<string,IList<int>> IncrementNestedCollection (IDictionary<string,IList<int>> d)
        {
            IDictionary<string,IList<int>> result = new Dictionary<string,IList<int>> ();
            foreach (var entry in d)
                result [entry.Key] = entry.Value.Select (x => x + 1).ToList ();
            return result;
        }

        [KRPCProcedure]
        public static IList<TestClass> AddToObjectList (IList<TestClass> l, string value)
        {
            l.Add (new TestClass (value));
            return l;
        }

        static int count = -1;

        [KRPCProcedure]
        public static int Counter ()
        {
            count++;
            return count;
        }

        [KRPCProcedure]
        public static void ThrowArgumentException ()
        {
            throw new ArgumentException ("Invalid argument");
        }

        [KRPCProcedure]
        public static void ThrowInvalidOperationException ()
        {
            throw new InvalidOperationException ("Invalid operation");
        }
    }
}
