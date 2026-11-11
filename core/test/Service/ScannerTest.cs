using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Messages;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ScannerTest
    {
        Services services;

        [SetUp]
        public void SetUp ()
        {
            services = global::KRPC.Service.KRPC.KRPC.GetServices ();
            Assert.IsNotNull (services);
        }

        [Test]
        public void Services ()
        {
            Assert.AreEqual (6, services.ServicesList.Count);
            CollectionAssert.AreEquivalent (
                new [] { "KRPC", "StdLib", "TestService", "TestService2", "TestService3Name", "TestServiceDeprecated" },
                services.ServicesList.Select (x => x.Name).ToList ());
        }

        [Test]
        public void TestService ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            Assert.AreEqual (84, service.Procedures.Count);
            Assert.AreEqual (3, service.Classes.Count);
            Assert.AreEqual (3, service.Enumerations.Count);
            Assert.AreEqual (3, service.Structs.Count);
            Assert.AreEqual ("<doc>\n<summary>\nTest service documentation.\n</summary>\n</doc>", service.Documentation);
            MessageAssert.IsNotDeprecated (service);
        }

        [Test]
        public void TestService2 ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService2");
            Assert.AreEqual (2, service.Procedures.Count);
            Assert.AreEqual (1, service.Classes.Count);
            Assert.AreEqual (1, service.Enumerations.Count);
            Assert.AreEqual ("<doc>\n<summary>\nTestService2 documentation.\n</summary>\n</doc>", service.Documentation);
        }

        [Test]
        public void TestService3Name ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService3Name");
            Assert.AreEqual (10, service.Procedures.Count);
            Assert.AreEqual (5, service.Classes.Count);
            Assert.AreEqual (1, service.Enumerations.Count);
            Assert.AreEqual (string.Empty, service.Documentation);
            MessageAssert.IsNotDeprecated (service);
        }

        [Test]
        public void TestService3NameExtensionMember ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService3Name");
            var proc = service.Procedures.First (x => x.Name == "TestClass3_ExtensionMethodOnOtherServicesClass");
            MessageAssert.HasParameters (proc, 1);
            MessageAssert.HasParameter (proc, 0, typeof(TestClass3), "this");
            MessageAssert.HasReturnType (proc, typeof(string));
            MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Editor);
            MessageAssert.HasNoDocumentation (proc);
        }

        [Test]
        public void ExtensionMembersInAClassThatIsNotPublic ()
        {
            var warnings = new List<string> ();
            var writer = Logger.Writer;
            var level = Logger.Level;
            try {
                Logger.Level = Logger.Severity.Warning;
                Logger.Writer = (message, severity) => warnings.Add (message);
                global::KRPC.Service.Scanner.Scanner.GetServices (new List<string> ());
            } finally {
                Logger.Writer = writer;
                Logger.Level = level;
            }
            // InvalidExtensions is internal, so its members are named in a warning and skipped
            Assert.AreEqual (1, warnings.Count (x => x.Contains ("InvalidExtensions")));
        }

        [Test]
        public void ProcedureIdsFromNames ()
        {
            var krpc = global::KRPC.Service.Services.Instance.Signatures ["KRPC"];
            Assert.AreEqual (2007570979, krpc.Procedures ["GetStatus"].Id);
            Assert.AreEqual (3459659327, krpc.Procedures ["GetClientID"].Id);
        }

        [TestCase ("TestService2", "ClassTypeFromOtherServiceAsReturn", 1u)]
        [TestCase ("TestService", "get_PropertyWithGetAndSet", 1u)]
        [TestCase ("TestService", "set_PropertyWithGetAndSet", 2u)]
        [TestCase ("TestService", "TestClass_ObjectToString", 3u)]
        [TestCase ("TestService", "TestClass_set_IntProperty", 4u)]
        [TestCase ("TestService", "TestClass_get_ExtensionReadWriteProperty", 5u)]
        public void ProcedureIdsSetInTheAttribute (string service, string procedure, uint id)
        {
            var signature = global::KRPC.Service.Services.Instance.Signatures [service];
            Assert.AreEqual (id, signature.Procedures [procedure].Id);
        }

        [TestCase ("TestBaseClass3_BaseMethod", 10u)]
        [TestCase ("TestBaseClass3_get_BaseProperty", 11u)]
        [TestCase ("TestDerivedClass3_set_BaseProperty", 12u)]
        [TestCase ("TestGenericDerivedClass3_GenericMethod", 13u)]
        [TestCase ("TestDerivedClass3_BaseMethod", 0u)]
        [TestCase ("TestDerivedClass3_get_BaseProperty", 0u)]
        [TestCase ("TestBaseClass3_set_BaseProperty", 0u)]
        public void ProcedureIdsOfInheritedMembers (string procedure, uint id)
        {
            var service = global::KRPC.Service.Services.Instance.Signatures ["TestService3Name"];
            Assert.AreEqual (global::KRPC.Service.TypeUtils.GetId (procedure, id), service.Procedures [procedure].Id);
        }

        [Test]
        public void ProcedureIdsOfTheOtherAccessor ()
        {
            var service = global::KRPC.Service.Services.Instance.Signatures ["TestService"];
            Assert.AreEqual (global::KRPC.Service.TypeUtils.GetId ("TestClass_get_IntProperty", 0),
                             service.Procedures ["TestClass_get_IntProperty"].Id);
            Assert.AreEqual (global::KRPC.Service.TypeUtils.GetId ("TestClass_set_ExtensionReadWriteProperty", 0),
                             service.Procedures ["TestClass_set_ExtensionReadWriteProperty"].Id);
        }

        [Test]
        public void ProcedureIdsShared ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("FirstMethodSharingAnId"));
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("SecondMethodSharingAnId"));
            service.AddProcedure (typeof(TestService).GetMethod ("ProcedureNoArgsNoReturn"));
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckProcedureIds (service, errors);
            Assert.AreEqual (1, errors.Count);
            StringAssert.Contains (
                "TestService.TestClass_FirstMethodSharingAnId, TestService.TestClass_SecondMethodSharingAnId share the id 7",
                errors [0]);
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => global::KRPC.Service.Scanner.Scanner.CheckProcedureIds (service, null));
        }

        [Test]
        public void TypeIdsFromNames ()
        {
            var krpc = global::KRPC.Service.Services.Instance.Signatures ["KRPC"];
            Assert.AreEqual (1727780136, krpc.Exceptions ["ArgumentException"].Id);
            Assert.AreEqual (4251095244, krpc.Enumerations ["GameScene"].Id);
            Assert.AreEqual (3155522758, krpc.Classes ["Expression"].Id);
            Assert.AreEqual (1932373256, krpc.Structs ["ServerStatus"].Id);
        }

        [Test]
        public void TypeIdsSetInTheAttribute ()
        {
            var service = global::KRPC.Service.Services.Instance.Signatures ["TestService3Name"];
            Assert.AreEqual (20, service.Classes ["TestClassWithAnId3"].Id);
            Assert.AreEqual (21, service.Enumerations ["TestEnumWithAnId3"].Id);
            Assert.AreEqual (22, service.Structs ["TestStructWithAnId3"].Id);
            Assert.AreEqual (23, service.Exceptions ["TestExceptionWithAnId3"].Id);
        }

        [Test]
        public void TypeNamesShared ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature ("TestTypes", 1);
            service.AddClass (typeof(TestClass3));
            service.AddEnum (typeof(TESTCLASS3));
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckTypes (service, errors);
            Assert.AreEqual (1, errors.Count);
            StringAssert.Contains ("TestTypes.TESTCLASS3, TestTypes.TestClass3 share a name", errors [0]);
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => global::KRPC.Service.Scanner.Scanner.CheckTypes (service, null));
        }

        [Test]
        public void TypeIdsShared ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature ("TestTypes", 1);
            service.AddClass (typeof(TestClassSharingAnId3));
            service.AddEnum (typeof(TestEnumWithAnId3));
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckTypes (service, errors);
            Assert.AreEqual (1, errors.Count);
            StringAssert.Contains ("TestTypes.TestClassSharingAnId3, TestTypes.TestEnumWithAnId3 share the id 21", errors [0]);
        }

        [Test]
        public void TestServiceDeprecated ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestServiceDeprecated");
            Assert.AreEqual (1, service.Procedures.Count);
            MessageAssert.IsDeprecated (service, "Use <see cref=\"T:TestService\" /> instead.");
        }

        [Test]
        public void TestServiceProcedures ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            int foundProcedures = 0;
            foreach (var proc in service.Procedures) {
                if (proc.Name == "ProcedureNoArgsNoReturn") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasDocumentation (proc, "<doc>\n<summary>\nProcedure with no return arguments.\n</summary>\n</doc>");
                    MessageAssert.IsNotDeprecated (proc);
                } else if (proc.Name == "DeprecatedProcedure") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.IsDeprecated (proc, "Use <see cref=\"M:TestService.ProcedureNoArgsNoReturn\" /> instead.");
                } else if (proc.Name == "DeprecatedProcedureNoMessage") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.IsDeprecated (proc, string.Empty);
                } else if (proc.Name == "get_DeprecatedProperty") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.IsDeprecated (proc, "Use <see cref=\"M:TestService.PropertyWithGet\" /> instead.");
                } else if (proc.Name == "DeprecatedClass_DeprecatedMethod") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.IsDeprecated (proc, "Use <see cref=\"M:TestService.TestClass.FloatToString\" /> instead.");
                } else if (proc.Name == "ProcedureSingleArgNoReturn") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(string), "x");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasDocumentation (proc, "<doc>\n<summary>\nProcedure with a single return argument.\n</summary>\n</doc>");
                } else if (proc.Name == "ProcedureThreeArgsNoReturn") {
                    MessageAssert.HasParameters (proc, 3);
                    MessageAssert.HasParameter (proc, 0, typeof(string), "x");
                    MessageAssert.HasParameter (proc, 1, typeof(int), "y");
                    MessageAssert.HasParameter (proc, 2, typeof(string), "z");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureNoArgsReturns") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureSingleArgReturns") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(string), "x");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "get_PropertyWithGetAndSet") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "set_PropertyWithGetAndSet") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(string), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "get_PropertyWithGet") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "set_PropertyWithSet") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(string), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "get_NullableProperty") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(string), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "set_NullableProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(string), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "get_NullableByAttributeProperty") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(string), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "set_NullableByAttributeProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(string), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "CreateTestObject") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(string), "value");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestClass));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "DeleteTestObject") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "obj");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoTestObject") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(TestService.TestClass), "obj");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestClass), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNullableString") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(string), "x");
                    MessageAssert.HasReturnType (proc, typeof(string), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNullableInt") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(int), "x");
                    MessageAssert.HasReturnType (proc, typeof(int), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNullableDouble") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(double), "x");
                    MessageAssert.HasReturnType (proc, typeof(double), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNullableEnum") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(TestService.TestEnum), "x");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestEnum), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoListOfNullableInts") {
                    MessageAssert.HasParameters (proc, 1);
                    // A Nullable<T> element is declared by its underlying type T
                    MessageAssert.HasParameter (proc, 0, typeof(IList<int?>), "l");
                    MessageAssert.HasNullableTypes (proc.Parameters [0].Type, typeof(int));
                    MessageAssert.HasReturnType (proc, typeof(IList<int?>));
                    MessageAssert.HasNullableTypes (proc.ReturnType, typeof(int));
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoListOfNullableObjects") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(IList<TestService.TestClass>), "l");
                    MessageAssert.HasNullableTypes (
                        proc.Parameters [0].Type, typeof(TestService.TestClass));
                    MessageAssert.HasReturnType (proc, typeof(IList<TestService.TestClass>));
                    MessageAssert.HasNullableTypes (proc.ReturnType, typeof(TestService.TestClass));
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoDictionaryOfNullableObjects") {
                    MessageAssert.HasParameters (proc, 1);
                    // The key is not nullable, and the value is
                    MessageAssert.HasNullableTypes (
                        proc.Parameters [0].Type, typeof(TestService.TestClass));
                    MessageAssert.HasNullableTypes (proc.ReturnType, typeof(TestService.TestClass));
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoTupleWithANullableObject") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableTypes (
                        proc.Parameters [0].Type, typeof(TestService.TestClass));
                    MessageAssert.HasNullableTypes (proc.ReturnType, typeof(TestService.TestClass));
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNestedListOfNullableObjects") {
                    MessageAssert.HasParameters (proc, 1);
                    // The inner lists are not nullable, and the objects they hold are
                    MessageAssert.HasNullableTypes (
                        proc.Parameters [0].Type, typeof(TestService.TestClass));
                    MessageAssert.HasNullableTypes (proc.ReturnType, typeof(TestService.TestClass));
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "CreateEvent") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(global::KRPC.Service.Messages.Event));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNullableList") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(IList<string>), "l");
                    MessageAssert.HasReturnType (proc, typeof(IList<string>), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ReturnNullWhenNotAllowed") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestClass), false);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_FloatToString") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasParameter (proc, 1, typeof(float), "x");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_ObjectToString") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasParameter (proc, 1, typeof(TestService.TestClass), "other");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_EchoNullableObject") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasNullableParameter (proc, 1, typeof(TestService.TestClass), "other");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestClass), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_IntToString") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasParameterWithDefaultValue (proc, 1, typeof(int), "x", 42);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_get_IntProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(int));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_set_IntProperty") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasParameter (proc, 1, typeof(int), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_get_ObjectProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestClass), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_set_ObjectProperty") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasNullableParameter (proc, 1, typeof(TestService.TestClass), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_static_StaticMethod") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, typeof(string), "a", string.Empty);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_static_StaticNullableMethod") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(string), "x");
                    MessageAssert.HasReturnType (proc, typeof(string), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_MethodAvailableInInheritedGameScene") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_MethodAvailableInSpecifiedGameScene") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.EditorVAB);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_get_ClassPropertyAvailableInInheritedGameScene") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_get_ClassPropertyAvailableInSpecifiedGameScene") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.EditorVAB);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestTopLevelClass_AMethod") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestTopLevelClass), "this");
                    MessageAssert.HasParameter (proc, 1, typeof(int), "x");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestTopLevelClass_get_AProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestTopLevelClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestTopLevelClass_set_AProperty") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestTopLevelClass), "this");
                    MessageAssert.HasParameter (proc, 1, typeof(string), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureSingleOptionalArgNoReturn") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, typeof(string), "x", "foo");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureThreeOptionalArgsNoReturn") {
                    MessageAssert.HasParameters (proc, 3);
                    MessageAssert.HasParameter (proc, 0, typeof(float), "x");
                    MessageAssert.HasParameterWithDefaultValue (proc, 1, typeof(string), "y", "alpha");
                    MessageAssert.HasParameterWithDefaultValue (proc, 2, typeof(int), "z", 42);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureOptionalNullArg") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameterWithDefaultValue (proc, 0, typeof(TestService.TestClass), "x", null);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureEnumArg") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestEnum), "x");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureEnumReturn") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestEnum));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "BlockingProcedureNoReturn") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(int), "n");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "BlockingProcedureReturns") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(int), "n");
                    MessageAssert.HasParameterWithDefaultValue (proc, 1, typeof(int), "sum", 0);
                    MessageAssert.HasReturnType (proc, typeof(int));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoList") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(IList<string>), "l");
                    MessageAssert.HasReturnType (proc, typeof(IList<string>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoDictionary") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(IDictionary<int,string>), "d");
                    MessageAssert.HasReturnType (proc, typeof(IDictionary<int,string>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoSet") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(HashSet<int>), "h");
                    MessageAssert.HasReturnType (proc, typeof(HashSet<int>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoTuple") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(Tuple<int,bool>), "t");
                    MessageAssert.HasReturnType (proc, typeof(Tuple<int,bool>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNestedCollection") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(IDictionary<int,IList<string>>), "c");
                    MessageAssert.HasReturnType (proc, typeof(IDictionary<int,IList<string>>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoListOfObjects") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(IList<TestService.TestClass>), "l");
                    MessageAssert.HasReturnType (proc, typeof(IList<TestService.TestClass>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TupleDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, typeof(Tuple<int,bool>), "x", new Tuple<int,bool> (1, false));
                    MessageAssert.HasReturnType (proc, typeof(Tuple<int,bool>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ListDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, typeof(IList<int>), "x", new List<int> { 1, 2, 3 });
                    MessageAssert.HasReturnType (proc, typeof(IList<int>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "SetDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, typeof(HashSet<int>), "x", new HashSet<int> { 1, 2, 3 });
                    MessageAssert.HasReturnType (proc, typeof(HashSet<int>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "DictionaryDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameterWithDefaultValue (proc, 0, typeof(IDictionary<int,bool>), "x",
                        new Dictionary<int,bool> { { 1,false }, { 2,true } });
                    MessageAssert.HasReturnType (proc, typeof(IDictionary<int,bool>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoStruct") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestStruct), "x");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestStruct));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoListOfStructs") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(IList<TestService.TestStruct>), "l");
                    MessageAssert.HasReturnType (proc, typeof(IList<TestService.TestStruct>));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "EchoNestedStruct") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestNestedStruct), "x");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestNestedStruct));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "StructDefault") {
                    MessageAssert.HasParameters (proc, 1);
                    var parameter = proc.Parameters [0];
                    Assert.AreEqual (typeof(TestService.TestStruct), parameter.Type.Type);
                    Assert.AreEqual ("x", parameter.Name);
                    Assert.IsTrue (parameter.HasDefaultValue);
                    var defaultValue = (TestService.TestStruct)parameter.DefaultValue;
                    Assert.AreEqual (42, defaultValue.IntField);
                    Assert.AreEqual ("alpha", defaultValue.StringField);
                    Assert.AreEqual (global::KRPC.Test.Service.TestService.TestEnum.Y, defaultValue.EnumField);
                    Assert.AreEqual ("home", defaultValue.ObjectField.Value);
                    CollectionAssert.AreEqual (new [] { "a", "b" }, defaultValue.ListField);
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestStruct));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureAvailableInInheritedGameScene") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "ProcedureAvailableInSpecifiedGameScene") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.EditorVAB);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "get_PropertyAvailableInInheritedGameScene") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "get_PropertyAvailableInSpecifiedGameScene") {
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.EditorVAB);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_ExtensionMethod") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasParameter (proc, 1, typeof(int), "x");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasDocumentation (proc, "<doc>\n<summary>\nExtension method documentation string.\n</summary>\n</doc>");
                } else if (proc.Name == "TestClass_ExtensionNullableMethod") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasNullableParameter (proc, 1, typeof(TestService.TestClass), "other");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestClass), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_ExtensionMethodAvailableInSpecifiedGameScene") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.EditorVAB);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_DeprecatedExtensionMethod") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.IsDeprecated (proc, "Use ExtensionMethod instead.");
                } else if (proc.Name == "TestClass_ExtensionMethodReturningClassFromOtherService") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(TestClass3));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_get_ExtensionProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasDocumentation (proc, "<doc>\n<summary>\nExtension property documentation string.\n</summary>\n</doc>");
                } else if (proc.Name == "TestClass_get_ExtensionReadWriteProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(int));
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_set_ExtensionReadWriteProperty") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasParameter (proc, 1, typeof(int), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_get_ExtensionNullableProperty") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestClass), true);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else if (proc.Name == "TestClass_set_ExtensionNullableProperty") {
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "this");
                    MessageAssert.HasNullableParameter (proc, 1, typeof(TestService.TestClass), "value");
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasGameScene (proc, global::KRPC.Service.GameScene.Flight | global::KRPC.Service.GameScene.SpaceCenter);
                    MessageAssert.HasNoDocumentation (proc);
                } else {
                    Assert.Fail ("Procedure not found");
                }
                foundProcedures++;
            }
            Assert.AreEqual (84, foundProcedures);
            Assert.AreEqual (84, service.Procedures.Count);
        }

        [Test]
        public void TestServiceClasses ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            int foundClasses = 0;
            foreach (var cls in service.Classes) {
                if (cls.Name == "TestClass") {
                    MessageAssert.HasNoDocumentation (cls);
                    MessageAssert.IsNotDeprecated (cls);
                } else if (cls.Name == "TestTopLevelClass") {
                    MessageAssert.HasDocumentation (cls, "<doc>\n<summary>\nA class defined at the top level, but included in a service\n</summary>\n</doc>");
                    MessageAssert.IsNotDeprecated (cls);
                } else if (cls.Name == "DeprecatedClass") {
                    MessageAssert.IsDeprecated (cls, "Use <see cref=\"T:TestService.TestClass\" /> instead.");
                } else {
                    Assert.Fail ();
                }
                foundClasses++;
            }
            Assert.AreEqual (3, foundClasses);
            Assert.AreEqual (3, service.Classes.Count);
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
                    MessageAssert.IsNotDeprecated (enumeration);
                    MessageAssert.ValueIsNotDeprecated (enumeration, 0);
                    Assert.IsFalse (enumeration.Flags);
                } else if (enumeration.Name == "TestFlags") {
                    MessageAssert.HasValues (enumeration, 5);
                    MessageAssert.HasValue (enumeration, 0, "None", 0);
                    MessageAssert.HasValue (enumeration, 1, "A", 1);
                    MessageAssert.HasValue (enumeration, 2, "B", 2);
                    MessageAssert.HasValue (enumeration, 3, "C", 4);
                    MessageAssert.HasValue (enumeration, 4, "AB", 3);
                    Assert.IsTrue (enumeration.Flags);
                } else if (enumeration.Name == "DeprecatedEnum") {
                    MessageAssert.HasValues (enumeration, 2);
                    MessageAssert.HasValue (enumeration, 0, "A", 0, "<doc>\n<summary>\nA value that is not deprecated.\n</summary>\n</doc>");
                    MessageAssert.HasValue (enumeration, 1, "B", 1, "<doc>\n<summary>\nA deprecated enumeration value, annotated with a reason.\n</summary>\n</doc>");
                    MessageAssert.IsDeprecated (enumeration, "Use <see cref=\"T:TestService.TestEnum\" /> instead.");
                    MessageAssert.ValueIsNotDeprecated (enumeration, 0);
                    MessageAssert.ValueIsDeprecated (enumeration, 1, "Use <see cref=\"M:TestService.DeprecatedEnum.A\" /> instead.");
                } else {
                    Assert.Fail ();
                }
                foundEnumerations++;
            }
            Assert.AreEqual (3, foundEnumerations);
            Assert.AreEqual (3, service.Enumerations.Count);
        }

        [Test]
        public void TestServiceStructs ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            int foundStructs = 0;
            foreach (var str in service.Structs) {
                if (str.Name == "TestStruct") {
                    MessageAssert.HasDocumentation (str, "<doc>\n<summary>\nDocumentation string for TestStruct.\n</summary>\n</doc>");
                    MessageAssert.HasFields (str, 5);
                    MessageAssert.HasField (str, 0, "IntField", typeof(int), "<doc>\n<summary>\nDocumented struct field\n</summary>\n</doc>");
                    MessageAssert.HasField (str, 1, "StringField", typeof(string));
                    MessageAssert.HasField (str, 2, "EnumField", typeof(TestService.TestEnum));
                    MessageAssert.HasField (str, 3, "ObjectField", typeof(TestService.TestClass));
                    MessageAssert.HasField (str, 4, "ListField", typeof(IList<string>));
                    MessageAssert.IsNotDeprecated (str);
                } else if (str.Name == "TestNestedStruct") {
                    MessageAssert.HasNoDocumentation (str);
                    MessageAssert.HasFields (str, 2);
                    MessageAssert.HasField (str, 0, "StructField", typeof(TestService.TestStruct));
                    MessageAssert.HasField (str, 1, "IntField", typeof(int));
                    MessageAssert.IsNotDeprecated (str);
                } else if (str.Name == "TestNullableStruct") {
                    MessageAssert.HasDocumentation (str, "<doc>\n<summary>\nDocumentation string for TestNullableStruct.\n</summary>\n</doc>");
                    MessageAssert.HasFields (str, 5);
                    MessageAssert.HasField (str, 0, "IntField", typeof(int));
                    // A Nullable<T> field is declared by its underlying type T
                    MessageAssert.HasNullableField (str, 1, "NullableIntField", typeof(int));
                    MessageAssert.HasNullableField (str, 2, "NullableEnumField", typeof(TestService.TestEnum));
                    MessageAssert.HasNullableField (str, 3, "NullableStringField", typeof(string));
                    MessageAssert.HasNullableField (str, 4, "NullableObjectField", typeof(TestService.TestClass));
                    MessageAssert.IsNotDeprecated (str);
                } else {
                    Assert.Fail ();
                }
                foundStructs++;
            }
            Assert.AreEqual (3, foundStructs);
            Assert.AreEqual (3, service.Structs.Count);
        }

        [Test]
        public void TestServiceExceptions ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            int foundExceptions = 0;
            foreach (var exception in service.Exceptions) {
                if (exception.Name == "MyException") {
                    MessageAssert.IsNotDeprecated (exception);
                } else if (exception.Name == "DeprecatedException") {
                    MessageAssert.IsDeprecated (exception, "Use MyException instead.");
                } else {
                    Assert.Fail ();
                }
                foundExceptions++;
            }
            Assert.AreEqual (2, foundExceptions);
            Assert.AreEqual (2, service.Exceptions.Count);
        }

        [Test]
        public void TestService2Procedures ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService2");
            int foundProcedures = 0;
            foreach (var proc in service.Procedures) {
                if (proc.Name == "ClassTypeFromOtherServiceAsParameter") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(TestService.TestClass), "obj");
                    MessageAssert.HasReturnType (proc, typeof(int));
                    MessageAssert.HasDocumentation (proc, "<doc>\n<summary>\nTestService2 procedure documentation.\n</summary>\n</doc>");
                } else if (proc.Name == "ClassTypeFromOtherServiceAsReturn") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(string), "value");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestClass));
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
