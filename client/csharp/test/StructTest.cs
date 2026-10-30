using System;
using System.Collections.Generic;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class StructTest
    {
        [Test]
        public void ComparisonOfACollectionFieldThrows ()
        {
            // A collection has no ordering, so comparing two structures whose collection
            // fields are different objects throws, as comparing two such tuples does
            var a = new TestStruct (1, "alpha", TestEnum.ValueA, new List<int> { 1 });
            var b = new TestStruct (1, "alpha", TestEnum.ValueA, new List<int> { 2 });
            Assert.Throws<ArgumentException> (() => a.CompareTo (b));
            Assert.Throws<ArgumentException> (
                () => Comparer<Tuple<int, IList<int>>>.Default.Compare (
                    new Tuple<int, IList<int>> (1, new List<int> { 1 }),
                    new Tuple<int, IList<int>> (1, new List<int> { 2 })));
        }
    }
}
