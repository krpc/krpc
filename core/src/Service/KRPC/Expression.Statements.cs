using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using KRPC.Service.Attributes;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    public partial class Expression
    {
        /// <summary>
        /// Replace return markers in a function body with jumps to a label at
        /// the end of the body. The result spec is nullable wherever the body's value or
        /// a returned value is.
        /// </summary>
        static LinqExpression BindReturns (Expression bodyExpression, out TypeSpec resultSpec)
        {
            LinqExpression body = bodyExpression;
            var returns = new ReturnFinder ().Find (body);
            // The common type of the returned values and the body's value. A body ending
            // with a statement, such as an if and else that both return, has no value
            var resultTypes = returns.Select (x => x.Key).ToList ();
            if (body.Type != typeof (void))
                resultTypes.Add (body.Type);
            var type = resultTypes.Count == 0
                ? typeof (void) : CommonType (resultTypes, "values a function returns");
            resultSpec = null;
            if (type != typeof (void)) {
                var specs = returns.Select (x => x.Value).ToList ();
                if (body.Type != typeof (void))
                    specs.Add (bodyExpression.Spec);
                foreach (var returned in specs) {
                    var returnedSpec = TypeSpec.Create (type, returned);
                    resultSpec = resultSpec == null ? returnedSpec : TypeSpec.Join (resultSpec, returnedSpec);
                }
            }
            if (type == typeof (void)) {
                var target = LinqExpression.Label ();
                var bound = new MarkerRewriter (null, null, target).Visit (body);
                return LinqExpression.Block (bound, LinqExpression.Label (target));
            } else {
                var target = LinqExpression.Label (type);
                var bound = new MarkerRewriter (null, null, target).Visit (body);
                if (bound.Type != typeof (void))
                    return LinqExpression.Label (target, Widen (bound, type));
                // Reaching the end of a body ending with a statement is an error
                var error = LinqExpression.New (
                    typeof (InvalidOperationException).GetConstructor (new [] { typeof (string) }),
                    LinqExpression.Constant ("The function ended without returning a value"));
                return LinqExpression.Label (
                    target, LinqExpression.Block (bound, LinqExpression.Throw (error, type)));
            }
        }

        /// <summary>
        /// Finds the type and spec of each return marker with a value in a function body,
        /// leaving out nested functions.
        /// </summary>
        sealed class ReturnFinder : BoundedVisitor
        {
            readonly List<KeyValuePair<System.Type, TypeSpec>> found =
                new List<KeyValuePair<System.Type, TypeSpec>> ();

            public IList<KeyValuePair<System.Type, TypeSpec>> Find (LinqExpression body)
            {
                Visit (body);
                return found;
            }

            protected override LinqExpression VisitLambda<T> (Expression<T> node)
            {
                return node;
            }

            protected override LinqExpression VisitMethodCall (MethodCallExpression node)
            {
                var method = node.Method;
                if (method.DeclaringType == typeof (Expression) &&
                    method.IsGenericMethod && method.Name == nameof (ReturnMarker))
                    found.Add (new KeyValuePair<System.Type, TypeSpec> (
                        method.GetGenericArguments () [0],
                        (TypeSpec)((ConstantExpression)node.Arguments [1]).Value));
                return base.VisitMethodCall (node);
            }
        }

        /// <summary>
        /// A local variable, declared by <see cref="BlockWithVariables"/>.
        /// </summary>
        /// <returns>A local variable.</returns>
        /// <param name="name">The name of the variable.</param>
        /// <param name="type">The type of the variable.</param>
        [KRPCMethod]
        public static Expression Variable (string name, Type type)
        {
            if (ReferenceEquals (name, null))
                throw new ArgumentNullException (nameof (name));
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            return new Expression (LinqExpression.Variable (type.InternalType, name), type.Spec);
        }

        /// <summary>
        /// A variable that keeps its value between evaluations of a function.
        /// </summary>
        /// <remarks>
        /// The initial value is evaluated once, when the variable is created, and gives the
        /// variable its type. The value belongs to the variable, so every function that
        /// contains it shares it.
        /// </remarks>
        /// <returns>A state variable.</returns>
        /// <param name="name">The name of the variable.</param>
        /// <param name="initialValue">The initial value of the variable.</param>
        [KRPCMethod]
        public static Expression StateVariable (string name, Expression initialValue)
        {
            if (ReferenceEquals (name, null))
                throw new ArgumentNullException (nameof (name));
            if (ReferenceEquals (initialValue, null))
                throw new ArgumentNullException (nameof (initialValue));
            if (initialValue.Type == typeof (void))
                throw new ArgumentException ("The initial value of a state variable has no value");
            if (typeof (Delegate).IsAssignableFrom (initialValue.Type))
                throw new ArgumentException ("The initial value of a state variable cannot be a function");
            // A lazily evaluated collection would be evaluated again on every read
            if (IsALazySequence (initialValue.Type))
                throw new ArgumentException (
                    "The initial value of a state variable cannot be a lazily evaluated " +
                    "collection. Use ToList or ToSet to convert it.");
            initialValue.Check ();
            var box = (IStateBox)Activator.CreateInstance (
                typeof (StateBox<>).MakeGenericType (initialValue.Type),
                name, initialValue.Evaluate ());
            return new Expression (
                LinqExpression.Field (LinqExpression.Constant (box), "Value"), initialValue.Spec);
        }

        /// <summary>
        /// The storage of a state variable.
        /// </summary>
        internal interface IStateBox
        {
            string Name { get; }
        }

        sealed class StateBox<T> : System.Runtime.CompilerServices.StrongBox<T>, IStateBox
        {
            public StateBox (string name, T value) : base (value)
            {
                Name = name;
            }

            public string Name { get; private set; }
        }

        /// <summary>
        /// The state variable an expression stands for, or null.
        /// </summary>
        internal static IStateBox AsStateVariable (LinqExpression expression)
        {
            var member = expression as MemberExpression;
            var constant = member == null ? null : member.Expression as ConstantExpression;
            return constant == null ? null : constant.Value as IStateBox;
        }

        /// <summary>
        /// An assignment to a local variable, state variable or function parameter.
        /// </summary>
        /// <remarks>
        /// The value's type must be assignable to the variable's type. A numeric value of a
        /// different type is widened, and narrowing it requires a cast.
        /// </remarks>
        /// <param name="variable">The variable to assign to.</param>
        /// <param name="value">The value to assign.</param>
        [KRPCMethod]
        public static Expression Assign (Expression variable, Expression value)
        {
            if (ReferenceEquals (variable, null))
                throw new ArgumentNullException (nameof (variable));
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            var parameter = variable.internalExpression as ParameterExpression;
            var state = AsStateVariable (variable.internalExpression);
            if (parameter == null && state == null)
                throw new ArgumentException ("The assignment target must be a local variable, state variable or function parameter");
            var name = parameter != null ? parameter.Name : state.Name;
            return new Expression (LinqExpression.Assign (
                variable, AssignedValue (variable, value, "The value assigned to " + name)),
                variable.Spec);
        }

        static LinqExpression AssignedValue (Expression variable, Expression value, string position)
        {
            var converted = ConvertTo (value, variable.Spec, position);
            var type = variable.Type;
            if (converted.Type != type && (type.IsValueType || !type.IsAssignableFrom (converted.Type)))
                throw new InvalidOperationException (
                    "No implicit conversion from " + converted.Type + " to " + type);
            return converted;
        }

        /// <summary>
        /// A block of statements, whose value is that of its last statement.
        /// </summary>
        /// <param name="statements">The statements.</param>
        [KRPCMethod]
        public static Expression Block (IList<Expression> statements)
        {
            CheckStatements (statements);
            return new Expression (LinqExpression.Block (
                statements.Select (x => x.internalExpression)), statements.Last ().Spec);
        }

        /// <summary>
        /// A block of statements with local variables.
        /// </summary>
        /// <remarks>
        /// The value of the block is that of its last statement. The variables are in scope
        /// for the statements of the block, including within nested functions.
        /// </remarks>
        /// <param name="variables">The local variables of the block.</param>
        /// <param name="statements">The statements.</param>
        [KRPCMethod]
        public static Expression BlockWithVariables (IList<Expression> variables, IList<Expression> statements)
        {
            if (ReferenceEquals (variables, null))
                throw new ArgumentNullException (nameof (variables));
            CheckStatements (statements);
            var variableNodes = variables.Select (x => AsVariable (
                x, "Expected a variable, created with Variable")).ToArray ();
            var duplicate = variableNodes.GroupBy (x => x).FirstOrDefault (x => x.Count () > 1);
            if (duplicate != null)
                throw new ArgumentException (
                    "The block declares the variable " + duplicate.Key.Name + " more than once");
            return new Expression (LinqExpression.Block (
                variableNodes, statements.Select (x => x.internalExpression)), statements.Last ().Spec);
        }

        static void CheckStatements (IList<Expression> statements)
        {
            if (ReferenceEquals (statements, null))
                throw new ArgumentNullException (nameof (statements));
            if (statements.Count == 0)
                throw new ArgumentException ("A block must contain at least one statement");
            CheckNoNullElements (statements, "statements of a block");
        }

        /// <summary>
        /// An if statement, with no value.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Conditional"/> for an if-then-else that produces a value.
        /// </remarks>
        /// <param name="condition">The boolean condition.</param>
        /// <param name="body">The statement evaluated when the condition is true.</param>
        [KRPCMethod]
        public static Expression IfThen (Expression condition, Expression body)
        {
            return new Expression (LinqExpression.IfThen (
                BooleanCondition (condition), AsStatement (body)));
        }

        /// <summary>
        /// An if-else statement, with no value.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Conditional"/> for an if-then-else that produces a value.
        /// </remarks>
        /// <param name="condition">The boolean condition.</param>
        /// <param name="body">The statement evaluated when the condition is true.</param>
        /// <param name="elseBody">The statement evaluated when the condition is false.</param>
        [KRPCMethod]
        public static Expression IfThenElse (Expression condition, Expression body, Expression elseBody)
        {
            return new Expression (LinqExpression.IfThenElse (
                BooleanCondition (condition), AsStatement (body), AsStatement (elseBody)));
        }

        static LinqExpression BooleanCondition (Expression condition)
        {
            if (ReferenceEquals (condition, null))
                throw new ArgumentNullException (nameof (condition));
            var unwrapped = UnwrapNullable (condition, "The condition");
            if (unwrapped.Type != typeof (bool))
                throw new ArgumentException ("The condition must evaluate to a boolean value");
            return unwrapped;
        }

        /// <summary>
        /// Discard a statement's value, so that differently typed statements can
        /// be used as the branches of an if statement.
        /// </summary>
        static LinqExpression AsStatement (Expression statement)
        {
            if (ReferenceEquals (statement, null))
                throw new ArgumentNullException (nameof (statement));
            var expression = statement.internalExpression;
            if (expression.Type == typeof (void))
                return expression;
            return LinqExpression.Block (typeof (void), expression);
        }

        // Markers for break, continue and return statements. They are replaced with
        // jumps when the enclosing loop or function is created. Expressions are built
        // from the innermost node outwards, so a marker binds to the nearest
        // enclosing construct. Check rejects an unbound marker, so none is evaluated
        internal static void BreakMarker ()
        {
        }

        internal static void ContinueMarker ()
        {
        }

        internal static T ReturnMarker<T> (T value, TypeSpec spec)
        {
            return value;
        }

        internal static void ReturnVoidMarker ()
        {
        }

        /// <summary>
        /// A break statement, which ends the enclosing loop.
        /// </summary>
        [KRPCMethod]
        public static Expression Break ()
        {
            return new Expression (LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (BreakMarker), BindingFlags.Static | BindingFlags.NonPublic)));
        }

        /// <summary>
        /// A continue statement, which skips to the next iteration of the enclosing loop.
        /// </summary>
        [KRPCMethod]
        public static Expression Continue ()
        {
            return new Expression (LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (ContinueMarker), BindingFlags.Static | BindingFlags.NonPublic)));
        }

        /// <summary>
        /// A return statement, which ends the enclosing function with a value.
        /// </summary>
        /// <param name="value">The value to return.</param>
        [KRPCMethod]
        public static Expression Return (Expression value)
        {
            if (ReferenceEquals (value, null))
                throw new ArgumentNullException (nameof (value));
            if (value.Type == typeof (void))
                throw new ArgumentException (
                    "A return statement must be given a value. Use ReturnNothing to " +
                    "return from a function that produces none.");
            var method = typeof (Expression)
                .GetMethod (nameof (ReturnMarker), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod (value.Type);
            // The spec rides along as a constant, for the function to join into its result
            return new Expression (LinqExpression.Call (
                method, value.internalExpression, LinqExpression.Constant (value.Spec)));
        }

        /// <summary>
        /// A return statement, which ends the enclosing function with no value.
        /// </summary>
        [KRPCMethod]
        public static Expression ReturnNothing ()
        {
            return new Expression (LinqExpression.Call (
                typeof (Expression).GetMethod (nameof (ReturnVoidMarker), BindingFlags.Static | BindingFlags.NonPublic)));
        }

        /// <summary>
        /// Replaces break, continue and return markers with jumps to the given labels.
        /// A nested function keeps its markers, which bind to its own loops and body.
        /// </summary>
        sealed class MarkerRewriter : BoundedVisitor
        {
            readonly LabelTarget breakTarget;
            readonly LabelTarget continueTarget;
            readonly LabelTarget returnTarget;

            public MarkerRewriter (LabelTarget breakLabel, LabelTarget continueLabel, LabelTarget returnLabel)
            {
                breakTarget = breakLabel;
                continueTarget = continueLabel;
                returnTarget = returnLabel;
            }

            protected override LinqExpression VisitLambda<T> (Expression<T> node)
            {
                return node;
            }

            protected override LinqExpression VisitMethodCall (MethodCallExpression node)
            {
                var method = node.Method;
                if (method.DeclaringType == typeof (Expression)) {
                    if (breakTarget != null && method.Name == nameof (BreakMarker))
                        return LinqExpression.Break (breakTarget);
                    if (continueTarget != null && method.Name == nameof (ContinueMarker))
                        return LinqExpression.Continue (continueTarget);
                    // The jump keeps the marker's type, so a return can stand where a value is expected
                    if (returnTarget != null && method.IsGenericMethod && method.Name == nameof (ReturnMarker))
                        return LinqExpression.Return (
                            returnTarget, Widen (Visit (node.Arguments [0]), returnTarget.Type), node.Type);
                    if (returnTarget != null && method.Name == nameof (ReturnVoidMarker)) {
                        if (returnTarget.Type != typeof (void))
                            throw new InvalidOperationException (
                                "A return statement must have a value in a function that produces a value");
                        return LinqExpression.Return (returnTarget, typeof (void));
                    }
                }
                return base.VisitMethodCall (node);
            }
        }

        sealed partial class Checker
        {
            // A marker left in the tree has no enclosing loop or function
            protected override LinqExpression VisitMethodCall (MethodCallExpression node)
            {
                var method = node.Method;
                if (method.DeclaringType == typeof (Expression)) {
                    if (method.Name == nameof (BreakMarker))
                        throw new InvalidOperationException ("A break statement must be inside a loop");
                    if (method.Name == nameof (ContinueMarker))
                        throw new InvalidOperationException ("A continue statement must be inside a loop");
                    if (method.Name == nameof (ReturnMarker) || method.Name == nameof (ReturnVoidMarker))
                        throw new InvalidOperationException ("A return statement must be inside a function");
                }
                return base.VisitMethodCall (node);
            }
        }

        /// <summary>
        /// A while loop, which evaluates the body while the condition is true.
        /// </summary>
        /// <remarks>
        /// A <see cref="Break"/> or <see cref="Continue"/> in the body applies to this loop.
        /// </remarks>
        /// <param name="condition">The boolean condition.</param>
        /// <param name="body">The statement evaluated on each iteration.</param>
        [KRPCMethod]
        public static Expression While (Expression condition, Expression body)
        {
            var test = BooleanCondition (condition);
            var breakTarget = LinqExpression.Label ();
            var continueTarget = LinqExpression.Label ();
            var boundBody = new MarkerRewriter (breakTarget, continueTarget, null)
                .Visit (AsStatement (body));
            return new Expression (LinqExpression.Loop (
                LinqExpression.IfThenElse (test, boundBody, LinqExpression.Break (breakTarget)),
                breakTarget, continueTarget));
        }

        /// <summary>
        /// A loop that evaluates the body once for each value of a collection.
        /// </summary>
        /// <remarks>
        /// The loop variable is a local variable declared by an enclosing
        /// <see cref="BlockWithVariables"/>, or a parameter of an enclosing <see cref="Lambda"/>.
        /// A <see cref="Break"/> or <see cref="Continue"/> in the body applies to this loop.
        /// </remarks>
        /// <param name="variable">The loop variable.</param>
        /// <param name="collection">The collection to iterate over.</param>
        /// <param name="body">The statement evaluated on each iteration.</param>
        [KRPCMethod]
        public static Expression ForEach (Expression variable, Expression collection, Expression body)
        {
            collection = NotNullOperand (collection, nameof (collection), "The collection given to ForEach");
            if (ReferenceEquals (variable, null))
                throw new ArgumentNullException (nameof (variable));
            if (!(variable.internalExpression is ParameterExpression))
                throw new ArgumentException ("The loop variable must be a local variable or function parameter");
            var valueType = GetEnumerableValueType (collection);
            var breakTarget = LinqExpression.Label ();
            var continueTarget = LinqExpression.Label ();
            var boundBody = new MarkerRewriter (breakTarget, continueTarget, null)
                .Visit (AsStatement (body));
            var elementSpec = ElementSpec (collection);
            Func<LinqExpression, LinqExpression> element = value => AssignedValue (
                variable,
                new Expression (value, elementSpec == null ? null : TypeSpec.Create (value.Type, elementSpec)),
                "A value of the loop variable");
            var listType = typeof (IList<>).MakeGenericType (valueType);
            if (listType.IsAssignableFrom (collection.Type))
                return new Expression (ForEachIndex (
                    variable, collection, listType, valueType, boundBody, breakTarget, continueTarget, element));
            var enumeratorType = typeof (IEnumerator<>).MakeGenericType (valueType);
            var enumerator = LinqExpression.Variable (enumeratorType, "enumerator");
            var getEnumerator = typeof (IEnumerable<>).MakeGenericType (valueType).GetMethod ("GetEnumerator");
            var moveNext = typeof (IEnumerator).GetMethod ("MoveNext");
            var current = enumeratorType.GetProperty ("Current");
            var loopBody = LinqExpression.Block (
                LinqExpression.Assign (variable, element (LinqExpression.Property (enumerator, current))),
                boundBody);
            var loop = LinqExpression.Loop (
                LinqExpression.IfThenElse (
                    LinqExpression.Call (enumerator, moveNext),
                    loopBody,
                    LinqExpression.Break (breakTarget)),
                breakTarget, continueTarget);
            var dispose = LinqExpression.Call (
                enumerator, typeof (IDisposable).GetMethod ("Dispose"));
            // The enumerator is created outside the try, so the finally only runs
            // once there is one to dispose of
            return new Expression (LinqExpression.Block (
                new [] { enumerator },
                LinqExpression.Assign (
                    enumerator, LinqExpression.Call (collection, getEnumerator)),
                LinqExpression.TryFinally (loop, dispose)));
        }

        // A list is iterated by position, so the loop sees elements added during it
        static LinqExpression ForEachIndex (
            LinqExpression variable, LinqExpression collection, System.Type listType,
            System.Type valueType, LinqExpression body,
            LabelTarget breakTarget, LabelTarget continueTarget,
            Func<LinqExpression, LinqExpression> element)
        {
            var list = LinqExpression.Variable (listType, "list");
            var index = LinqExpression.Variable (typeof (int), "index");
            var count = typeof (ICollection<>).MakeGenericType (valueType).GetProperty ("Count");
            var item = listType.GetProperty ("Item");
            var loop = LinqExpression.Loop (
                LinqExpression.IfThenElse (
                    LinqExpression.LessThan (index, LinqExpression.Property (list, count)),
                    LinqExpression.Block (
                        LinqExpression.Assign (variable, element (LinqExpression.Property (list, item, index))),
                        LinqExpression.PreIncrementAssign (index),
                        body),
                    LinqExpression.Break (breakTarget)),
                breakTarget, continueTarget);
            return LinqExpression.Block (
                typeof (void),
                new [] { list, index },
                LinqExpression.Assign (list, collection.Type == listType
                    ? collection : LinqExpression.Convert (collection, listType)),
                LinqExpression.Assign (index, LinqExpression.Constant (0)),
                loop);
        }
    }
}
