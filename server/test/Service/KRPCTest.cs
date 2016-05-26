using NUnit.Framework;
using KRPC.Schema.KRPC;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class KRPCTest
    {
        [Test]
        public void GetVersion ()
        {
            var status = KRPC.Service.KRPC.GetStatus ();
            Assert.AreNotEqual ("", status.Version);
        }

        [Test]
        public void GetServices ()
        {
            var services = KRPC.Service.KRPC.GetServices ();
            Assert.IsNotNull (services);
            Assert.AreEqual (4, services.Services_.Count);
            bool foundKRPCService = false;
            foreach (var service in services.Services_) {
                if (service.Name == "KRPC") {
                    foundKRPCService = true;
                    Assert.AreEqual (5, service.Procedures.Count);
                    int found = 0;
                    foreach (var method in service.Procedures) {
                        if (method.Name == "GetStatus") {
                            Assert.AreEqual ("KRPC.Status", method.ReturnType);
                            Assert.AreEqual (0, method.Parameters.Count);
                            Assert.AreEqual (0, method.Attributes.Count);
                            Assert.AreNotEqual ("", method.Documentation);
                            found++;
                        }
                        if (method.Name == "GetServices") {
                            Assert.AreEqual ("KRPC.Services", method.ReturnType);
                            Assert.AreEqual (0, method.Parameters.Count);
                            Assert.AreEqual (0, method.Attributes.Count);
                            Assert.AreNotEqual ("", method.Documentation);
                            found++;
                        }
                        if (method.Name == "AddStream") {
                            Assert.AreEqual ("uint32", method.ReturnType);
                            Assert.AreEqual (1, method.Parameters.Count);
                            Assert.AreEqual ("request", method.Parameters [0].Name);
                            Assert.AreEqual ("KRPC.Request", method.Parameters [0].Type);
                            Assert.IsNull (method.Parameters [0].DefaultValue);
                            Assert.AreEqual (0, method.Attributes.Count);
                            Assert.AreNotEqual ("", method.Documentation);
                            found++;
                        }
                        if (method.Name == "RemoveStream") {
                            Assert.AreEqual ("", method.ReturnType);
                            Assert.AreEqual (1, method.Parameters.Count);
                            Assert.AreEqual ("id", method.Parameters [0].Name);
                            Assert.AreEqual ("uint32", method.Parameters [0].Type);
                            Assert.IsNull (method.Parameters [0].DefaultValue);
                            Assert.AreEqual (0, method.Attributes.Count);
                            Assert.AreNotEqual ("", method.Documentation);
                            found++;
                        }
                        if (method.Name == "get_CurrentGameScene") {
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (0, method.Parameters.Count);
                            Assert.AreEqual (2, method.Attributes.Count);
                            Assert.AreEqual ("Property.Get(CurrentGameScene)", method.Attributes [0]);
                            Assert.AreEqual ("ReturnType.Enum(KRPC.GameScene)", method.Attributes [1]);
                            Assert.AreNotEqual ("", method.Documentation);
                            found++;
                        }
                    }
                    Assert.AreEqual (5, found);
                    Assert.AreEqual (0, service.Classes.Count);
                    Assert.AreEqual (1, service.Enumerations.Count);
                    bool foundGameSceneEnumeration = false;
                    foreach (var enm in service.Enumerations) {
                        if (enm.Name == "GameScene") {
                            foundGameSceneEnumeration = true;
                            Assert.AreEqual (5, enm.Values.Count);
                            Assert.AreEqual ("SpaceCenter", enm.Values [0].Name);
                            Assert.AreEqual (0, enm.Values [0].Value);
                            Assert.AreEqual ("Flight", enm.Values [1].Name);
                            Assert.AreEqual (1, enm.Values [1].Value);
                            Assert.AreEqual ("TrackingStation", enm.Values [2].Name);
                            Assert.AreEqual (2, enm.Values [2].Value);
                            Assert.AreEqual ("EditorVAB", enm.Values [3].Name);
                            Assert.AreEqual (3, enm.Values [3].Value);
                            Assert.AreEqual ("EditorSPH", enm.Values [4].Name);
                            Assert.AreEqual (4, enm.Values [4].Value);
                        }
                    }
                    Assert.IsTrue (foundGameSceneEnumeration);
                }
            }
            Assert.IsTrue (foundKRPCService);
        }
    }
}

