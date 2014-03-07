namespace KRPC.Continuations
{
    /// <summary>
    /// A continuation with a single parameter of type TIn and a return value of
    /// type TOut
    /// </summary>
    public sealed class ParameterizedContinuation<TIn,TOut> : Continuation<TOut>
    {
        public delegate TOut Fn (TIn data);
        Fn fn;
        TIn data;

        public ParameterizedContinuation (Fn fn, TIn data)
        {
            this.fn = fn;
            this.data = data;
        }

        public override TOut Run ()
        {
            return fn (data);
        }
    }


    /// <summary>
    /// A continuation with a single parameter of type TIn and no return value
    /// </summary>
    public sealed class ParameterizedContinuation<TIn> : Continuation
    {
        public delegate void Fn (TIn data);
        Fn fn;
        TIn data;

        public ParameterizedContinuation (Fn fn, TIn data)
        {
            this.fn = fn;
            this.data = data;
        }

        public override void Run ()
        {
            fn (data);
        }
    }
}