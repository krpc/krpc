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
            Assert.AreEqual (7, services.ServicesList.Count);
            CollectionAssert.AreEquivalent (
                new [] { "KRPC", "StdLib", "TestService", "TestService2", "TestService3Name", "TestServiceDeprecated", "TestServiceMod" },
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
            var errors = new List<string> ();
            var warnings = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.GetServices (errors, warnings);
            CollectionAssert.IsEmpty (errors);
            // The classes are internal, so their members are named in a warning and skipped
            Assert.AreEqual (new [] {
                "Ignoring the extension members of KRPC.Test.Service.InvalidExtensions, as the class is not public",
                "Ignoring the extension members of KRPC.Test.Service.ClashingExtensions, as the class is not public"
            }, warnings.OrderByDescending (x => x.Contains ("InvalidExtensions")).ToArray ());
        }

        [Test]
        public void WarningsAreErrorsWithoutAWarningList ()
        {
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.GetServices (errors);
            Assert.AreEqual (2, errors.Count);
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => global::KRPC.Service.Scanner.Scanner.GetServices ());
        }

        [Test]
        public void ServiceProblemsOfTheTestAssembly ()
        {
            CollectionAssert.IsEmpty (global::KRPC.Service.ServiceProblems.Errors);
            Assert.AreEqual (2, global::KRPC.Service.ServiceProblems.Warnings.Count);
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

        static global::KRPC.Service.Scanner.ServiceSignature ServiceWithProceduresSharingAnId ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("FirstMethodSharingAnId"));
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("SecondMethodSharingAnId"));
            service.AddProcedure (typeof(TestService).GetMethod ("ProcedureNoArgsNoReturn"));
            return service;
        }

        [Test]
        public void ProcedureIdsShared ()
        {
            var service = ServiceWithProceduresSharingAnId ();
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckProcedureIds (service, errors);
            Assert.AreEqual (1, errors.Count);
            StringAssert.Contains (
                "TestService.TestClass_FirstMethodSharingAnId, TestService.TestClass_SecondMethodSharingAnId share the id 7",
                errors [0]);
            // Neither procedure keeps the id, so both are left out
            CollectionAssert.AreEquivalent (new [] { "ProcedureNoArgsNoReturn" }, service.Procedures.Keys);
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => global::KRPC.Service.Scanner.Scanner.CheckProcedureIds (ServiceWithProceduresSharingAnId (), null));
        }

        [Test]
        public void ServiceIdsShared ()
        {
            var services = new Dictionary<string, global::KRPC.Service.Scanner.ServiceSignature> {
                { "A", new global::KRPC.Service.Scanner.ServiceSignature ("A", 1) },
                { "B", new global::KRPC.Service.Scanner.ServiceSignature ("B", 1) },
                { "C", new global::KRPC.Service.Scanner.ServiceSignature ("C", 0) },
                { "D", new global::KRPC.Service.Scanner.ServiceSignature ("D", 2) }
            };
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckServiceIds (services, errors);
            Assert.AreEqual (new [] {
                "In service C: Service has id 0",
                "Services A, B share the id 1"
            }, errors.ToArray ());
            CollectionAssert.AreEquivalent (new [] { "D" }, services.Keys);
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
            CollectionAssert.IsEmpty (service.Classes);
            CollectionAssert.IsEmpty (service.Enumerations);
            service = new global::KRPC.Service.Scanner.ServiceSignature ("TestTypes", 1);
            service.AddClass (typeof(TestClass3));
            service.AddEnum (typeof(TESTCLASS3));
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
            CollectionAssert.IsEmpty (service.Classes);
            CollectionAssert.IsEmpty (service.Enumerations);
        }

        static global::KRPC.Service.Scanner.ServiceSignature ServiceWithMemberNamesShared ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature ("TestNames", 1);
            // The check reads the members of a scope from the procedure names
            var procedure = global::KRPC.Service.Services.Instance.Signatures ["TestService"].Procedures ["ProcedureNoArgsNoReturn"];
            foreach (var name in new [] {
                "Foo", "FOO", "get_Values", "set_Values", "GetValues", "get_Name", "set_Name",
                "get_Mode", "SetMode", "get_Speed", "get_GetSpeed",
                "Cls_Bar", "Cls_get_BAR", "Cls_Baz", "Cls_static_Baz", "Cls_Qux", "Cls_get_Qux",
                "Other_Bar", "Other_Baz" })
                service.Procedures [name] = procedure;
            return service;
        }

        [Test]
        public void MemberNamesShared ()
        {
            var service = ServiceWithMemberNamesShared ();
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckMemberNames (service, errors);
            Assert.AreEqual (new [] {
                "In service TestNames: Members TestNames.FOO, TestNames.Foo share the name FOO, compared case-insensitively",
                "In service TestNames: Members TestNames.GetSpeed (property), TestNames.Speed (property) share the name GetSpeed, compared case-insensitively",
                "In service TestNames: Members TestNames.GetValues, TestNames.Values (property) share the name GetValues, compared case-insensitively",
                "In service TestNames: Members TestNames.Mode (property), TestNames.SetMode share the name SetMode, compared case-insensitively",
                "In service TestNames: Members TestNames.Cls.BAR (property), TestNames.Cls.Bar share the name BAR, compared case-insensitively",
                "In service TestNames: Members TestNames.Cls.Baz, TestNames.Cls.Baz (static) share the name Baz, compared case-insensitively",
                "In service TestNames: Members TestNames.Cls.Qux, TestNames.Cls.Qux (property) share the name Qux, compared case-insensitively"
            }, errors.ToArray ());
            // Every member that shares a name is left out
            CollectionAssert.AreEquivalent (new [] { "get_Name", "set_Name", "Other_Bar", "Other_Baz" }, service.Procedures.Keys);
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => global::KRPC.Service.Scanner.Scanner.CheckMemberNames (ServiceWithMemberNamesShared (), null));
        }

        [Test]
        public void ValueAndFieldNamesShared ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature ("TestNames", 1);
            service.Enumerations ["E"] = new global::KRPC.Service.Scanner.EnumerationSignature (
                "TestNames", "E", typeof(TestService.TestEnum), new [] {
                    new global::KRPC.Service.Scanner.EnumerationValueSignature ("TestNames", "E", "Foo", 0, string.Empty, false, string.Empty),
                    new global::KRPC.Service.Scanner.EnumerationValueSignature ("TestNames", "E", "FOO", 1, string.Empty, false, string.Empty)
                }, string.Empty, false, string.Empty);
            service.Structs ["S"] = new global::KRPC.Service.Scanner.StructSignature (
                "TestNames", "S", typeof(TestService.TestStruct), new [] {
                    new global::KRPC.Service.Scanner.StructFieldSignature ("TestNames", "S", "Bar", null, string.Empty, false, string.Empty),
                    new global::KRPC.Service.Scanner.StructFieldSignature ("TestNames", "S", "BAR", null, string.Empty, false, string.Empty)
                }, string.Empty, false, string.Empty);
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckMemberNames (service, errors);
            Assert.AreEqual (new [] {
                "In service TestNames: Values TestNames.E.FOO, TestNames.E.Foo share the name FOO, compared case-insensitively",
                "In service TestNames: Fields TestNames.S.BAR, TestNames.S.Bar share the name BAR, compared case-insensitively"
            }, errors.ToArray ());
            CollectionAssert.IsEmpty (service.Enumerations);
            CollectionAssert.IsEmpty (service.Structs);
        }

        [Test]
        public void ParameterNamesShared ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("MethodWithParametersSharingAName"));
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckMemberNames (service, errors);
            Assert.AreEqual (1, errors.Count);
            StringAssert.Contains (
                "Parameters TestService.TestClass_MethodWithParametersSharingAName(VALUE), " +
                "TestService.TestClass_MethodWithParametersSharingAName(value) share the name VALUE",
                errors [0]);
            Assert.IsFalse (service.Procedures.ContainsKey ("TestClass_MethodWithParametersSharingAName"));
        }

        [Test]
        public void ExtensionMemberNamedAsAClassMember ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            service.AddClassMethod (cls, classType, classType.GetMethod ("FloatToString"));
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("FloatToString")));
            service.RemoveDuplicates ();
            // The class keeps its own member
            CollectionAssert.AreEquivalent (new [] { "TestClass_FloatToString" }, service.Procedures.Keys);
        }

        [Test]
        public void ExtensionMemberSharingANameWithAClassMember ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            service.AddClassMethod (cls, classType, classType.GetMethod ("FloatToString"));
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("FLOATTOSTRING"));
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckMemberNames (service, errors);
            Assert.AreEqual (1, errors.Count);
            // The class keeps its own member, and loses the extension member
            CollectionAssert.AreEquivalent (new [] { "TestClass_FloatToString" }, service.Procedures.Keys);
        }

        [Test]
        public void ExtensionMembersNamedAlike ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("ExtensionDeclaredTwice"));
            var exn = Assert.Throws<global::KRPC.Service.ServiceException> (
                () => service.AddClassExtensionMethod (cls, classType, typeof(ClashingExtensions).GetMethod ("ExtensionDeclaredTwice")));
            StringAssert.Contains ("which is declared by KRPC.Test.Service.InvalidExtensions", exn.Message);
            service.RemoveDuplicates ();
            // Neither takes precedence, so both are left out
            CollectionAssert.IsEmpty (service.Procedures);
        }

        [TestCase (false)]
        [TestCase (true)]
        public void ExtensionPropertySplitAcrossClasses (bool getterFirst)
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            var getter = typeof(InvalidExtensions).GetMethod ("GetSplitProperty");
            var accessors = new [] {
                typeof(ClashingExtensions).GetMethod ("GetSplitProperty"),
                typeof(ClashingExtensions).GetMethod ("SetSplitProperty")
            };
            var order = getterFirst ? new [] { getter }.Concat (accessors) : accessors.Concat (new [] { getter });
            foreach (var method in order) {
                try {
                    service.AddClassExtensionProperty (cls, classType, method);
                } catch (global::KRPC.Service.ServiceException) {
                }
            }
            service.RemoveDuplicates ();
            // Neither class takes precedence, whatever the order
            CollectionAssert.IsEmpty (service.Procedures);
        }

        [Test]
        public void ExtensionMemberSharingANameWithTwoClassMembers ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            service.AddClassMethod (cls, classType, classType.GetMethod ("FloatToString"));
            // Another member of the class itself, whose name differs only in case
            service.Procedures ["TestClass_FloatTostring"] = service.Procedures ["TestClass_FloatToString"];
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("FLOATTOSTRING"));
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckMemberNames (service, errors);
            Assert.AreEqual (1, errors.Count);
            // The class's own members still clash once the extension member goes, so all go
            CollectionAssert.IsEmpty (service.Procedures);
        }

        [Test]
        public void UnavailableTypeInAnotherService ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            service.AddEnum (typeof(TestService.TestEnum));
            service.AddStruct (typeof(TestService.TestStruct));
            var other = new global::KRPC.Service.Scanner.ServiceSignature ("Other", 2);
            other.AddProcedure (typeof(TestService).GetMethod ("EchoStruct"));
            var services = new Dictionary<string, global::KRPC.Service.Scanner.ServiceSignature> {
                { service.Name, service }, { other.Name, other }
            };
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.RemoveUnavailableMembers (services, errors);
            CollectionAssert.IsEmpty (service.Structs);
            CollectionAssert.IsEmpty (other.Procedures);
            Assert.AreEqual (
                "In service Other: Procedure Other.EchoStruct is unavailable, as it uses KRPC.Test.Service.TestService+TestStruct, which is unavailable",
                errors [1]);
        }

        [Test]
        public void TypeDeclaredTwice ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature ("TestService3Name", 1);
            service.AddClass (typeof(TestClass3));
            var exn = Assert.Throws<global::KRPC.Service.ServiceException> (() => service.AddClass (typeof(TestClass3)));
            Assert.AreEqual ("Class TestService3Name.TestClass3 is declared more than once, so each copy is unavailable", exn.Message);
            service.AddEnum (typeof(TestEnumWithAnId3));
            service.RemoveDuplicates ();
            CollectionAssert.IsEmpty (service.Classes);
            CollectionAssert.AreEquivalent (new [] { "TestEnumWithAnId3" }, service.Enumerations.Keys);
        }

        [Test]
        public void MembersUsingAnUnavailableType ()
        {
            // TestStruct has a TestClass field, and the service has no TestClass
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            service.AddEnum (typeof(TestService.TestEnum));
            service.AddStruct (typeof(TestService.TestStruct));
            service.AddStruct (typeof(TestService.TestNestedStruct));
            service.AddProcedure (typeof(TestService).GetMethod ("EchoStruct"));
            service.AddProcedure (typeof(TestService).GetMethod ("CreateTestObject"));
            service.AddProcedure (typeof(TestService).GetMethod ("ProcedureEnumReturn"));
            var services = new Dictionary<string, global::KRPC.Service.Scanner.ServiceSignature> { { service.Name, service } };
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.RemoveUnavailableMembers (services, errors);
            // Leaving out TestStruct leaves out the struct and the procedure that use it
            Assert.AreEqual (new [] {
                "In service TestService: Struct TestService.TestStruct is unavailable, as its field ObjectField uses KRPC.Test.Service.TestService+TestClass, which is unavailable",
                "In service TestService: Procedure TestService.CreateTestObject is unavailable, as it uses KRPC.Test.Service.TestService+TestClass, which is unavailable",
                "In service TestService: Struct TestService.TestNestedStruct is unavailable, as its field StructField uses KRPC.Test.Service.TestService+TestStruct, which is unavailable",
                "In service TestService: Procedure TestService.EchoStruct is unavailable, as it uses KRPC.Test.Service.TestService+TestStruct, which is unavailable"
            }, errors.ToArray ());
            CollectionAssert.AreEquivalent (new [] { "TestEnum" }, service.Enumerations.Keys);
            CollectionAssert.IsEmpty (service.Structs);
            CollectionAssert.AreEquivalent (new [] { "ProcedureEnumReturn" }, service.Procedures.Keys);
        }

        [Test]
        public void MembersOfAnUnavailableClass ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            service.AddClassMethod (cls, classType, classType.GetMethod ("FloatToString"));
            service.AddClassMethod (cls, classType, classType.GetMethod ("StaticMethod"));
            service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("ExtensionDeclaredTwice"));
            service.AddProcedure (typeof(TestService).GetMethod ("ProcedureNoArgsNoReturn"));
            service.Classes.Remove (cls);
            var services = new Dictionary<string, global::KRPC.Service.Scanner.ServiceSignature> { { service.Name, service } };
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.RemoveUnavailableMembers (services, errors);
            // The error that left out the class names it, so its members add none
            CollectionAssert.IsEmpty (errors);
            CollectionAssert.AreEquivalent (new [] { "ProcedureNoArgsNoReturn" }, service.Procedures.Keys);
        }

        [Test]
        public void InvalidParameterName ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var classType = typeof(TestService.TestClass);
            var cls = service.AddClass (classType);
            var exn = Assert.Throws<global::KRPC.Service.ServiceException> (
                () => service.AddClassExtensionMethod (cls, classType, typeof(InvalidExtensions).GetMethod ("MethodWithAnInvalidParameterName")));
            StringAssert.Contains ("some_value is not a valid kRPC name", exn.Message);
        }

        [TestCase ("Foo_Bar")]
        [TestCase ("\u00dcnit")]
        [TestCase ("1Foo")]
        [TestCase ("Foo\n")]
        [TestCase ("")]
        public void InvalidEnumerationValueName (string name)
        {
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => new global::KRPC.Service.Scanner.EnumerationValueSignature ("TestNames", "E", name, 0, string.Empty, false, string.Empty));
        }

        [TestCase ("Foo")]
        [TestCase ("foo2")]
        public void ValidEnumerationValueName (string name)
        {
            Assert.DoesNotThrow (
                () => new global::KRPC.Service.Scanner.EnumerationValueSignature ("TestNames", "E", name, 0, string.Empty, false, string.Empty));
        }

        [Test]
        public void ServiceNamesShared ()
        {
            var services = new Dictionary<string, global::KRPC.Service.Scanner.ServiceSignature> {
                { "TestServices", new global::KRPC.Service.Scanner.ServiceSignature ("TestServices", 1) },
                { "TESTSERVICES", new global::KRPC.Service.Scanner.ServiceSignature ("TESTSERVICES", 2) },
                { "OtherService", new global::KRPC.Service.Scanner.ServiceSignature ("OtherService", 3) }
            };
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => global::KRPC.Service.Scanner.Scanner.CheckServiceNames (services, null));
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckServiceNames (services, errors);
            Assert.AreEqual (1, errors.Count);
            StringAssert.Contains ("Services TESTSERVICES, TestServices share a name", errors [0]);
            CollectionAssert.AreEquivalent (new [] { "OtherService" }, services.Keys);
        }

        [Test]
        public void ServiceDeclaredTwice ()
        {
            var signatures = new Dictionary<string, global::KRPC.Service.Scanner.ServiceSignature> ();
            Assert.IsTrue (global::KRPC.Service.Scanner.Scanner.CheckServiceNotDeclared (signatures, typeof(TestService3), null));
            // TestService declares the name that TestService3 declares
            signatures ["TestService3Name"] = new global::KRPC.Service.Scanner.ServiceSignature (typeof(TestService), 1);
            var errors = new List<string> ();
            Assert.IsFalse (global::KRPC.Service.Scanner.Scanner.CheckServiceNotDeclared (signatures, typeof(TestService3), errors));
            Assert.AreEqual (1, errors.Count);
            StringAssert.Contains (
                "In service TestService3Name: Service is declared by both KRPC.Test.Service.TestService and KRPC.Test.Service.TestService3",
                errors [0]);
            Assert.Throws<global::KRPC.Service.ServiceException> (
                () => global::KRPC.Service.Scanner.Scanner.CheckServiceNotDeclared (signatures, typeof(TestService3), null));
        }

        [Test]
        public void TypeNamedAsItsService ()
        {
            var service = new global::KRPC.Service.Scanner.ServiceSignature ("TESTCLASS3", 1);
            service.AddClass (typeof(TestClass3));
            var errors = new List<string> ();
            global::KRPC.Service.Scanner.Scanner.CheckTypes (service, errors);
            Assert.AreEqual (1, errors.Count);
            StringAssert.Contains ("Type TESTCLASS3.TestClass3 has the name of its service", errors [0]);
            CollectionAssert.IsEmpty (service.Classes);
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
