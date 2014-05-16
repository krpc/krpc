using NUnit.Framework;
using System;

namespace KRPCTest.Service.Scanner
{
    [TestFixture ()]
    public class ScannerTest
    {
        /// <summary>
        /// Check the output of the service scanner
        /// </summary>
        [Test]
        public void GetServices ()
        {
            var services = KRPC.Service.KRPC.GetServices ();
            Assert.IsNotNull (services);
            Assert.AreEqual (4, services.Services_Count);
            int foundServices = 0;
            foreach (KRPC.Schema.KRPC.Service service in services.Services_List) {
                if (service.Name == "TestService") {
                    foundServices++;
                    Assert.AreEqual (36, service.ProceduresCount);
                    int foundProcedures = 0;
                    foreach (var method in service.ProceduresList) {
                        if (method.Name == "ProcedureNoArgsNoReturn") {
                            Assert.AreEqual (0, method.ParametersCount);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureSingleArgNoReturn") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("data", method.ParametersList [0].Name);
                            Assert.AreEqual ("KRPC.Response", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureThreeArgsNoReturn") {
                            Assert.AreEqual (3, method.ParametersCount);
                            Assert.AreEqual ("x", method.ParametersList [0].Name);
                            Assert.AreEqual ("y", method.ParametersList [1].Name);
                            Assert.AreEqual ("z", method.ParametersList [2].Name);
                            Assert.AreEqual ("KRPC.Response", method.ParametersList [0].Type);
                            Assert.AreEqual ("KRPC.Request", method.ParametersList [1].Type);
                            Assert.AreEqual ("KRPC.Response", method.ParametersList [2].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [2].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureNoArgsReturns") {
                            Assert.AreEqual (0, method.ParametersCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureSingleArgReturns") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("data", method.ParametersList [0].Name);
                            Assert.AreEqual ("KRPC.Response", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureWithValueTypes") {
                            Assert.AreEqual (3, method.ParametersCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("x", method.ParametersList [0].Name);
                            Assert.AreEqual ("y", method.ParametersList [1].Name);
                            Assert.AreEqual ("z", method.ParametersList [2].Name);
                            Assert.AreEqual ("float", method.ParametersList [0].Type);
                            Assert.AreEqual ("string", method.ParametersList [1].Type);
                            Assert.AreEqual ("bytes", method.ParametersList [2].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [2].HasDefaultArgument);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "get_PropertyWithGetAndSet") {
                            Assert.AreEqual (0, method.ParametersCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Get(PropertyWithGetAndSet)", method.AttributesList [0]);
                            foundProcedures++;
                        }
                        if (method.Name == "set_PropertyWithGetAndSet") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("value", method.ParametersList [0].Name);
                            Assert.AreEqual ("string", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Set(PropertyWithGetAndSet)", method.AttributesList [0]);
                            foundProcedures++;
                        }
                        if (method.Name == "get_PropertyWithGet") {
                            Assert.AreEqual (0, method.ParametersCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Get(PropertyWithGet)", method.AttributesList [0]);
                            foundProcedures++;
                        }
                        if (method.Name == "set_PropertyWithSet") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("value", method.ParametersList [0].Name);
                            Assert.AreEqual ("string", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Set(PropertyWithSet)", method.AttributesList [0]);
                            foundProcedures++;
                        }
                        if (method.Name == "CreateTestObject") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("value", method.ParametersList [0].Name);
                            Assert.AreEqual ("string", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint64", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", method.AttributesList [0]);
                            foundProcedures++;
                        }
                        if (method.Name == "DeleteTestObject") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("obj", method.ParametersList [0].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [0]);
                            foundProcedures++;
                        }
                        if (method.Name == "EchoTestObject") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("obj", method.ParametersList [0].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint64", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [0]);
                            Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestClass_FloatToString") {
                            Assert.AreEqual (2, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("x", method.ParametersList [1].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.AreEqual ("float", method.ParametersList [1].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Method(TestService.TestClass,FloatToString)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestClass_ObjectToString") {
                            Assert.AreEqual (2, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("other", method.ParametersList [1].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.AreEqual ("uint64", method.ParametersList [1].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (3, method.AttributesCount);
                            Assert.AreEqual ("Class.Method(TestService.TestClass,ObjectToString)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [1]);
                            Assert.AreEqual ("ParameterType(1).Class(TestService.TestClass)", method.AttributesList [2]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestClass_IntToString") {
                            Assert.AreEqual (2, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("x", method.ParametersList [1].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.AreEqual ("int32", method.ParametersList [1].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.ParametersList [1].HasDefaultArgument);
                            Assert.AreEqual (new byte[] { 0x2a }, method.ParametersList [1].DefaultArgument.ToByteArray ());
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Method(TestService.TestClass,IntToString)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestClass_get_IntProperty") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Get(TestService.TestClass,IntProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestClass_set_IntProperty") {
                            Assert.AreEqual (2, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("value", method.ParametersList [1].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.AreEqual ("int32", method.ParametersList [1].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Set(TestService.TestClass,IntProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestClass_get_ObjectProperty") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint64", method.ReturnType);
                            Assert.AreEqual (3, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Get(TestService.TestClass,ObjectProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [1]);
                            Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", method.AttributesList [2]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestClass_set_ObjectProperty") {
                            Assert.AreEqual (2, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("value", method.ParametersList [1].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.AreEqual ("uint64", method.ParametersList [1].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (3, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Set(TestService.TestClass,ObjectProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [1]);
                            Assert.AreEqual ("ParameterType(1).Class(TestService.TestClass)", method.AttributesList [2]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestTopLevelClass_AMethod") {
                            Assert.AreEqual (2, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("x", method.ParametersList [1].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.AreEqual ("int32", method.ParametersList [1].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Method(TestService.TestTopLevelClass,AMethod)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestTopLevelClass_get_AProperty") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Get(TestService.TestTopLevelClass,AProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "TestTopLevelClass_set_AProperty") {
                            Assert.AreEqual (2, method.ParametersCount);
                            Assert.AreEqual ("this", method.ParametersList [0].Name);
                            Assert.AreEqual ("value", method.ParametersList [1].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.AreEqual ("string", method.ParametersList [1].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Set(TestService.TestTopLevelClass,AProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestTopLevelClass)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureSingleOptionalArgNoReturn") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("x", method.ParametersList [0].Name);
                            Assert.AreEqual ("string", method.ParametersList [0].Type);
                            Assert.IsTrue (method.ParametersList [0].HasDefaultArgument);
                            Assert.AreEqual (new byte[] { 0x03, 0x66, 0x6f, 0x6f}, method.ParametersList [0].DefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureThreeOptionalArgsNoReturn") {
                            Assert.AreEqual (3, method.ParametersCount);
                            Assert.AreEqual ("x", method.ParametersList [0].Name);
                            Assert.AreEqual ("y", method.ParametersList [1].Name);
                            Assert.AreEqual ("z", method.ParametersList [2].Name);
                            Assert.AreEqual ("float", method.ParametersList [0].Type);
                            Assert.AreEqual ("string", method.ParametersList [1].Type);
                            Assert.AreEqual ("int32", method.ParametersList [2].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.ParametersList [1].HasDefaultArgument);
                            Assert.IsTrue (method.ParametersList [2].HasDefaultArgument);
                            Assert.AreEqual (new byte[] {0x03, 0x6a, 0x65, 0x62}, method.ParametersList [1].DefaultArgument);
                            Assert.AreEqual (new byte[] {0x2a}, method.ParametersList [2].DefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureEnumArg") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("x", method.ParametersList [0].Name);
                            Assert.AreEqual ("Test.TestEnum", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureEnumReturn") {
                            Assert.AreEqual (0, method.ParametersCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("Test.TestEnum", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureCSharpEnumArg") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("x", method.ParametersList [0].Name);
                            Assert.AreEqual ("int32", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Enum(TestService.CSharpEnum)", method.AttributesList [0]);
                            foundProcedures++;
                        }
                        if (method.Name == "ProcedureCSharpEnumReturn") {
                            Assert.AreEqual (0, method.ParametersCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ReturnType.Enum(TestService.CSharpEnum)", method.AttributesList [0]);
                            foundProcedures++;
                        }
                        if (method.Name == "BlockingProcedureNoReturn") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("n", method.ParametersList [0].Name);
                            Assert.AreEqual ("int32", method.ParametersList [0].Type);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "BlockingProcedureReturns") {
                            Assert.AreEqual (2, method.ParametersCount);
                            Assert.AreEqual ("n", method.ParametersList [0].Name);
                            Assert.AreEqual ("int32", method.ParametersList [0].Type);
                            Assert.AreEqual ("sum", method.ParametersList [1].Name);
                            Assert.AreEqual ("int32", method.ParametersList [1].Type);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            foundProcedures++;
                        }
                        if (method.Name == "EchoList") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("l", method.ParametersList [0].Name);
                            Assert.AreEqual ("KRPC.List", method.ParametersList [0].Type);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.List", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).List(string)", method.AttributesList [0]);
                            Assert.AreEqual ("ReturnType.List(string)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "EchoDictionary") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("d", method.ParametersList [0].Name);
                            Assert.AreEqual ("KRPC.Dictionary", method.ParametersList [0].Type);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Dictionary", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Dictionary(int32,string)", method.AttributesList [0]);
                            Assert.AreEqual ("ReturnType.Dictionary(int32,string)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "EchoSet") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("h", method.ParametersList [0].Name);
                            Assert.AreEqual ("KRPC.Set", method.ParametersList [0].Type);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Set", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Set(int32)", method.AttributesList [0]);
                            Assert.AreEqual ("ReturnType.Set(int32)", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "EchoNestedCollection") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("c", method.ParametersList [0].Name);
                            Assert.AreEqual ("KRPC.Dictionary", method.ParametersList [0].Type);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Dictionary", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Dictionary(int32,List(string))", method.AttributesList [0]);
                            Assert.AreEqual ("ReturnType.Dictionary(int32,List(string))", method.AttributesList [1]);
                            foundProcedures++;
                        }
                        if (method.Name == "EchoListOfObjects") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("l", method.ParametersList [0].Name);
                            Assert.AreEqual ("KRPC.List", method.ParametersList [0].Type);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.List", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).List(Class(TestService.TestClass))", method.AttributesList [0]);
                            Assert.AreEqual ("ReturnType.List(Class(TestService.TestClass))", method.AttributesList [1]);
                            foundProcedures++;
                        }
                    }
                    Assert.AreEqual (36, foundProcedures);
                    int foundClasses = 0;
                    foreach (var cls in service.ClassesList) {
                        if (cls.Name == "TestClass")
                            foundClasses++;
                        if (cls.Name == "TestTopLevelClass")
                            foundClasses++;
                    }
                    Assert.AreEqual (2, foundClasses);
                    int foundEnums = 0;
                    foreach (var enm in service.EnumerationsList) {
                        if (enm.Name == "CSharpEnum") {
                            Assert.AreEqual (3, enm.ValuesCount);
                            Assert.AreEqual ("x", enm.ValuesList [0].Name);
                            Assert.AreEqual (0, enm.ValuesList [0].Value);
                            Assert.AreEqual ("y", enm.ValuesList [1].Name);
                            Assert.AreEqual (1, enm.ValuesList [1].Value);
                            Assert.AreEqual ("z", enm.ValuesList [2].Name);
                            Assert.AreEqual (2, enm.ValuesList [2].Value);
                            foundEnums++;
                        }
                    }
                    Assert.AreEqual (1, foundEnums);
                }
                if (service.Name == "TestService2") {
                    foundServices++;
                    Assert.AreEqual (2, service.ProceduresCount);
                    int found = 0;
                    foreach (var method in service.ProceduresList) {
                        if (method.Name == "ClassTypeFromOtherServiceAsParameter") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("obj", method.ParametersList [0].Name);
                            Assert.AreEqual ("uint64", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Class(TestService.TestClass)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "ClassTypeFromOtherServiceAsReturn") {
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("value", method.ParametersList [0].Name);
                            Assert.AreEqual ("string", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint64", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ReturnType.Class(TestService.TestClass)", method.AttributesList [0]);
                            found++;
                        }
                    }
                    Assert.AreEqual (2, found);
                }
                if (service.Name == "TestService3Name") {
                    foundServices++;
                    Assert.AreEqual (1, service.ProceduresCount);
                }
            }
            Assert.AreEqual (3, foundServices);
        }
    }
}

