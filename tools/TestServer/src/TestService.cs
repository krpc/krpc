using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC;
using KRPC.Continuations;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace TestServer
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

            return BitConverter.ToString (value).Replace ("-", string.Empty).ToLower ();
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
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public static string StringPropertyPrivateSet {
            get { return stringPropertyPrivateSet; }
            private set { stringPropertyPrivateSet = value; }
        }

        [KRPCProcedure]
        public static TestClass CreateTestObject (string value)
        {
            return new TestClass (value);
        }

        [KRPCProcedure (Nullable = true)]
        public static TestClass EchoTestObject (TestClass value)
        {
            return value;
        }

        [KRPCProperty (Nullable = true)]
        public static TestClass ObjectProperty { get; set; }

        [KRPCProcedure]
        public static TestClass ReturnNullWhenNotAllowed ()
        {
            return null;
        }

        /// <summary>
        /// Class documentation string.
        /// </summary>
        [KRPCClass]
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design", "ImplementEqualsAndGetHashCodeInPairRule")]
        public sealed class TestClass : Equatable<TestClass>
        {
            internal readonly string instanceValue;

            public TestClass (string value)
            {
                instanceValue = value;
            }

            public sealed override bool Equals (TestClass other)
            {
                return !ReferenceEquals (other, null) && instanceValue == other.instanceValue;
            }

            public sealed override int GetHashCode ()
            {
                return instanceValue.GetHashCode ();
            }

            /// <summary>
            /// Method documentation string.
            /// </summary>
            [KRPCMethod]
            public string GetValue ()
            {
                return "value=" + instanceValue;
            }

            [KRPCMethod]
            public string FloatToString (float x)
            {
                return instanceValue + x;
            }

            [KRPCMethod]
            public string ObjectToString (TestClass other)
            {
                return instanceValue + (ReferenceEquals (other, null) ? "null" : other.instanceValue);
            }

            /// <summary>
            /// Property documentation string.
            /// </summary>
            [KRPCProperty]
            public int IntProperty { get; set; }

            [KRPCProperty (Nullable = true)]
            public TestClass ObjectProperty { get; set; }

            [KRPCMethod]
            [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
            [SuppressMessage ("Gendarme.Rules.Correctness", "CheckParametersNullityInVisibleMethodsRule")]
            public string OptionalArguments (string x, string y = "foo", string z = "bar", TestClass obj = null)
            {
                return x + y + z + (obj == null ? "null" : obj.instanceValue);
            }

            [KRPCMethod]
            public static string StaticMethod (string a = "", string b = "")
            {
                return "jeb" + a + b;
            }
        }

        [KRPCProcedure]
        [SuppressMessage ("Gendarme.Rules.Correctness", "CheckParametersNullityInVisibleMethodsRule")]
        public static string OptionalArguments (string x, string y = "foo", string z = "bar", TestClass obj = null)
        {
            return x + y + z + (obj == null ? "null" : obj.instanceValue);
        }

        /// <summary>
        /// Enum documentation string.
        /// </summary>
        [KRPCEnum]
        [Serializable]
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public enum TestEnum
        {
            /// <summary>
            /// Enum ValueA documentation string.
            /// </summary>
            ValueA,
            /// <summary>
            /// Enum ValueB documentation string.
            /// </summary>
            ValueB,
            /// <summary>
            /// Enum ValueC documentation string.
            /// </summary>
            ValueC
        }

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
            if (l == null)
                throw new ArgumentNullException (nameof (l));
            return l.Select (x => x + 1).ToList ();
        }

        [KRPCProcedure]
        public static IDictionary<string,int> IncrementDictionary (IDictionary<string,int> d)
        {
            if (d == null)
                throw new ArgumentNullException (nameof (d));
            var result = new Dictionary<string,int> ();
            foreach (var entry in d)
                result [entry.Key] = entry.Value + 1;
            return result;
        }

        [KRPCProcedure]
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule")]
        public static HashSet<int> IncrementSet (HashSet<int> h)
        {
            if (h == null)
                throw new ArgumentNullException (nameof (h));
            var result = new HashSet<int> ();
            foreach (var item in h)
                result.Add (item + 1);
            return result;
        }

        [KRPCProcedure]
        public static KRPC.Utils.Tuple<int,long> IncrementTuple (KRPC.Utils.Tuple<int,long> t)
        {
            return KRPC.Utils.Tuple.Create (t.Item1 + 1, t.Item2 + 1);
        }

        [KRPCProcedure]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        public static IDictionary<string,IList<int>> IncrementNestedCollection (IDictionary<string,IList<int>> d)
        {
            if (d == null)
                throw new ArgumentNullException (nameof (d));
            IDictionary<string,IList<int>> result = new Dictionary<string,IList<int>> ();
            foreach (var entry in d)
                result [entry.Key] = entry.Value.Select (x => x + 1).ToList ();
            return result;
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
            return x;
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
            return x;
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
            return x;
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
            return x;
        }

        [KRPCProcedure]
        public static IList<TestClass> AddToObjectList (IList<TestClass> l, string value)
        {
            if (l == null)
                throw new ArgumentNullException (nameof (l));
            l.Add (new TestClass (value));
            return l;
        }

        static IDictionary<Guid, IDictionary<string, int>> counters = new Dictionary<Guid, IDictionary<string, int>> ();

        [KRPCProcedure]
        public static int Counter (string id = "", int divisor = 1)
        {
            var client = CallContext.Client.Guid;
            if (!counters.ContainsKey (client))
                counters [client] = new Dictionary<string, int> ();
            if (!counters [client].ContainsKey (id))
                counters [client][id] = 0;
            counters [client][id]++;
            return counters [client][id] / divisor;
        }

        [KRPCProcedure]
        public static int ThrowInvalidOperationException()
        {
            throw new InvalidOperationException("Invalid operation");
        }

        static int invalidOperationExceptionCount;

        [KRPCProcedure]
        public static void ResetInvalidOperationExceptionLater()
        {
            invalidOperationExceptionCount = 0;
        }

        [KRPCProcedure]
        public static int ThrowInvalidOperationExceptionLater()
        {
            if (invalidOperationExceptionCount > 100)
                throw new InvalidOperationException("Invalid operation");
            invalidOperationExceptionCount++;
            return 0;
        }

        [KRPCProcedure]
        public static int ThrowArgumentException ()
        {
            throw new ArgumentException ("Invalid argument");
        }

        [KRPCProcedure]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnusedParametersRule")]
        public static int ThrowArgumentNullException (string foo)
        {
            throw new ArgumentNullException (nameof (foo));
        }

        [KRPCProcedure]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnusedParametersRule")]
        public static int ThrowArgumentOutOfRangeException (int foo)
        {
            throw new ArgumentOutOfRangeException (nameof (foo));
        }

        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Exceptions", "MissingExceptionConstructorsRule")]
        [SuppressMessage ("Gendarme.Rules.Serialization", "MissingSerializableAttributeOnISerializableTypeRule")]
        [SuppressMessage ("Gendarme.Rules.Serialization", "MissingSerializationConstructorRule")]
        [KRPCException]
        public sealed class CustomException : System.Exception
        {
            public CustomException ()
            {
            }

            public CustomException (string message)
            : base (message)
            {
            }

            public CustomException (string message, System.Exception innerException)
            : base(message, innerException)
            {
            }
        }

        [KRPCProcedure]
        public static int ThrowCustomException ()
        {
            throw new CustomException ("A custom kRPC exception");
        }

        static int customExceptionCount;

        [KRPCProcedure]
        public static void ResetCustomExceptionLater()
        {
            customExceptionCount = 0;
        }

        [KRPCProcedure]
        public static int ThrowCustomExceptionLater()
        {
            if (customExceptionCount > 100)
                throw new CustomException("A custom kRPC exception");
            customExceptionCount++;
            return 0;
        }

        [KRPCProcedure]
        public static KRPC.Service.Messages.Event OnTimer (uint milliseconds, uint repeats = 1) {
            var evnt = new KRPC.Service.Event ();
            var timer = new System.Timers.Timer (milliseconds);
            timer.Elapsed += (s, e) => {
                evnt.Trigger ();
                repeats--;
                if (repeats == 0) {
                    evnt.Remove ();
                    timer.Enabled = false;
                }
            };
            timer.Start();
            return evnt.Message;
        }
    }
}
