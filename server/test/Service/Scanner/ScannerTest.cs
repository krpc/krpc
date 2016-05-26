using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KRPC.Service.Messages;

namespace KRPC.Test.Service.Scanner
{
    [TestFixture]
    public class ScannerTest
    {
        Services services;

        void AssertHasNoParameters (Procedure procedure)
        {
            Assert.AreEqual (0, procedure.Parameters.Count);
        }

        void AssertHasParameters (Procedure procedure, int count)
        {
            Assert.AreEqual (count, procedure.Parameters.Count);
        }

        void AssertHasParameter (Procedure procedure, int position, string type, string name)
        {
            Assert.Less (position, procedure.Parameters.Count);
            var parameter = procedure.Parameters [position];
            Assert.AreEqual (type, parameter.Type);
            Assert.AreEqual (name, parameter.Name);
            Assert.IsFalse (parameter.HasDefaultValue);
            Assert.IsNull (parameter.DefaultValue);
        }

        void AssertHasParameterWithDefaultValue (Procedure procedure, int position, string type, string name, object defaultValue)
        {
            Assert.Less (position, procedure.Parameters.Count);
            var parameter = procedure.Parameters [position];
            Assert.AreEqual (type, parameter.Type);
            Assert.AreEqual (name, parameter.Name);
            Assert.IsTrue (parameter.HasDefaultValue);
            Assert.AreEqual (defaultValue, parameter.DefaultValue);
        }

        void AssertHasNoReturnType (Procedure procedure)
        {
            Assert.IsFalse (procedure.HasReturnType);
            Assert.AreEqual ("", procedure.ReturnType);
        }

        void AssertHasReturnType (Procedure procedure, string returnType)
        {
            Assert.IsTrue (procedure.HasReturnType);
            Assert.AreEqual (returnType, procedure.ReturnType);
        }

        void AssertHasNoAttributes (Procedure procedure)
        {
            Assert.AreEqual (0, procedure.Attributes.Count);
        }

        void AssertHasAttributes (Procedure procedure, int count)
        {
            Assert.AreEqual (count, procedure.Attributes.Count);
        }

        void AssertHasAttribute (Procedure procedure, int position, string attribute)
        {
            Assert.Less (position, procedure.Attributes.Count);
            Assert.AreEqual (attribute, procedure.Attributes [position]);
        }

        void AssertHasNoDocumentation (Procedure procedure)
        {
            Assert.AreEqual ("", procedure.Documentation);
        }

