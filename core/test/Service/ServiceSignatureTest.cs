using KRPC.Service;
using KRPC.Service.Scanner;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ServiceSignatureTest
    {
        static ServiceSignature ServiceWithTestClass (out string cls)
        {
            var service = new ServiceSignature (typeof(TestService), 1);
            cls = service.AddClass (typeof(TestService.TestClass));
            return service;
        }

        [Test]
        public void ExtensionMemberClashingWithAClassMethod ()
        {
            string cls;
            var service = ServiceWithTestClass (out cls);
            var classType = typeof(TestService.TestClass);
            service.AddClassMethod (cls, classType, classType.GetMethod ("FloatToString"));
            var exn = Assert.Throws<ServiceException> (
                () => service.AddClassExtensionMethod (
                    cls, classType, typeof(InvalidExtensions).GetMethod ("FloatToString")));
            StringAssert.Contains ("InvalidExtensions.FloatToString", exn.Message);
            StringAssert.Contains ("TestService.TestClass_FloatToString", exn.Message);
        }

        [Test]
        public void ExtensionMemberClashingWithAnotherExtensionMember ()
        {
            string cls;
            var service = ServiceWithTestClass (out cls);
            var classType = typeof(TestService.TestClass);
            var method = typeof(TestServiceExtensions).GetMethod ("ExtensionMethod");
            service.AddClassExtensionMethod (cls, classType, method);
            var exn = Assert.Throws<ServiceException> (
                () => service.AddClassExtensionMethod (cls, classType, method));
            StringAssert.Contains ("TestServiceExtensions", exn.Message);
        }

        [Test]
        public void ExtensionPropertyClashingWithAClassProperty ()
        {
            string cls;
            var service = ServiceWithTestClass (out cls);
            var classType = typeof(TestService.TestClass);
            var property = classType.GetProperty ("ClassPropertyAvailableInInheritedGameScene");
            service.AddClassProperty (cls, classType, property);
            var exn = Assert.Throws<ServiceException> (
                () => service.AddClassExtensionProperty (
                    cls, classType,
                    typeof(InvalidExtensions).GetMethod ("SetClassPropertyAvailableInInheritedGameScene")));
            StringAssert.Contains ("TestService.TestClass_get_ClassPropertyAvailableInInheritedGameScene", exn.Message);
            StringAssert.Contains ("the class itself", exn.Message);
        }

        [Test]
        public void ExtensionPropertyAccessorPair ()
        {
            string cls;
            var service = ServiceWithTestClass (out cls);
            var classType = typeof(TestService.TestClass);
            var extensions = typeof(TestServiceExtensions);
            service.AddClassExtensionProperty (cls, classType, extensions.GetMethod ("GetExtensionReadWriteProperty"));
            Assert.DoesNotThrow (
                () => service.AddClassExtensionProperty (
                    cls, classType, extensions.GetMethod ("SetExtensionReadWriteProperty")));
        }

        [Test]
        public void ExtensionMemberOnAClassThatIsNotInTheService ()
        {
            var service = new ServiceSignature (typeof(TestService), 1);
            Assert.Throws<ServiceException> (
                () => service.AddClassExtensionMethod (
                    "TestClass", typeof(TestService.TestClass),
                    typeof(TestServiceExtensions).GetMethod ("ExtensionMethod")));
        }
    }
}
