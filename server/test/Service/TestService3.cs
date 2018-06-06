using System.Diagnostics.CodeAnalysis;
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
    }

    [KRPCClass (Service = "TestService3Name")]
    [SuppressMessage ("Gendarme.Rules.Correctness", "AvoidConstructorsInStaticTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Design", "ConsiderUsingStaticTypeRule")]
    public class TestClass3
    {
    }
}
