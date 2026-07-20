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
    }
}
