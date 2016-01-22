using System.Linq;
using NUnit.Framework;

namespace KRPC.Test.Service.Scanner
{
    [TestFixture]
    public class ScannerTest
    {
        KRPC.Schema.KRPC.Services services;

        [SetUp]
        public void SetUp ()
        {
            services = KRPC.Service.KRPC.GetServices ();
            Assert.IsNotNull (services);
        }

        [Test]
        public void Services ()
        {
            Assert.AreEqual (4, services.Services_.Count);
            CollectionAssert.AreEquivalent (
                new [] { "KRPC", "TestService", "TestService2", "TestService3Name" },
                services.Services_.Select (x => x.Name).ToList ());
        }

        [Test]
        public void TestService ()
        {
            var service = services.Services_.First (x => x.Name == "TestService");
            Assert.AreEqual (39, service.Procedures.Count);
            Assert.AreEqual (2, service.Classes.Count);
            Assert.AreEqual (1, service.Enumerations.Count);
            Assert.AreEqual ("<doc>\n  <summary>\nTest service documentation.\n</summary>\n</doc>", service.Documentation);
        }

        [Test]
        public void TestService2 ()
        {
            var service = services.Services_.First (x => x.Name == "TestService2");
            Assert.AreEqual (2, service.Procedures.Count);
            Assert.AreEqual (0, service.Classes.Count);
            Assert.AreEqual (0, service.Enumerations.Count);
            Assert.AreEqual ("<doc>\n  <summary>\nTestService2 documentation.\n</summary>\n</doc>", service.Documentation);
        }

        [Test]
        public void TestService3Name ()
        {
            var service = services.Services_.First (x => x.Name == "TestService3Name");
            Assert.AreEqual (1, service.Procedures.Count);
            Assert.AreEqual (1, service.Classes.Count);
            Assert.AreEqual (0, service.Enumerations.Count);
            Assert.AreEqual ("", service.Documentation);
        }

