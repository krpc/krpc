/*[[[cog
import cog
def wrap(s, x, e): return s + x + e if x != '' else ''
def prepend (s, x): return s + x if x != '' else ''

cog.out("""
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Continuations
{
""")

for n in range(int(nargs)+1):
    cog.outl("""
    /// <summary>
    /// A continuation wrapping a function that takes """ + str(n) + """ arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<""" + ','.join(['TReturn'] + ['TArg%d' % i for i in range(n)]) + """> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (""" + ', '.join(['TArg%d arg%d' % (i,i) for i in range(n)]) + """);

        readonly Fn fn;""" + ''.join(['\n        readonly TArg%d arg%d;' % (i,i) for i in range(n)]) + """

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn""" + prepend(', ', ', '.join(['TArg%d arg%d' % (i,i) for i in range(n)])) + """)
        {
            this.fn = fn;""" + ''.join(['\n            this.arg%d = arg%d;' % (i,i) for i in range(n)]) + """
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (""" + ', '.join(['arg%d' % i for i in range(n)]) + """);
        }
    }""")

    cog.outl("""
    /// <summary>
    /// A continuation wrapping a function that takes """ + str(n) + """ arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid""" + wrap('<', ','.join(['TArg%d' % i for i in range(n)]), '>') + """ : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (""" + ', '.join(['TArg%d arg%d' % (i,i) for i in range(n)]) + """);

        readonly Fn fn;""" + ''.join(['\n        readonly TArg%d arg%d;' % (i,i) for i in range(n)]) + """

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn""" + prepend(', ', ', '.join(['TArg%d arg%d' % (i,i) for i in range(n)])) + """)
        {
            this.fn = fn;""" + ''.join(['\n            this.arg%d = arg%d;' % (i,i) for i in range(n)]) + """
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (""" + ', '.join(['arg%d' % i for i in range(n)]) + """);
        }
    }""")
cog.outl('}')
]]]*/

using System.Diagnostics.CodeAnalysis;

namespace KRPC.Continuations
{

    /// <summary>
    /// A continuation wrapping a function that takes 0 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn ();

        readonly Fn fn;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn)
        {
            this.fn = fn;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn ();
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 0 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn ();

        readonly Fn fn;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn)
        {
            this.fn = fn;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn ();
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 1 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0);

        readonly Fn fn;
        readonly TArg0 arg0;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0)
        {
            this.fn = fn;
            this.arg0 = arg0;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 1 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0);

        readonly Fn fn;
        readonly TArg0 arg0;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0)
        {
            this.fn = fn;
            this.arg0 = arg0;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 2 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 2 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 3 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1, arg2);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 3 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1, arg2);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 4 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1, arg2, arg3);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 4 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1, arg2, arg3);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 5 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1, arg2, arg3, arg4);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 5 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1, arg2, arg3, arg4);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 6 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1, arg2, arg3, arg4, arg5);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 6 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1, arg2, arg3, arg4, arg5);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 7 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;
        readonly TArg6 arg6;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
            this.arg6 = arg6;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 7 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;
        readonly TArg6 arg6;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
            this.arg6 = arg6;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 8 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;
        readonly TArg6 arg6;
        readonly TArg7 arg7;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
            this.arg6 = arg6;
            this.arg7 = arg7;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 8 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;
        readonly TArg6 arg6;
        readonly TArg7 arg7;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
            this.arg6 = arg6;
            this.arg7 = arg7;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 9 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7,TArg8> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;
        readonly TArg6 arg6;
        readonly TArg7 arg7;
        readonly TArg8 arg8;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
            this.arg6 = arg6;
            this.arg7 = arg7;
            this.arg8 = arg8;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 9 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7,TArg8> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;
        readonly TArg6 arg6;
        readonly TArg7 arg7;
        readonly TArg8 arg8;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
            this.arg6 = arg6;
            this.arg7 = arg7;
            this.arg8 = arg8;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 10 arguments and returns a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7,TArg8,TArg9> : Continuation<TReturn>
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;
        readonly TArg6 arg6;
        readonly TArg7 arg7;
        readonly TArg8 arg8;
        readonly TArg9 arg9;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
            this.arg6 = arg6;
            this.arg7 = arg7;
            this.arg8 = arg8;
            this.arg9 = arg9;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments, and return the result.
        /// </summary>
        public override TReturn Run ()
        {
            return fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
    }

    /// <summary>
    /// A continuation wrapping a function that takes 10 arguments, but does not return a result.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
    public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7,TArg8,TArg9> : Continuation
    {
        /// <summary>
        /// Delegate used to invoke the continuation.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidVisibleNestedTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidDeclaringCustomDelegatesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "AvoidExcessiveParametersOnGenericTypesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9);

        readonly Fn fn;
        readonly TArg0 arg0;
        readonly TArg1 arg1;
        readonly TArg2 arg2;
        readonly TArg3 arg3;
        readonly TArg4 arg4;
        readonly TArg5 arg5;
        readonly TArg6 arg6;
        readonly TArg7 arg7;
        readonly TArg8 arg8;
        readonly TArg9 arg9;

        /// <summary>
        /// Create a continuation from a delegate and its arguments.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
        {
            this.fn = fn;
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
            this.arg4 = arg4;
            this.arg5 = arg5;
            this.arg6 = arg6;
            this.arg7 = arg7;
            this.arg8 = arg8;
            this.arg9 = arg9;
        }

        /// <summary>
        /// Invoke the continuation delegate with the stored arguments.
        /// </summary>
        public override void Run ()
        {
            fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
    }
}
//[[[end]]]
