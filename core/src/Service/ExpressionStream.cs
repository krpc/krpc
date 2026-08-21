using System;

namespace KRPC.Service
{
    /// <summary>
    /// A stream that evaluates a server side expression on each update and
    /// streams its value to the client.
    /// </summary>
    sealed class ExpressionStream : Stream
    {
        readonly Func<object> evaluate;

        public ExpressionStream (global::KRPC.Service.KRPC.Expression expression)
        {
            // Check that the type of the value produced can be sent to the client
            expression.GetValidReturnType ();
            evaluate = expression.Evaluator;
        }

        public override bool Equals (Stream other)
        {
            return ReferenceEquals (this, other);
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
                // Evaluating the expression again from the start is the only way to
                // resume it, and that repeats everything it did before the procedure
                // paused, so report the pause rather than retrying
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
