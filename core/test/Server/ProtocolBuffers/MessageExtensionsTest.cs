using KRPC.Server.ProtocolBuffers;
using KRPC.Service;
using KRPC.Service.Messages;
using NUnit.Framework;
using WireGameScene = KRPC.Schema.KRPC.Procedure.Types.GameScene;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class MessageExtensionsTest
    {
        static Schema.KRPC.Procedure Encode (GameScene scenes)
        {
            var procedure = new Procedure ("ProcedureName");
            procedure.GameScene = scenes;
            return procedure.ToProtobufMessage ();
        }

        [Test]
        public void EncodeProcedureGameScenes ()
        {
            CollectionAssert.AreEqual (
                new [] { WireGameScene.Flight }, Encode (GameScene.Flight).GameScenes);
            CollectionAssert.AreEqual (
                new [] { WireGameScene.EditorVab, WireGameScene.EditorSph },
                Encode (GameScene.Editor).GameScenes);
            CollectionAssert.AreEqual (
                new [] { WireGameScene.SpaceCenter, WireGameScene.TrackingStation },
                Encode (GameScene.SpaceCenter | GameScene.TrackingStation).GameScenes);
            CollectionAssert.AreEqual (
                new [] { WireGameScene.SpaceCenter, WireGameScene.AstronautComplex },
                Encode (GameScene.SpaceCenter | GameScene.AstronautComplex).GameScenes);
        }

        [Test]
        public void EncodeProcedureGameScenesAll ()
        {
            CollectionAssert.IsEmpty (Encode (GameScene.All).GameScenes);
        }

        [Test]
        public void EncodeProcedureResultWithValue ()
        {
            var result = new ProcedureResult ();
            result.Value = 42;
            var wire = result.ToProtobufMessage ();
            Assert.IsFalse (wire.IsNull);
            Assert.Greater (wire.Value.Length, 0);
        }

        [Test]
        public void EncodeProcedureResultWithNullValue ()
        {
            var result = new ProcedureResult ();
            result.Value = null;
            var wire = result.ToProtobufMessage ();
            Assert.IsTrue (wire.IsNull);
            Assert.AreEqual (0, wire.Value.Length);
        }

        [Test]
        public void EncodeParameterWithNoDefault ()
        {
            var wire = new Parameter ("x", typeof(int), false).ToProtobufMessage ();
            Assert.IsFalse (wire.HasDefaultValue);
            Assert.IsFalse (wire.DefaultValueIsNull);
            Assert.AreEqual (0, wire.DefaultValue.Length);
        }

        [Test]
        public void EncodeParameterWithValueDefault ()
        {
            var parameter = new Parameter ("x", typeof(int), false);
            parameter.DefaultValue = 42;
            var wire = parameter.ToProtobufMessage ();
            Assert.IsTrue (wire.HasDefaultValue);
            Assert.IsFalse (wire.DefaultValueIsNull);
            Assert.Greater (wire.DefaultValue.Length, 0);
        }

        [Test]
        public void EncodeParameterWithNullDefault ()
        {
            var parameter = new Parameter ("x", typeof(string), true);
            parameter.DefaultValue = null;
            var wire = parameter.ToProtobufMessage ();
            Assert.IsTrue (wire.HasDefaultValue);
            Assert.IsTrue (wire.DefaultValueIsNull);
            Assert.AreEqual (0, wire.DefaultValue.Length);
        }

        [Test]
        public void DecodeArgumentWithValue ()
        {
            var argument = new Schema.KRPC.Argument ();
            argument.Position = 0;
            argument.Value = Encoder.Encode ("foo");
            var message = argument.ToMessage (typeof(string));
            Assert.AreEqual ("foo", message.Value);
        }

        [Test]
        public void DecodeArgumentWithNull ()
        {
            var argument = new Schema.KRPC.Argument ();
            argument.Position = 0;
            argument.IsNull = true;
            var message = argument.ToMessage (typeof(string));
            Assert.IsNull (message.Value);
        }
    }
}
