using NUnit.Framework;
using System;

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
            Assert.AreEqual (2, services.Services_Count);
            foreach (KRPC.Schema.KRPC.Service service in services.Services_List) {
                if (service.Name == "KRPC") {
                    Assert.AreEqual (2, service.ProceduresCount);
                    foreach (KRPC.Schema.KRPC.Procedure method in service.ProceduresList) {
                        if (method.Name == "GetStatus") {
                            Assert.AreEqual ("GetStatus", method.Name);
                            Assert.AreEqual ("KRPC.Status", method.ReturnType);
                            Assert.AreEqual (0, method.ParameterTypesCount);
                        }
                        if (method.Name == "GetServices") {
                            Assert.AreEqual ("GetServices", method.Name);
                            Assert.AreEqual ("KRPC.Services", method.ReturnType);
                            Assert.AreEqual (0, method.ParameterTypesCount);
                        }
                    }
                }
            }
        }
    }
}

