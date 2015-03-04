using NUnit.Framework;

namespace KRPCTest.Service
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
            Assert.AreEqual (4, services.Services_Count);
            bool foundKRPCService = false;
            foreach (KRPC.Schema.KRPC.Service service in services.Services_List) {
                if (service.Name == "KRPC") {
                    foundKRPCService = true;
                    Assert.AreEqual (4, service.ProceduresCount);
                    int found = 0;
                    foreach (KRPC.Schema.KRPC.Procedure method in service.ProceduresList) {
                        if (method.Name == "GetStatus") {
                            Assert.AreEqual ("KRPC.Status", method.ReturnType);
                            Assert.AreEqual (0, method.ParametersCount);
                            found++;
                        }
                        if (method.Name == "GetServices") {
                            Assert.AreEqual ("KRPC.Services", method.ReturnType);
                            Assert.AreEqual (0, method.ParametersCount);
                            found++;
                        }
                        if (method.Name == "AddStream") {
                            Assert.AreEqual ("uint32", method.ReturnType);
                            Assert.AreEqual (0, method.ParametersCount);
                            // TODO: check the parameters
                            found++;
                        }
                        if (method.Name == "RemoveStream") {
                            // TODO: check return type is void
                            Assert.AreEqual (1, method.ParametersCount);
                            // TODO: check the parameters
                            found++;
                        }
                    }
                    Assert.AreEqual (4, found);
                    Assert.AreEqual (0, service.ClassesCount);
                    Assert.AreEqual (0, service.EnumerationsCount);
                }
            }
            Assert.IsTrue (foundKRPCService);
        }
    }
}

