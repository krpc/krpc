using System;
using System.Collections;

namespace KRPC.Service
{
    static class ValueUtils
    {
        public static bool Equal(object x, object y) {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return ReferenceEquals(x, y);
            var type = x.GetType();
            if (y.GetType() != type)
                throw new ArgumentException("Types of x and y do not match.");
            if (TypeUtils.IsAListCollectionType(type))
                return ListsEqual((IList)x, (IList)y);
            if (TypeUtils.IsASetCollectionType(type))
                return SetsEqual((IEnumerable)x, (IEnumerable)y);
            if (TypeUtils.IsADictionaryCollectionType(type))
                return DictionariesEqual((IDictionary)x, (IDictionary)y);
            if (TypeUtils.IsAStructType(type))
                return StructsEqual(type, x, y);
            return x.Equals(y);
        }

        /// <summary>
        /// Compare two structure values field by field. The default equality of a C# struct
        /// compares its fields with their own equality, which for a collection-typed field is
        /// reference equality, so a structure holding one has to be compared here instead.
        /// </summary>
        static bool StructsEqual(Type type, object x, object y) {
            foreach (var field in TypeUtils.GetStructFields(type)) {
                var getter = field.GetGetMethod();
                if (!Equal(getter.Invoke(x, null), getter.Invoke(y, null)))
                    return false;
            }
            return true;
        }

        static bool ListsEqual(IList x, IList y) {
            var sizeX = x.Count;
            if (sizeX != y.Count)
                return false;
            for (var i = 0; i < sizeX; i++) {
                if (!Equal(x[i], y[i]))
                    return false;
            }
            return true;
        }

        static bool SetsEqual(IEnumerable x, IEnumerable y) {
            var enumX = x.GetEnumerator();
            var enumY = y.GetEnumerator();
            var sizeX = 0;
            var sizeY = 0;
            while (enumX.MoveNext())
                sizeX++;
            while (enumY.MoveNext())
                sizeY++;
            if (sizeX != sizeY)
                return false;
            enumX.Reset();
            while (enumX.MoveNext()) {
                bool found = false;
                enumY.Reset();
                while (enumY.MoveNext()) {
                    if (Equal(enumX.Current, enumY.Current)) {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }

        static bool DictionariesEqual(IDictionary x, IDictionary y) {
            if (x.Count != y.Count)
                return false;
            foreach (var key in x.Keys) {
                if (!y.Contains(key))
                    return false;
                if (!Equal(x[key], y[key]))
                    return false;
            }
            return true;
        }
    }
}
