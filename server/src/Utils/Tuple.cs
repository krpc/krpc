/*[[[cog
import cog
import itertools
def wrap(s, x, e): return s + x + e if x != '' else ''
def prepend (s, x): return s + x if x != '' else ''

cog.out("""
namespace KRPC.Utils
{
    /// <summary>
    /// Interface for tuples.
    /// </summary>
    public interface ITuple
    {
    }
""")

for n in range(1,int(nargs)+1):
    cog.out("""
    /// <summary>
    /// A tuple with """ + str(n) + """ elements
    /// </summary>
    public class Tuple<""" + ','.join(['T%d' % (i+1) for i in range(n)]) + """> : ITuple
    {""")
    for i in range(n):
        cog.out("""
        /// <summary>
        /// Get/set the value of element """ + str(i+1) + """
        /// </summary>
        """ + 'public T%d Item%d { get; private set; }' % (i+1,i+1) + '\n')
    cog.out("""
        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (""" + ', '.join(['T%d item%d' % (i+1,i+1) for i in range(n)]) + """)
        {
            """ + '\n            '.join(['Item%d = item%d;' % (i+1,i+1) for i in range(n)]) + """
        }
    }\n""")

cog.out("""
    /// <summary>
    /// Static functions for constructing tuples
    /// </summary>
    public static class Tuple
    {""")
for n in range(1,int(nargs)+1):
    cog.outl("""
        /// <summary>
        /// Construct a tuple with """ + str(n) + """ elements
        /// </summary>
        public static Tuple<""" + ','.join(['T%d' % (i+1) for i in range(n)]) + '> ' + 'Create<' + ','.join(['T%d' % (i+1) for i in range(n)]) + '> (' + ', '.join(['T%d item%d' % (i+1,i+1) for i in range(n)]) + """)
        {
            return new Tuple<""" + ','.join(['T%d' % (i+1) for i in range(n)]) + """> (""" + ', '.join(['item%d' % (i+1) for i in range(n)]) + """);
        }""")
cog.outl('    }')
cog.outl('}')
]]]*/

namespace KRPC.Utils
{
    /// <summary>
    /// Interface for tuples.
    /// </summary>
    public interface ITuple
    {
    }

