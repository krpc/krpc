using NUnit.Framework;

namespace KRPCTest.Service
{
    [TestFixture]
    public class KRPCTest
    {
        [Test]
        [Ignore]
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
            foreach (Krpc.Service service in services.Services_) {
                if (service.Name == "KRPC") {
                    foundKRPCService = true;
                    Assert.AreEqual (4, service.Procedures.Count);
                    int found = 0;
                    foreach (Krpc.Procedure method in service.Procedures) {
                        if (method.Name == "GetStatus") {
                            Assert.AreEqual ("Krpc.Status", method.ReturnType);
                            Assert.AreEqual (0, method.Parameters.Count);
                            found++;
                        }
                        if (method.Name == "GetServices") {
                            Assert.AreEqual ("Krpc.Services", method.ReturnType);
                            Assert.AreEqual (0, method.Parameters.Count);
                            found++;
                        }
                        if (method.Name == "AddStream") {
                            Assert.AreEqual ("uint32", method.ReturnType);
                            Assert.AreEqual (1, method.Parameters.Count);
                            Assert.AreEqual ("request", method.Parameters [0].Name);
                            Assert.AreEqual ("Krpc.Request", method.Parameters [0].Type);
                            Assert.IsTrue (method.Parameters [0].DefaultArgument.IsEmpty);
                            found++;
                        }
                        if (method.Name == "RemoveStream") {
                            Assert.AreEqual ("", method.ReturnType);
                            Assert.AreEqual (1, method.Parameters.Count);
                            Assert.AreEqual ("id", method.Parameters [0].Name);
                            Assert.AreEqual ("uint32", method.Parameters [0].Type);
                            Assert.IsTrue (method.Parameters [0].DefaultArgument.IsEmpty);
                            found++;
                        }
                    }
                    Assert.AreEqual (4, found);
                    Assert.AreEqual (0, service.Classes.Count);
                    Assert.AreEqual (0, service.Enumerations.Count);
                }
            }
            Assert.IsTrue (foundKRPCService);
        }
    }
}

