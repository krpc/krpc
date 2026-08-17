using System.Collections;

namespace KRPC.Client
{
    /// <summary>
    /// Equality and hashing for the values of the fields of a structure. A collection is
    /// compared by its contents, where the equality a collection type provides itself
    /// compares references.
    /// </summary>
    public static class ValueUtils
    {
        /// <summary>
        /// Returns true if the two field values are equal.
        /// </summary>
        public static bool Equal (object x, object y)
        {
            if (ReferenceEquals (x, y))
                return true;
            if (ReferenceEquals (x, null) || ReferenceEquals (y, null))
                return false;
            var dictionary = x as IDictionary;
            if (dictionary != null)
                return DictionariesEqual (dictionary, y as IDictionary);
            var list = x as IList;
            if (list != null)
                return ListsEqual (list, y as IList);
            if (x is IEnumerable && !(x is string))
                return SetsEqual ((IEnumerable)x, y as IEnumerable);
            return x.Equals (y);
        }

        /// <summary>
        /// Returns a hash code for a field value, equal for values that compare equal.
        /// </summary>
        public static int HashCode (object value)
        {
            if (ReferenceEquals (value, null))
                return 0;
            var dictionary = value as IDictionary;
            if (dictionary != null) {
                int hash = 0;
                foreach (DictionaryEntry entry in dictionary)
                    hash ^= HashCode (entry.Key) * 31 + HashCode (entry.Value);
                return hash;
            }
            var list = value as IList;
            if (list != null) {
                int hash = 17;
                foreach (var item in list)
                    hash = hash * 31 + HashCode (item);
                return hash;
            }
            if (value is IEnumerable && !(value is string)) {
                int hash = 0;
                foreach (var item in (IEnumerable)value)
                    hash ^= HashCode (item);
                return hash;
            }
            return value.GetHashCode ();
        }

        static bool ListsEqual (IList x, IList y)
        {
            if (y == null || x.Count != y.Count)
                return false;
            for (int i = 0; i < x.Count; i++) {
                if (!Equal (x [i], y [i]))
                    return false;
            }
            return true;
        }

        static bool SetsEqual (IEnumerable x, IEnumerable y)
        {
            if (y == null)
                return false;
            var itemsY = new ArrayList ();
            foreach (var item in y)
                itemsY.Add (item);
            int sizeX = 0;
            foreach (var itemX in x) {
                sizeX++;
                bool found = false;
                foreach (var itemY in itemsY) {
                    if (Equal (itemX, itemY)) {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return sizeX == itemsY.Count;
        }

        static bool DictionariesEqual (IDictionary x, IDictionary y)
        {
            if (y == null || x.Count != y.Count)
                return false;
            foreach (DictionaryEntry entry in x) {
                if (!y.Contains (entry.Key))
                    return false;
                if (!Equal (entry.Value, y [entry.Key]))
                    return false;
            }
            return true;
        }
    }
}
