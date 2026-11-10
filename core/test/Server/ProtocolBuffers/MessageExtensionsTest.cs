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
            var procedure = new Procedure ("ProcedureName", 42);
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
        public void EncodeIds ()
        {
            Assert.AreEqual (42, Encode (GameScene.All).Id);
            Assert.AreEqual (7, new global::KRPC.Service.Messages.Service ("ServiceName", 7).ToProtobufMessage ().Id);
            Assert.AreEqual (8, new Class ("ClassName", 8).ToProtobufMessage ().Id);
            Assert.AreEqual (9, new Enumeration ("EnumerationName", 9).ToProtobufMessage ().Id);
            Assert.AreEqual (10, new Struct ("StructName", 10).ToProtobufMessage ().Id);
            Assert.AreEqual (11, new global::KRPC.Service.Messages.Exception ("ExceptionName", 11).ToProtobufMessage ().Id);
            var error = new Error ("ServiceName", "ExceptionName", 12, 13, "Description", string.Empty).ToProtobufMessage ();
            Assert.AreEqual (12, error.ServiceId);
            Assert.AreEqual (13, error.ExceptionId);
        }

        [Test]
        public void EncodeTypeIds ()
        {
            var type = TypeSpec.Create (typeof(global::KRPC.Test.Service.TestClassWithAnId3)).ToProtobufMessage ();
            Assert.AreEqual ("TestService3Name", type.Service);
            Assert.AreEqual ("TestClassWithAnId3", type.Name);
            Assert.AreEqual (1234, type.ServiceId);
            Assert.AreEqual (20, type.Id);
            var list = TypeSpec.Create (typeof(System.Collections.Generic.IList<global::KRPC.Test.Service.TestEnumWithAnId3>)).ToProtobufMessage ();
            Assert.AreEqual (21, list.Types_ [0].Id);
            Assert.AreEqual (0, list.Id);
            Assert.AreEqual (0, TypeSpec.Create (typeof(int)).ToProtobufMessage ().ServiceId);
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
            result.Spec = TypeSpec.Create (typeof(int));
            result.Value = 42;
            var wire = result.ToProtobufMessage ();
            Assert.IsFalse (wire.IsNull);
            Assert.Greater (wire.Value.Length, 0);
        }

        [Test]
        public void EncodeProcedureResultWithEncodedValue ()
        {
            var result = new ProcedureResult ();
            result.Spec = TypeSpec.Create (typeof(int));
            result.Value = 42;
            result.EncodedValue = Encoder.Encode (43, result.Spec);
            Assert.AreEqual (result.EncodedValue, result.ToProtobufMessage ().Value);
            result.Value = 44;
            Assert.AreEqual (Encoder.Encode (44, result.Spec), result.ToProtobufMessage ().Value);
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
            var wire = new Parameter ("x", TypeSpec.Create (typeof(int))).ToProtobufMessage ();
            Assert.IsFalse (wire.HasDefaultValue);
            Assert.IsFalse (wire.DefaultValueIsNull);
            Assert.AreEqual (0, wire.DefaultValue.Length);
        }

        [Test]
        public void EncodeParameterWithValueDefault ()
        {
            var parameter = new Parameter ("x", TypeSpec.Create (typeof(int)));
            parameter.DefaultValue = 42;
            var wire = parameter.ToProtobufMessage ();
            Assert.IsTrue (wire.HasDefaultValue);
            Assert.IsFalse (wire.DefaultValueIsNull);
            Assert.Greater (wire.DefaultValue.Length, 0);
        }

        [Test]
        public void EncodeParameterWithNullDefault ()
        {
            var parameter = new Parameter ("x", TypeSpec.Create (typeof(string), true, null, "x"));
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
            argument.Value = Encoder.Encode ("foo", TypeSpec.Create (typeof(string)));
            var message = argument.ToMessage (TypeSpec.Create (typeof(string)));
            Assert.AreEqual ("foo", message.Value);
        }

        [Test]
        public void DecodeArgumentWithNull ()
        {
            var argument = new Schema.KRPC.Argument ();
            argument.Position = 0;
            argument.IsNull = true;
            var message = argument.ToMessage (TypeSpec.Create (typeof(string)));
            Assert.IsNull (message.Value);
        }
    }
}
