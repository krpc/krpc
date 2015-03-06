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
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Status", method.ReturnType);
                            Assert.AreEqual (0, method.ParametersCount);
                            found++;
                        }
                        if (method.Name == "GetServices") {
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Services", method.ReturnType);
                            Assert.AreEqual (0, method.ParametersCount);
                            found++;
                        }
                        if (method.Name == "AddStream") {
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint32", method.ReturnType);
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("request", method.ParametersList [0].Name);
                            Assert.AreEqual ("KRPC.Request", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
                            found++;
                        }
                        if (method.Name == "RemoveStream") {
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.ParametersCount);
                            Assert.AreEqual ("id", method.ParametersList [0].Name);
                            Assert.AreEqual ("uint32", method.ParametersList [0].Type);
                            Assert.IsFalse (method.ParametersList [0].HasDefaultArgument);
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

