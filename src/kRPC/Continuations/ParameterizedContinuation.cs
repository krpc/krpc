namespace KRPC.Continuations
{
/*[[[cog
import cog
for i in range(int(nargs)):
    cog.outl("""
    public sealed class ParameterizedContinuation<""" + ','.join(['TReturn'] + ['TArg%d' % i for i in range(i+1)]) + """> : Continuation<TReturn> {
        public delegate TReturn Fn (""" + ', '.join(['TArg%d arg%d' % (i,i) for i in range(i+1)]) + """);
        Fn fn; """ + ' '.join(['TArg%d arg%d;' % (i,i) for i in range(i+1)]) + """
        public ParameterizedContinuation (Fn fn, """ + ', '.join(['TArg%d arg%d' % (i,i) for i in range(i+1)]) + ') { ' + \
            'this.fn = fn; ' + ' '.join(['this.arg%d = arg%d;' % (i,i) for i in range(i+1)]) + """ }
        public override TReturn Run () { return fn (""" + ', '.join(['arg%d' % i for i in range(i+1)]) + """); }
    }""")

    cog.outl("""
    public sealed class ParameterizedContinuationVoid<""" + ','.join(['TArg%d' % i for i in range(i+1)]) + """> : Continuation {
        public delegate void Fn (""" + ', '.join(['TArg%d arg%d' % (i,i) for i in range(i+1)]) + """);
        Fn fn; """ + ' '.join(['TArg%d arg%d;' % (i,i) for i in range(i+1)]) + """
        public ParameterizedContinuationVoid (Fn fn, """ + ', '.join(['TArg%d arg%d' % (i,i) for i in range(i+1)]) + ') { ' + \
            'this.fn = fn; ' + ' '.join(['this.arg%d = arg%d;' % (i,i) for i in range(i+1)]) + """ }
        public override void Run () { fn (""" + ', '.join(['arg%d' % i for i in range(i+1)]) + """); }
    }""")
]]]*/

public sealed class ParameterizedContinuation<TReturn,TArg0> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0);
    Fn fn; TArg0 arg0;
    public ParameterizedContinuation (Fn fn, TArg0 arg0) { this.fn = fn; this.arg0 = arg0; }
    public override TReturn Run () { return fn (arg0); }
}

public sealed class ParameterizedContinuationVoid<TArg0> : Continuation {
    public delegate void Fn (TArg0 arg0);
    Fn fn; TArg0 arg0;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0) { this.fn = fn; this.arg0 = arg0; }
    public override void Run () { fn (arg0); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1);
    Fn fn; TArg0 arg0; TArg1 arg1;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; }
    public override TReturn Run () { return fn (arg0, arg1); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1);
    Fn fn; TArg0 arg0; TArg1 arg1;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; }
    public override void Run () { fn (arg0, arg1); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; }
    public override TReturn Run () { return fn (arg0, arg1, arg2); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; }
    public override void Run () { fn (arg0, arg1, arg2); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; }
    public override TReturn Run () { return fn (arg0, arg1, arg2, arg3); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; }
    public override void Run () { fn (arg0, arg1, arg2, arg3); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; }
    public override TReturn Run () { return fn (arg0, arg1, arg2, arg3, arg4); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; }
    public override void Run () { fn (arg0, arg1, arg2, arg3, arg4); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; }
    public override TReturn Run () { return fn (arg0, arg1, arg2, arg3, arg4, arg5); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; }
    public override void Run () { fn (arg0, arg1, arg2, arg3, arg4, arg5); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5; TArg6 arg6;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; }
    public override TReturn Run () { return fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5; TArg6 arg6;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; }
    public override void Run () { fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5; TArg6 arg6; TArg7 arg7;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; }
    public override TReturn Run () { return fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5; TArg6 arg6; TArg7 arg7;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; }
    public override void Run () { fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7,TArg8> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5; TArg6 arg6; TArg7 arg7; TArg8 arg8;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; this.arg8 = arg8; }
    public override TReturn Run () { return fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7,TArg8> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5; TArg6 arg6; TArg7 arg7; TArg8 arg8;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; this.arg8 = arg8; }
    public override void Run () { fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8); }
}

public sealed class ParameterizedContinuation<TReturn,TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7,TArg8,TArg9> : Continuation<TReturn> {
    public delegate TReturn Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5; TArg6 arg6; TArg7 arg7; TArg8 arg8; TArg9 arg9;
    public ParameterizedContinuation (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; this.arg8 = arg8; this.arg9 = arg9; }
    public override TReturn Run () { return fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9); }
}

public sealed class ParameterizedContinuationVoid<TArg0,TArg1,TArg2,TArg3,TArg4,TArg5,TArg6,TArg7,TArg8,TArg9> : Continuation {
    public delegate void Fn (TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9);
    Fn fn; TArg0 arg0; TArg1 arg1; TArg2 arg2; TArg3 arg3; TArg4 arg4; TArg5 arg5; TArg6 arg6; TArg7 arg7; TArg8 arg8; TArg9 arg9;
    public ParameterizedContinuationVoid (Fn fn, TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9) { this.fn = fn; this.arg0 = arg0; this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; this.arg8 = arg8; this.arg9 = arg9; }
    public override void Run () { fn (arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9); }
}
//[[[end]]]
}