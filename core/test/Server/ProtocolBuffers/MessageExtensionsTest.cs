using System.Collections.Generic;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service;
using KRPC.Test.Service;
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

        static Schema.KRPC.Request Request (string procedure, params object[] values)
        {
            var services = global::KRPC.Service.Services.Instance;
            var signature = services.GetProcedureSignature (new ProcedureCall ("TestService", procedure));
            var call = new Schema.KRPC.ProcedureCall { Service = "TestService", Procedure = procedure };
            for (int i = 0; i < values.Length; i++)
                call.Arguments.Add (new Schema.KRPC.Argument {
                    Position = (uint)i,
                    Value = Encoder.Encode (values [i], signature.Parameters [i].Spec)
                });
            var request = new Schema.KRPC.Request { Id = 3 };
            request.Calls.Add (call);
            return request;
        }

        [Test]
        public void DecodeValuesOffTheGameThread ()
        {
            var list = new List<string> { "a", "b", "c" };
            var request = Request ("EchoList", list).ToMessageOffGameThread ();
            Assert.AreEqual (3, request.Id);
            var argument = request.Calls [0].Arguments [0];
            Assert.IsNull (argument.Encoded);
            CollectionAssert.AreEqual (list, (IList<string>)argument.Value);
            Assert.IsNull (request.Calls [0].Encoded);
        }

        [Test]
        public void LeaveObjectsForTheGameThread ()
        {
            var obj = new TestService.TestClass ("foo");
            var request = Request ("EchoTestObject", obj).ToMessageOffGameThread ();
            var argument = request.Calls [0].Arguments [0];
            Assert.IsNotNull (argument.Encoded);
            Assert.IsNull (argument.Value);
            request.DecodeRemaining ();
            Assert.AreSame (obj, request.Calls [0].Arguments [0].Value);
            Assert.IsNull (request.Calls [0].Encoded);
        }

        [Test]
        public void DecodeAFailedCallAgainOnTheGameThread ()
        {
            var request = Request ("BlockingProcedureNoReturn", 1);
            request.Calls [0].Arguments [0].Value = Google.Protobuf.ByteString.CopyFrom (0xff);
            var decoded = request.ToMessageOffGameThread ();
            Assert.IsTrue (decoded.Calls [0].DecodeFailed);
            decoded.DecodeRemaining ();
            Assert.IsNotNull (decoded.Calls [0].DecodeException);
            Assert.AreEqual (
                request.ToMessage ().Calls [0].DecodeException.Message,
                decoded.Calls [0].DecodeException.Message);
        }
    }
}
