using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class TypeSpecTest
    {
        static TypeSpec Create (Type type, params Position[] path)
        {
            return TypeSpec.Create (type, false, new [] { path }, "test");
        }

        [Test]
        public void NoNullablePosition ()
        {
            var spec = TypeSpec.Create (typeof(IList<int>));
            Assert.AreEqual (typeof(IList<int>), spec.Type);
            Assert.IsFalse (spec.Nullable);
            Assert.AreEqual (1, spec.Types.Count);
            Assert.AreEqual (typeof(int), spec.Types [0].Type);
            Assert.IsFalse (spec.Types [0].Nullable);
        }

        [Test]
        public void NullableValueItself ()
        {
            var spec = Create (typeof(IList<int>));
            Assert.IsTrue (spec.Nullable);
            Assert.IsFalse (spec.Types [0].Nullable);
        }

        [Test]
        public void NullableValueTypeIsStructural ()
        {
            // A Nullable<T> is the type T at a position that allows a null
            var spec = TypeSpec.Create (typeof(IList<int?>));
            Assert.AreEqual (typeof(int), spec.Types [0].Type);
            Assert.IsTrue (spec.Types [0].Nullable);
        }

        [Test]
        public void NullableListElement ()
        {
            var spec = Create (typeof(IList<string>), Position.Element);
            Assert.IsFalse (spec.Nullable);
            Assert.IsTrue (spec.Types [0].Nullable);
        }

        [Test]
        public void NullableDictionaryValue ()
        {
            var spec = Create (typeof(IDictionary<string,string>), Position.Value);
            Assert.IsFalse (spec.Types [0].Nullable);
            Assert.IsTrue (spec.Types [1].Nullable);
        }

        [Test]
        public void NullableTupleItem ()
        {
            var spec = Create (typeof(Tuple<int,string>), Position.Item2);
            Assert.IsFalse (spec.Types [0].Nullable);
            Assert.IsTrue (spec.Types [1].Nullable);
        }

        [Test]
        public void NullablePositionInsideANullablePosition ()
        {
            var spec = Create (typeof(IList<IList<string>>), Position.Element, Position.Element);
            Assert.IsFalse (spec.Types [0].Nullable);
            Assert.IsTrue (spec.Types [0].Types [0].Nullable);
        }

        [Test]
        public void SeveralNullablePositions ()
        {
            var spec = TypeSpec.Create (
                typeof(IDictionary<string,Tuple<string,string>>), false,
                new [] {
                    new [] { Position.Value },
                    new [] { Position.Value, Position.Item1 }
                }, "test");
            Assert.IsTrue (spec.Types [1].Nullable);
            Assert.IsTrue (spec.Types [1].Types [0].Nullable);
            Assert.IsFalse (spec.Types [1].Types [1].Nullable);
        }

        // A path step that names no position of the type it is applied to

        [TestCase (typeof(IList<string>), Position.Value)]
        [TestCase (typeof(IDictionary<string,string>), Position.Element)]
        [TestCase (typeof(Tuple<string,string>), Position.Item3)]
        [TestCase (typeof(HashSet<string>), Position.Element)]
        [TestCase (typeof(int), Position.Element)]
        [TestCase (typeof(TestService.TestStruct), Position.Element)]
        public void PositionThatTheTypeDoesNotHave (Type type, Position position)
        {
            Assert.Throws<ServiceException> (() => Create (type, position));
        }

        [Test]
        public void PositionThatTheNestedTypeDoesNotHave ()
        {
            Assert.Throws<ServiceException> (
                () => Create (typeof(IList<IList<string>>), Position.Element, Position.Value));
        }

        // A position marked nullable whose type holds no null of its own

        [TestCase (typeof(IList<int>), Position.Element)]
        [TestCase (typeof(IDictionary<string,int>), Position.Value)]
        [TestCase (typeof(Tuple<string,TestService.TestEnum>), Position.Item2)]
        [TestCase (typeof(IList<TestService.TestStruct>), Position.Element)]
        public void PositionThatCannotHoldNull (Type type, Position position)
        {
            Assert.Throws<ServiceException> (() => Create (type, position));
        }

        [Test]
        public void NestedPositionThatCannotHoldNull ()
        {
            Assert.Throws<ServiceException> (
                () => Create (typeof(IList<IList<int>>), Position.Element, Position.Element));
        }

        [Test]
        public void NullableValueTypePositionNamedByAPath ()
        {
            var spec = Create (typeof(IList<int?>), Position.Element);
            Assert.IsTrue (spec.Types [0].Nullable);
        }

        [Test]
        public void DeclaredTypeIsTheTypeAsDeclared ()
        {
            var spec = TypeSpec.Create (typeof(int?), false, null, "test");
            Assert.AreEqual (typeof(int?), spec.DeclaredType);
            Assert.AreEqual (typeof(int), spec.Type);
            Assert.IsTrue (spec.Nullable);
        }

        [TestCase (typeof(int))]
        [TestCase (typeof(TestService.TestEnum))]
        [TestCase (typeof(TestService.TestStruct))]
        public void ValueItselfThatCannotHoldNull (Type type)
        {
            Assert.Throws<ServiceException> (
                () => TypeSpec.Create (type, true, null, "test"));
        }

        [TestCase (typeof(int?))]
        [TestCase (typeof(string))]
        [TestCase (typeof(TestService.TestClass))]
        [TestCase (typeof(IList<int>))]
        public void ValueItselfThatCanHoldNull (Type type)
        {
            Assert.IsTrue (TypeSpec.Create (type, true, null, "test").Nullable);
        }
    }
}
