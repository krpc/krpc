using System;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class EnumerationsTest
    {
        [Flags]
        public enum NegativeValue
        {
            None = 0,
            A = 1,
            All = -1
        }

        [Flags]
        public enum UndeclaredBit
        {
            None = 0,
            A = 1,
            AC = 5
        }

        [Flags]
        public enum NoZero
        {
            A = 1,
            B = 2
        }

        [Flags]
        public enum Valid
        {
            None = 0,
            A = 1,
            B = 2,
            AB = 3
        }

        [Test]
        public void IsFlags ()
        {
            Assert.IsTrue (Enumerations.IsFlags (typeof(TestService.TestFlags)));
            Assert.IsFalse (Enumerations.IsFlags (typeof(TestService.TestEnum)));
        }

        [TestCase (0, true)]
        [TestCase (3, true)]
        [TestCase (7, true)]
        [TestCase (8, false)]
        [TestCase (9, false)]
        [TestCase (-1, false)]
        public void FlagsValueIsValid (int value, bool valid)
        {
            Assert.AreEqual (valid, Enumerations.IsValid (typeof(TestService.TestFlags), value));
        }

        [TestCase (0, true)]
        [TestCase (2, true)]
        [TestCase (3, false)]
        [TestCase (-1, false)]
        public void EnumValueIsValid (int value, bool valid)
        {
            Assert.AreEqual (valid, Enumerations.IsValid (typeof(TestService.TestEnum), value));
        }

        [Test]
        public void ValidFlags ()
        {
            Assert.DoesNotThrow (() => Enumerations.ValidateFlags (typeof(Valid)));
        }

        [Test]
        public void FlagsWithoutZero ()
        {
            var exn = Assert.Throws<ServiceException> (() => Enumerations.ValidateFlags (typeof(NoZero)));
            StringAssert.Contains ("no value of 0", exn.Message);
        }

        [Test]
        public void NegativeFlagsValue ()
        {
            var exn = Assert.Throws<ServiceException> (() => Enumerations.ValidateFlags (typeof(NegativeValue)));
            StringAssert.Contains ("negative", exn.Message);
        }

        [Test]
        public void UndeclaredFlagsBit ()
        {
            var exn = Assert.Throws<ServiceException> (() => Enumerations.ValidateFlags (typeof(UndeclaredBit)));
            StringAssert.Contains ("AC", exn.Message);
        }
    }
}
