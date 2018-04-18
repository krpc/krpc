using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Messages;
using NUnit.Framework;
using Newtonsoft.Json;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ValueUtilsTest
    {
        [TestCase ("foo", "foo")]
        [TestCase (1, 1)]
        [TestCase (true, true)]
        public void Equal(object x, object y)
        {
            Assert.IsTrue(ValueUtils.Equal(x, y));
        }

        [TestCase ("foo", "bar")]
        [TestCase (1, 2)]
        [TestCase (true, false)]
        public void NotEqual(object x, object y)
        {
            Assert.IsFalse(ValueUtils.Equal(x, y));
        }

        [TestCase]
        public void TuplesEqual()
        {
            var x = Tuple.Create(1, "foo", false);
            var y = Tuple.Create(1, "foo", false);
            Assert.IsTrue(ValueUtils.Equal(x, y));
        }

        [TestCase]
        public void TuplesNotEqual()
        {
            var x = Tuple.Create(1, "foo", false);
            var y = Tuple.Create(1, "bar", false);
            Assert.IsFalse(ValueUtils.Equal(x, y));
        }

        [TestCase (new int[] {})]
        [TestCase (new int[] { 1 })]
        [TestCase (new int[] { 1, 2, 3 })]
        public void ListsEqual(int[] values)
        {
            var x = new List<int>();
            var y = new List<int>();
            foreach (var v in values) {
                x.Add(v);
                y.Add(v);
            }
            Assert.IsTrue(ValueUtils.Equal(x, y));
        }

        [TestCase (new int[] {}, new int[] { 1 })]
        [TestCase (new int[] { 1 }, new int[] { })]
        [TestCase (new int[] { 1 }, new int[] { 2 })]
        [TestCase (new int[] { 1, 2, 3 }, new int[] { 1, 3, 2 })]
        public void ListsNotEqual(int[] valuesX, int[] valuesY)
        {
            var x = new List<int>();
            var y = new List<int>();
            foreach (var v in valuesX)
                x.Add(v);
            foreach (var v in valuesY)
                y.Add(v);
            Assert.IsFalse(ValueUtils.Equal(x, y));
        }

        [TestCase (new int[] {})]
        [TestCase (new int[] { 1 })]
        [TestCase (new int[] { 1, 2, 3 })]
        public void SetsEqual(int[] values)
        {
            var x = new HashSet<int>();
            var y = new HashSet<int>();
            foreach (var v in values) {
                x.Add(v);
                y.Add(v);
            }
            Assert.IsTrue(ValueUtils.Equal(x, y));
        }

        [TestCase (new int[] {}, new int[] { 1 })]
        [TestCase (new int[] { 1 }, new int[] { })]
        [TestCase (new int[] { 1 }, new int[] { 2 })]
        [TestCase (new int[] { 1, 2, 3 }, new int[] { 1, 4, 2 })]
        public void SetsNotEqual(int[] valuesX, int[] valuesY)
        {
            var x = new HashSet<int>();
            var y = new HashSet<int>();
            foreach (var v in valuesX)
                x.Add(v);
            foreach (var v in valuesY)
                y.Add(v);
            Assert.IsFalse(ValueUtils.Equal(x, y));
        }

        [TestCase (new int[] {}, new string[] {})]
        [TestCase (new int[] { 1 }, new string[] { "foo" })]
        [TestCase (new int[] { 1, 2, 3 }, new string[] { "foo", "bar", "baz" })]
        public void DictionariesEqual(int[] keys, string[] values)
        {
            var x = new Dictionary<int, string>();
            var y = new Dictionary<int, string>();
            for (int i = 0; i < keys.Length; i++) {
                x[keys[i]] = values[i];
                y[keys[i]] = values[i];
            }
            Assert.IsTrue(ValueUtils.Equal(x, y));
        }

        [TestCase (new int[] {}, new string[] {},
                   new int[] { 1 }, new string[] { "foo" })]
        [TestCase (new int[] { 1, 2 }, new string[] { "foo", "bar" },
                   new int[] { 1, 2 }, new string[] { "foo", "baz" })]
        [TestCase (new int[] { 1, 2 }, new string[] { "foo", "bar" },
                   new int[] { 1, 3 }, new string[] { "foo", "bar" })]
        public void DictionariesNotEqual(int[] keysX, string[] valuesX, int[] keysY, string[] valuesY)
        {
            var x = new Dictionary<int, string>();
            var y = new Dictionary<int, string>();
            for (int i = 0; i < keysX.Length; i++)
                x[keysX[i]] = valuesX[i];
            for (int i = 0; i < keysY.Length; i++)
                x[keysY[i]] = valuesY[i];
            Assert.IsFalse(ValueUtils.Equal(x, y));
        }
    }
}
