using System.Collections.Generic;
using System.Linq;

namespace KRPC.Utils
{
    static class IEnumerableExtensions
    {
        public static IEnumerable<T> Duplicates<T> (this IEnumerable<T> enumerable)
        {
            return enumerable
                .GroupBy (x => x)
                .Where (group => group.Count () > 1)
                .Select (group => group.Key);
        }
    }
}
