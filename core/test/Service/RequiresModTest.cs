using System;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Utils;
using NUnit.Framework;
using Expression = KRPC.Service.KRPC.Expression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class RequiresModTest
    {
        const string NotInstalled = "TestMod is not available: not installed";

        static ProcedureResult Run (string procedure, params Argument[] args)
        {
            var call = new ProcedureCall ("TestServiceMod", procedure);
            foreach (var arg in args)
                call.Arguments.Add (arg);
            return new ProcedureCallContinuation (call).Run ();
        }

        static void CheckValue (object expected, ProcedureResult result)
        {
            Assert.IsFalse (result.HasError, result.HasError ? result.Error.Description : null);
            Assert.AreEqual (expected, result.Value);
        }

        static void CheckError (string description, ProcedureResult result)
        {
            Assert.IsTrue (result.HasError);
            Assert.AreEqual ("InvalidOperationException", result.Error.Exception);
            Assert.AreEqual (description, result.Error.Description);
        }

        [SetUp]
        public void SetUp ()
        {
            CallContext.GameScene = GameScene.Flight;
        }

        [TearDown]
        public void TearDown ()
        {
            TestMod.Unready = null;
            SecondTestMod.Unready = null;
            TestMod.Load (false);
        }

        [Test]
        public void Procedure ()
        {
            TestMod.Load (true);
            CheckValue (42, Run ("get_Number"));
            TestMod.Load (false);
            CheckError (NotInstalled, Run ("get_Number"));
        }

        [Test]
        public void ClassMember ()
        {
            TestMod.Load (true);
            var instance = new TestModClass ();
            CheckValue (44, Run ("TestModClass_get_Number", new Argument (0, instance)));
            TestMod.Load (false);
            CheckError (NotInstalled, Run ("TestModClass_get_Number", new Argument (0, instance)));
        }

        [Test]
        public void NearestAttribute ()
        {
            TestMod.Load (true);
            CheckError ("OtherTestMod is not available: not installed", Run ("OtherModProcedure"));
        }

        [Test]
        public void AnyMod ()
        {
            TestMod.Load (true);
            CheckValue (45, Run ("AnyModProcedure"));
            TestMod.Load (false);
            CheckError ("OtherTestMod or TestMod is not available: not installed", Run ("AnyModProcedure"));
        }

        [Test]
        public void NotReady ()
        {
            TestMod.Load (true);
            TestMod.Unready = "loading";
            CheckError ("TestMod is not ready: loading", Run ("get_Number"));
            CheckError ("OtherTestMod or TestMod is not available: OtherTestMod not installed; TestMod not ready: loading",
                Run ("AnyModProcedure"));
            TestMod.Unready = null;
            CheckValue (42, Run ("get_Number"));
        }

        [Test]
        public void NotReadyAlternatives ()
        {
            TestMod.Load (true);
            TestMod.Unready = "loading";
            CheckValue (47, Run ("EitherModProcedure"));
            SecondTestMod.Unready = "loading";
            CheckError ("TestMod or SecondTestMod is not ready: loading", Run ("EitherModProcedure"));
        }

        [Test]
        public void AllMods ()
        {
            TestMod.Load (true);
            CheckError ("OtherTestMod is not available: not installed", Run ("AllModsProcedure"));
            TestMod.Load (false);
            CheckError ("TestMod and OtherTestMod are not available: not installed", Run ("AllModsProcedure"));
        }

        [Test]
        public void AvailableIsExempt ()
        {
            TestMod.Load (false);
            CheckValue (false, Run ("get_Available"));
            TestMod.Load (true);
            CheckValue (true, Run ("get_Available"));
        }

        [Test]
        public void BeforeGameScene ()
        {
            TestMod.Load (false);
            CallContext.GameScene = GameScene.None;
            CheckError (NotInstalled, Run ("get_Number"));
        }

        [Test]
        public void ServerSideFunction ()
        {
            TestMod.Load (true);
            var expression = Expression.Call (new ProcedureCall ("TestServiceMod", "get_Number"));
            var function = LinqExpression.Lambda<Func<int>> (expression).Compile ();
            Assert.AreEqual (42, function ());
            TestMod.Load (false);
            var error = Assert.Throws<InvalidOperationException> (() => function ());
            Assert.AreEqual (NotInstalled, error.Message);
        }

        [Test]
        public void NotAModAPI ()
        {
            var error = Assert.Throws<ServiceException> (
                () => TypeUtils.GetRequiredMod (typeof (InvalidRequiresMod).GetMethod ("NotAModAPI"), null));
            Assert.AreEqual (
                "RequiresMod on Void NotAModAPI() names System.String, which is not a subclass of KRPC.Utils.ModAPI",
                error.Message);
        }

        [Test]
        public void NoMods ()
        {
            var error = Assert.Throws<ServiceException> (
                () => TypeUtils.GetRequiredMod (typeof (InvalidRequiresMod).GetMethod ("NoMods"), null));
            Assert.AreEqual ("RequiresMod on Void NoMods() names no integration", error.Message);
        }

        [Test]
        public void AccessorTagged ()
        {
            var error = Assert.Throws<ServiceException> (
                () => TypeUtils.GetPropertyRequiredMod (typeof (InvalidRequiresMod).GetProperty ("AccessorTagged"), null));
            Assert.AreEqual ("RequiresMod on an accessor of AccessorTagged, in place of on the property", error.Message);
        }

        [Test]
        public void NoConstructor ()
        {
            var error = Assert.Throws<ServiceException> (
                () => TypeUtils.GetRequiredMod (typeof (InvalidRequiresMod).GetMethod ("NoConstructor"), null));
            Assert.AreEqual (
                "RequiresMod on Void NoConstructor() names KRPC.Test.Service.NoConstructorTestMod, " +
                "which has no parameterless constructor", error.Message);
        }
    }
}
