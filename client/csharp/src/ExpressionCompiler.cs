using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using KRPC.Client.Attributes;
using ServerExpression = KRPC.Client.Services.KRPC.Expression;
using ServerType = KRPC.Client.Services.KRPC.Type;

namespace KRPC.Client
{
    /// <summary>
    /// Compiles LINQ expression trees into server side expressions.
    /// Sub-expressions that do not interact with the server are evaluated once,
    /// when the expression is compiled, and embedded as constants. Remote
    /// procedure calls become calls embedded in the expression, re-invoked on
    /// each evaluation.
    /// </summary>
    class ExpressionCompiler
    {
        /// <summary>
        /// The result of compiling an expression tree node: either a value known
        /// at compile time, or a server side expression.
        /// </summary>
        sealed class Result
        {
            public object Value { get; private set; }
            public bool IsValue { get; private set; }
            public ServerExpression Expression { get; private set; }

            public static Result FromValue (object value)
            {
                return new Result { Value = value, IsValue = true };
            }

            public static Result FromExpression (ServerExpression expression)
            {
                return new Result { Expression = expression };
            }
        }

        /// <summary>
        /// Detects whether a subtree interacts with the server: a call or member
        /// access backed by an RPC, or a reference to a parameter of a function
        /// being compiled.
        /// </summary>
        sealed class RemoteNodeFinder : ExpressionVisitor
        {
            readonly ICollection<ParameterExpression> bound;
            public bool Found { get; private set; }

            public RemoteNodeFinder (ICollection<ParameterExpression> boundParameters)
            {
                bound = boundParameters;
            }

            protected override Expression VisitMember (MemberExpression node)
            {
                if (GetRPCAttribute (node.Member) != null)
                    Found = true;
                return base.VisitMember (node);
            }

            protected override Expression VisitMethodCall (MethodCallExpression node)
            {
                if (GetRPCAttribute (node.Method) != null)
                    Found = true;
                return base.VisitMethodCall (node);
            }

            protected override Expression VisitParameter (ParameterExpression node)
            {
                if (bound.Contains (node))
                    Found = true;
                return base.VisitParameter (node);
            }
        }

        readonly IConnection connection;
        readonly IDictionary<ParameterExpression, ServerExpression> parameters =
            new Dictionary<ParameterExpression, ServerExpression> ();

        /// <summary>
        /// Whether the subtree can be evaluated on the client, without
        /// interacting with the server.
        /// </summary>
        bool IsClientSide (Expression node)
        {
            var finder = new RemoteNodeFinder (parameters.Keys.ToList ());
            finder.Visit (node);
            return !finder.Found;
        }

        ExpressionCompiler (IConnection serverConnection)
        {
            connection = serverConnection;
        }

