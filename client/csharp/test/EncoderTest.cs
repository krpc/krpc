using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class EncoderTest
    {
        [Test]
        public void NullableSpecSharesItsType ()
        {
            // A nullable position holds a value of the type it holds anywhere else, so the
            // nullable form of a type is that type with the position marked
            Assert.AreEqual (typeof(string), TypeSpec.Null (typeof(string)).Type);
            Assert.AreEqual (typeof(uint), TypeSpec.For (typeof(uint?)).Type);
            Assert.AreEqual (
                typeof(Services.TestService.TestEnum),
                TypeSpec.For (typeof(Services.TestService.TestEnum?)).Type);
            Assert.IsTrue (TypeSpec.For (typeof(uint?)).Nullable);
            Assert.IsFalse (TypeSpec.For (typeof(uint)).Nullable);
        }

        static Schema.KRPC.Type Described (Schema.KRPC.Type.Types.TypeCode code, bool nullable = false)
        {
            return new Schema.KRPC.Type { Code = code, Nullable = nullable };
        }

        [Test]
        public void DescribedSpec ()
        {
            var list = Described (Schema.KRPC.Type.Types.TypeCode.List);
            list.Types_.Add (Described (Schema.KRPC.Type.Types.TypeCode.String, true));
            // A reference type element takes the server's nullability
            var spec = TypeSpec.FromDescribed (typeof(IList<string>), list);
            Assert.IsTrue (spec.At (0).Nullable);
            var data = Google.Protobuf.ByteString.CopyFrom (0x0a, 0x03, 0x01, 0x01, 0x61, 0x0a, 0x01, 0x00);
            CollectionAssert.AreEqual (
                new [] { "a", null }, (IList<string>)Encoder.Decode (data, spec, null));
            // A value type element takes its own
            var ints = Described (Schema.KRPC.Type.Types.TypeCode.List);
            ints.Types_.Add (Described (Schema.KRPC.Type.Types.TypeCode.Sint32, true));
            Assert.IsFalse (TypeSpec.FromDescribed (typeof(IList<int>), ints).At (0).Nullable);
            Assert.AreSame (TypeSpec.For (typeof(IList<int>)), TypeSpec.FromDescribed (typeof(IList<int>), ints));
        }
    }
}
