using KRPC.Service;
using Newtonsoft.Json;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class GameSceneTest
    {
        [TestCase ("[\"SPACE_CENTER\",\"FLIGHT\",\"TRACKING_STATION\",\"EDITOR_VAB\",\"EDITOR_SPH\",\"MISSION_BUILDER\"]", GameScene.All)]
        [TestCase ("[\"SPACE_CENTER\"]", GameScene.SpaceCenter)]
        [TestCase ("[\"FLIGHT\"]", GameScene.Flight)]
        [TestCase ("[\"TRACKING_STATION\"]", GameScene.TrackingStation)]
        [TestCase ("[\"EDITOR_VAB\"]", GameScene.EditorVAB)]
        [TestCase ("[\"EDITOR_SPH\"]", GameScene.EditorSPH)]
        [TestCase ("[\"EDITOR_VAB\",\"EDITOR_SPH\"]", GameScene.Editor)]
        [TestCase ("[\"MISSION_BUILDER\"]", GameScene.MissionBuilder)]
        [TestCase ("[]", GameScene.None)]
        [TestCase ("[]", GameScene.Inherit)]
        [TestCase ("[\"SPACE_CENTER\",\"TRACKING_STATION\"]", GameScene.TrackingStation | GameScene.SpaceCenter)]
        [TestCase ("[\"SPACE_CENTER\",\"TRACKING_STATION\",\"EDITOR_VAB\",\"EDITOR_SPH\"]", GameScene.TrackingStation | GameScene.Editor | GameScene.SpaceCenter)]
        public void SerializeGameScene (string serialized, GameScene gameScene)
        {
            Assert.AreEqual (serialized, JsonConvert.SerializeObject (GameSceneUtils.Serialize (gameScene)));
        }
    }
}