        [Test]
        public void TestServiceProcedures ()
        {
            var service = services.Services_.First (x => x.Name == "TestService");
            int foundProcedures = 0;
            foreach (var proc in service.Procedures) {
                if (proc.Name == "ProcedureNoArgsNoReturn") {
                    Assert.AreEqual (0, proc.Parameters.Count);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("<doc>\n  <summary>\nProcedure with no return arguments.\n</summary>\n</doc>", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleArgNoReturn") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("data", proc.Parameters [0].Name);
                    Assert.AreEqual ("KRPC.Response", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("<doc>\n  <summary>\nProcedure with a single return argument.\n</summary>\n</doc>", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureThreeArgsNoReturn") {
                    Assert.AreEqual (3, proc.Parameters.Count);
                    Assert.AreEqual ("x", proc.Parameters [0].Name);
                    Assert.AreEqual ("y", proc.Parameters [1].Name);
                    Assert.AreEqual ("z", proc.Parameters [2].Name);
                    Assert.AreEqual ("KRPC.Response", proc.Parameters [0].Type);
                    Assert.AreEqual ("KRPC.Request", proc.Parameters [1].Type);
                    Assert.AreEqual ("KRPC.Response", proc.Parameters [2].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [1].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [2].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureNoArgsReturns") {
                    Assert.AreEqual (0, proc.Parameters.Count);
                    Assert.AreEqual ("KRPC.Response", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleArgReturns") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("data", proc.Parameters [0].Name);
                    Assert.AreEqual ("KRPC.Response", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("KRPC.Response", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureWithValueTypes") {
                    Assert.AreEqual (3, proc.Parameters.Count);
                    Assert.AreEqual ("x", proc.Parameters [0].Name);
                    Assert.AreEqual ("y", proc.Parameters [1].Name);
                    Assert.AreEqual ("z", proc.Parameters [2].Name);
                    Assert.AreEqual ("float", proc.Parameters [0].Type);
                    Assert.AreEqual ("string", proc.Parameters [1].Type);
                    Assert.AreEqual ("bytes", proc.Parameters [2].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [1].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [2].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "get_PropertyWithGetAndSet") {
                    Assert.AreEqual (0, proc.Parameters.Count);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("Property.Get(PropertyWithGetAndSet)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "set_PropertyWithGetAndSet") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("value", proc.Parameters [0].Name);
                    Assert.AreEqual ("string", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("Property.Set(PropertyWithGetAndSet)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "get_PropertyWithGet") {
                    Assert.AreEqual (0, proc.Parameters.Count);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("Property.Get(PropertyWithGet)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "set_PropertyWithSet") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("value", proc.Parameters [0].Name);
                    Assert.AreEqual ("string", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("Property.Set(PropertyWithSet)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "CreateTestObject") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("value", proc.Parameters [0].Name);
                    Assert.AreEqual ("string", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("uint64", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "DeleteTestObject") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("obj", proc.Parameters [0].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoTestObject") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("obj", proc.Parameters [0].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("uint64", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [0]);
                    Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_FloatToString") {
                    Assert.AreEqual (2, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("x", proc.Parameters [1].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.AreEqual ("float", proc.Parameters [1].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [1].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Method(TestService.TestClass,FloatToString)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_ObjectToString") {
                    Assert.AreEqual (2, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("other", proc.Parameters [1].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.AreEqual ("uint64", proc.Parameters [1].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [1].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (3, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Method(TestService.TestClass,ObjectToString)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [1]);
                    Assert.AreEqual ("ParameterType(1).Class(TestService.TestClass)", proc.Attributes [2]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_IntToString") {
                    Assert.AreEqual (2, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("x", proc.Parameters [1].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.AreEqual ("int32", proc.Parameters [1].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual (new byte[] { 0x2a }, proc.Parameters [1].DefaultArgument.ToByteArray ());
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Method(TestService.TestClass,IntToString)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_get_IntProperty") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Property.Get(TestService.TestClass,IntProperty)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_set_IntProperty") {
                    Assert.AreEqual (2, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("value", proc.Parameters [1].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.AreEqual ("int32", proc.Parameters [1].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [1].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Property.Set(TestService.TestClass,IntProperty)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_get_ObjectProperty") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("uint64", proc.ReturnType);
                    Assert.AreEqual (3, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Property.Get(TestService.TestClass,ObjectProperty)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [1]);
                    Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", proc.Attributes [2]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_set_ObjectProperty") {
                    Assert.AreEqual (2, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("value", proc.Parameters [1].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.AreEqual ("uint64", proc.Parameters [1].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [1].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (3, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Property.Set(TestService.TestClass,ObjectProperty)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [1]);
                    Assert.AreEqual ("ParameterType(1).Class(TestService.TestClass)", proc.Attributes [2]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_StaticMethod") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("a", proc.Parameters [0].Name);
                    Assert.AreEqual ("string", proc.Parameters [0].Type);
                    Assert.AreEqual (new byte[] { 0x00 }, proc.Parameters [0].DefaultArgument);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("Class.StaticMethod(TestService.TestClass,StaticMethod)", proc.Attributes [0]);
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_AMethod") {
                    Assert.AreEqual (2, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("x", proc.Parameters [1].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.AreEqual ("int32", proc.Parameters [1].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [1].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Method(TestService.TestTopLevelClass,AMethod)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_get_AProperty") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Property.Get(TestService.TestTopLevelClass,AProperty)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_set_AProperty") {
                    Assert.AreEqual (2, proc.Parameters.Count);
                    Assert.AreEqual ("this", proc.Parameters [0].Name);
                    Assert.AreEqual ("value", proc.Parameters [1].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.AreEqual ("string", proc.Parameters [1].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.IsTrue (proc.Parameters [1].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("Class.Property.Set(TestService.TestTopLevelClass,AProperty)", proc.Attributes [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleOptionalArgNoReturn") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("x", proc.Parameters [0].Name);
                    Assert.AreEqual ("string", proc.Parameters [0].Type);
                    Assert.AreEqual (new byte[] { 0x03, 0x66, 0x6f, 0x6f }, proc.Parameters [0].DefaultArgument);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureThreeOptionalArgsNoReturn") {
                    Assert.AreEqual (3, proc.Parameters.Count);
                    Assert.AreEqual ("x", proc.Parameters [0].Name);
                    Assert.AreEqual ("y", proc.Parameters [1].Name);
                    Assert.AreEqual ("z", proc.Parameters [2].Name);
                    Assert.AreEqual ("float", proc.Parameters [0].Type);
                    Assert.AreEqual ("string", proc.Parameters [1].Type);
                    Assert.AreEqual ("int32", proc.Parameters [2].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual (new byte[] { 0x03, 0x6a, 0x65, 0x62 }, proc.Parameters [1].DefaultArgument);
                    Assert.AreEqual (new byte[] { 0x2a }, proc.Parameters [2].DefaultArgument);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureOptionalNullArg") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("x", proc.Parameters [0].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.AreEqual (new byte[] { 0x00 }, proc.Parameters [0].DefaultArgument);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureEnumArg") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("x", proc.Parameters [0].Name);
                    Assert.AreEqual ("Test.TestEnum", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureEnumReturn") {
                    Assert.AreEqual (0, proc.Parameters.Count);
                    Assert.AreEqual ("Test.TestEnum", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureCSharpEnumArg") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("x", proc.Parameters [0].Name);
                    Assert.AreEqual ("int32", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Enum(TestService.CSharpEnum)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureCSharpEnumReturn") {
                    Assert.AreEqual (0, proc.Parameters.Count);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("ReturnType.Enum(TestService.CSharpEnum)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "BlockingProcedureNoReturn") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("n", proc.Parameters [0].Name);
                    Assert.AreEqual ("int32", proc.Parameters [0].Type);
                    Assert.AreEqual ("", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "BlockingProcedureReturns") {
                    Assert.AreEqual (2, proc.Parameters.Count);
                    Assert.AreEqual ("n", proc.Parameters [0].Name);
                    Assert.AreEqual ("int32", proc.Parameters [0].Type);
                    Assert.AreEqual ("sum", proc.Parameters [1].Name);
                    Assert.AreEqual ("int32", proc.Parameters [1].Type);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (0, proc.Attributes.Count);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoList") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("l", proc.Parameters [0].Name);
                    Assert.AreEqual ("KRPC.List", proc.Parameters [0].Type);
                    Assert.AreEqual ("KRPC.List", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).List(string)", proc.Attributes [0]);
                    Assert.AreEqual ("ReturnType.List(string)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoDictionary") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("d", proc.Parameters [0].Name);
                    Assert.AreEqual ("KRPC.Dictionary", proc.Parameters [0].Type);
                    Assert.AreEqual ("KRPC.Dictionary", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Dictionary(int32,string)", proc.Attributes [0]);
                    Assert.AreEqual ("ReturnType.Dictionary(int32,string)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoSet") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("h", proc.Parameters [0].Name);
                    Assert.AreEqual ("KRPC.Set", proc.Parameters [0].Type);
                    Assert.AreEqual ("KRPC.Set", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Set(int32)", proc.Attributes [0]);
                    Assert.AreEqual ("ReturnType.Set(int32)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoTuple") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("t", proc.Parameters [0].Name);
                    Assert.AreEqual ("KRPC.Tuple", proc.Parameters [0].Type);
                    Assert.AreEqual ("KRPC.Tuple", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Tuple(int32,bool)", proc.Attributes [0]);
                    Assert.AreEqual ("ReturnType.Tuple(int32,bool)", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoNestedCollection") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("c", proc.Parameters [0].Name);
                    Assert.AreEqual ("KRPC.Dictionary", proc.Parameters [0].Type);
                    Assert.AreEqual ("KRPC.Dictionary", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Dictionary(int32,List(string))", proc.Attributes [0]);
                    Assert.AreEqual ("ReturnType.Dictionary(int32,List(string))", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoListOfObjects") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("l", proc.Parameters [0].Name);
                    Assert.AreEqual ("KRPC.List", proc.Parameters [0].Type);
                    Assert.AreEqual ("KRPC.List", proc.ReturnType);
                    Assert.AreEqual (2, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).List(Class(TestService.TestClass))", proc.Attributes [0]);
                    Assert.AreEqual ("ReturnType.List(Class(TestService.TestClass))", proc.Attributes [1]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
            }
            Assert.AreEqual (39, foundProcedures);
            Assert.AreEqual (39, service.Procedures.Count);
        }

        [Test]
        public void TestServiceClasses ()
        {
            var service = services.Services_.First (x => x.Name == "TestService");
            int foundClasses = 0;
            foreach (var cls in service.Classes) {
                if (cls.Name == "TestClass") {
                    foundClasses++;
                    Assert.AreEqual ("", cls.Documentation);
                }
                if (cls.Name == "TestTopLevelClass") {
                    foundClasses++;
                    Assert.AreEqual ("<doc>\n  <summary>\nA class defined at the top level, but included in a service\n</summary>\n</doc>", cls.Documentation);
                }
            }
            Assert.AreEqual (2, foundClasses);
            Assert.AreEqual (2, service.Classes.Count);
        }

        [Test]
        public void TestServiceEnumerations ()
        {
            var service = services.Services_.First (x => x.Name == "TestService");
            int foundEnumerations = 0;
            foreach (var enm in service.Enumerations) {
                if (enm.Name == "CSharpEnum") {
                    Assert.AreEqual ("<doc>\n  <summary>\nDocumentation string for CSharpEnum.\n</summary>\n</doc>", enm.Documentation);
                    Assert.AreEqual (3, enm.Values.Count);
                    Assert.AreEqual ("x", enm.Values [0].Name);
                    Assert.AreEqual (0, enm.Values [0].Value);
                    Assert.AreEqual ("<doc>\n  <summary>\nDocumented enum field\n</summary>\n</doc>", enm.Values [0].Documentation);
                    Assert.AreEqual ("y", enm.Values [1].Name);
                    Assert.AreEqual (1, enm.Values [1].Value);
                    Assert.AreEqual ("", enm.Values [1].Documentation);
                    Assert.AreEqual ("z", enm.Values [2].Name);
                    Assert.AreEqual (2, enm.Values [2].Value);
                    Assert.AreEqual ("", enm.Values [2].Documentation);
                    foundEnumerations++;
                }
            }
            Assert.AreEqual (1, foundEnumerations);
            Assert.AreEqual (1, service.Enumerations.Count);
        }

        [Test]
        public void TestService2Procedures ()
        {
            var service = services.Services_.First (x => x.Name == "TestService2");
            int foundProcedures = 0;
            foreach (var proc in service.Procedures) {
                if (proc.Name == "ClassTypeFromOtherServiceAsParameter") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("obj", proc.Parameters [0].Name);
                    Assert.AreEqual ("uint64", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.Attributes [0]);
                    Assert.AreEqual ("<doc>\n  <summary>\nTestService2 procedure documentation.\n</summary>\n</doc>", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ClassTypeFromOtherServiceAsReturn") {
                    Assert.AreEqual (1, proc.Parameters.Count);
                    Assert.AreEqual ("value", proc.Parameters [0].Name);
                    Assert.AreEqual ("string", proc.Parameters [0].Type);
                    Assert.IsTrue (proc.Parameters [0].DefaultArgument.IsEmpty);
                    Assert.AreEqual ("uint64", proc.ReturnType);
                    Assert.AreEqual (1, proc.Attributes.Count);
                    Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", proc.Attributes [0]);
                    Assert.AreEqual ("", proc.Documentation);
                    foundProcedures++;
                }
            }
            Assert.AreEqual (2, foundProcedures);
            Assert.AreEqual (2, service.Procedures.Count);
        }
    }
}

