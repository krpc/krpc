using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Messages;
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
            Assert.AreEqual (5, services.ServicesList.Count);
            CollectionAssert.AreEquivalent (
                new [] { "KRPC", "TestService", "TestService2", "TestService3Name", "TestServiceDeprecated" },
                services.ServicesList.Select (x => x.Name).ToList ());
        }

        [Test]
        public void TestService ()
        {
            var service = services.ServicesList.First (x => x.Name == "TestService");
            Assert.AreEqual (61, service.Procedures.Count);
            Assert.AreEqual (3, service.Classes.Count);
            Assert.AreEqual (2, service.Enumerations.Count);
            Assert.AreEqual ("<doc>\n<summary>\nTest service documentation.\n</summary>\n</doc>", service.Documentation);
            MessageAssert.IsNotDeprecated (service);
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
            MessageAssert.IsNotDeprecated (service);
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
                } else if (proc.Name == "EchoNullableEnum") {
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasNullableParameter (proc, 0, typeof(TestService.TestEnum), "x");
                    MessageAssert.HasReturnType (proc, typeof(TestService.TestEnum), true);
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
                    MessageAssert.HasParameterWithDefaultValue (proc, 1, typeof(string), "y", "jeb");
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
                } else {
                    Assert.Fail ("Procedure not found");
                }
                foundProcedures++;
            }
            Assert.AreEqual (61, foundProcedures);
            Assert.AreEqual (61, service.Procedures.Count);
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
            Assert.AreEqual (2, foundEnumerations);
            Assert.AreEqual (2, service.Enumerations.Count);
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
