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
}
