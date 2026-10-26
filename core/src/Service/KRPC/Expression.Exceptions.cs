using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KRPC.Service.Attributes;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    public partial class Expression
    {
        static IList<System.Type> ExceptionTypes (string service, string name)
        {
            if (ReferenceEquals (service, null))
                throw new ArgumentNullException (nameof (service));
            if (ReferenceEquals (name, null))
                throw new ArgumentNullException (nameof (name));
            return Services.Instance.GetExceptionTypes (service, name);
        }

        /// <summary>
        /// A throw statement, which raises the named exception.
        /// </summary>
        /// <remarks>
        /// The client receives the exception as an error from the function, or from the stream
        /// or event evaluating it. For example, the service <c>KRPC</c> and the name
        /// <c>Error</c> raise <see cref="Error"/>. Throws an exception if the service does not
        /// declare it.
        /// </remarks>
        /// <param name="service">The name of the service the exception is defined in.</param>
        /// <param name="name">The name of the exception.</param>
        /// <param name="message">The message of the exception.</param>
        [KRPCMethod]
        public static Expression Throw (string service, string name, Expression message)
        {
            var value = StringOperand (message, nameof (message), "The third argument of Throw");
            var type = ExceptionTypes (service, name) [0];
            var constructor = type.GetConstructor (new [] { typeof (string) });
            if (constructor == null)
                throw new InvalidOperationException (
                    "Exception \"" + name + "\" in service \"" + service + "\" " +
                    "cannot be raised, as it cannot be constructed from a message");
            return new Expression (LinqExpression.Throw (LinqExpression.New (constructor, value)));
        }

        /// <summary>
        /// A try-catch statement that handles the named exception.
        /// </summary>
        /// <remarks>
        /// Only an exception a service declares can be named, and <see cref="TryCatchAll"/>
        /// handles any exception. Throws an exception if the service does not declare it.
        /// </remarks>
        /// <param name="body">The statement to evaluate.</param>
        /// <param name="service">The name of the service the exception is defined in.</param>
        /// <param name="name">The name of the exception.</param>
        /// <param name="message">A string variable assigned the exception's message, or null.</param>
        /// <param name="handler">The statement evaluated when the exception is caught.</param>
        [KRPCMethod]
        public static Expression TryCatch (Expression body, string service, string name, [KRPCNullable] Expression message, Expression handler)
        {
            var types = ExceptionTypes (service, name);
            return new Expression (LinqExpression.TryCatch (
                AsStatement (body),
                types.Select (x => BuildCatchBlock (
                    x, types [0], message, "The fourth argument of TryCatch", handler)).ToArray ()));
        }

        /// <summary>
        /// A try-catch statement that handles any exception.
        /// </summary>
        /// <param name="body">The statement to evaluate.</param>
        /// <param name="message">A string variable assigned the exception's message, or null.</param>
        /// <param name="handler">The statement evaluated when an exception is caught.</param>
        [KRPCMethod]
        public static Expression TryCatchAll (Expression body, [KRPCNullable] Expression message, Expression handler)
        {
            // Rethrow the YieldException of a procedure that pauses execution, so the pause is
            // still reported
            var yielded = LinqExpression.Catch (
                LinqExpression.Parameter (typeof (YieldException), "yielded"),
                LinqExpression.Rethrow (typeof (void)));
            return new Expression (LinqExpression.TryCatch (
                AsStatement (body),
                yielded,
                BuildCatchBlock (
                    typeof (System.Exception), null, message, "The second argument of TryCatchAll", handler)));
        }

        /// <summary>
        /// A try-finally statement.
        /// </summary>
        /// <remarks>
        /// The finalizer is evaluated after the body, whether or not the body raises an
        /// exception. A break, continue or return cannot leave the finalizer.
        /// </remarks>
        /// <param name="body">The statement to evaluate.</param>
        /// <param name="finalizer">The statement evaluated after the body.</param>
        [KRPCMethod]
        public static Expression TryFinally (Expression body, Expression finalizer)
        {
            return new Expression (LinqExpression.TryFinally (
                AsStatement (body), AsStatement (finalizer)));
        }

        sealed partial class Checker
        {
            sealed class FinallyBlock
            {
                public readonly HashSet<LabelTarget> Labels = new HashSet<LabelTarget> ();
                public readonly List<LabelTarget> Jumps = new List<LabelTarget> ();
            }

            // The finally blocks being visited, innermost first
            readonly Stack<FinallyBlock> finallyBlocks = new Stack<FinallyBlock> ();

            protected override CatchBlock VisitCatchBlock (CatchBlock node)
            {
                var variables = node.Variable == null
                    ? new ParameterExpression [0] : new [] { node.Variable };
                Declare (variables, () => {
                    Visit (node.Filter);
                    Visit (node.Body);
                });
                return node;
            }

            // A jump from a finally block must land inside it
            protected override LinqExpression VisitTry (TryExpression node)
            {
                if (node.Finally == null)
                    return base.VisitTry (node);
                Visit (node.Body);
                Visit (node.Handlers, VisitCatchBlock);
                Visit (node.Fault);
                finallyBlocks.Push (new FinallyBlock ());
                Visit (node.Finally);
                var block = finallyBlocks.Pop ();
                if (block.Jumps.Any (x => !block.Labels.Contains (x)))
                    throw new InvalidOperationException (
                        "A break, continue or return cannot leave a finally block");
                if (finallyBlocks.Count > 0)
                    finallyBlocks.Peek ().Labels.UnionWith (block.Labels);
                return node;
            }

            protected override LinqExpression VisitLabel (LabelExpression node)
            {
                if (finallyBlocks.Count > 0)
                    finallyBlocks.Peek ().Labels.Add (node.Target);
                return base.VisitLabel (node);
            }

            protected override LinqExpression VisitLoop (LoopExpression node)
            {
                if (finallyBlocks.Count > 0) {
                    var labels = finallyBlocks.Peek ().Labels;
                    if (node.BreakLabel != null)
                        labels.Add (node.BreakLabel);
                    if (node.ContinueLabel != null)
                        labels.Add (node.ContinueLabel);
                }
                return base.VisitLoop (node);
            }

            protected override LinqExpression VisitGoto (GotoExpression node)
            {
                if (finallyBlocks.Count > 0)
                    finallyBlocks.Peek ().Jumps.Add (node.Target);
                return base.VisitGoto (node);
            }
        }

        /// <summary>
        /// A catch block that assigns the exception's message to a variable and evaluates
        /// the handler.
        /// </summary>
        /// <param name="type">The CLR exception type to catch.</param>
        /// <param name="name">The kRPC exception type the catch names, or null for any exception.</param>
        /// <param name="message">A string variable to assign the message to, or null.</param>
        /// <param name="position">The argument the message variable was given as, for errors.</param>
        /// <param name="handler">The statement to evaluate.</param>
        static CatchBlock BuildCatchBlock (System.Type type, System.Type name, Expression message, string position, Expression handler)
        {
            var body = AsStatement (handler);
            if (ReferenceEquals (message, null) && name == null)
                return LinqExpression.Catch (type, body);
            if (!ReferenceEquals (message, null)) {
                if (!(message.internalExpression is ParameterExpression))
                    throw new ArgumentException (position + " must be a variable or parameter");
                if (message.Type != typeof (string))
                    throw new InvalidOperationException (position + " is not a string");
            }
            var caught = LinqExpression.Parameter (type, "caught");
            var statements = new List<LinqExpression> ();
            // A subclass that reaches the client under another name is rethrown
            if (name != null)
                statements.Add (LinqExpression.IfThen (
                    LinqExpression.Not (LinqExpression.Call (
                        typeof (Services).GetMethod (nameof (Services.ExpressionExceptionIsNamed)),
                        caught, LinqExpression.Constant (name, typeof (System.Type)))),
                    LinqExpression.Rethrow (typeof (void))));
            if (!ReferenceEquals (message, null))
                statements.Add (LinqExpression.Assign (
                    message, LinqExpression.Property (
                        caught, typeof (System.Exception).GetProperty ("Message"))));
            statements.Add (body);
            return LinqExpression.Catch (
                caught, LinqExpression.Block (typeof (void), statements));
        }
    }
}
