/*[[[cog
import cog
import itertools

cog.out("""
using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    /// <summary>
    /// Interface for tuples.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design", "AvoidEmptyInterfaceRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidTypeInterfaceInconsistencyRule")]
    public interface ITuple
    {
    }
""")

for n in range(1,int(nargs)+1):
    template_params = ','.join(['T%d' % (i+1) for i in range(n)])
    cog.out("""
    /// <summary>
    /// A tuple with """ + str(n) + """ elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<""" + template_params + """> : ITuple, IEquatable<Tuple<""" + template_params + """>>
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public Tuple (""" + ', '.join(['T%d item%d' % (i+1,i+1) for i in range(n)]) + """)
        {
            """ + '\n            '.join(['Item%d = item%d;' % (i+1,i+1) for i in range(n)]) + """
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<""" + template_params + """>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<""" + template_params + """> other)
        {
            return
            !ReferenceEquals (other, null) && """ + ' &&\n            '.join(['Item%d.Equals (other.Item%d)' % (i+1,i+1) for i in range(n)]) + """;
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            """ + ' ^\n            '.join(['Item%d.GetHashCode ()' % (i+1) for i in range(n)]) + """;
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

using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    /// <summary>
    /// Interface for tuples.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design", "AvoidEmptyInterfaceRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidTypeInterfaceInconsistencyRule")]
    public interface ITuple
    {
    }

    /// <summary>
    /// A tuple with 1 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1> : ITuple, IEquatable<Tuple<T1>>
    {
        /// <summary>
        /// Get/set the value of element 1
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public Tuple (T1 item1)
        {
            Item1 = item1;
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode ();
        }
    }

    /// <summary>
    /// A tuple with 2 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1,T2> : ITuple, IEquatable<Tuple<T1,T2>>
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public Tuple (T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1,T2>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1,T2> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1) &&
            Item2.Equals (other.Item2);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode () ^
            Item2.GetHashCode ();
        }
    }

    /// <summary>
    /// A tuple with 3 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1,T2,T3> : ITuple, IEquatable<Tuple<T1,T2,T3>>
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public Tuple (T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1,T2,T3>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1,T2,T3> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1) &&
            Item2.Equals (other.Item2) &&
            Item3.Equals (other.Item3);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode () ^
            Item2.GetHashCode () ^
            Item3.GetHashCode ();
        }
    }

    /// <summary>
    /// A tuple with 4 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1,T2,T3,T4> : ITuple, IEquatable<Tuple<T1,T2,T3,T4>>
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1,T2,T3,T4>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1,T2,T3,T4> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1) &&
            Item2.Equals (other.Item2) &&
            Item3.Equals (other.Item3) &&
            Item4.Equals (other.Item4);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode () ^
            Item2.GetHashCode () ^
            Item3.GetHashCode () ^
            Item4.GetHashCode ();
        }
    }

    /// <summary>
    /// A tuple with 5 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1,T2,T3,T4,T5> : ITuple, IEquatable<Tuple<T1,T2,T3,T4,T5>>
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1,T2,T3,T4,T5>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1,T2,T3,T4,T5> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1) &&
            Item2.Equals (other.Item2) &&
            Item3.Equals (other.Item3) &&
            Item4.Equals (other.Item4) &&
            Item5.Equals (other.Item5);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode () ^
            Item2.GetHashCode () ^
            Item3.GetHashCode () ^
            Item4.GetHashCode () ^
            Item5.GetHashCode ();
        }
    }

    /// <summary>
    /// A tuple with 6 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1,T2,T3,T4,T5,T6> : ITuple, IEquatable<Tuple<T1,T2,T3,T4,T5,T6>>
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1,T2,T3,T4,T5,T6>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1,T2,T3,T4,T5,T6> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1) &&
            Item2.Equals (other.Item2) &&
            Item3.Equals (other.Item3) &&
            Item4.Equals (other.Item4) &&
            Item5.Equals (other.Item5) &&
            Item6.Equals (other.Item6);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode () ^
            Item2.GetHashCode () ^
            Item3.GetHashCode () ^
            Item4.GetHashCode () ^
            Item5.GetHashCode () ^
            Item6.GetHashCode ();
        }
    }

    /// <summary>
    /// A tuple with 7 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1,T2,T3,T4,T5,T6,T7> : ITuple, IEquatable<Tuple<T1,T2,T3,T4,T5,T6,T7>>
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
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

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1,T2,T3,T4,T5,T6,T7>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1,T2,T3,T4,T5,T6,T7> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1) &&
            Item2.Equals (other.Item2) &&
            Item3.Equals (other.Item3) &&
            Item4.Equals (other.Item4) &&
            Item5.Equals (other.Item5) &&
            Item6.Equals (other.Item6) &&
            Item7.Equals (other.Item7);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode () ^
            Item2.GetHashCode () ^
            Item3.GetHashCode () ^
            Item4.GetHashCode () ^
            Item5.GetHashCode () ^
            Item6.GetHashCode () ^
            Item7.GetHashCode ();
        }
    }

    /// <summary>
    /// A tuple with 8 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1,T2,T3,T4,T5,T6,T7,T8> : ITuple, IEquatable<Tuple<T1,T2,T3,T4,T5,T6,T7,T8>>
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
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
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

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1,T2,T3,T4,T5,T6,T7,T8>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1,T2,T3,T4,T5,T6,T7,T8> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1) &&
            Item2.Equals (other.Item2) &&
            Item3.Equals (other.Item3) &&
            Item4.Equals (other.Item4) &&
            Item5.Equals (other.Item5) &&
            Item6.Equals (other.Item6) &&
            Item7.Equals (other.Item7) &&
            Item8.Equals (other.Item8);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode () ^
            Item2.GetHashCode () ^
            Item3.GetHashCode () ^
            Item4.GetHashCode () ^
            Item5.GetHashCode () ^
            Item6.GetHashCode () ^
            Item7.GetHashCode () ^
            Item8.GetHashCode ();
        }
    }

    /// <summary>
    /// A tuple with 9 elements
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    public class Tuple<T1,T2,T3,T4,T5,T6,T7,T8,T9> : ITuple, IEquatable<Tuple<T1,T2,T3,T4,T5,T6,T7,T8,T9>>
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
        /// Get/set the value of element 9
        /// </summary>
        public T9 Item9 { get; private set; }

        /// <summary>
        /// Create a tuple with the given values as its elements
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public Tuple (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
            Item8 = item8;
            Item9 = item9;
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public override bool Equals (object obj)
        {
            return !ReferenceEquals (obj, null) && Equals (obj as Tuple<T1,T2,T3,T4,T5,T6,T7,T8,T9>);
        }

        /// <summary>
        /// Compare two tuples for equality
        /// </summary>
        public bool Equals (Tuple<T1,T2,T3,T4,T5,T6,T7,T8,T9> other)
        {
            return
            !ReferenceEquals (other, null) && Item1.Equals (other.Item1) &&
            Item2.Equals (other.Item2) &&
            Item3.Equals (other.Item3) &&
            Item4.Equals (other.Item4) &&
            Item5.Equals (other.Item5) &&
            Item6.Equals (other.Item6) &&
            Item7.Equals (other.Item7) &&
            Item8.Equals (other.Item8) &&
            Item9.Equals (other.Item9);
        }

        /// <summary>
        /// Hash the tuple
        /// </summary>
        public override int GetHashCode ()
        {
            return
            Item1.GetHashCode () ^
            Item2.GetHashCode () ^
            Item3.GetHashCode () ^
            Item4.GetHashCode () ^
            Item5.GetHashCode () ^
            Item6.GetHashCode () ^
            Item7.GetHashCode () ^
            Item8.GetHashCode () ^
            Item9.GetHashCode ();
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

        /// <summary>
        /// Construct a tuple with 9 elements
        /// </summary>
        public static Tuple<T1,T2,T3,T4,T5,T6,T7,T8,T9> Create<T1,T2,T3,T4,T5,T6,T7,T8,T9> (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        {
            return new Tuple<T1,T2,T3,T4,T5,T6,T7,T8,T9> (item1, item2, item3, item4, item5, item6, item7, item8, item9);
        }
    }
}
//[[[end]]]
