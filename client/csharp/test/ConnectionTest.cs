using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.TestService;
using NUnit.Framework;
using GameScene = KRPC.Client.Services.KRPC.GameScene;
using TestEnum = KRPC.Client.Services.TestService.TestEnum;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class ConnectionTest : ServerTestCase
    {
        [Test]
        public void GetStatus ()
        {
            var status = Connection.KRPC ().GetStatus ();
            StringAssert.IsMatch ("^[0-9]+\\.[0-9]+\\.[0-9]+$", status.Version);
            Assert.Greater (status.BytesRead, 0);
        }

        [Test]
        public void CurrentGameScene ()
        {
            Assert.AreEqual (GameScene.SpaceCenter, Connection.KRPC ().CurrentGameScene);
        }

        [Test]
        public void Error ()
        {
            var e1 = Assert.Throws<RPCException> (Connection.TestService ().ThrowArgumentException);
            Assert.That (e1.Message, Is.StringContaining ("Invalid argument"));
            var e2 = Assert.Throws<RPCException> (Connection.TestService ().ThrowInvalidOperationException);
            Assert.That (e2.Message, Is.StringContaining ("Invalid operation"));
        }

        [Test]
        public void ValueParameters ()
        {
            Assert.AreEqual ("3.14159", Connection.TestService ().FloatToString (3.14159f));
            Assert.AreEqual ("3.14159", Connection.TestService ().DoubleToString (3.14159));
            Assert.AreEqual ("42", Connection.TestService ().Int32ToString (42));
            Assert.AreEqual ("123456789000", Connection.TestService ().Int64ToString (123456789000L));
            Assert.AreEqual ("True", Connection.TestService ().BoolToString (true));
            Assert.AreEqual ("False", Connection.TestService ().BoolToString (false));
            Assert.AreEqual (12345, Connection.TestService ().StringToInt32 ("12345"));
            Assert.AreEqual ("deadbeef", Connection.TestService ().BytesToHexString (new byte[] {
                0xDE,
                0xAD,
                0xBE,
                0xEF
            }));
        }

        [Test]
        public void MultipleValueParameters ()
        {
            Assert.AreEqual ("3.14159", Connection.TestService ().AddMultipleValues (0.14159f, 1, 2));
        }

        [Test]
        public void Properties ()
        {
            Connection.TestService ().StringProperty = "foo";
            Assert.AreEqual ("foo", Connection.TestService ().StringProperty);
            Assert.AreEqual ("foo", Connection.TestService ().StringPropertyPrivateSet);
            Connection.TestService ().StringPropertyPrivateGet = "foo";
            var obj = Connection.TestService ().CreateTestObject ("bar");
            Connection.TestService ().ObjectProperty = obj;
            Assert.AreEqual (obj.id, Connection.TestService ().ObjectProperty.id);
        }

        [Test]
        public void ClassAsReturnValue ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            Assert.AreEqual (typeof(TestClass), obj.GetType ());
        }

        [Test]
        public void ClassNullValues ()
        {
            Assert.AreEqual (null, Connection.TestService ().EchoTestObject (null));
            var obj = Connection.TestService ().CreateTestObject ("bob");
            Assert.AreEqual ("bobnull", obj.ObjectToString (null));
            Connection.TestService ().ObjectProperty = null;
            Assert.IsNull (Connection.TestService ().ObjectProperty);
        }

        [Test]
        public void ClassMethods ()
        {
            var obj = Connection.TestService ().CreateTestObject ("bob");
            Assert.AreEqual ("value=bob", obj.GetValue ());
            Assert.AreEqual ("bob3.14159", obj.FloatToString (3.14159f));
            var obj2 = Connection.TestService ().CreateTestObject ("bill");
            Assert.AreEqual ("bobbill", obj.ObjectToString (obj2));
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Performance", "UseStringEmptyRule")]
        public void ClassStaticMethods ()
        {
            Assert.AreEqual ("jeb", TestClass.StaticMethod (Connection));
            Assert.AreEqual ("jebbobbill", TestClass.StaticMethod (Connection, "bob", "bill"));
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public void ClassProperties ()
        {
            var obj = Connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 0;
            Assert.AreEqual (0, obj.IntProperty);
            obj.IntProperty = 42;
            Assert.AreEqual (42, obj.IntProperty);
            var obj2 = Connection.TestService ().CreateTestObject ("kermin");
            obj.ObjectProperty = obj2;
            Assert.AreEqual (obj2.id, obj.ObjectProperty.id);
        }

        [Test]
        public void OptionalArguments ()
        {
            Assert.AreEqual ("jebfoobarbaz", Connection.TestService ().OptionalArguments ("jeb"));
            Assert.AreEqual ("jebbobbillbaz", Connection.TestService ().OptionalArguments ("jeb", "bob", "bill"));
        }

        [TestCase (0, 0)]
        [TestCase (1, 1)]
        [TestCase (2, 3)]
        [TestCase (42, 903)]
        public void BlockingProcedure (int input, int output)
        {
            Assert.AreEqual (output, Connection.TestService ().BlockingProcedure (input));
        }

        [Test]
        public void Enums ()
        {
            Assert.AreEqual (TestEnum.ValueB, Connection.TestService ().EnumReturn ());
            Assert.AreEqual (TestEnum.ValueA, Connection.TestService ().EnumEcho (TestEnum.ValueA));
            Assert.AreEqual (TestEnum.ValueB, Connection.TestService ().EnumEcho (TestEnum.ValueB));
            Assert.AreEqual (TestEnum.ValueC, Connection.TestService ().EnumEcho (TestEnum.ValueC));
            Assert.AreEqual (TestEnum.ValueA, Connection.TestService ().EnumDefaultArg (TestEnum.ValueA));
            Assert.AreEqual (TestEnum.ValueC, Connection.TestService ().EnumDefaultArg ());
            Assert.AreEqual (TestEnum.ValueB, Connection.TestService ().EnumDefaultArg (TestEnum.ValueB));
        }

        [TestCase (new int[] { }, new int[] { })]
        [TestCase (new [] { 42 }, new [] { 43 })]
        [TestCase (new [] { 0, 1, 2 }, new [] { 1, 2, 3 })]
        public void CollectionsList (IList<int> input, IList<int> output)
        {
            CollectionAssert.AreEqual (output, Connection.TestService ().IncrementList (input));
        }

        [TestCase (new string[] { }, new int[] { }, new int[] { })]
        [TestCase (new [] { "foo" }, new [] { 42 }, new [] { 43 })]
        [TestCase (new [] { "a", "b", "c" }, new [] { 0, 1, 2 }, new [] { 1, 2, 3 })]
        public void CollectionsDictionary (IList<string> keys, IList<int> inputValues, IList<int> outputValues)
        {
            var input = new Dictionary<string,int> ();
            var output = new Dictionary<string,int> ();
            for (int i = 0; i < keys.Count; i++) {
                input [keys [i]] = inputValues [i];
                output [keys [i]] = outputValues [i];
            }
            CollectionAssert.AreEqual (output, Connection.TestService ().IncrementDictionary (input));
        }

        [TestCase (new int [] { }, new int [] { })]
        [TestCase (new [] { 42 }, new [] { 43 })]
        [TestCase (new [] { 0, 1, 2 }, new [] { 1, 2, 3 })]
        public void CollectionsSet (IList<int> inputValues, IList<int> outputValues)
        {
            var input = new HashSet<int> (inputValues);
            var output = new HashSet<int> (outputValues);
            CollectionAssert.AreEqual (output, Connection.TestService ().IncrementSet (input));
        }

        [Test]
        public void CollectionsTuple ()
        {
            var input = new Tuple<int,long> (0, 1);
            var output = new Tuple<int,long> (1, 2);
            Assert.AreEqual (output, Connection.TestService ().IncrementTuple (input));
        }

        [Test]
        public void NestedCollections ()
        {
            CollectionAssert.AreEqual (
                new Dictionary<string,IList<int>> (),
                Connection.TestService ().IncrementNestedCollection (new Dictionary<string,IList<int>> ()));
            CollectionAssert.AreEqual (
                new Dictionary<string,IList<int>> {
                    { "a", new List<int> { 1, 2 } },
                    { "b", new List<int> () },
                    { "c", new List<int> { 3 } }
                },
                Connection.TestService ().IncrementNestedCollection (new Dictionary<string,IList<int>> {
                    { "a", new List<int> { 0, 1 } },
                    { "b", new List<int> () },
                    { "c", new List<int> { 2 } }
                }));
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public void CollectionsOfObjects ()
        {
            var l = Connection.TestService ().AddToObjectList (new List<TestClass> (), "jeb");
            Assert.AreEqual (1, l.Count);
            Assert.AreEqual ("value=jeb", l [0].GetValue ());
            l = Connection.TestService ().AddToObjectList (l, "bob");
            Assert.AreEqual (2, l.Count);
            Assert.AreEqual ("value=jeb", l [0].GetValue ());
            Assert.AreEqual ("value=bob", l [1].GetValue ());
        }

        [Test]
        public void CollectionsDefaultValues ()
        {
            Assert.AreEqual (new Tuple<int,bool> (1, false), Connection.TestService ().TupleDefault ());
            Assert.AreEqual (new List<int> { 1, 2, 3 }, Connection.TestService ().ListDefault ());
            Assert.AreEqual (new HashSet<int> { 1, 2, 3 }, Connection.TestService ().SetDefault ());
            Assert.AreEqual (new Dictionary<int, bool> { { 1, false }, { 2,true } }, Connection.TestService ().DictionaryDefault ());
        }

        [TestCase ("foo\nbar")]
        [TestCase ("foo\rbar")]
        [TestCase ("foo\n\rbar")]
        [TestCase ("foo\r\nbar")]
        [TestCase ("foo\x10bar")]
        [TestCase ("foo\x13bar")]
        [TestCase ("foo\x10\x13bar")]
        [TestCase ("foo\x13\x10bar")]
        public void LineEndings (string data)
        {
            Connection.TestService ().StringProperty = data;
            Assert.AreEqual (data, Connection.TestService ().StringProperty);
        }

        [Test]
        public void ThreadSafe ()
        {
            const int threadCount = 4;
            const int repeats = 1000;
            var counter = new CountdownEvent (threadCount);
            for (int i = 0; i < threadCount; i++) {
                new Thread (() => {
                    for (int j = 0; j < repeats; j++) {
                        Assert.AreEqual ("False", Connection.TestService ().BoolToString (false));
                        Assert.AreEqual (12345, Connection.TestService ().StringToInt32 ("12345"));
                    }
                    counter.Signal ();
                }).Start ();
            }
            counter.Wait (10 * 1000);
            Assert.IsTrue (counter.IsSet);
        }
    }
}
