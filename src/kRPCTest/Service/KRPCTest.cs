using NUnit.Framework;
using System;

namespace KRPCTest.Service
{
    [TestFixture]
    public class KRPCTest
    {
        [Test]
        public void GetServices ()
        {
            var services = KRPC.Service.KRPC.GetServices () as KRPC.Schema.KRPC.Services;
            Assert.IsNotNull (services);
            Assert.AreEqual (2, services.Services_Count);
            foreach (KRPC.Schema.KRPC.Service service in services.Services_List) {
                if (service.Name == "KRPC") {
                    foreach (KRPC.Schema.KRPC.Method method in service.MethodsList) {
                        Assert.AreEqual ("GetServices", method.Name);
                        Assert.AreEqual ("KRPC.Services", method.ReturnType);
                        Assert.IsFalse (method.HasParameterType);
                    }
                }
            }
        }
    }
}

