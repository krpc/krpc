using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Test.Service
{
    // The integrations for the RequiresMod tests. TestMod's main assembly is this test assembly,
    // so a test loads it with the assembly to make it available, or without to make it
    // unavailable. OtherTestMod is never installed.

    public sealed class TestMod : ModAPI
    {
        TestMod () : base ("TestMod", "KRPC.Core.Test")
        {
        }

        /// <summary>
        /// Why the mod is not ready, or null when it is.
        /// </summary>
        public static string Unready;

        protected override void Bind (ModBinder mod)
        {
        }

        protected override string NotReady ()
        {
            return Unready;
        }

        /// <summary>
        /// Load the integrations, with the mod installed or not.
        /// </summary>
        public static void Load (bool installed)
        {
            ModAPI.LoadAll (new Assemblies (installed), new [] { typeof (TestMod), typeof (SecondTestMod), typeof (OtherTestMod) });
        }

        sealed class Assemblies : IModAssemblies
        {
            readonly bool installed;

            public Assemblies (bool installed)
            {
                this.installed = installed;
            }

            public IEnumerable<LoadedAssembly> Loaded {
                get {
                    if (installed)
                        yield return new LoadedAssembly (typeof (TestMod).Assembly, "/GameData/Test/KRPC.Core.Test.dll");
                }
            }
        }
    }

    public sealed class SecondTestMod : ModAPI
    {
        SecondTestMod () : base ("SecondTestMod", "KRPC.Core.Test")
        {
        }

        /// <summary>
        /// Why the mod is not ready, or null when it is.
        /// </summary>
        public static string Unready;

        protected override void Bind (ModBinder mod)
        {
        }

        protected override string NotReady ()
        {
            return Unready;
        }
    }

    public sealed class OtherTestMod : ModAPI
    {
        OtherTestMod () : base ("OtherTestMod", "NoSuchAssembly")
        {
        }

        protected override void Bind (ModBinder mod)
        {
        }
    }

    [KRPCService]
    [RequiresMod (typeof (TestMod))]
    public static class TestServiceMod
    {
        [KRPCProperty]
        public static bool Available {
            get { return ModAPI.Get<TestMod> ().IsAvailable; }
        }

        [KRPCProperty]
        public static int Number {
            get { return 42; }
        }

        [KRPCProcedure]
        public static TestModClass CreateClass ()
        {
            return new TestModClass ();
        }

        [KRPCProcedure]
        [RequiresMod (typeof (OtherTestMod))]
        public static int OtherModProcedure ()
        {
            return 43;
        }

        [KRPCProcedure]
        [RequiresMod (typeof (OtherTestMod))]
        [RequiresMod (typeof (TestMod))]
        public static int AnyModProcedure ()
        {
            return 45;
        }

        [KRPCProcedure]
        [RequiresMod (typeof (TestMod), typeof (OtherTestMod))]
        public static int AllModsProcedure ()
        {
            return 46;
        }

        [KRPCProcedure]
        [RequiresMod (typeof (TestMod))]
        [RequiresMod (typeof (SecondTestMod))]
        public static int EitherModProcedure ()
        {
            return 47;
        }
    }

    [KRPCClass (Service = "TestServiceMod")]
    public class TestModClass
    {
        [KRPCProperty]
        public int Number {
            get { return 44; }
        }
    }

    public sealed class NoConstructorTestMod : ModAPI
    {
        public NoConstructorTestMod (string name) : base (name, "NoSuchAssembly")
        {
        }

        protected override void Bind (ModBinder mod)
        {
        }
    }

    // Not a KRPCClass, so that the scan of the test assembly skips it
    public static class InvalidRequiresMod
    {
        [RequiresMod (typeof (string))]
        public static void NotAModAPI ()
        {
        }

        [RequiresMod (typeof (NoConstructorTestMod))]
        public static void NoConstructor ()
        {
        }

        [RequiresMod]
        public static void NoMods ()
        {
        }

        public static int AccessorTagged {
            [RequiresMod (typeof (TestMod))]
            get { return 0; }
        }
    }
}
