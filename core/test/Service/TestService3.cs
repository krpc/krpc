using System;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    [KRPCService (Name = "TestService3Name", GameScene = GameScene.Editor, Id = 1234)]
    public static class TestService3
    {
        [KRPCProcedure]
        public static void AProcedure ()
        {
        }

        [KRPCProperty]
        public static int AProperty {
            get { return 0; }
        }
    }

    [KRPCClass (Service = "TestService3Name")]
    public class TestClass3
    {
    }

    [KRPCClass (Service = "TestService3Name")]
    public class TestBaseClass3
    {
        [KRPCMethod (Id = 10)]
        public void BaseMethod ()
        {
        }

        [KRPCProperty (GetterId = 11)]
        public int BaseProperty { get; set; }
    }

    [KRPCClass (Service = "TestService3Name")]
    [KRPCProcedureId (nameof (TestBaseClass3.BaseProperty), SetterId = 12)]
    public class TestDerivedClass3 : TestBaseClass3
    {
    }

    public class TestGenericBase3<T>
    {
        [KRPCMethod]
        public void GenericMethod ()
        {
        }
    }

    [KRPCClass (Service = "TestService3Name")]
    [KRPCProcedureId (nameof (TestGenericBase3<TestGenericDerivedClass3>.GenericMethod), Id = 13)]
    public class TestGenericDerivedClass3 : TestGenericBase3<TestGenericDerivedClass3>
    {
    }

    [KRPCClass (Service = "TestService3Name", Id = 20)]
    public class TestClassWithAnId3
    {
    }

    [KRPCEnum (Service = "TestService3Name", Id = 21)]
    public enum TestEnumWithAnId3
    {
        Value
    }

    [KRPCStruct (Service = "TestService3Name", Id = 22)]
    public struct TestStructWithAnId3
    {
        [KRPCProperty]
        public int Field { get; set; }
    }

    [KRPCException (Service = "TestService3Name", Id = 23)]
    public class TestExceptionWithAnId3 : Exception
    {
    }

    // Types that clash with the types above, when a test adds them to one service. Each is in a
    // service with no type it clashes with, so that the scan of the test assembly passes.

    [KRPCEnum (Service = "TestService2")]
    public enum TESTCLASS3
    {
        Value
    }

    [KRPCClass (Service = "TestService2", Id = 21)]
    public class TestClassSharingAnId3
    {
    }
}
