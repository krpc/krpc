using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.Scanner;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ParameterSignatureTest
    {
        public static void DeclaredDefault (TestService.TestEnum x = TestService.TestEnum.Z)
        {
        }

        public static void UndeclaredDefault (TestService.TestEnum x = (TestService.TestEnum)7)
        {
        }

        public static void UndeclaredDefaultInAList (
            [KRPCDefaultValue (typeof(UndeclaredList))] IList<TestService.TestEnum> x)
        {
        }

        public static class UndeclaredList
        {
            public static IList<TestService.TestEnum> Create ()
            {
                return new List<TestService.TestEnum> { TestService.TestEnum.X, (TestService.TestEnum)7 };
            }
        }

        public static void UndeclaredDefaultInADictionary (
            [KRPCDefaultValue (typeof(UndeclaredDictionary))] IDictionary<string,TestService.TestEnum> x)
        {
        }

        public static class UndeclaredDictionary
        {
            public static IDictionary<string,TestService.TestEnum> Create ()
            {
                return new Dictionary<string,TestService.TestEnum> { { "a", (TestService.TestEnum)7 } };
            }
        }

        public static void UndeclaredDefaultInATuple (
            [KRPCDefaultValue (typeof(UndeclaredTuple))] Tuple<int,TestService.TestEnum> x)
        {
        }

        public static class UndeclaredTuple
        {
            public static Tuple<int,TestService.TestEnum> Create ()
            {
                return new Tuple<int,TestService.TestEnum> (1, (TestService.TestEnum)7);
            }
        }

        static ParameterSignature Signature (string method)
        {
            var parameter = typeof(ParameterSignatureTest).GetMethod (method).GetParameters () [0];
            return new ParameterSignature ("TestService." + method, new ProcedureParameter (parameter));
        }

        [Test]
        public void DeclaredEnumerationDefault ()
        {
            Assert.AreEqual (TestService.TestEnum.Z, Signature ("DeclaredDefault").DefaultValue);
        }

        [TestCase ("UndeclaredDefault")]
        [TestCase ("UndeclaredDefaultInAList")]
        [TestCase ("UndeclaredDefaultInADictionary")]
        [TestCase ("UndeclaredDefaultInATuple")]
        public void UndeclaredEnumerationDefault (string method)
        {
            var exn = Assert.Throws<ServiceException> (() => Signature (method));
            StringAssert.Contains ("TestService." + method, exn.Message);
            StringAssert.Contains ("holds 7", exn.Message);
            StringAssert.Contains ("TestEnum", exn.Message);
        }
    }
}
