namespace KRPC.Utils
{
    /*[[[cog
import cog
def wrap(s, x, e): return s + x + e if x != '' else ''
def prepend (s, x): return s + x if x != '' else ''

for n in range(1,int(nargs)+1):
    cog.out("""
        public class Tuple<""" + ','.join(['T%d' % (i+1) for i in range(n)]) + """>
        {
            """ + '\n\n            '.join(['public T%d Item%d { get; private set; }' % (i+1,i+1) for i in range(n)]) + """

            public Tuple (""" + ', '.join(['T%d item%d' % (i+1,i+1) for i in range(n)]) + """)
            {
                """ + '\n                '.join(['Item%d = item%d;' % (i+1,i+1) for i in range(n)]) + """
            }
        }
    """)

cog.outl('\n    public static class Tuple\n    {')
for n in range(1,int(nargs)+1):
    cog.outl("""
        public static Tuple<""" + ','.join(['T%d' % (i+1) for i in range(n)]) + '> ' + 'Create<' + ','.join(['T%d' % (i+1) for i in range(n)]) + '> (' + ', '.join(['T%d item%d' % (i+1,i+1) for i in range(n)]) + """)
        {
            return new Tuple<""" + ','.join(['T%d' % (i+1) for i in range(n)]) + """> (""" + ', '.join(['item%d' % (i+1) for i in range(n)]) + """);
        }""")
cog.outl('    }')
]]]*/

    public class Tuple<T1>
    {
        public T1 Item1 { get; private set; }

        public Tuple (T1 item1)
        {
            Item1 = item1;
        }
    }

    public class Tuple<T1,T2>
    {
        public T1 Item1 { get; private set; }

        public T2 Item2 { get; private set; }

        public Tuple (T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    public class Tuple<T1,T2,T3>
    {
        public T1 Item1 { get; private set; }

        public T2 Item2 { get; private set; }

        public T3 Item3 { get; private set; }

        public Tuple (T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
    }

    public class Tuple<T1,T2,T3,T4>
    {
        public T1 Item1 { get; private set; }

        public T2 Item2 { get; private set; }

        public T3 Item3 { get; private set; }

        public T4 Item4 { get; private set; }

        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }
    }

    public class Tuple<T1,T2,T3,T4,T5>
    {
        public T1 Item1 { get; private set; }

        public T2 Item2 { get; private set; }

        public T3 Item3 { get; private set; }

        public T4 Item4 { get; private set; }

        public T5 Item5 { get; private set; }

        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
        }
    }

    public class Tuple<T1,T2,T3,T4,T5,T6>
    {
        public T1 Item1 { get; private set; }

        public T2 Item2 { get; private set; }

        public T3 Item3 { get; private set; }

        public T4 Item4 { get; private set; }

        public T5 Item5 { get; private set; }

        public T6 Item6 { get; private set; }

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

    public class Tuple<T1,T2,T3,T4,T5,T6,T7>
    {
        public T1 Item1 { get; private set; }

        public T2 Item2 { get; private set; }

        public T3 Item3 { get; private set; }

        public T4 Item4 { get; private set; }

        public T5 Item5 { get; private set; }

        public T6 Item6 { get; private set; }

        public T7 Item7 { get; private set; }

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

    public class Tuple<T1,T2,T3,T4,T5,T6,T7,T8>
    {
        public T1 Item1 { get; private set; }

        public T2 Item2 { get; private set; }

        public T3 Item3 { get; private set; }

        public T4 Item4 { get; private set; }

        public T5 Item5 { get; private set; }

        public T6 Item6 { get; private set; }

        public T7 Item7 { get; private set; }

        public T8 Item8 { get; private set; }

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

public static class Tuple
{

    public static Tuple<T1> Create<T1> (T1 item1)
    {
        return new Tuple<T1> (item1);
    }

    public static Tuple<T1,T2> Create<T1,T2> (T1 item1, T2 item2)
    {
        return new Tuple<T1,T2> (item1, item2);
    }

    public static Tuple<T1,T2,T3> Create<T1,T2,T3> (T1 item1, T2 item2, T3 item3)
    {
        return new Tuple<T1,T2,T3> (item1, item2, item3);
    }

    public static Tuple<T1,T2,T3,T4> Create<T1,T2,T3,T4> (T1 item1, T2 item2, T3 item3, T4 item4)
    {
        return new Tuple<T1,T2,T3,T4> (item1, item2, item3, item4);
    }

    public static Tuple<T1,T2,T3,T4,T5> Create<T1,T2,T3,T4,T5> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
    {
        return new Tuple<T1,T2,T3,T4,T5> (item1, item2, item3, item4, item5);
    }

    public static Tuple<T1,T2,T3,T4,T5,T6> Create<T1,T2,T3,T4,T5,T6> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
    {
        return new Tuple<T1,T2,T3,T4,T5,T6> (item1, item2, item3, item4, item5, item6);
    }

    public static Tuple<T1,T2,T3,T4,T5,T6,T7> Create<T1,T2,T3,T4,T5,T6,T7> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
    {
        return new Tuple<T1,T2,T3,T4,T5,T6,T7> (item1, item2, item3, item4, item5, item6, item7);
    }

    public static Tuple<T1,T2,T3,T4,T5,T6,T7,T8> Create<T1,T2,T3,T4,T5,T6,T7,T8> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
    {
        return new Tuple<T1,T2,T3,T4,T5,T6,T7,T8> (item1, item2, item3, item4, item5, item6, item7, item8);
    }
}
    //[[[end]]]
}
