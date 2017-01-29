using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInTypeNameRule")]
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    public class ScannerTest
    {
        Services services;

        [SetUp]
        public void SetUp ()
        {
            services = KRPC.Service.KRPC.GetServices ();
            Assert.IsNotNull (services);
        }

        [Test]
        public void Services ()
        {
            Assert.AreEqual (4, services.ServicesList.Count);
            CollectionAssert.AreEquivalent (
                new [] { "KRPC", "TestService", "TestService2", "TestService3Name" },
                services.ServicesList.Select (x => x.Name).ToList ());
        }

        [Test]
        public void TestService ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            Assert.AreEqual (41, service.Procedures.Count);
            Assert.AreEqual (2, service.Classes.Count);
            Assert.AreEqual (1, service.Enumerations.Count);
            Assert.AreEqual ("<doc>\n<summary>\nTest service documentation.\n</summary>\n</doc>", service.Documentation);
        }

        [Test]
        public void TestService2 ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService2");
            Assert.AreEqual (2, service.Procedures.Count);
            Assert.AreEqual (0, service.Classes.Count);
            Assert.AreEqual (0, service.Enumerations.Count);
            Assert.AreEqual ("<doc>\n<summary>\nTestService2 documentation.\n</summary>\n</doc>", service.Documentation);
        }

        [Test]
        public void TestService3Name ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService3Name");
            Assert.AreEqual (1, service.Procedures.Count);
            Assert.AreEqual (1, service.Classes.Count);
            Assert.AreEqual (0, service.Enumerations.Count);
            Assert.AreEqual (string.Empty, service.Documentation);
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void TestServiceProcedures ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            int foundProcedures = 0;
            foreach (var proc in service.Procedures) {
                if (proc.Name == "ProcedureNoArgsNoReturn") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasDocumentation (proc, "<doc>\n<summary>\nProcedure with no return arguments.\n</summary>\n</doc>");
                } else if (proc.Name == "ProcedureSingleArgNoReturn") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "KRPC.Response", "data");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasDocumentation (proc, "<doc>\n<summary>\nProcedure with a single return argument.\n</summary>\n</doc>");
                } else if (proc.Name == "ProcedureThreeArgsNoReturn") {
                    MessageAssert.HasParameters (proc, 3);
                    MessageAssert.HasParameter (proc, 0, "KRPC.Response", "x");
                    MessageAssert.HasParameter (proc, 1, "KRPC.Request", "y");
                    MessageAssert.HasParameter (proc, 2, "KRPC.Response", "z");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureNoArgsReturns") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, "KRPC.Response");
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureSingleArgReturns") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "KRPC.Response", "data");
                    MessageAssert.HasReturnType (proc, "KRPC.Response");
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureWithValueTypes") {
                    MessageAssert.HasParameters (proc, 3);
                    MessageAssert.HasParameter (proc, 0, "float", "x");
                    MessageAssert.HasParameter (proc, 1, "string", "y");
                    MessageAssert.HasParameter (proc, 2, "bytes", "z");
                    MessageAssert.HasReturnType (proc, "int32");
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "get_PropertyWithGetAndSet") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, "string");
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "Property.Get(PropertyWithGetAndSet)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "set_PropertyWithGetAndSet") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "string", "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "Property.Set(PropertyWithGetAndSet)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "get_PropertyWithGet") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, "string");
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "Property.Get(PropertyWithGet)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "set_PropertyWithSet") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "string", "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "Property.Set(PropertyWithSet)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "CreateTestObject") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "string", "value");
                    MessageAssert.HasReturnType (proc, "uint64");
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "ReturnType.Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "DeleteTestObject") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "uint64", "obj");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoTestObject") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "uint64", "obj");
                    MessageAssert.HasReturnType (proc, "uint64");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_FloatToString") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasParameter (proc, 1, "float", "x");
                    MessageAssert.HasReturnType (proc, "string");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "Class.Method(TestService.TestClass,FloatToString)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_ObjectToString") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasParameter (proc, 1, "uint64", "other");
                    MessageAssert.HasReturnType (proc, "string");
                    MessageAssert.HasAttributes (proc, 3);
                    MessageAssert.HasAttribute (proc, 0, "Class.Method(TestService.TestClass,ObjectToString)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasAttribute (proc, 2, "ParameterType(1).Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_IntToString") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasParameterWithDefaultValue (proc, 1, "int32", "x", 42);
                    MessageAssert.HasReturnType (proc, "string");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "Class.Method(TestService.TestClass,IntToString)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_get_IntProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasReturnType (proc, "int32");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "Class.Property.Get(TestService.TestClass,IntProperty)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_set_IntProperty") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasParameter (proc, 1, "int32", "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "Class.Property.Set(TestService.TestClass,IntProperty)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_get_ObjectProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasReturnType (proc, "uint64");
                    MessageAssert.HasAttributes (proc, 3);
                    MessageAssert.HasAttribute (proc, 0, "Class.Property.Get(TestService.TestClass,ObjectProperty)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasAttribute (proc, 2, "ReturnType.Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_set_ObjectProperty") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasParameter (proc, 1, "uint64", "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasAttributes (proc, 3);
                    MessageAssert.HasAttribute (proc, 0, "Class.Property.Set(TestService.TestClass,ObjectProperty)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasAttribute (proc, 2, "ParameterType(1).Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_StaticMethod") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, "string", "a", string.Empty);
                    MessageAssert.HasReturnType (proc, "string");
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "Class.StaticMethod(TestService.TestClass,StaticMethod)");
                } else if (proc.Name == "TestTopLevelClass_AMethod") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasParameter (proc, 1, "int32", "x");
                    MessageAssert.HasReturnType (proc, "string");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "Class.Method(TestService.TestTopLevelClass,AMethod)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestTopLevelClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestTopLevelClass_get_AProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasReturnType (proc, "string");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "Class.Property.Get(TestService.TestTopLevelClass,AProperty)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestTopLevelClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestTopLevelClass_set_AProperty") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, "uint64", "this");
                    MessageAssert.HasParameter (proc, 1, "string", "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "Class.Property.Set(TestService.TestTopLevelClass,AProperty)");
                    MessageAssert.HasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestTopLevelClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureSingleOptionalArgNoReturn") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, "string", "x", "foo");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureThreeOptionalArgsNoReturn") {
                    MessageAssert.HasParameters (proc, 3);
                    MessageAssert.HasParameter (proc, 0, "float", "x");
                    MessageAssert.HasParameterWithDefaultValue (proc, 1, "string", "y", "jeb");
                    MessageAssert.HasParameterWithDefaultValue (proc, 2, "int32", "z", 42);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureOptionalNullArg") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, "uint64", "x", null);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureEnumArg") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "int32", "x");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Enum(TestService.TestEnum)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureEnumReturn") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, "int32");
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "ReturnType.Enum(TestService.TestEnum)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "BlockingProcedureNoReturn") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "int32", "n");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "BlockingProcedureReturns") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, "int32", "n");
                    MessageAssert.HasParameterWithDefaultValue (proc, 1, "int32", "sum", 0);
                    MessageAssert.HasReturnType (proc, "int32");
                    MessageAssert.HasNoAttributes (proc);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoList") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "KRPC.List", "l");
                    MessageAssert.HasReturnType (proc, "KRPC.List");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).List(string)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.List(string)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoDictionary") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "KRPC.Dictionary", "d");
                    MessageAssert.HasReturnType (proc, "KRPC.Dictionary");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Dictionary(int32,string)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.Dictionary(int32,string)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoSet") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "KRPC.Set", "h");
                    MessageAssert.HasReturnType (proc, "KRPC.Set");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Set(int32)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.Set(int32)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoTuple") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "KRPC.Tuple", "t");
                    MessageAssert.HasReturnType (proc, "KRPC.Tuple");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Tuple(int32,bool)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.Tuple(int32,bool)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNestedCollection") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "KRPC.Dictionary", "c");
                    MessageAssert.HasReturnType (proc, "KRPC.Dictionary");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Dictionary(int32,List(string))");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.Dictionary(int32,List(string))");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoListOfObjects") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "KRPC.List", "l");
                    MessageAssert.HasReturnType (proc, "KRPC.List");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).List(Class(TestService.TestClass))");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.List(Class(TestService.TestClass))");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TupleDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, "KRPC.Tuple", "x", new KRPC.Utils.Tuple<int,bool> (1, false));
                    MessageAssert.HasReturnType (proc, "KRPC.Tuple");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Tuple(int32,bool)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.Tuple(int32,bool)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ListDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, "KRPC.List", "x", new List<int> { 1, 2, 3 });
                    MessageAssert.HasReturnType (proc, "KRPC.List");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).List(int32)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.List(int32)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "SetDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, "KRPC.Set", "x", new HashSet<int> { 1, 2, 3 });
                    MessageAssert.HasReturnType (proc, "KRPC.Set");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Set(int32)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.Set(int32)");
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "DictionaryDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, "KRPC.Dictionary", "x",
                        new Dictionary<int,bool> { { 1,false }, { 2,true } });
                    MessageAssert.HasReturnType (proc, "KRPC.Dictionary");
                    MessageAssert.HasAttributes (proc, 2);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Dictionary(int32,bool)");
                    MessageAssert.HasAttribute (proc, 1, "ReturnType.Dictionary(int32,bool)");
                    MessageAssert.HasNoDocumentation (proc);
                } else {
                    Assert.Fail ();
                }
                foundProcedures++;
            }
            Assert.AreEqual (41, foundProcedures);
            Assert.AreEqual (41, service.Procedures.Count);
        }

        [Test]
        public void TestServiceClasses ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            int foundClasses = 0;
            foreach (var cls in service.Classes) {
                if (cls.Name == "TestClass") {
                    MessageAssert.HasNoDocumentation (cls);
                } else if (cls.Name == "TestTopLevelClass") {
                    MessageAssert.HasDocumentation (cls, "<doc>\n<summary>\nA class defined at the top level, but included in a service\n</summary>\n</doc>");
                } else {
                    Assert.Fail ();
                }
                foundClasses++;
            }
            Assert.AreEqual (2, foundClasses);
            Assert.AreEqual (2, service.Classes.Count);
        }

        [Test]
        public void TestServiceEnumerations ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            int foundEnumerations = 0;
            foreach (var enumeration in service.Enumerations) {
                if (enumeration.Name == "TestEnum") {
                    MessageAssert.HasDocumentation (enumeration, "<doc>\n<summary>\nDocumentation string for TestEnum.\n</summary>\n</doc>");
                    MessageAssert.HasValues (enumeration, 3);
                    MessageAssert.HasValue (enumeration, 0, "X", 0, "<doc>\n<summary>\nDocumented enum field\n</summary>\n</doc>");
                    MessageAssert.HasValue (enumeration, 1, "Y", 1);
                    MessageAssert.HasValue (enumeration, 2, "Z", 2);
                } else {
                    Assert.Fail ();
                }
                foundEnumerations++;
            }
            Assert.AreEqual (1, foundEnumerations);
            Assert.AreEqual (1, service.Enumerations.Count);
        }

        [Test]
        public void TestService2Procedures ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService2");
            int foundProcedures = 0;
            foreach (var proc in service.Procedures) {
                if (proc.Name == "ClassTypeFromOtherServiceAsParameter") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "uint64", "obj");
                    MessageAssert.HasReturnType (proc, "int32");
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "ParameterType(0).Class(TestService.TestClass)");
                    MessageAssert.HasDocumentation (proc, "<doc>\n<summary>\nTestService2 procedure documentation.\n</summary>\n</doc>");
                } else if (proc.Name == "ClassTypeFromOtherServiceAsReturn") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, "string", "value");
                    MessageAssert.HasReturnType (proc, "uint64");
                    MessageAssert.HasAttributes (proc, 1);
                    MessageAssert.HasAttribute (proc, 0, "ReturnType.Class(TestService.TestClass)");
                    MessageAssert.HasNoDocumentation (proc);
                } else {
                    Assert.Fail ();
                }
                foundProcedures++;
            }
            Assert.AreEqual (2, foundProcedures);
            Assert.AreEqual (2, service.Procedures.Count);
        }
    }
}