        /// <summary>
        /// Compile a lambda expression, taking no arguments, into a server side
        /// expression that computes the same result on the server.
        /// </summary>
        public static ServerExpression Compile (IConnection connection, LambdaExpression expression)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));
            if (expression.Parameters.Count != 0)
                throw new ExpressionCompilationException ("The expression to compile must take no arguments");
            var compiler = new ExpressionCompiler (connection);
            var body = expression.Body;
            var result = compiler.CompileNode (body);
            var serverExpression = compiler.ToExpression (result, body.Type);
            // A lazily evaluated sequence cannot be sent to a client; make it concrete
            if (IsALazySequence (body.Type))
                serverExpression = ServerExpression.ToList (connection, serverExpression);
            return serverExpression;
        }

        static bool IsALazySequence (Type type)
        {
            if (!type.IsGenericType)
                return false;
            var definition = type.GetGenericTypeDefinition ();
            return definition == typeof(IEnumerable<>) || definition == typeof(IOrderedEnumerable<>);
        }

        Result CompileNode (Expression node)
        {
            switch (node) {
            case ConstantExpression constant:
                return Result.FromValue (constant.Value);
            case ParameterExpression parameter:
                if (parameters.TryGetValue (parameter, out var serverParameter))
                    return Result.FromExpression (serverParameter);
                throw Error (node, "unbound parameter '" + parameter.Name + "'");
            case MemberExpression member:
                return CompileMember (member);
            case MethodCallExpression call:
                return CompileCall (call);
            case BinaryExpression binary:
                return CompileBinary (binary);
            case UnaryExpression unary:
                return CompileUnary (unary);
            case ConditionalExpression conditional:
                return CompileConditional (conditional);
            case NewExpression create when create.Type.Name.StartsWith ("Tuple`", StringComparison.Ordinal):
                return CompileTuple (create);
            case NewExpression create when IsAStruct (create.Type):
                return CompileStruct (create);
            case ListInitExpression listInit:
                return CompileListInit (listInit);
            default:
                throw Error (node, "unsupported expression (" + node.NodeType + ")");
            }
        }

        Result CompileMember (MemberExpression node)
        {
            var attribute = GetRPCAttribute (node.Member);
            if (attribute != null) {
                // A property backed by an RPC
                var arguments = new List<Result> ();
                if (ExpressionUtils.IsAClassProperty (node))
                    arguments.Add (CompileNode (node.Expression));
                return CallNode (attribute, arguments);
            }
            if (IsClientSide (node))
                return Fold (node);
            // A field of a structure computed on the server
            if (node.Expression != null && IsAStruct (node.Expression.Type)) {
                var value = CompileNode (node.Expression);
                if (!value.IsValue)
                    return Result.FromExpression (ServerExpression.GetField (
                        connection, value.Expression, node.Member.Name));
            }
            // A member of a value computed on the server
            if (node.Member.Name == "Count" && node.Expression != null) {
                var instance = CompileNode (node.Expression);
                if (!instance.IsValue)
                    return Result.FromExpression (ServerExpression.Count (connection, instance.Expression));
            }
            throw Error (node, "'" + node.Member.Name + "' is not a remote member");
        }

        Result CompileCall (MethodCallExpression node)
        {
            var method = node.Method;
            var attribute = GetRPCAttribute (method);
            if (attribute != null) {
                var arguments = new List<Result> ();
                int position = 0;
                if (ExpressionUtils.IsAClassMethod (node))
                    arguments.Add (CompileNode (node.Object));
                else if (ExpressionUtils.IsAClassStaticMethod (node))
                    position = 1;  // Skip the connection argument
                for (; position < node.Arguments.Count; position++)
                    arguments.Add (CompileNode (node.Arguments [position]));
                return CallNode (attribute, arguments);
            }
            if (method.DeclaringType == typeof(Enumerable))
                return CompileEnumerableCall (node);
            if (method.DeclaringType == typeof(Math) && !IsClientSide (node)) {
                if (method.Name == "Pow")
                    return CompileNumericBinary (
                        node, node.Arguments [0], node.Arguments [1],
                        (a, b) => ServerExpression.Power (connection, a, b));
                if (mathProcedures.ContainsKey (method.Name))
                    return CompileStdLibCall (node, mathProcedures [method.Name]);
            }
            if (method.Name == "ToString" && node.Arguments.Count == 0 &&
                node.Object != null && !IsClientSide (node)) {
                var instance = ToExpression (CompileNode (node.Object), node.Object.Type);
                return Result.FromExpression (
                    ServerExpression.ConvertToString (connection, instance));
            }
            if (method.DeclaringType == typeof(string) && method.Name == "Concat" &&
                !IsClientSide (node))
                return CompileStringConcat (node);
            if (IsClientSide (node))
                return Fold (node);
            if (method.Name == "get_Item" && node.Object != null) {
                var instance = CompileNode (node.Object);
                var index = CompileNode (node.Arguments [0]);
                return Result.FromExpression (ServerExpression.Get (
                    connection,
                    ToExpression (instance, node.Object.Type),
                    ToExpression (index, node.Arguments [0].Type)));
            }
            throw Error (node, "cannot call '" + method.Name + "' with an argument computed on the server");
        }

        static readonly IDictionary<string, string> mathProcedures =
            new Dictionary<string, string> {
                { "Abs", "Abs" },
                { "Sign", "Sign" },
                { "Floor", "Floor" },
                { "Ceiling", "Ceiling" },
                { "Round", "Round" },
                { "Sqrt", "Sqrt" },
                { "Sin", "Sin" },
                { "Cos", "Cos" },
                { "Tan", "Tan" },
                { "Asin", "Asin" },
                { "Acos", "Acos" },
                { "Atan", "Atan" },
                { "Atan2", "Atan2" },
                { "Log", "Log" },
                { "Log10", "Log10" },
                { "Exp", "Exp" },
                { "Min", "Min" },
                { "Max", "Max" }
            };

        /// <summary>
        /// Compile a call of a System.Math method to the equivalent StdLib
        /// procedure. Arguments are converted to doubles, and the result is
        /// converted back to the type the Math method produces.
        /// </summary>
        Result CompileStdLibCall (MethodCallExpression node, string procedure)
        {
            if (node.Arguments.Any (argument => !IsNumeric (argument.Type)))
                throw Error (node, "unsupported argument types for Math." + node.Method.Name);
            var call = new Schema.KRPC.ProcedureCall ();
            call.Service = "StdLib";
            call.Procedure = procedure;
            var arguments = new Dictionary<int, ServerExpression> ();
            foreach (var argument in node.Arguments) {
                var compiled = ToExpression (CompileNode (argument), argument.Type);
                if (argument.Type != typeof(double))
                    compiled = ServerExpression.Cast (
                        connection, compiled, ServerType.Double (connection));
                arguments [arguments.Count] = compiled;
            }
            ServerExpression result = ServerExpression.CallWithArguments (connection, call, arguments);
            if (node.Type != typeof(double))
                result = ServerExpression.Cast (
                    connection, result, RemoteType (node.Type, node));
            return Result.FromExpression (result);
        }

        static bool IsNumeric (Type type)
        {
            return type == typeof(double) || type == typeof(float) ||
                type == typeof(int) || type == typeof(long) ||
                type == typeof(uint) || type == typeof(ulong);
        }

        Result CompileStringConcat (MethodCallExpression node)
        {
            var parts = new List<ServerExpression> ();
            foreach (var argument in node.Arguments)
                parts.Add (StringPart (argument));
            return Result.FromExpression (
                ServerExpression.ConcatStrings (connection, parts));
        }

        /// <summary>
        /// Compile a sub-expression to a string valued server expression,
        /// converting non-string values to their string representation.
        /// </summary>
        ServerExpression StringPart (Expression node)
        {
            var compiled = ToExpression (CompileNode (node), node.Type);
            if (node.Type == typeof(string))
                return compiled;
            return ServerExpression.ConvertToString (connection, compiled);
        }

        Result CompileEnumerableCall (MethodCallExpression node)
        {
            if (IsClientSide (node))
                return Fold (node);
            var name = node.Method.Name;
            var source = node.Arguments [0];
            var sourceExpression = ToExpression (CompileNode (source), source.Type);
            switch (name) {
            case "Select" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Select (
                    connection, sourceExpression, CompileFunction (node.Arguments [1])));
            case "Where" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Where (
                    connection, sourceExpression, CompileFunction (node.Arguments [1])));
            case "Any" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Any (
                    connection, sourceExpression, CompileFunction (node.Arguments [1])));
            case "All" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.All (
                    connection, sourceExpression, CompileFunction (node.Arguments [1])));
            case "OrderBy" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.OrderBy (
                    connection, sourceExpression, CompileFunction (node.Arguments [1])));
            case "Contains" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Contains (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type)));
            case "Count" when node.Arguments.Count == 1:
                if (IsALazySequence (source.Type))
                    sourceExpression = ServerExpression.ToList (connection, sourceExpression);
                return Result.FromExpression (ServerExpression.Count (connection, sourceExpression));
            case "Sum" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.Sum (connection, sourceExpression));
            case "Min" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.Min (connection, sourceExpression));
            case "Max" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.Max (connection, sourceExpression));
            case "Average" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.Average (connection, sourceExpression));
            case "ToList" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.ToList (connection, sourceExpression));
            case "ToHashSet" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.ToSet (connection, sourceExpression));
            case "Concat" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Concat (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type)));
            case "Skip" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Skip (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type)));
            case "Take" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Take (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type)));
            case "SelectMany" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.SelectMany (
                    connection, sourceExpression, CompileFunction (node.Arguments [1])));
            case "ToDictionary" when node.Arguments.Count == 3:
                return Result.FromExpression (ServerExpression.BuildDictionary (
                    connection, sourceExpression,
                    CompileFunction (node.Arguments [1]),
                    CompileFunction (node.Arguments [2])));
            default:
                throw Error (node, "unsupported collection operation '" + name + "'");
            }
        }

        ServerExpression CompileFunction (Expression node)
        {
            if (node is UnaryExpression quote && quote.NodeType == ExpressionType.Quote)
                node = quote.Operand;
            var lambda = node as LambdaExpression;
            if (lambda == null)
                throw Error (node, "expected a lambda expression");
            var functionParameters = new List<ServerExpression> ();
            foreach (var parameter in lambda.Parameters) {
                var serverParameter = ServerExpression.Parameter (
                    connection, parameter.Name, RemoteType (parameter.Type, parameter));
                parameters [parameter] = serverParameter;
                functionParameters.Add (serverParameter);
            }
            try {
                var body = ToExpression (CompileNode (lambda.Body), lambda.Body.Type);
                return ServerExpression.Function (connection, functionParameters, body);
            } finally {
                foreach (var parameter in lambda.Parameters)
                    parameters.Remove (parameter);
            }
        }

        delegate ServerExpression BinaryOp (ServerExpression left, ServerExpression right);

        Result CompileBinary (BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.ArrayIndex) {
                var array = CompileNode (node.Left);
                var index = CompileNode (node.Right);
                if (array.IsValue && index.IsValue)
                    return Fold (node);
                return Result.FromExpression (ServerExpression.Get (
                    connection, ToExpression (array, node.Left.Type), ToExpression (index, node.Right.Type)));
            }
            if ((node.NodeType == ExpressionType.Add || node.NodeType == ExpressionType.AddChecked) &&
                node.Type == typeof(string)) {
                if (IsClientSide (node))
                    return Fold (node);
                return Result.FromExpression (ServerExpression.ConcatStrings (
                    connection,
                    new List<ServerExpression> {
                        StringPart (node.Left), StringPart (node.Right)
                    }));
            }
            BinaryOp op;
            switch (node.NodeType) {
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
                op = (a, b) => ServerExpression.Add (connection, a, b);
                break;
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
                op = (a, b) => ServerExpression.Subtract (connection, a, b);
                break;
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
                op = (a, b) => ServerExpression.Multiply (connection, a, b);
                break;
            case ExpressionType.Divide:
                op = (a, b) => ServerExpression.Divide (connection, a, b);
                break;
            case ExpressionType.Modulo:
                op = (a, b) => ServerExpression.Modulo (connection, a, b);
                break;
            case ExpressionType.Power:
                op = (a, b) => ServerExpression.Power (connection, a, b);
                break;
            case ExpressionType.LeftShift:
                op = (a, b) => ServerExpression.LeftShift (connection, a, b);
                break;
            case ExpressionType.RightShift:
                op = (a, b) => ServerExpression.RightShift (connection, a, b);
                break;
            case ExpressionType.Equal:
                op = (a, b) => ServerExpression.Equal (connection, a, b);
                break;
            case ExpressionType.NotEqual:
                op = (a, b) => ServerExpression.NotEqual (connection, a, b);
                break;
            case ExpressionType.GreaterThan:
                op = (a, b) => ServerExpression.GreaterThan (connection, a, b);
                break;
            case ExpressionType.GreaterThanOrEqual:
                op = (a, b) => ServerExpression.GreaterThanOrEqual (connection, a, b);
                break;
            case ExpressionType.LessThan:
                op = (a, b) => ServerExpression.LessThan (connection, a, b);
                break;
            case ExpressionType.LessThanOrEqual:
                op = (a, b) => ServerExpression.LessThanOrEqual (connection, a, b);
                break;
            // Note: conditional boolean operators do not short-circuit when
            // evaluated on the server
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                op = (a, b) => ServerExpression.And (connection, a, b);
                break;
            case ExpressionType.Or:
            case ExpressionType.OrElse:
                op = (a, b) => ServerExpression.Or (connection, a, b);
                break;
            case ExpressionType.ExclusiveOr:
                op = (a, b) => ServerExpression.ExclusiveOr (connection, a, b);
                break;
            default:
                throw Error (node, "unsupported operator (" + node.NodeType + ")");
            }
            return CompileNumericBinary (node, node.Left, node.Right, op);
        }

        Result CompileNumericBinary (Expression node, Expression leftNode, Expression rightNode, BinaryOp op)
        {
            var left = CompileNode (leftNode);
            var right = CompileNode (rightNode);
            if (left.IsValue && right.IsValue)
                return Fold (node);
            return Result.FromExpression (op (
                ToExpression (left, leftNode.Type), ToExpression (right, rightNode.Type)));
        }

        Result CompileUnary (UnaryExpression node)
        {
            switch (node.NodeType) {
            case ExpressionType.Quote:
                return CompileNode (node.Operand);
            case ExpressionType.Not: {
                    var operand = CompileNode (node.Operand);
                    if (operand.IsValue)
                        return Fold (node);
                    return Result.FromExpression (ServerExpression.Not (
                        connection, ToExpression (operand, node.Operand.Type)));
                }
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked: {
                    var operand = CompileNode (node.Operand);
                    if (operand.IsValue)
                        return Fold (node);
                    return Result.FromExpression (ServerExpression.Multiply (
                        connection,
                        ServerExpression.Cast (
                            connection,
                            ServerExpression.ConstantInt (connection, -1),
                            RemoteType (node.Operand.Type, node)),
                        ToExpression (operand, node.Operand.Type)));
                }
            case ExpressionType.OnesComplement: {
                    var operand = CompileNode (node.Operand);
                    if (operand.IsValue)
                        return Fold (node);
                    return Result.FromExpression (ServerExpression.Not (
                        connection, ToExpression (operand, node.Operand.Type)));
                }
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked: {
                    // A conversion to object is a boxing conversion inserted by
                    // the compiler, for example around the arguments of
                    // string.Concat; compile the value itself
                    if (node.Type == typeof(object))
                        return CompileNode (node.Operand);
                    var operand = CompileNode (node.Operand);
                    if (operand.IsValue)
                        return Fold (node);
                    return Result.FromExpression (ServerExpression.Cast (
                        connection,
                        ToExpression (operand, node.Operand.Type),
                        RemoteType (node.Type, node)));
                }
            default:
                throw Error (node, "unsupported operator (" + node.NodeType + ")");
            }
        }

        Result CompileConditional (ConditionalExpression node)
        {
            var condition = CompileNode (node.Test);
            if (condition.IsValue)
                return CompileNode ((bool)condition.Value ? node.IfTrue : node.IfFalse);
            return Result.FromExpression (ServerExpression.Conditional (
                connection,
                condition.Expression,
                ToExpression (CompileNode (node.IfTrue), node.IfTrue.Type),
                ToExpression (CompileNode (node.IfFalse), node.IfFalse.Type)));
        }

        Result CompileTuple (NewExpression node)
        {
            var results = node.Arguments.Select (CompileNode).ToList ();
            if (results.All (result => result.IsValue))
                return Fold (node);
            var elements = new List<ServerExpression> ();
            for (int i = 0; i < results.Count; i++)
                elements.Add (ToExpression (results [i], node.Arguments [i].Type));
            return Result.FromExpression (ServerExpression.CreateTuple (connection, elements));
        }

        /// <summary>
        /// Compile the construction of a structure. The generated constructor takes
        /// the fields in the order the structure declares them, which is the order
        /// the server builds one from.
        /// </summary>
        Result CompileStruct (NewExpression node)
        {
            var results = node.Arguments.Select (CompileNode).ToList ();
            if (results.All (result => result.IsValue))
                return Fold (node);
            var fields = new List<ServerExpression> ();
            for (int i = 0; i < results.Count; i++)
                fields.Add (ToExpression (results [i], node.Arguments [i].Type));
            return Result.FromExpression (ServerExpression.CreateStruct (
                connection, RemoteType (node.Type, node), fields));
        }

        Result CompileListInit (ListInitExpression node)
        {
            var elements = new List<Result> ();
            var types = new List<Type> ();
            foreach (var initializer in node.Initializers) {
                if (initializer.Arguments.Count != 1)
                    throw Error (node, "unsupported collection initializer");
                elements.Add (CompileNode (initializer.Arguments [0]));
                types.Add (initializer.Arguments [0].Type);
            }
            if (elements.All (element => element.IsValue))
                return Fold (node);
            var expressions = new List<ServerExpression> ();
            for (int i = 0; i < elements.Count; i++)
                expressions.Add (ToExpression (elements [i], types [i]));
            if (node.Type.IsGenericType && node.Type.GetGenericTypeDefinition () == typeof(HashSet<>))
                return Result.FromExpression (ServerExpression.CreateSet (
                    connection, new HashSet<ServerExpression> (expressions)));
            return Result.FromExpression (ServerExpression.CreateList (connection, expressions));
        }

        Result CallNode (RPCAttribute attribute, IList<Result> arguments)
        {
            var call = new Schema.KRPC.ProcedureCall ();
            call.Service = attribute.Service;
            call.Procedure = attribute.Procedure;
            var expressions = new Dictionary<int, ServerExpression> ();
            foreach (var argument in arguments)
                expressions [expressions.Count] = ToExpression (argument, null);
            return Result.FromExpression (
                ServerExpression.CallWithArguments (connection, call, expressions));
        }

        static RPCAttribute GetRPCAttribute (MemberInfo member)
        {
            var attributes = member.GetCustomAttributes (typeof(RPCAttribute), false);
            return attributes.Length == 1 ? (RPCAttribute)attributes [0] : null;
        }

        /// <summary>
        /// Evaluate a node that does not interact with the server, and embed
        /// the result as a value.
        /// </summary>
        static Result Fold (Expression node)
        {
            try {
                return Result.FromValue (
                    Expression.Lambda (node).Compile ().DynamicInvoke ());
            } catch (Exception exception) {
                var message = exception.InnerException != null
                    ? exception.InnerException.Message : exception.Message;
                throw new ExpressionCompilationException (
                    "Cannot compile expression: error evaluating client side sub-expression: " + message,
                    exception);
            }
        }

        /// <summary>
        /// Convert a compiled result to a server side expression, embedding
        /// values as constants. The declared type takes precedence over the
        /// value's runtime type, when given.
        /// </summary>
        ServerExpression ToExpression (Result result, Type type)
        {
            if (!result.IsValue)
                return result.Expression;
            var value = result.Value;
            if (value == null)
                throw new ExpressionCompilationException (
                    "Cannot compile expression: null values are not supported");
            type = type ?? value.GetType ();
            if (type == typeof(double))
                return ServerExpression.ConstantDouble (connection, (double)value);
            if (type == typeof(float))
                return ServerExpression.ConstantFloat (connection, (float)value);
            if (type == typeof(int))
                return ServerExpression.ConstantInt (connection, (int)value);
            if (type == typeof(bool))
                return ServerExpression.ConstantBool (connection, (bool)value);
            if (type == typeof(string))
                return ServerExpression.ConstantString (connection, (string)value);
            if (type == typeof(long) || type == typeof(uint) || type == typeof(ulong)) {
                var converted = Convert.ToInt64 (value, null);
                if (converted < int.MinValue || converted > int.MaxValue)
                    throw new ExpressionCompilationException (
                        "Cannot compile expression: integer constant " + converted + " is out of range");
                return ServerExpression.Cast (
                    connection,
                    ServerExpression.ConstantInt (connection, (int)converted),
                    RemoteType (type, null));
            }
            if (value is RemoteObject remoteObject)
                return ServerExpression.ConstantObject (connection, remoteObject.id);
            if (type.IsEnum)
                return ServerExpression.Cast (
                    connection,
                    ServerExpression.ConstantInt (connection, Convert.ToInt32 (value, null)),
                    RemoteType (type, null));
            if (value is IDictionary dictionary) {
                var keys = new List<ServerExpression> ();
                var values = new List<ServerExpression> ();
                foreach (DictionaryEntry entry in dictionary) {
                    keys.Add (ToExpression (Result.FromValue (entry.Key), null));
                    values.Add (ToExpression (Result.FromValue (entry.Value), null));
                }
                if (keys.Count == 0)
                    throw new ExpressionCompilationException (
                        "Cannot compile expression: empty collections are not supported");
                return ServerExpression.CreateDictionary (connection, keys, values);
            }
            if (value is IEnumerable enumerable && !(value is string)) {
                var elements = new List<ServerExpression> ();
                foreach (var element in enumerable)
                    elements.Add (ToExpression (Result.FromValue (element), null));
                if (elements.Count == 0)
                    throw new ExpressionCompilationException (
                        "Cannot compile expression: empty collections are not supported");
                if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof(HashSet<>))
                    return ServerExpression.CreateSet (
                        connection, new HashSet<ServerExpression> (elements));
                return ServerExpression.CreateList (connection, elements);
            }
            if (type.Name.StartsWith ("Tuple`", StringComparison.Ordinal)) {
                var elements = new List<ServerExpression> ();
                foreach (var property in type.GetProperties ().Where (p => p.Name.StartsWith ("Item", StringComparison.Ordinal)))
                    elements.Add (ToExpression (Result.FromValue (property.GetValue (value)), property.PropertyType));
                return ServerExpression.CreateTuple (connection, elements);
            }
            if (IsAStruct (type)) {
                var fields = new List<ServerExpression> ();
                foreach (var property in StructFields (type))
                    fields.Add (ToExpression (
                        Result.FromValue (property.GetValue (value)), property.PropertyType));
                return ServerExpression.CreateStruct (
                    connection, RemoteType (type, null), fields);
            }
            throw new ExpressionCompilationException (
                "Cannot compile expression: cannot use a value of type " + type.Name + " in an expression");
        }

        /// <summary>
        /// The KRPC.Type object describing a CLR type.
        /// </summary>
        ServerType RemoteType (Type type, Expression node)
        {
            if (type == typeof(double))
                return ServerType.Double (connection);
            if (type == typeof(float))
                return ServerType.Float (connection);
            if (type == typeof(int))
                return ServerType.Int (connection);
            if (type == typeof(long))
                return ServerType.Long (connection);
            if (type == typeof(uint))
                return ServerType.UInt (connection);
            if (type == typeof(ulong))
                return ServerType.ULong (connection);
            if (type == typeof(bool))
                return ServerType.Bool (connection);
            if (type == typeof(string))
                return ServerType.String (connection);
            if (type == typeof(byte[]))
                return ServerType.Bytes (connection);
            if (typeof(RemoteObject).IsAssignableFrom (type))
                return ServerType.ClassType (connection, ServiceName (type), type.Name);
            if (type.IsEnum)
                return ServerType.EnumerationType (connection, ServiceName (type), type.Name);
            if (IsAStruct (type))
                return ServerType.StructType (connection, ServiceName (type), type.Name);
            if (type.Name.StartsWith ("Tuple`", StringComparison.Ordinal))
                return ServerType.TupleType (
                    connection,
                    type.GetGenericArguments ().Select (t => RemoteType (t, node)).ToList ());
            if (type.IsGenericType) {
                var definition = type.GetGenericTypeDefinition ();
                var genericArguments = type.GetGenericArguments ();
                if (definition == typeof(IList<>) || definition == typeof(List<>) ||
                    definition == typeof(IEnumerable<>))
                    return ServerType.ListType (connection, RemoteType (genericArguments [0], node));
                if (definition == typeof(HashSet<>) || definition == typeof(ISet<>))
                    return ServerType.SetType (connection, RemoteType (genericArguments [0], node));
                if (definition == typeof(IDictionary<,>) || definition == typeof(Dictionary<,>))
                    return ServerType.DictionaryType (
                        connection,
                        RemoteType (genericArguments [0], node),
                        RemoteType (genericArguments [1], node));
            }
            throw Error (node, "cannot express the type " + type.Name + " on the server");
        }

        /// <summary>
        /// Whether the type is a structure a service defines.
        /// </summary>
        static bool IsAStruct (Type type)
        {
            return type.IsDefined (typeof(KRPCStructAttribute), false);
        }

        /// <summary>
        /// The properties carrying the fields of a structure, in the order the type
        /// declares them, which is the order the server builds one from. Reflection
        /// does not promise an order for the properties of a type, so they are
        /// ordered by metadata token, which is assigned in declaration order.
        /// </summary>
        static IEnumerable<PropertyInfo> StructFields (Type type)
        {
            return type.GetProperties (BindingFlags.Public | BindingFlags.Instance)
                .OrderBy (property => property.MetadataToken);
        }

        /// <summary>
        /// The name of the service a generated class, enumeration or structure
        /// belongs to, from its namespace.
        /// </summary>
        static string ServiceName (Type type)
        {
            var name = type.Namespace ?? string.Empty;
            return name.Substring (name.LastIndexOf ('.') + 1);
        }

        static ExpressionCompilationException Error (Expression node, string message)
        {
            var location = node == null ? string.Empty : " in '" + node + "'";
            return new ExpressionCompilationException (
                "Cannot compile expression: " + message + location);
        }
    }
}