    /// <summary>
    /// A tuple with 1 elements
    /// </summary>
    public class Tuple<T1> : ITuple
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (T1 item1)
        {
            Item1 = item1;
        }
    }

    /// <summary>
    /// A tuple with 2 elements
    /// </summary>
    public class Tuple<T1,T2> : ITuple
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Get/set the value of element 2
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    /// <summary>
    /// A tuple with 3 elements
    /// </summary>
    public class Tuple<T1,T2,T3> : ITuple
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Get/set the value of element 2
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// Get/set the value of element 3
        /// </summary>
        public T3 Item3 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
    }

    /// <summary>
    /// A tuple with 4 elements
    /// </summary>
    public class Tuple<T1,T2,T3,T4> : ITuple
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Get/set the value of element 2
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// Get/set the value of element 3
        /// </summary>
        public T3 Item3 { get; private set; }

        /// <summary>
        /// Get/set the value of element 4
        /// </summary>
        public T4 Item4 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }
    }

    /// <summary>
    /// A tuple with 5 elements
    /// </summary>
    public class Tuple<T1,T2,T3,T4,T5> : ITuple
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Get/set the value of element 2
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// Get/set the value of element 3
        /// </summary>
        public T3 Item3 { get; private set; }

        /// <summary>
        /// Get/set the value of element 4
        /// </summary>
        public T4 Item4 { get; private set; }

        /// <summary>
        /// Get/set the value of element 5
        /// </summary>
        public T5 Item5 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
        }
    }

    /// <summary>
    /// A tuple with 6 elements
    /// </summary>
    public class Tuple<T1,T2,T3,T4,T5,T6> : ITuple
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Get/set the value of element 2
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// Get/set the value of element 3
        /// </summary>
        public T3 Item3 { get; private set; }

        /// <summary>
        /// Get/set the value of element 4
        /// </summary>
        public T4 Item4 { get; private set; }

        /// <summary>
        /// Get/set the value of element 5
        /// </summary>
        public T5 Item5 { get; private set; }

        /// <summary>
        /// Get/set the value of element 6
        /// </summary>
        public T6 Item6 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
        }
    }

    /// <summary>
    /// A tuple with 7 elements
    /// </summary>
    public class Tuple<T1,T2,T3,T4,T5,T6,T7> : ITuple
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Get/set the value of element 2
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// Get/set the value of element 3
        /// </summary>
        public T3 Item3 { get; private set; }

        /// <summary>
        /// Get/set the value of element 4
        /// </summary>
        public T4 Item4 { get; private set; }

        /// <summary>
        /// Get/set the value of element 5
        /// </summary>
        public T5 Item5 { get; private set; }

        /// <summary>
        /// Get/set the value of element 6
        /// </summary>
        public T6 Item6 { get; private set; }

        /// <summary>
        /// Get/set the value of element 7
        /// </summary>
        public T7 Item7 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
        }
    }

    /// <summary>
    /// A tuple with 8 elements
    /// </summary>
    public class Tuple<T1,T2,T3,T4,T5,T6,T7,T8> : ITuple
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Get/set the value of element 2
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// Get/set the value of element 3
        /// </summary>
        public T3 Item3 { get; private set; }

        /// <summary>
        /// Get/set the value of element 4
        /// </summary>
        public T4 Item4 { get; private set; }

        /// <summary>
        /// Get/set the value of element 5
        /// </summary>
        public T5 Item5 { get; private set; }

        /// <summary>
        /// Get/set the value of element 6
        /// </summary>
        public T6 Item6 { get; private set; }

        /// <summary>
        /// Get/set the value of element 7
        /// </summary>
        public T7 Item7 { get; private set; }

        /// <summary>
        /// Get/set the value of element 8
        /// </summary>
        public T8 Item8 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
            Item8 = item8;
        }
    }

    /// <summary>
    /// Static functions for constructing tuples
    /// </summary>
    public static class Tuple
    {
        /// <summary>
        /// Construct a tuple with 1 elements
        /// </summary>
        public static Tuple<T1> Create<T1> (T1 item1)
        {
            return new Tuple<T1> (item1);
        }

        /// <summary>
        /// Construct a tuple with 2 elements
        /// </summary>
        public static Tuple<T1,T2> Create<T1,T2> (T1 item1, T2 item2)
        {
            return new Tuple<T1,T2> (item1, item2);
        }

        /// <summary>
        /// Construct a tuple with 3 elements
        /// </summary>
        public static Tuple<T1,T2,T3> Create<T1,T2,T3> (T1 item1, T2 item2, T3 item3)
        {
            return new Tuple<T1,T2,T3> (item1, item2, item3);
        }

        /// <summary>
        /// Construct a tuple with 4 elements
        /// </summary>
        public static Tuple<T1,T2,T3,T4> Create<T1,T2,T3,T4> (T1 item1, T2 item2, T3 item3, T4 item4)
        {
            return new Tuple<T1,T2,T3,T4> (item1, item2, item3, item4);
        }

        /// <summary>
        /// Construct a tuple with 5 elements
        /// </summary>
        public static Tuple<T1,T2,T3,T4,T5> Create<T1,T2,T3,T4,T5> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            return new Tuple<T1,T2,T3,T4,T5> (item1, item2, item3, item4, item5);
        }

        /// <summary>
        /// Construct a tuple with 6 elements
        /// </summary>
        public static Tuple<T1,T2,T3,T4,T5,T6> Create<T1,T2,T3,T4,T5,T6> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            return new Tuple<T1,T2,T3,T4,T5,T6> (item1, item2, item3, item4, item5, item6);
        }

        /// <summary>
        /// Construct a tuple with 7 elements
        /// </summary>
        public static Tuple<T1,T2,T3,T4,T5,T6,T7> Create<T1,T2,T3,T4,T5,T6,T7> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        {
            return new Tuple<T1,T2,T3,T4,T5,T6,T7> (item1, item2, item3, item4, item5, item6, item7);
        }

        /// <summary>
        /// Construct a tuple with 8 elements
        /// </summary>
        public static Tuple<T1,T2,T3,T4,T5,T6,T7,T8> Create<T1,T2,T3,T4,T5,T6,T7,T8> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        {
            return new Tuple<T1,T2,T3,T4,T5,T6,T7,T8> (item1, item2, item3, item4, item5, item6, item7, item8);
        }
    }
}
//[[[end]]]