        void AssertHasDocumentation (Procedure procedure, string documentation)
        {
            Assert.AreEqual (documentation, procedure.Documentation);
        }

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
            Assert.AreEqual (37, service.Procedures.Count);
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
                    AssertHasNoParameters (proc);
                    AssertHasNoReturnType (proc);
                    AssertHasNoAttributes (proc);
                    AssertHasDocumentation (proc, "<doc>\n  <summary>\nProcedure with no return arguments.\n</summary>\n</doc>");
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleArgNoReturn") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "KRPC.Response", "data");
                    AssertHasNoReturnType (proc);
                    AssertHasNoAttributes (proc);
                    AssertHasDocumentation (proc, "<doc>\n  <summary>\nProcedure with a single return argument.\n</summary>\n</doc>");
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureThreeArgsNoReturn") {
                    AssertHasParameters (proc, 3);
                    AssertHasParameter (proc, 0, "KRPC.Response", "x");
                    AssertHasParameter (proc, 1, "KRPC.Request", "y");
                    AssertHasParameter (proc, 2, "KRPC.Response", "z");
                    AssertHasNoReturnType (proc);
                    AssertHasNoAttributes (proc);
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureNoArgsReturns") {
                    AssertHasNoParameters (proc);
                    AssertHasReturnType (proc, "KRPC.Response");
                    AssertHasNoAttributes (proc);
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleArgReturns") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "KRPC.Response", "data");
                    AssertHasReturnType (proc, "KRPC.Response");
                    AssertHasNoAttributes (proc);
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureWithValueTypes") {
                    AssertHasParameters (proc, 3);
                    AssertHasParameter (proc, 0, "float", "x");
                    AssertHasParameter (proc, 1, "string", "y");
                    AssertHasParameter (proc, 2, "bytes", "z");
                    AssertHasReturnType (proc, "int32");
                    AssertHasNoAttributes (proc);
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "get_PropertyWithGetAndSet") {
                    AssertHasNoParameters (proc);
                    AssertHasReturnType (proc, "string");
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "Property.Get(PropertyWithGetAndSet)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "set_PropertyWithGetAndSet") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "string", "value");
                    AssertHasNoReturnType (proc);
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "Property.Set(PropertyWithGetAndSet)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "get_PropertyWithGet") {
                    AssertHasNoParameters (proc);
                    AssertHasReturnType (proc, "string");
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "Property.Get(PropertyWithGet)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "set_PropertyWithSet") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "string", "value");
                    AssertHasNoReturnType (proc);
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "Property.Set(PropertyWithSet)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "CreateTestObject") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "string", "value");
                    AssertHasReturnType (proc, "uint64");
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "ReturnType.Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "DeleteTestObject") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "uint64", "obj");
                    AssertHasNoReturnType (proc);
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "EchoTestObject") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "uint64", "obj");
                    AssertHasReturnType (proc, "uint64");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasAttribute (proc, 1, "ReturnType.Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_FloatToString") {
                    AssertHasParameters (proc, 2);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasParameter (proc, 1, "float", "x");
                    AssertHasReturnType (proc, "string");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "Class.Method(TestService.TestClass,FloatToString)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_ObjectToString") {
                    AssertHasParameters (proc, 2);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasParameter (proc, 1, "uint64", "other");
                    AssertHasReturnType (proc, "string");
                    AssertHasAttributes (proc, 3);
                    AssertHasAttribute (proc, 0, "Class.Method(TestService.TestClass,ObjectToString)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasAttribute (proc, 2, "ParameterType(1).Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_IntToString") {
                    AssertHasParameters (proc, 2);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasParameterWithDefaultValue (proc, 1, "int32", "x", 42);
                    AssertHasReturnType (proc, "string");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "Class.Method(TestService.TestClass,IntToString)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_get_IntProperty") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasReturnType (proc, "int32");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "Class.Property.Get(TestService.TestClass,IntProperty)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_set_IntProperty") {
                    AssertHasParameters (proc, 2);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasParameter (proc, 1, "int32", "value");
                    AssertHasNoReturnType (proc);
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "Class.Property.Set(TestService.TestClass,IntProperty)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_get_ObjectProperty") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasReturnType (proc, "uint64");
                    AssertHasAttributes (proc, 3);
                    AssertHasAttribute (proc, 0, "Class.Property.Get(TestService.TestClass,ObjectProperty)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasAttribute (proc, 2, "ReturnType.Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_set_ObjectProperty") {
                    AssertHasParameters (proc, 2);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasParameter (proc, 1, "uint64", "value");
                    AssertHasNoReturnType (proc);
                    AssertHasAttributes (proc, 3);
                    AssertHasAttribute (proc, 0, "Class.Property.Set(TestService.TestClass,ObjectProperty)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasAttribute (proc, 2, "ParameterType(1).Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestClass_StaticMethod") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameterWithDefaultValue (proc, 0, "string", "a", "");
                    AssertHasReturnType (proc, "string");
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "Class.StaticMethod(TestService.TestClass,StaticMethod)");
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_AMethod") {
                    AssertHasParameters (proc, 2);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasParameter (proc, 1, "int32", "x");
                    AssertHasReturnType (proc, "string");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "Class.Method(TestService.TestTopLevelClass,AMethod)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestTopLevelClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_get_AProperty") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasReturnType (proc, "string");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "Class.Property.Get(TestService.TestTopLevelClass,AProperty)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestTopLevelClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "TestTopLevelClass_set_AProperty") {
                    AssertHasParameters (proc, 2);
                    AssertHasParameter (proc, 0, "uint64", "this");
                    AssertHasParameter (proc, 1, "string", "value");
                    AssertHasNoReturnType (proc);
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "Class.Property.Set(TestService.TestTopLevelClass,AProperty)");
                    AssertHasAttribute (proc, 1, "ParameterType(0).Class(TestService.TestTopLevelClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureSingleOptionalArgNoReturn") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameterWithDefaultValue (proc, 0, "string", "x", "foo");
                    AssertHasNoReturnType (proc);
                    AssertHasNoAttributes (proc);
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureThreeOptionalArgsNoReturn") {
                    AssertHasParameters (proc, 3);
                    AssertHasParameter (proc, 0, "float", "x");
                    AssertHasParameterWithDefaultValue (proc, 1, "string", "y", "jeb");
                    AssertHasParameterWithDefaultValue (proc, 2, "int32", "z", 42);
                    AssertHasNoReturnType (proc);
                    AssertHasNoAttributes (proc);
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureOptionalNullArg") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameterWithDefaultValue (proc, 0, "uint64", "x", null);
                    AssertHasNoReturnType (proc);
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureEnumArg") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "int32", "x");
                    AssertHasNoReturnType (proc);
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Enum(TestService.TestEnum)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "ProcedureEnumReturn") {
                    AssertHasNoParameters (proc);
                    AssertHasReturnType (proc, "int32");
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "ReturnType.Enum(TestService.TestEnum)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "BlockingProcedureNoReturn") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "int32", "n");
                    AssertHasNoReturnType (proc);
                    AssertHasNoAttributes (proc);
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "BlockingProcedureReturns") {
                    AssertHasParameters (proc, 2);
                    AssertHasParameter (proc, 0, "int32", "n");
                    AssertHasParameterWithDefaultValue (proc, 1, "int32", "sum", 0);
                    AssertHasReturnType (proc, "int32");
                    AssertHasNoAttributes (proc);
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "EchoList") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "KRPC.List", "l");
                    AssertHasReturnType (proc, "KRPC.List");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "ParameterType(0).List(string)");
                    AssertHasAttribute (proc, 1, "ReturnType.List(string)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "EchoDictionary") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "KRPC.Dictionary", "d");
                    AssertHasReturnType (proc, "KRPC.Dictionary");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Dictionary(int32,string)");
                    AssertHasAttribute (proc, 1, "ReturnType.Dictionary(int32,string)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "EchoSet") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "KRPC.Set", "h");
                    AssertHasReturnType (proc, "KRPC.Set");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Set(int32)");
                    AssertHasAttribute (proc, 1, "ReturnType.Set(int32)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "EchoTuple") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "KRPC.Tuple", "t");
                    AssertHasReturnType (proc, "KRPC.Tuple");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Tuple(int32,bool)");
                    AssertHasAttribute (proc, 1, "ReturnType.Tuple(int32,bool)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "EchoNestedCollection") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "KRPC.Dictionary", "c");
                    AssertHasReturnType (proc, "KRPC.Dictionary");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Dictionary(int32,List(string))");
                    AssertHasAttribute (proc, 1, "ReturnType.Dictionary(int32,List(string))");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
                if (proc.Name == "EchoListOfObjects") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "KRPC.List", "l");
                    AssertHasReturnType (proc, "KRPC.List");
                    AssertHasAttributes (proc, 2);
                    AssertHasAttribute (proc, 0, "ParameterType(0).List(Class(TestService.TestClass))");
                    AssertHasAttribute (proc, 1, "ReturnType.List(Class(TestService.TestClass))");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
            }
            Assert.AreEqual (37, foundProcedures);
            Assert.AreEqual (37, service.Procedures.Count);
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
                if (enm.Name == "TestEnum") {
                    Assert.AreEqual ("<doc>\n  <summary>\nDocumentation string for TestEnum.\n</summary>\n</doc>", enm.Documentation);
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
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "uint64", "obj");
                    AssertHasReturnType (proc, "int32");
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "ParameterType(0).Class(TestService.TestClass)");
                    AssertHasDocumentation (proc, "<doc>\n  <summary>\nTestService2 procedure documentation.\n</summary>\n</doc>");
                    foundProcedures++;
                }
                if (proc.Name == "ClassTypeFromOtherServiceAsReturn") {
                    AssertHasParameters (proc, 1);
                    AssertHasParameter (proc, 0, "string", "value");
                    AssertHasReturnType (proc, "uint64");
                    AssertHasAttributes (proc, 1);
                    AssertHasAttribute (proc, 0, "ReturnType.Class(TestService.TestClass)");
                    AssertHasNoDocumentation (proc);
                    foundProcedures++;
                }
            }
            Assert.AreEqual (2, foundProcedures);
            Assert.AreEqual (2, service.Procedures.Count);
        }
    }
}
