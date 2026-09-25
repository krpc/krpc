using System;

namespace KRPC.Service
{
    /// <summary>
    /// A stream that evaluates a server side function on each update and
    /// streams its value to the client.
    /// </summary>
    sealed class FunctionStream : Stream
    {
        readonly Func<object> evaluate;
        readonly TypeSpec spec;

        public FunctionStream (global::KRPC.Service.KRPC.Expression function)
        {
            // Check that the type of the value produced can be sent to the client
            spec = TypeSpec.Create (function.GetValidReturnType ());
            function.CheckMarkersBound ();
            evaluate = function.Evaluator;
        }

        public override bool Equals (Stream other)
        {
            var stream = other as FunctionStream;
            if (ReferenceEquals (stream, null))
                return false;
            // A function compiles once and keeps the delegate, so two streams over
            // the same function share it
            return ReferenceEquals (evaluate, stream.evaluate);
        }

        public override int GetHashCode ()
        {
            return evaluate.GetHashCode ();
        }

        public override void UpdateInternal ()
        {
            var result = StreamResult.Result;
            bool wasSet = result.HasValue;
            object oldValue = result.Value;

            object value;
            try {
                value = evaluate ();
            } catch (YieldException e) {
                result.Reset ();
                result.Error = Services.Instance.HandleException (
                    new InvalidOperationException (
                        global::KRPC.Service.KRPC.Expression.YieldedMessage, e));
                Changed = true;
                return;
            } catch (System.Exception e) {
                result.Reset ();
                result.Error = Services.Instance.HandleException (e);
                Changed = true;
                return;
            }

            result.Reset ();
            result.Spec = spec;
            result.Value = value;
            if (!wasSet)
                Changed = true;
            else if (!ReferenceEquals (value, null))
                Changed |= !ValueUtils.Equal (value, oldValue);
            else
                Changed |= !ReferenceEquals (oldValue, null);
        }
    }
}
