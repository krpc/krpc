using KRPC.Client;
using KRPC.Client.Services;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.TestService;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

using CSharpEnum = KRPC.Client.Services.TestService.CSharpEnum;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class ConnectionTest : ServerTestCase
    {
        [Test]
        public void GetStatus ()
        {
            var status = connection.KRPC ().GetStatus ();
            StringAssert.IsMatch ("^[0-9]+\\.[0-9]+\\.[0-9]+$", status.Version);
            Assert.Greater (status.BytesRead, 0);
        }

        [Test]
        public void Error ()
        {
            var e1 = Assert.Throws<RPCException> (connection.TestService ().ThrowArgumentException);
            Assert.AreEqual (e1.Message, "Invalid argument");
            var e2 = Assert.Throws<RPCException> (connection.TestService ().ThrowInvalidOperationException);
            Assert.AreEqual (e2.Message, "Invalid operation");
        }

        [Test]
        public void ValueParameters ()
        {
            Assert.AreEqual ("3.14159", connection.TestService ().FloatToString (3.14159f));
            Assert.AreEqual ("3.14159", connection.TestService ().DoubleToString (3.14159));
            Assert.AreEqual ("42", connection.TestService ().Int32ToString (42));
            Assert.AreEqual ("123456789000", connection.TestService ().Int64ToString (123456789000L));
            Assert.AreEqual ("True", connection.TestService ().BoolToString (true));
            Assert.AreEqual ("False", connection.TestService ().BoolToString (false));
            Assert.AreEqual (12345, connection.TestService ().StringToInt32 ("12345"));
            Assert.AreEqual ("deadbeef", connection.TestService ().BytesToHexString (new byte[] {
                0xDE,
                0xAD,
                0xBE,
                0xEF
            }));
        }

        [Test]
        public void MultipleValueParameters ()
        {
            Assert.AreEqual ("3.14159", connection.TestService ().AddMultipleValues (0.14159f, 1, 2));
        }

        [Test]
        public void Properties ()
        {
            connection.TestService ().StringProperty = "foo";
            Assert.AreEqual ("foo", connection.TestService ().StringProperty);
            Assert.AreEqual ("foo", connection.TestService ().StringPropertyPrivateSet);
            connection.TestService ().StringPropertyPrivateGet = "foo";
            var obj = connection.TestService ().CreateTestObject ("bar");
            connection.TestService ().ObjectProperty = obj;
            Assert.AreEqual (obj._ID, connection.TestService ().ObjectProperty._ID);
        }

        [Test]
        public void ClassAsReturnValue ()
        {
            var obj = connection.TestService ().CreateTestObject ("jeb");
            Assert.AreEqual (typeof(KRPC.Client.Services.TestService.TestClass), obj.GetType ());
        }

        [Test]
        public void ClassNullValues ()
        {
            Assert.AreEqual (null, connection.TestService ().EchoTestObject (null));
            var obj = connection.TestService ().CreateTestObject ("bob");
            Assert.AreEqual ("bobnull", obj.ObjectToString (null));
            connection.TestService ().ObjectProperty = null;
            Assert.IsNull (connection.TestService ().ObjectProperty);
        }

        [Test]
        public void ClassMethods ()
        {
            var obj = connection.TestService ().CreateTestObject ("bob");
            Assert.AreEqual ("value=bob", obj.GetValue ());
            Assert.AreEqual ("bob3.14159", obj.FloatToString (3.14159f));
            var obj2 = connection.TestService ().CreateTestObject ("bill");
            Assert.AreEqual ("bobbill", obj.ObjectToString (obj2));
        }

        [Test]
        public void ClassStaticMethods ()
        {
            //TODO: way to avoid passing client object to static class methods?
            Assert.AreEqual ("jeb", TestClass.StaticMethod (connection));
            Assert.AreEqual ("jebbobbill", TestClass.StaticMethod (connection, "bob", "bill"));
        }

        [Test]
        public void ClassProperties ()
        {
            var obj = connection.TestService ().CreateTestObject ("jeb");
            obj.IntProperty = 0;
            Assert.AreEqual (0, obj.IntProperty);
            obj.IntProperty = 42;
            Assert.AreEqual (42, obj.IntProperty);
            var obj2 = connection.TestService ().CreateTestObject ("kermin");
            obj.ObjectProperty = obj2;
            Assert.AreEqual (obj2._ID, obj.ObjectProperty._ID);
        }

        [Test]
        public void OptionalArguments ()
        {
            Assert.AreEqual ("jebfoobarbaz", connection.TestService ().OptionalArguments ("jeb"));
            Assert.AreEqual ("jebbobbillbaz", connection.TestService ().OptionalArguments ("jeb", "bob", "bill"));
        }

        [Test]
        public void BlockingProcedure ()
        {
            Assert.AreEqual (0, connection.TestService ().BlockingProcedure (0, 0));
            Assert.AreEqual (1, connection.TestService ().BlockingProcedure (1, 0));
            Assert.AreEqual (1 + 2, connection.TestService ().BlockingProcedure (2));
            Assert.AreEqual (Enumerable.Range (1, 42).Sum (), connection.TestService ().BlockingProcedure (42));
        }

        //    def test_protobuf_enums(self):
        //        Assert.AreEqual (TestSchema.a, self.conn.test_service.enum_return())
        //        Assert.AreEqual (TestSchema.a, self.conn.test_service.enum_echo(TestSchema.a))
        //        Assert.AreEqual (TestSchema.b, self.conn.test_service.enum_echo(TestSchema.b))
        //        Assert.AreEqual (TestSchema.c, self.conn.test_service.enum_echo(TestSchema.c))
        //
        //        Assert.AreEqual (TestSchema.a, self.conn.test_service.enum_default_arg(TestSchema.a))
        //        Assert.AreEqual (TestSchema.c, self.conn.test_service.enum_default_arg())
        //        Assert.AreEqual (TestSchema.b, self.conn.test_service.enum_default_arg(TestSchema.b))

        [Test]
        public void Enums ()
        {
            Assert.AreEqual (CSharpEnum.ValueB, connection.TestService ().CSharpEnumReturn ());
            Assert.AreEqual (CSharpEnum.ValueA, connection.TestService ().CSharpEnumEcho (CSharpEnum.ValueA));
            Assert.AreEqual (CSharpEnum.ValueB, connection.TestService ().CSharpEnumEcho (CSharpEnum.ValueB));
            Assert.AreEqual (CSharpEnum.ValueC, connection.TestService ().CSharpEnumEcho (CSharpEnum.ValueC));

            Assert.AreEqual (CSharpEnum.ValueA, connection.TestService ().CSharpEnumDefaultArg (CSharpEnum.ValueA));
            Assert.AreEqual (CSharpEnum.ValueC, connection.TestService ().CSharpEnumDefaultArg ());
            Assert.AreEqual (CSharpEnum.ValueB, connection.TestService ().CSharpEnumDefaultArg (CSharpEnum.ValueB));
        }

        [Test]
        public void CollectionsList ()
        {
            CollectionAssert.AreEqual (new int[] { }, connection.TestService ().IncrementList (new int[] { }));
            CollectionAssert.AreEqual (new int[] { 1, 2, 3 }, connection.TestService ().IncrementList (new int[] {
                0,
                1,
                2
            }));
        }

        [Test]
        public void Collections ()
        {
            CollectionAssert.AreEqual (
                new Dictionary<string,int> (),
                connection.TestService ().IncrementDictionary (new Dictionary<string,int> ()));
            CollectionAssert.AreEqual (
                new Dictionary<string,int> { { "a",1 }, { "b",2 }, { "c",3 } },
                connection.TestService ().IncrementDictionary (new Dictionary<string,int> {
                    { "a",0 },
                    { "b",1 },
                    { "c",2 }
                }));
            CollectionAssert.AreEqual (new HashSet<int> (), connection.TestService ().IncrementSet (new HashSet<int> ()));
            CollectionAssert.AreEqual (new HashSet<int> { 1, 2, 3 }, connection.TestService ().IncrementSet (new HashSet<int> {
                0,
                1,
                2
            }));
            //Assert.AreEqual (new KRPCClient.Tuple<int,long> (2,3), client.TestService().IncrementTuple (new KRPCClient.Tuple<int,long> (1,2)));
        }

        [Test]
        public void NestedCollections ()
        {
            CollectionAssert.AreEqual (
                new Dictionary<string,IList<int>> (),
                connection.TestService ().IncrementNestedCollection (new Dictionary<string,IList<int>> ()));
            CollectionAssert.AreEqual (
                new Dictionary<string,IList<int>> {
                    { "a", new List<int> { 1, 2 } },
                    { "b",  new List<int> () }, {
                        "c",
                        new List<int> { 3 }
                    }
                },
                connection.TestService ().IncrementNestedCollection (new Dictionary<string,IList<int>> { {
                        "a",
                        new List<int> {
                            0,
                            1
                        }
                    }, {
                        "b",
                        new List<int> ()
                    }, {
                        "c",
                        new List<int> { 2 }
                    }
                }));
        }

        [Test]
        public void CollectionsOfObjects ()
        {
            var l = connection.TestService ().AddToObjectList (new List<TestClass> (), "jeb");
            Assert.AreEqual (1, l.Count);
            Assert.AreEqual ("value=jeb", l [0].GetValue ());
            l = connection.TestService ().AddToObjectList (l, "bob");
            Assert.AreEqual (2, l.Count);
            Assert.AreEqual ("value=jeb", l [0].GetValue ());
            Assert.AreEqual ("value=bob", l [1].GetValue ());
        }

        [Test, Sequential]
        public void LineEndings (
            [Values (
                "foo\nbar",
                "foo\rbar",
                "foo\n\rbar",
                "foo\r\nbar",
                "foo\x10bar",
                "foo\x13bar",
                "foo\x10\x13bar",
                "foo\x13\x10bar"
            )] string data)
        {
            connection.TestService ().StringProperty = data;
            Assert.AreEqual (data, connection.TestService ().StringProperty);
        }

        //def test_types_from_different_connections(self):
        //conn1 = self.connect()
        //conn2 = self.connect()
        //self.assertNotEqual(conn1.test_service.TestClass, conn2.test_service.TestClass)
        //obj2 = conn2.test_service.TestClass(0)
        //obj1 = conn1._types.coerce_to(obj2, conn1._types.as_type('Class(TestService.TestClass)'))
        //self.assertEqual(obj1, obj2)
        //self.assertNotEqual(type(obj1), type(obj2))
        //self.assertEqual(type(obj1), conn1.test_service.TestClass)
        //self.assertEqual(type(obj2), conn2.test_service.TestClass)
    }
}
