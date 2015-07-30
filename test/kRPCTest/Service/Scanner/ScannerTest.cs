using System.Linq;
using NUnit.Framework;

namespace KRPCTest.Service.Scanner
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
            Assert.AreEqual (4, services.Services_Count);
            CollectionAssert.AreEquivalent (
                new [] { "KRPC", "TestService", "TestService2", "TestService3Name" },
                services.Services_List.Select (x => x.Name).ToList ());
        }

        [Test]
        public void TestService ()
        {
            var service = services.Services_List.First (x => x.Name == "TestService");
            Assert.AreEqual (39, service.ProceduresCount);
            Assert.AreEqual (2, service.ClassesCount);
            Assert.AreEqual (1, service.EnumerationsCount);
            Assert.IsTrue (service.HasDocumentation);
            Assert.AreEqual ("<doc>\n  <summary>\nTest service documentation.\n</summary>\n</doc>", service.Documentation);
        }

        [Test]
        public void TestService2 ()
        {
            var service = services.Services_List.First (x => x.Name == "TestService2");
            Assert.AreEqual (2, service.ProceduresCount);
            Assert.AreEqual (0, service.ClassesCount);
            Assert.AreEqual (0, service.EnumerationsCount);
            Assert.IsTrue (service.HasDocumentation);
            Assert.AreEqual ("<doc>\n  <summary>\nTestService2 documentation.\n</summary>\n</doc>", service.Documentation);
        }

        [Test]
        public void TestService3Name ()
        {
            var service = services.Services_List.First (x => x.Name == "TestService3Name");
            Assert.AreEqual (1, service.ProceduresCount);
            Assert.AreEqual (1, service.ClassesCount);
            Assert.AreEqual (0, service.EnumerationsCount);
            Assert.IsFalse (service.HasDocumentation);
        }

        [Test]
        public void TestServiceProcedures ()
        {
            var service = services.Services_List.First (x => x.Name == "TestService");
            int foundProcedures = 0;
            foreach (var proc in service.ProceduresList) {
                if (proc.Name == "ProcedureNoArgsNoReturn") {
                    Assert.AreEqual (0, proc.ParametersCount);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsTrue (proc.HasDocumentation);
                    Assert.AreEqual ("<doc>\n  <summary>\nProcedure with no return arguments.\n</summary>\n</doc>", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleArgNoReturn") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("data", proc.ParametersList [0].Name);
                    Assert.AreEqual ("KRPC.Response", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsTrue (proc.HasDocumentation);
                    Assert.AreEqual ("<doc>\n  <summary>\nProcedure with a single return argument.\n</summary>\n</doc>", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureThreeArgsNoReturn") {
                    Assert.AreEqual (3, proc.ParametersCount);
                    Assert.AreEqual ("x", proc.ParametersList [0].Name);
                    Assert.AreEqual ("y", proc.ParametersList [1].Name);
                    Assert.AreEqual ("z", proc.ParametersList [2].Name);
                    Assert.AreEqual ("KRPC.Response", proc.ParametersList [0].Type);
                    Assert.AreEqual ("KRPC.Request", proc.ParametersList [1].Type);
                    Assert.AreEqual ("KRPC.Response", proc.ParametersList [2].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [2].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureNoArgsReturns") {
                    Assert.AreEqual (0, proc.ParametersCount);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("KRPC.Response", proc.ReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleArgReturns") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("data", proc.ParametersList [0].Name);
                    Assert.AreEqual ("KRPC.Response", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("KRPC.Response", proc.ReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureWithValueTypes") {
                    Assert.AreEqual (3, proc.ParametersCount);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("x", proc.ParametersList [0].Name);
                    Assert.AreEqual ("y", proc.ParametersList [1].Name);
                    Assert.AreEqual ("z", proc.ParametersList [2].Name);
                    Assert.AreEqual ("float", proc.ParametersList [0].Type);
                    Assert.AreEqual ("string", proc.ParametersList [1].Type);
                    Assert.AreEqual ("bytes", proc.ParametersList [2].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [2].HasDefaultArgument);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "get_PropertyWithGetAndSet") {
                    Assert.AreEqual (0, proc.ParametersCount);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("Property.Get(PropertyWithGetAndSet)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "set_PropertyWithGetAndSet") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("value", proc.ParametersList [0].Name);
                    Assert.AreEqual ("string", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("Property.Set(PropertyWithGetAndSet)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "get_PropertyWithGet") {
                    Assert.AreEqual (0, proc.ParametersCount);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("Property.Get(PropertyWithGet)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "set_PropertyWithSet") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("value", proc.ParametersList [0].Name);
                    Assert.AreEqual ("string", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("Property.Set(PropertyWithSet)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "CreateTestObject") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("value", proc.ParametersList [0].Name);
                    Assert.AreEqual ("string", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("uint64", proc.ReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "DeleteTestObject") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("obj", proc.ParametersList [0].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoTestObject") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("obj", proc.ParametersList [0].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("uint64", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [0]);
                    Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_FloatToString") {
                    Assert.AreEqual (2, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("x", proc.ParametersList [1].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.AreEqual ("float", proc.ParametersList [1].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("Class.Method(TestService.TestClass,FloatToString)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_ObjectToString") {
                    Assert.AreEqual (2, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("other", proc.ParametersList [1].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.AreEqual ("uint64", proc.ParametersList [1].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (3, proc.AttributesCount);
                    Assert.AreEqual ("Class.Method(TestService.TestClass,ObjectToString)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [1]);
                    Assert.AreEqual ("ParameterType(1).Class(TestService.TestClass)", proc.AttributesList [2]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_IntToString") {
                    Assert.AreEqual (2, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("x", proc.ParametersList [1].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.AreEqual ("int32", proc.ParametersList [1].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.ParametersList [1].HasDefaultArgument);
                    Assert.AreEqual (new byte[] { 0x2a }, proc.ParametersList [1].DefaultArgument.ToByteArray ());
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("Class.Method(TestService.TestClass,IntToString)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_get_IntProperty") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("Class.Property.Get(TestService.TestClass,IntProperty)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_set_IntProperty") {
                    Assert.AreEqual (2, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("value", proc.ParametersList [1].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.AreEqual ("int32", proc.ParametersList [1].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("Class.Property.Set(TestService.TestClass,IntProperty)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_get_ObjectProperty") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("uint64", proc.ReturnType);
                    Assert.AreEqual (3, proc.AttributesCount);
                    Assert.AreEqual ("Class.Property.Get(TestService.TestClass,ObjectProperty)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [1]);
                    Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", proc.AttributesList [2]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_set_ObjectProperty") {
                    Assert.AreEqual (2, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("value", proc.ParametersList [1].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.AreEqual ("uint64", proc.ParametersList [1].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (3, proc.AttributesCount);
                    Assert.AreEqual ("Class.Property.Set(TestService.TestClass,ObjectProperty)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [1]);
                    Assert.AreEqual ("ParameterType(1).Class(TestService.TestClass)", proc.AttributesList [2]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_StaticMethod") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("a", proc.ParametersList [0].Name);
                    Assert.AreEqual ("string", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.ParametersList [0].HasDefaultArgument);
                    Assert.AreEqual (new byte[] { 0x00 }, proc.ParametersList [0].DefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("Class.StaticMethod(TestService.TestClass,StaticMethod)", proc.AttributesList [0]);
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_AMethod") {
                    Assert.AreEqual (2, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("x", proc.ParametersList [1].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.AreEqual ("int32", proc.ParametersList [1].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("Class.Method(TestService.TestTopLevelClass,AMethod)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_get_AProperty") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("string", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("Class.Property.Get(TestService.TestTopLevelClass,AProperty)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_set_AProperty") {
                    Assert.AreEqual (2, proc.ParametersCount);
                    Assert.AreEqual ("this", proc.ParametersList [0].Name);
                    Assert.AreEqual ("value", proc.ParametersList [1].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.AreEqual ("string", proc.ParametersList [1].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("Class.Property.Set(TestService.TestTopLevelClass,AProperty)", proc.AttributesList [0]);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleOptionalArgNoReturn") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("x", proc.ParametersList [0].Name);
                    Assert.AreEqual ("string", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.ParametersList [0].HasDefaultArgument);
                    Assert.AreEqual (new byte[] { 0x03, 0x66, 0x6f, 0x6f }, proc.ParametersList [0].DefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureThreeOptionalArgsNoReturn") {
                    Assert.AreEqual (3, proc.ParametersCount);
                    Assert.AreEqual ("x", proc.ParametersList [0].Name);
                    Assert.AreEqual ("y", proc.ParametersList [1].Name);
                    Assert.AreEqual ("z", proc.ParametersList [2].Name);
                    Assert.AreEqual ("float", proc.ParametersList [0].Type);
                    Assert.AreEqual ("string", proc.ParametersList [1].Type);
                    Assert.AreEqual ("int32", proc.ParametersList [2].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.ParametersList [1].HasDefaultArgument);
                    Assert.IsTrue (proc.ParametersList [2].HasDefaultArgument);
                    Assert.AreEqual (new byte[] { 0x03, 0x6a, 0x65, 0x62 }, proc.ParametersList [1].DefaultArgument);
                    Assert.AreEqual (new byte[] { 0x2a }, proc.ParametersList [2].DefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureOptionalNullArg") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("x", proc.ParametersList [0].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.ParametersList [0].HasDefaultArgument);
                    Assert.AreEqual (new byte[] { 0x00 }, proc.ParametersList [0].DefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureEnumArg") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("x", proc.ParametersList [0].Name);
                    Assert.AreEqual ("Test.TestEnum", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureEnumReturn") {
                    Assert.AreEqual (0, proc.ParametersCount);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("Test.TestEnum", proc.ReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureCSharpEnumArg") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("x", proc.ParametersList [0].Name);
                    Assert.AreEqual ("int32", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Enum(TestService.CSharpEnum)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureCSharpEnumReturn") {
                    Assert.AreEqual (0, proc.ParametersCount);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("ReturnType.Enum(TestService.CSharpEnum)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "BlockingProcedureNoReturn") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("n", proc.ParametersList [0].Name);
                    Assert.AreEqual ("int32", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.HasReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "BlockingProcedureReturns") {
                    Assert.AreEqual (2, proc.ParametersCount);
                    Assert.AreEqual ("n", proc.ParametersList [0].Name);
                    Assert.AreEqual ("int32", proc.ParametersList [0].Type);
                    Assert.AreEqual ("sum", proc.ParametersList [1].Name);
                    Assert.AreEqual ("int32", proc.ParametersList [1].Type);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (0, proc.AttributesCount);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoList") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("l", proc.ParametersList [0].Name);
                    Assert.AreEqual ("KRPC.List", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("KRPC.List", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).List(string)", proc.AttributesList [0]);
                    Assert.AreEqual ("ReturnType.List(string)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoDictionary") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("d", proc.ParametersList [0].Name);
                    Assert.AreEqual ("KRPC.Dictionary", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("KRPC.Dictionary", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Dictionary(int32,string)", proc.AttributesList [0]);
                    Assert.AreEqual ("ReturnType.Dictionary(int32,string)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoSet") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("h", proc.ParametersList [0].Name);
                    Assert.AreEqual ("KRPC.Set", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("KRPC.Set", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Set(int32)", proc.AttributesList [0]);
                    Assert.AreEqual ("ReturnType.Set(int32)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoTuple") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("t", proc.ParametersList [0].Name);
                    Assert.AreEqual ("KRPC.Tuple", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("KRPC.Tuple", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Tuple(int32,bool)", proc.AttributesList [0]);
                    Assert.AreEqual ("ReturnType.Tuple(int32,bool)", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoNestedCollection") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("c", proc.ParametersList [0].Name);
                    Assert.AreEqual ("KRPC.Dictionary", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("KRPC.Dictionary", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Dictionary(int32,List(string))", proc.AttributesList [0]);
                    Assert.AreEqual ("ReturnType.Dictionary(int32,List(string))", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
                if (proc.Name == "EchoListOfObjects") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("l", proc.ParametersList [0].Name);
                    Assert.AreEqual ("KRPC.List", proc.ParametersList [0].Type);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("KRPC.List", proc.ReturnType);
                    Assert.AreEqual (2, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).List(Class(TestService.TestClass))", proc.AttributesList [0]);
                    Assert.AreEqual ("ReturnType.List(Class(TestService.TestClass))", proc.AttributesList [1]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
            }
            Assert.AreEqual (39, foundProcedures);
            Assert.AreEqual (39, service.ProceduresCount);
        }

        [Test]
        public void TestServiceClasses ()
        {
            var service = services.Services_List.First (x => x.Name == "TestService");
            int foundClasses = 0;
            foreach (var cls in service.ClassesList) {
                if (cls.Name == "TestClass") {
                    foundClasses++;
                    Assert.IsFalse (cls.HasDocumentation);
                }
                if (cls.Name == "TestTopLevelClass") {
                    foundClasses++;
                    Assert.AreEqual ("<doc>\n  <summary>\nA class defined at the top level, but included in a service\n</summary>\n</doc>", cls.Documentation);
                }
            }
            Assert.AreEqual (2, foundClasses);
            Assert.AreEqual (2, service.ClassesCount);
        }

        [Test]
        public void TestServiceEnumerations ()
        {
            var service = services.Services_List.First (x => x.Name == "TestService");
            int foundEnumerations = 0;
            foreach (var enm in service.EnumerationsList) {
                if (enm.Name == "CSharpEnum") {
                    Assert.AreEqual ("<doc>\n  <summary>\nDocumentation string for CSharpEnum.\n</summary>\n</doc>", enm.Documentation);
                    Assert.AreEqual (3, enm.ValuesCount);
                    Assert.AreEqual ("x", enm.ValuesList [0].Name);
                    Assert.AreEqual (0, enm.ValuesList [0].Value);
                    Assert.AreEqual ("<doc>\n  <summary>\nDocumented enum field\n</summary>\n</doc>", enm.ValuesList [0].Documentation);
                    Assert.AreEqual ("y", enm.ValuesList [1].Name);
                    Assert.AreEqual (1, enm.ValuesList [1].Value);
                    Assert.IsFalse (enm.ValuesList [1].HasDocumentation);
                    Assert.AreEqual ("z", enm.ValuesList [2].Name);
                    Assert.AreEqual (2, enm.ValuesList [2].Value);
                    Assert.IsFalse (enm.ValuesList [2].HasDocumentation);
                    foundEnumerations++;
                }
            }
            Assert.AreEqual (1, foundEnumerations);
            Assert.AreEqual (1, service.EnumerationsCount);
        }

        [Test]
        public void TestService2Procedures ()
        {
            var service = services.Services_List.First (x => x.Name == "TestService2");
            int foundProcedures = 0;
            foreach (var proc in service.ProceduresList) {
                if (proc.Name == "ClassTypeFromOtherServiceAsParameter") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("obj", proc.ParametersList [0].Name);
                    Assert.AreEqual ("uint64", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("int32", proc.ReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", proc.AttributesList [0]);
                    Assert.IsTrue (proc.HasDocumentation);
                    Assert.AreEqual ("<doc>\n  <summary>\nTestService2 procedure documentation.\n</summary>\n</doc>", proc.Documentation);
                    foundProcedures++;
                }
                if (proc.Name == "ClassTypeFromOtherServiceAsReturn") {
                    Assert.AreEqual (1, proc.ParametersCount);
                    Assert.AreEqual ("value", proc.ParametersList [0].Name);
                    Assert.AreEqual ("string", proc.ParametersList [0].Type);
                    Assert.IsFalse (proc.ParametersList [0].HasDefaultArgument);
                    Assert.IsTrue (proc.HasReturnType);
                    Assert.AreEqual ("uint64", proc.ReturnType);
                    Assert.AreEqual (1, proc.AttributesCount);
                    Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", proc.AttributesList [0]);
                    Assert.IsFalse (proc.HasDocumentation);
                    foundProcedures++;
                }
            }
            Assert.AreEqual (2, foundProcedures);
            Assert.AreEqual (2, service.ProceduresCount);
        }
    }
}

