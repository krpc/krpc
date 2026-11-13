using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class ModAPITest
    {
        const string Assembly = "KRPC.Core.Test";
        const string Target = "KRPC.Test.Utils.ModAPITest+ModTarget";

        static class ModTarget
        {
            public static int Number {
                get { return 42; }
            }

            public static int Twice (int x)
            {
                return 2 * x;
            }
        }

        sealed class Fine : ModAPI
        {
            Fine () : base ("Fine", Assembly)
            {
            }

            public static Func<int> Number;
            public static Func<int> Gone;
            public static Func<int> GoneType;

            protected override void Bind (ModBinder mod)
            {
                var target = mod.Type (Target);
                Number = target.StaticGetter<int> ("Number");
                Gone = target.StaticGetter<int> ("Gone");
                GoneType = mod.Type ("No.Such").StaticGetter<int> ("Number");
            }
        }

        sealed class NotInstalled : ModAPI
        {
            NotInstalled () : base ("Absent", "NoSuchAssembly")
            {
            }

            protected override void Bind (ModBinder mod)
            {
                throw new InvalidOperationException ("not called");
            }
        }

        sealed class RequiredType : ModAPI
        {
            RequiredType () : base ("RequiredType", Assembly)
            {
            }

            protected override void Bind (ModBinder mod)
            {
                mod.Require.Type ("No.Such");
            }
        }

        sealed class RequiredMember : ModAPI
        {
            RequiredMember () : base ("RequiredMember", Assembly)
            {
            }

            protected override void Bind (ModBinder mod)
            {
                mod.Type (Target).Require.StaticGetter<int> ("Gone");
            }
        }

        sealed class Throws : ModAPI
        {
            Throws () : base ("Throws", Assembly)
            {
            }

            protected override void Bind (ModBinder mod)
            {
                throw new InvalidOperationException ("a bug");
            }
        }

        sealed class Waiting : ModAPI
        {
            Waiting () : base ("Waiting", Assembly)
            {
            }

            public static string State;

            protected override void Bind (ModBinder mod)
            {
            }

            protected override string NotReady ()
            {
                return State;
            }
        }

        static class StaticsAPI
        {
            public static Func<int, int> Twice { get; internal set; }

            public static Func<int> Thrice { get; internal set; }
        }

        sealed class WithStatics : ModAPI
        {
            WithStatics () : base ("WithStatics", Assembly)
            {
            }

            protected override void Bind (ModBinder mod)
            {
                mod.Statics (typeof (StaticsAPI), Target);
            }
        }

        sealed class NoConstructor : ModAPI
        {
            public NoConstructor (string name) : base (name, Assembly)
            {
            }

            protected override void Bind (ModBinder mod)
            {
            }
        }

        sealed class Assemblies : IModAssemblies
        {
            public IEnumerable<LoadedAssembly> Loaded {
                get {
                    yield return new LoadedAssembly (typeof (ModAPITest).Assembly, "/GameData/Test/" + Assembly + ".dll");
                }
            }
        }

        Action<string, Logger.Severity> writer;
        List<KeyValuePair<Logger.Severity, string>> log;

        [SetUp]
        public void SetUp ()
        {
            writer = Logger.Writer;
            log = new List<KeyValuePair<Logger.Severity, string>> ();
            Logger.Writer = (message, severity) => log.Add (new KeyValuePair<Logger.Severity, string> (severity, message));
            ModAPI.LoadAll (new Assemblies (), new [] {
                typeof (Fine), typeof (NotInstalled), typeof (RequiredType), typeof (RequiredMember),
                typeof (Throws), typeof (NoConstructor), typeof (Waiting), typeof (WithStatics)
            });
        }

        [TearDown]
        public void TearDown ()
        {
            Logger.Writer = writer;
        }

        IList<string> Messages (Logger.Severity severity)
        {
            return log.Where (x => x.Key == severity).Select (x => x.Value.Substring (x.Value.IndexOf (" - ", StringComparison.Ordinal) + 3)).ToList ();
        }

        [Test]
        public void Available ()
        {
            var api = ModAPI.Get<Fine> ();
            Assert.IsTrue (api.IsInstalled);
            Assert.IsTrue (api.IsAvailable);
            Assert.IsNull (api.Reason);
            Assert.DoesNotThrow (api.CheckAvailable);
            Assert.AreEqual (42, Fine.Number ());
            CollectionAssert.AreEqual (new [] { "ModTarget.Gone", "Such" }, api.MissingMembers);
            var error = Assert.Throws<ModMemberMissingException> (() => Fine.Gone ());
            Assert.AreEqual ("Fine: ModTarget.Gone is not available in the installed version", error.Message);
            Assert.Throws<ModMemberMissingException> (() => Fine.GoneType ());
        }

        [Test]
        public void NotInstalledMod ()
        {
            var api = ModAPI.Get<NotInstalled> ();
            Assert.IsFalse (api.IsInstalled);
            Assert.IsFalse (api.IsAvailable);
            var error = Assert.Throws<InvalidOperationException> (api.CheckAvailable);
            Assert.AreEqual ("Absent is not available: not installed", error.Message);
            CollectionAssert.Contains (Messages (Logger.Severity.Info), "Absent is not installed");
        }

        [Test]
        public void RequiredTypeMissing ()
        {
            var api = ModAPI.Get<RequiredType> ();
            Assert.IsTrue (api.IsInstalled);
            Assert.IsFalse (api.IsAvailable);
            var error = Assert.Throws<InvalidOperationException> (api.CheckAvailable);
            Assert.AreEqual ("RequiredType is not available: type Such is not available in the installed version", error.Message);
            CollectionAssert.Contains (Messages (Logger.Severity.Error),
                "RequiredType failed to load: type Such is not available in the installed version");
        }

        [Test]
        public void RequiredMemberMissing ()
        {
            var api = ModAPI.Get<RequiredMember> ();
            Assert.IsTrue (api.IsInstalled);
            Assert.IsFalse (api.IsAvailable);
            Assert.AreEqual ("ModTarget.Gone is not available in the installed version", api.Reason);
        }

        [Test]
        public void BindThrows ()
        {
            var api = ModAPI.Get<Throws> ();
            Assert.IsTrue (api.IsInstalled);
            Assert.IsFalse (api.IsAvailable);
            Assert.AreEqual ("InvalidOperationException: a bug", api.Reason);
            StringAssert.StartsWith ("Throws failed to load: System.InvalidOperationException: a bug",
                Messages (Logger.Severity.Error).Single (x => x.StartsWith ("Throws", StringComparison.Ordinal)));
        }

        [Test]
        public void Statics ()
        {
            var api = ModAPI.Get<WithStatics> ();
            Assert.IsTrue (api.IsAvailable);
            Assert.AreEqual (6, StaticsAPI.Twice (3));
            Assert.Throws<ModMemberMissingException> (() => StaticsAPI.Thrice ());
            CollectionAssert.AreEqual (new [] { "ModTarget.Thrice" }, api.MissingMembers);
        }

        [Test]
        public void NotReady ()
        {
            var api = ModAPI.Get<Waiting> ();
            Waiting.State = "loading";
            Assert.IsTrue (api.IsAvailable);
            var error = Assert.Throws<InvalidOperationException> (api.CheckAvailable);
            Assert.AreEqual ("Waiting is not ready: loading", error.Message);
            Waiting.State = null;
            Assert.DoesNotThrow (api.CheckAvailable);
        }

        [Test]
        public void ConstructorFails ()
        {
            Assert.IsTrue (Messages (Logger.Severity.Error).Any (x => x.StartsWith (
                "Failed to create the mod integration " + typeof (NoConstructor), StringComparison.Ordinal)));
            Assert.Throws<InvalidOperationException> (() => ModAPI.Get<NoConstructor> ());
            Assert.IsTrue (ModAPI.Get<WithStatics> ().IsAvailable);
        }

        [Test]
        public void Problems ()
        {
            CollectionAssert.AreEquivalent (new [] {
                "Fine", "RequiredType", "RequiredMember", "Throws", "WithStatics"
            }, ServiceProblems.Mods.Select (x => x.Name));
        }
    }
}
