using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using KRPC.Client.Attributes;
using ServerExpression = KRPC.Client.Services.KRPC.Expression;
using ServerType = KRPC.Client.Services.KRPC.Type;

namespace KRPC.Client
{
    /// <summary>
    /// Compiles LINQ expression trees into server side functions.
    /// </summary>
    /// <remarks>
    /// A subtree with no remote procedure call is evaluated on the client when compiling,
    /// and becomes a constant.
    /// </remarks>
    class FunctionCompiler
    {
        /// <summary>
        /// The KRPC.Type objects built for CLR types, as named and as nullable.
        /// </summary>
        internal sealed class TypeCache
        {
            public readonly IDictionary<Type, ServerType> Types =
                new System.Collections.Concurrent.ConcurrentDictionary<Type, ServerType> ();
            public readonly IDictionary<Type, ServerType> NullableTypes =
                new System.Collections.Concurrent.ConcurrentDictionary<Type, ServerType> ();
        }

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
                if (GetRPCAttribute (node.Method) != null || IsWriteLine (node.Method))
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

        /// <summary>
        /// Finds code whose body the compiler cannot see, which could make a remote procedure
        /// call when evaluated on the client: a call of a method, delegate, constructor, property
        /// or operator, or a delegate or a value with methods of its own passed to the base
        /// library, which can call them.
        /// </summary>
        /// <remarks>
        /// An auto-property only reads a field, so it is evaluated on the client.
        /// </remarks>
        sealed class ClientFunctionFinder : ExpressionVisitor
        {
            public Expression Found { get; private set; }

            public string Message { get; private set; }

            void Find (Expression node, string message)
            {
                if (Found != null)
                    return;
                Found = node;
                Message = message;
            }

            void FindCall (Expression node)
            {
                Find (node, "cannot call a method, delegate, constructor, property or operator " +
                      "the compiler cannot see into; call it outside the lambda and use its result");
            }

            protected override Expression VisitMethodCall (MethodCallExpression node)
            {
                // A delegate called through its Invoke method
                if (IsClientCode (node.Method) || typeof(Delegate).IsAssignableFrom (node.Method.DeclaringType))
                    FindCall (node);
                else
                    CheckOperands (node.Object == null
                                   ? node.Arguments
                                   : new [] { node.Object }.Concat (node.Arguments));
                return base.VisitMethodCall (node);
            }

            protected override Expression VisitMember (MemberExpression node)
            {
                var property = node.Member as PropertyInfo;
                var getter = property == null ? null : property.GetGetMethod (true);
                if (getter != null && IsClientCode (getter)) {
                    if (!IsCompilerGenerated (getter))
                        FindCall (node);
                } else if (getter != null && node.Expression != null) {
                    // The value of a Lazy is computed by its factory delegate
                    if (IsGeneric (getter.DeclaringType, typeof(Lazy<>)) && property.Name == "Value")
                        FindCall (node);
                    else
                        CheckOperands (new [] { node.Expression });
                }
                return base.VisitMember (node);
            }

            protected override Expression VisitBinary (BinaryExpression node)
            {
                if (node.Method != null && IsClientCode (node.Method))
                    FindCall (node);
                else if (node.Method != null)
                    CheckOperands (new [] { node.Left, node.Right });
                return base.VisitBinary (node);
            }

            protected override Expression VisitUnary (UnaryExpression node)
            {
                if (node.Method != null && IsClientCode (node.Method))
                    FindCall (node);
                return base.VisitUnary (node);
            }

            protected override Expression VisitInvocation (InvocationExpression node)
            {
                FindCall (node);
                return base.VisitInvocation (node);
            }

            protected override Expression VisitNew (NewExpression node)
            {
                if (node.Constructor != null) {
                    if (IsClientCode (node.Constructor) && !IsAnonymous (node.Type))
                        FindCall (node);
                    else
                        CheckOperands (node.Arguments);
                }
                return base.VisitNew (node);
            }

            protected override Expression VisitMemberInit (MemberInitExpression node)
            {
                foreach (var binding in node.Bindings) {
                    var property = binding.Member as PropertyInfo;
                    var setter = property == null ? null : property.GetSetMethod (true);
                    if (setter != null && IsClientCode (setter) && !IsCompilerGenerated (setter))
                        FindCall (node);
                }
                return base.VisitMemberInit (node);
            }

            protected override Expression VisitListInit (ListInitExpression node)
            {
                foreach (var initializer in node.Initializers) {
                    if (IsClientCode (initializer.AddMethod))
                        FindCall (node);
                    else
                        CheckOperands (initializer.Arguments);
                }
                return base.VisitListInit (node);
            }

            /// <summary>
            /// Check the values passed to a base library member, which can call their methods,
            /// such as ToString, or invoke a delegate.
            /// </summary>
            void CheckOperands (IEnumerable<Expression> operands)
            {
                foreach (var operand in operands) {
                    var value = WithoutConversions (operand);
                    if (value is NewArrayExpression array && array.NodeType == ExpressionType.NewArrayInit) {
                        CheckOperands (array.Expressions);
                    } else if (typeof(Delegate).IsAssignableFrom (value.Type)) {
                        if (!IsVisibleDelegate (value))
                            Find (operand, "cannot pass a delegate the compiler cannot see into; " +
                                  "pass a lambda expression");
                    } else {
                        var type = ValueType (value);
                        if (HasClientMethods (type))
                            Find (operand, "cannot pass a " + type.Name + ", whose methods the " +
                                  "compiler cannot see into, to the base library; " +
                                  "use it outside the lambda");
                    }
                }
            }

            static Expression WithoutConversions (Expression node)
            {
                while ((node.NodeType == ExpressionType.Convert ||
                        node.NodeType == ExpressionType.ConvertChecked ||
                        node.NodeType == ExpressionType.TypeAs) &&
                       ((UnaryExpression)node).Method == null)
                    node = ((UnaryExpression)node).Operand;
                return node;
            }

            /// <summary>
            /// Whether a delegate's body is visible to the compiler: a lambda in the expression,
            /// or a method group of the base library.
            /// </summary>
            static bool IsVisibleDelegate (Expression node)
            {
                if (node.NodeType == ExpressionType.Quote)
                    node = ((UnaryExpression)node).Operand;
                if (node is LambdaExpression)
                    return true;
                // A method group is converted to a delegate by a call of CreateDelegate, given
                // the method as a constant
                var call = node as MethodCallExpression;
                if (call == null || call.Method.Name != "CreateDelegate")
                    return false;
                var method = new [] { call.Object }.Concat (call.Arguments)
                    .OfType<ConstantExpression> ()
                    .Select (constant => constant.Value as MethodInfo)
                    .FirstOrDefault (candidate => candidate != null);
                return method != null && !IsClientCode (method);
            }

            /// <summary>
            /// The type of a value: the type of the value a captured variable holds, or else
            /// the node's static type.
            /// </summary>
            static Type ValueType (Expression node)
            {
                object value;
                if (TryCapturedValue (node, out value) && value != null)
                    return value.GetType ();
                return node.Type;
            }

            static bool TryCapturedValue (Expression node, out object value)
            {
                value = null;
                if (node is ConstantExpression constant) {
                    value = constant.Value;
                    return true;
                }
                var member = node as MemberExpression;
                var field = member == null ? null : member.Member as FieldInfo;
                if (field == null)
                    return false;
                object instance = null;
                if (member.Expression != null &&
                    (!TryCapturedValue (member.Expression, out instance) || instance == null))
                    return false;
                value = field.GetValue (instance);
                return true;
            }

            /// <summary>
            /// Whether a type, or a type it is built from such as the element type of a
            /// collection, implements a virtual method outside the base library and the client,
            /// such as an override of ToString.
            /// </summary>
            internal static bool HasClientMethods (Type type, int depth = 0)
            {
                if (type == null || depth > 4 || IsAnonymous (type))
                    return false;
                if (type.GetMethods (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any (method => method.IsVirtual && IsClientCode (method)))
                    return true;
                if (type.HasElementType && HasClientMethods (type.GetElementType (), depth + 1))
                    return true;
                return type.IsGenericType &&
                    type.GetGenericArguments ().Any (argument => HasClientMethods (argument, depth + 1));
            }

            static bool IsCompilerGenerated (MethodInfo method)
            {
                return method.IsDefined (typeof(CompilerGeneratedAttribute), false) ||
                    method.DeclaringType.IsDefined (typeof(CompilerGeneratedAttribute), false);
            }

            // An anonymous type only stores and compares the values it is created with
            static bool IsAnonymous (Type type)
            {
                return type.IsDefined (typeof(CompilerGeneratedAttribute), false) &&
                    type.Name.Contains ("AnonymousType");
            }

            // The base library, the client and the generated service stubs are known not to
            // make remote calls when evaluated on the client
            static bool IsClientCode (MethodBase method)
            {
                var type = method.DeclaringType;
                var ns = type.Namespace ?? string.Empty;
                return !IsInNamespace (ns, "System") && !IsInNamespace (ns, "KRPC.Client.Services") &&
                    type.Assembly != typeof (Connection).Assembly;
            }

            static bool IsInNamespace (string ns, string root)
            {
                return ns == root || ns.StartsWith (root + ".", StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Replaces the operands of a node with the values they were compiled to.
        /// </summary>
        sealed class OperandReplacer : ExpressionVisitor
        {
            readonly IDictionary<Expression, Expression> replacements;

            public OperandReplacer (IDictionary<Expression, Expression> operandValues)
            {
                replacements = operandValues;
            }

            public override Expression Visit (Expression node)
            {
                Expression replacement;
                if (node != null && replacements.TryGetValue (node, out replacement))
                    return replacement;
                return base.Visit (node);
            }
        }

        /// <summary>
        /// Finds a read of a stream's value, which the compiler would evaluate once on the
        /// client.
        /// </summary>
        sealed class StreamReadFinder : ExpressionVisitor
        {
            public MethodCallExpression Found { get; private set; }

            protected override Expression VisitMethodCall (MethodCallExpression node)
            {
                var type = node.Method.DeclaringType;
                if (Found == null && type.IsGenericType &&
                    type.GetGenericTypeDefinition () == typeof (Stream<>))
                    Found = node;
                return base.VisitMethodCall (node);
            }
        }

        /// <summary>
        /// Whether an argument of a method call interacts with the server.
        /// </summary>
        internal static bool HasRemoteArguments (Expression call)
        {
            var methodCall = call as MethodCallExpression;
            if (methodCall == null)
                return false;
            var finder = new RemoteNodeFinder (new List<ParameterExpression> ());
            foreach (var argument in methodCall.Arguments)
                finder.Visit (argument);
            return finder.Found;
        }

        readonly IConnection connection;
        readonly TypeCache remoteTypes;
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

        FunctionCompiler (IConnection serverConnection, TypeCache typeCache)
        {
            connection = serverConnection;
            remoteTypes = typeCache;
        }

        /// <summary>
        /// Compile a lambda expression into a server side expression that computes the same result
        /// on the server.
        /// </summary>
        /// <remarks>
        /// A lambda with parameters compiles to a function that takes them. A streamed expression,
        /// or one with no value, must make a remote procedure call, and a streamed expression
        /// cannot take parameters.
        /// </remarks>
        public static ServerExpression Compile (
            IConnection connection, LambdaExpression expression,
            TypeCache typeCache, bool streamed = false)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));
            if (streamed && expression.Parameters.Count != 0)
                throw new FunctionCompilationException (
                    "A function that is streamed, or checked for an event, cannot take parameters");
            var compiler = new FunctionCompiler (connection, typeCache);
            var functionParameters = new List<ServerExpression> ();
            foreach (var parameter in expression.Parameters) {
                var serverParameter = ServerExpression.Parameter (
                    connection, parameter.Name, compiler.VariableType (parameter.Type, parameter));
                compiler.parameters [parameter] = serverParameter;
                functionParameters.Add (serverParameter);
            }
            var body = ResultBody (expression);
            var streamRead = new StreamReadFinder ();
            streamRead.Visit (body);
            if (streamRead.Found != null)
                throw Error (streamRead.Found,
                             "Stream." + streamRead.Found.Method.Name +
                             " is evaluated once, on the client. " +
                             "Make the stream's remote procedure call in the lambda instead");
            if (streamed && compiler.IsClientSide (body))
                throw Error (body,
                             "the lambda makes no remote procedure call, " +
                             "so its value is the same on every update");
            if (expression.ReturnType == typeof(void) && compiler.IsClientSide (body))
                throw Error (body,
                             "the lambda makes no remote procedure call, " +
                             "so it has no effect on the server");
            try {
                var result = compiler.CompileNode (body);
                var serverExpression = compiler.ToExpression (result, body.Type);
                // A lazily evaluated sequence cannot be sent to a client; make it concrete
                if (IsALazySequence (body.Type))
                    serverExpression = ServerExpression.ToList (connection, serverExpression);
                if (functionParameters.Count > 0)
                    serverExpression = ServerExpression.Lambda (connection, functionParameters, serverExpression);
                return serverExpression;
            } catch (Exception exception) when (
                exception is RPCException || exception is ArgumentException ||
                exception is InvalidOperationException) {
                // An error building a node on the server
                throw new FunctionCompilationException (
                    "Cannot compile expression: " + exception.Message, exception);
            }
        }

        /// <summary>
        /// The body of a lambda, without the conversions that widen its value to the lambda's
        /// result type, such as boxing to object.
        /// </summary>
        /// <remarks>
        /// The compiled function evaluates to the type of this body.
        /// </remarks>
        internal static Expression ResultBody (LambdaExpression expression)
        {
            var body = expression.Body;
            while (body.NodeType == ExpressionType.Convert && !body.Type.IsValueType &&
                   body.Type.IsAssignableFrom (((UnaryExpression)body).Operand.Type))
                body = ((UnaryExpression)body).Operand;
            return body;
        }

        internal static bool IsALazySequence (Type type)
        {
            if (!type.IsGenericType)
                return false;
            var definition = type.GetGenericTypeDefinition ();
            return definition == typeof(IEnumerable<>) || definition == typeof(IOrderedEnumerable<>);
        }

        Result CompileNode (Expression node)
        {
            // A subtree that does not interact with the server is evaluated once, as a
            // whole, so a side effect in it happens once
            if (!(node is ConstantExpression) && !(node is ParameterExpression) &&
                IsClientSide (node))
                return Fold (node);
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
            case NewExpression create when IsATuple (create.Type):
                return CompileTuple (create, create.Arguments);
            case NewExpression create when IsAStruct (create.Type):
                return CompileStruct (create);
            case ListInitExpression listInit:
                return CompileListInit (listInit);
            default:
                if (IsClientSide (node))
                    return Fold (node);
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
            if (node.Expression == null)
                throw Error (node, "'" + node.Member.Name + "' is not a remote member");
            var compiled = CompileNode (node.Expression);
            // A member of a value the client computed, such as the branch a client side
            // condition chooses
            if (compiled.IsValue)
                return Fold (node, new [] { node.Expression }, new [] { compiled });
            var instance = compiled.Expression;
            var name = node.Member.Name;
            if (Nullable.GetUnderlyingType (node.Expression.Type) != null) {
                if (name == "HasValue")
                    return Result.FromExpression (ServerExpression.Not (
                        connection, ServerExpression.IsNull (connection, instance)));
                // The cast throws when the value is null
                if (name == "Value")
                    return Result.FromExpression (ServerExpression.Cast (
                        connection, instance, RemoteType (node.Type, node)));
            }
            if (IsAStruct (node.Expression.Type))
                return Result.FromExpression (ServerExpression.GetField (connection, instance, name));
            if (name == "Length" && node.Expression.Type == typeof(string))
                return Result.FromExpression (ServerExpression.StringLength (connection, instance));
            if (IsATuple (node.Expression.Type) && name.StartsWith ("Item", StringComparison.Ordinal)) {
                var index = int.Parse (
                    name.Substring (4), System.Globalization.CultureInfo.InvariantCulture) - 1;
                return Result.FromExpression (ServerExpression.Get (
                    connection, instance, ServerExpression.ConstantInt (connection, index)));
            }
            if (name == "Keys" && IsADictionary (node.Expression.Type))
                return Result.FromExpression (ServerExpression.DictionaryKeys (connection, instance));
            if (name == "Values" && IsADictionary (node.Expression.Type))
                return Result.FromExpression (ServerExpression.DictionaryValues (connection, instance));
            if (name == "Count")
                return Result.FromExpression (ServerExpression.Count (connection, instance));
            throw Error (node, "'" + node.Member.Name + "' is not a remote member");
        }

        static bool IsADictionary (Type type)
        {
            return new [] { type }.Concat (type.GetInterfaces ()).Any (
                candidate => candidate.IsGenericType &&
                    (candidate.GetGenericTypeDefinition () == typeof(IDictionary<,>) ||
                     candidate.GetGenericTypeDefinition () == typeof(IReadOnlyDictionary<,>)));
        }

        Result CompileCall (MethodCallExpression node)
        {
            var method = node.Method;
            var attribute = GetRPCAttribute (method);
            if (attribute != null)
                return CompileRPC (node, attribute);
            if (method.DeclaringType == typeof(Enumerable))
                return CompileEnumerableCall (node);
            if (method.DeclaringType == typeof(Tuple) && method.Name == "Create")
                return CompileTuple (node, node.Arguments);
            if (method.DeclaringType == typeof(Math) && !IsClientSide (node)) {
                if (method.Name == "Pow")
                    return CompileNumericBinary (
                        node, node.Arguments [0], node.Arguments [1],
                        (a, b) => ServerExpression.Power (connection, a, b));
                if (method.Name == "Round")
                    return CompileRoundCall (node);
                if (IsAnInteger (node.Type) &&
                    (method.Name == "Abs" || method.Name == "Min" ||
                     method.Name == "Max" || method.Name == "Clamp"))
                    return CompileIntegerMath (node);
                if (method.Name == "Sign" &&
                    (node.Arguments [0].Type == typeof(double) || node.Arguments [0].Type == typeof(float)))
                    return CompileFloatSign (node);
                Tuple<string, int, int> procedure;
                if (mathProcedures.TryGetValue (method.Name, out procedure)) {
                    if (node.Arguments.Count < procedure.Item2 ||
                        node.Arguments.Count > procedure.Item3)
                        throw Error (node, "Math." + method.Name + " with " +
                                     node.Arguments.Count + " arguments has no server " +
                                     "side equivalent");
                    return CompileStdLibCall (node, procedure.Item1);
                }
                throw Error (node, "Math." + method.Name + " has no server side equivalent");
            }
            if (IsWriteLine (method))
                return CompileWriteLine (node);
            if (method.Name == "ToString" && node.Object != null && !IsClientSide (node))
                return CompileToString (node);
            if (method.DeclaringType == typeof(string) && method.Name == "Concat" &&
                !IsClientSide (node))
                return CompileStringConcat (node);
            if (method.DeclaringType == typeof(string) && !IsClientSide (node)) {
                var str = CompileStringCall (node);
                if (str != null)
                    return str;
            }
            if (IsClientSide (node))
                return Fold (node);
            if (method.Name == "GetValueOrDefault" && node.Object != null &&
                Nullable.GetUnderlyingType (node.Object.Type) != null)
                return CompileGetValueOrDefault (node);
            if (method.Name == "get_Item" && node.Object != null) {
                var instance = CompileNode (node.Object);
                var index = CompileNode (node.Arguments [0]);
                return Result.FromExpression (ServerExpression.Get (
                    connection,
                    ToExpression (instance, node.Object.Type),
                    ToExpression (index, node.Arguments [0].Type)));
            }
            if (method.Name == "ContainsKey" && node.Object != null &&
                node.Arguments.Count == 1) {
                var instance = CompileNode (node.Object);
                var key = CompileNode (node.Arguments [0]);
                return Result.FromExpression (ServerExpression.ContainsKey (
                    connection,
                    ToExpression (instance, node.Object.Type),
                    ToExpression (key, node.Arguments [0].Type)));
            }
            if (method.Name == "Contains" && node.Object != null && node.Arguments.Count == 1 &&
                typeof(IEnumerable).IsAssignableFrom (node.Object.Type)) {
                var instance = CompileNode (node.Object);
                var value = CompileNode (node.Arguments [0]);
                return Result.FromExpression (ServerExpression.Contains (
                    connection,
                    ToExpression (instance, node.Object.Type),
                    ToExpression (value, node.Arguments [0].Type)));
            }
            if (method.DeclaringType == typeof(string) && method.Name == "Format") {
                var arguments = node.Arguments.ToList ();
                if (method.GetParameters () [0].ParameterType != typeof(IFormatProvider))
                    return CompileFormat (node, arguments [0], arguments.Skip (1).ToList (), false);
                if (!IsInvariantCulture (arguments [0]))
                    throw Error (node, "the format provider must be CultureInfo.InvariantCulture");
                return CompileFormat (node, arguments [1], arguments.Skip (2).ToList (), true);
            }
            if (method.DeclaringType == typeof(FormattableString) && method.Name == "Invariant") {
                var create = node.Arguments [0] as MethodCallExpression;
                if (create == null || create.Method.Name != "Create")
                    throw Error (node, "FormattableString.Invariant takes an interpolated string");
                return CompileFormat (
                    node, create.Arguments [0], create.Arguments.Skip (1).ToList (), true);
            }
            if (method.DeclaringType == typeof(string) && method.Name == "get_Chars")
                throw Error (node, "a string cannot be indexed on the server. Call Substring (index, 1)");
            if (node.Object != null && !IsClientSide (node.Object) &&
                node.Arguments.All (IsClientSide))
                throw Error (node, "'" + method.Name + "' has no server side equivalent");
            throw Error (node, "cannot call '" + method.Name + "' with an argument computed on the server");
        }

        // The System.Math methods with a StdLib equivalent, each with the smallest
        // and largest number of arguments that equivalent takes
        static readonly IDictionary<string, Tuple<string, int, int>> mathProcedures =
            new Dictionary<string, Tuple<string, int, int>> {
                { "Abs", Tuple.Create ("Abs", 1, 1) },
                { "Sign", Tuple.Create ("Sign", 1, 1) },
                { "Floor", Tuple.Create ("Floor", 1, 1) },
                { "Ceiling", Tuple.Create ("Ceiling", 1, 1) },
                { "Sqrt", Tuple.Create ("Sqrt", 1, 1) },
                { "Sin", Tuple.Create ("Sin", 1, 1) },
                { "Cos", Tuple.Create ("Cos", 1, 1) },
                { "Tan", Tuple.Create ("Tan", 1, 1) },
                { "Asin", Tuple.Create ("Asin", 1, 1) },
                { "Acos", Tuple.Create ("Acos", 1, 1) },
                { "Atan", Tuple.Create ("Atan", 1, 1) },
                { "Atan2", Tuple.Create ("Atan2", 2, 2) },
                { "Log", Tuple.Create ("Log", 1, 2) },
                { "Log10", Tuple.Create ("Log10", 1, 1) },
                { "Exp", Tuple.Create ("Exp", 1, 1) },
                { "Min", Tuple.Create ("Min", 2, 2) },
                { "Max", Tuple.Create ("Max", 2, 2) },
                { "Clamp", Tuple.Create ("Clamp", 3, 3) }
            };

        // The StdLib procedure for each midpoint rounding rule, by the name of the
        // MidpointRounding member, which builds against every target framework
        static readonly IDictionary<string, string> roundingProcedures =
            new Dictionary<string, string> {
                { "ToEven", "Round" },
                { "AwayFromZero", "RoundHalfAwayFromZero" }
            };

        /// <summary>
        /// Compile a call of Math.Round to the StdLib procedure for the rounding rule it asks for.
        /// </summary>
        /// <remarks>
        /// The rule is part of the procedure's name, so a mode computed at run time cannot be
        /// compiled.
        /// </remarks>
        Result CompileRoundCall (MethodCallExpression node)
        {
            var arguments = new List<Expression> (node.Arguments);
            var procedure = "Round";
            if (arguments.Count > 0 &&
                arguments [arguments.Count - 1].Type == typeof(MidpointRounding)) {
                var mode = arguments [arguments.Count - 1];
                if (!IsClientSide (mode))
                    throw Error (node, "the rounding mode given to Math.Round must be a client side value");
                var value = Fold (mode).Value;
                if (!roundingProcedures.TryGetValue (value.ToString (), out procedure))
                    throw Error (node, "Math.Round has no server side equivalent for " +
                                 "MidpointRounding." + value);
                arguments.RemoveAt (arguments.Count - 1);
            }
            if (arguments.Count < 1 || arguments.Count > 2 ||
                arguments.Any (argument => !IsNumeric (argument.Type)))
                throw Error (node, "unsupported argument types for Math.Round");
            var args = new List<ServerExpression> { DoubleArgument (arguments [0]) };
            if (arguments.Count == 2)
                args.Add (ToExpression (CompileNode (arguments [1]), arguments [1].Type));
            return StdLibCall (node, procedure, args);
        }

        static bool IsAnInteger (Type type)
        {
            return type == typeof(int) || type == typeof(long) ||
                type == typeof(uint) || type == typeof(ulong);
        }

        /// <summary>
        /// Compile Math.Abs, Min, Max or Clamp of integers, exactly at their own type.
        /// </summary>
        /// <remarks>
        /// The arguments are held in variables, so each is evaluated once.
        /// </remarks>
        Result CompileIntegerMath (MethodCallExpression node)
        {
            var type = RemoteType (node.Type, node);
            var variables = new List<ServerExpression> ();
            var statements = new List<ServerExpression> ();
            for (int i = 0; i < node.Arguments.Count; i++) {
                var variable = ServerExpression.Variable (connection, "argument" + i, type);
                variables.Add (variable);
                statements.Add (ServerExpression.Assign (connection, variable, Argument (node, i)));
            }
            var x = variables [0];
            var zero = ServerExpression.ConstantInt (connection, 0);
            ServerExpression result;
            switch (node.Method.Name) {
            case "Abs":
                result = ServerExpression.Conditional (
                    connection, ServerExpression.LessThan (connection, x, zero),
                    ServerExpression.Negate (connection, x), x);
                break;
            case "Min":
                result = ServerExpression.Conditional (
                    connection, ServerExpression.LessThanOrEqual (connection, x, variables [1]),
                    x, variables [1]);
                break;
            case "Max":
                result = ServerExpression.Conditional (
                    connection, ServerExpression.GreaterThanOrEqual (connection, x, variables [1]),
                    x, variables [1]);
                break;
            default:
                result = ClampChecked (
                    variables [1], variables [2],
                    ServerExpression.Conditional (
                        connection, ServerExpression.LessThan (connection, x, variables [1]),
                        variables [1],
                        ServerExpression.Conditional (
                            connection, ServerExpression.GreaterThan (connection, x, variables [2]),
                            variables [2], x)));
                break;
            }
            statements.Add (result);
            return Result.FromExpression (
                ServerExpression.BlockWithVariables (connection, variables, statements));
        }

        /// <summary>
        /// The value of a clamp, or a throw when the minimum is greater than the
        /// maximum, as Math.Clamp does.
        /// </summary>
        ServerExpression ClampChecked (
            ServerExpression min, ServerExpression max, ServerExpression clamped)
        {
            var message = ServerExpression.StringConcat (connection, new List<ServerExpression> {
                ServerExpression.ConstantString (connection, "'"),
                ServerExpression.ConvertToString (connection, min),
                ServerExpression.ConstantString (connection, "' cannot be greater than "),
                ServerExpression.ConvertToString (connection, max),
                ServerExpression.ConstantString (connection, ".")
            });
            return ServerExpression.Conditional (
                connection, ServerExpression.GreaterThan (connection, min, max),
                ServerExpression.Throw (connection, "KRPC", "ArgumentException", message),
                clamped);
        }

        /// <summary>
        /// Compile a call of a System.Math method to the equivalent StdLib procedure.
        /// </summary>
        /// <remarks>
        /// Arguments are converted to doubles, and the result is converted back to the type the
        /// Math method produces.
        /// </remarks>
        Result CompileStdLibCall (MethodCallExpression node, string procedure)
        {
            if (node.Arguments.Any (argument => !IsNumeric (argument.Type)))
                throw Error (node, "unsupported argument types for Math." + node.Method.Name);
            return StdLibCall (
                node, procedure,
                node.Arguments.Select (DoubleArgument).ToList ());
        }

        /// <summary>
        /// Compile Math.Sign of a float or double, which throws for a NaN, where
        /// StdLib.Sign gives 0.
        /// </summary>
        Result CompileFloatSign (MethodCallExpression node)
        {
            return Result.FromExpression (WithVariables (
                new List<ServerExpression> { DoubleArgument (node.Arguments [0]) }, values =>
                ServerExpression.Conditional (
                    connection, StdLibCall ("IsNan", values),
                    ServerExpression.Throw (
                        connection, "KRPC", "Error", ServerExpression.ConstantString (
                            connection, "Function does not accept floating point Not-a-Number values.")),
                    StdLibCall ("Sign", values))));
        }

        ServerExpression DoubleArgument (Expression argument)
        {
            var compiled = ToExpression (CompileNode (argument), argument.Type);
            if (argument.Type != typeof(double))
                compiled = ServerExpression.Cast (
                    connection, compiled, ServerType.Double (connection));
            return compiled;
        }

        Result StdLibCall (
            MethodCallExpression node, string procedure, IList<ServerExpression> arguments)
        {
            var result = StdLibCall (procedure, arguments);
            if (node.Type != typeof(double))
                result = ServerExpression.Cast (
                    connection, result, RemoteType (node.Type, node));
            return Result.FromExpression (result);
        }

        ServerExpression StdLibCall (string procedure, IList<ServerExpression> arguments)
        {
            var call = new Schema.KRPC.ProcedureCall ();
            call.Service = "StdLib";
            call.Procedure = procedure;
            var args = new Dictionary<int, ServerExpression> ();
            foreach (var argument in arguments)
                args [args.Count] = argument;
            return ServerExpression.CallWithArguments (connection, call, args);
        }

        // Console.WriteLine is a call of StdLib.Print, so it is never evaluated on the client
        static bool IsWriteLine (MethodInfo method)
        {
            return method.DeclaringType == typeof(Console) && method.Name == "WriteLine";
        }

        /// <summary>
        /// Compile Console.WriteLine to a call of StdLib.Print, which writes the line to the game's
        /// log.
        /// </summary>
        /// <remarks>
        /// Its arguments are formatted as string.Format formats them.
        /// </remarks>
        Result CompileWriteLine (MethodCallExpression node)
        {
            var arguments = node.Arguments;
            ServerExpression message;
            if (arguments.Count == 0)
                message = ServerExpression.ConstantString (connection, string.Empty);
            else if (arguments [0].Type == typeof(char[]))
                throw Error (node, "Console.WriteLine of a character array has no server " +
                             "side equivalent");
            else if (arguments.Count == 1)
                message = StringPart (arguments [0]);
            else
                message = CompileFormat (
                    node, arguments [0], arguments.Skip (1).ToList (), false).Expression;
            return Result.FromExpression (
                StdLibCall ("Print", new List<ServerExpression> { message }));
        }

        static bool IsNumeric (Type type)
        {
            return type == typeof(double) || type == typeof(float) ||
                type == typeof(int) || type == typeof(long) ||
                type == typeof(uint) || type == typeof(ulong);
        }

        /// <summary>
        /// Compile a call of a System.String member to the server's string operation, or
        /// return null for a member with no equivalent.
        /// </summary>
        Result CompileStringCall (MethodCallExpression node)
        {
            var name = node.Method.Name;
            var count = node.Arguments.Count;
            // On .NET Framework, Trim, TrimStart and TrimEnd take a params array, which
            // is empty when no characters are given
            if (name.StartsWith ("Trim", StringComparison.Ordinal) && count == 1 &&
                node.Arguments [0].Type == typeof(char[]) && IsClientSide (node.Arguments [0]) &&
                ((Fold (node.Arguments [0]).Value as char[]) ?? new char [0]).Length == 0)
                count = 0;
            if (name == "Join" && count == 2 && node.Object == null) {
                if (!typeof(IEnumerable<string>).IsAssignableFrom (node.Arguments [1].Type))
                    throw Error (node, "string.Join takes a collection of strings. " +
                                 "Convert the values with Select (x => x.ToString ())");
                // A null separator is an empty string, as in C#
                return Result.FromExpression (ServerExpression.StringJoin (
                    connection, StringPart (node.Arguments [0]), StringElements (node, 1)));
            }
            if (node.Object == null)
                return null;
            var instance = ToExpression (CompileNode (node.Object), typeof(string));
            switch (name) {
            case "Substring":
                if (count != 2)
                    return null;
                return Result.FromExpression (ServerExpression.StringSubstring (
                    connection, instance, Argument (node, 0), Argument (node, 1)));
            case "Replace":
                if (count != 2)
                    return null;
                // A null new value is an empty string, as in C#
                return Result.FromExpression (ServerExpression.StringReplace (
                    connection, instance, StringArgument (node, 0), StringPart (node.Arguments [1])));
            case "IndexOf":
                if (!IsOrdinalSearch (node))
                    return null;
                return Result.FromExpression (ServerExpression.StringIndexOf (
                    connection, instance, StringArgument (node, 0)));
            case "Contains":
                if (count != 1)
                    return null;
                return Result.FromExpression (ServerExpression.StringContains (
                    connection, instance, StringArgument (node, 0)));
            case "StartsWith":
                if (!IsOrdinalSearch (node))
                    return null;
                return Result.FromExpression (ServerExpression.StringStartsWith (
                    connection, instance, StringArgument (node, 0)));
            case "EndsWith":
                if (!IsOrdinalSearch (node))
                    return null;
                return Result.FromExpression (ServerExpression.StringEndsWith (
                    connection, instance, StringArgument (node, 0)));
            case "ToUpper":
                CheckInvariantCasing (node);
                return Result.FromExpression (ServerExpression.StringToUpper (connection, instance));
            case "ToUpperInvariant":
                return count == 0
                    ? Result.FromExpression (
                        ServerExpression.StringToUpper (connection, instance))
                    : null;
            case "ToLower":
                CheckInvariantCasing (node);
                return Result.FromExpression (ServerExpression.StringToLower (connection, instance));
            case "ToLowerInvariant":
                return count == 0
                    ? Result.FromExpression (
                        ServerExpression.StringToLower (connection, instance))
                    : null;
            case "Trim":
                return count == 0
                    ? Result.FromExpression (
                        ServerExpression.StringTrim (connection, instance))
                    : null;
            case "TrimStart":
                return count == 0
                    ? Result.FromExpression (
                        ServerExpression.StringTrimStart (connection, instance))
                    : null;
            case "TrimEnd":
                return count == 0
                    ? Result.FromExpression (
                        ServerExpression.StringTrimEnd (connection, instance))
                    : null;
            default:
                return null;
            }
        }

        /// <summary>
        /// Whether a call of IndexOf, StartsWith or EndsWith compares by character code, as the
        /// server does.
        /// </summary>
        /// <remarks>
        /// A search for a string compares with the current culture unless given
        /// StringComparison.Ordinal.
        /// </remarks>
        bool IsOrdinalSearch (MethodCallExpression node)
        {
            var arguments = node.Arguments;
            if (arguments.Count == 1 && arguments [0].Type == typeof(char))
                return true;
            if (arguments.Count == 2 && arguments [1].Type == typeof(StringComparison)) {
                if (!IsClientSide (arguments [1]) ||
                    !Equals (Fold (arguments [1]).Value, StringComparison.Ordinal))
                    throw Error (node, "the server compares strings by character code. " +
                                 "Pass StringComparison.Ordinal");
                return true;
            }
            if (arguments.Count == 1 && arguments [0].Type == typeof(string))
                throw Error (node, node.Method.Name + " of a string depends on the current culture. " +
                             "Pass StringComparison.Ordinal");
            return false;
        }

        /// <summary>
        /// Check that a call of ToUpper or ToLower changes case as the invariant culture does.
        /// </summary>
        void CheckInvariantCasing (MethodCallExpression node)
        {
            if (node.Arguments.Count == 1 && IsInvariantCulture (node.Arguments [0]))
                return;
            throw Error (node, node.Method.Name + " depends on the current culture. Call " +
                         node.Method.Name + "Invariant");
        }

        /// <summary>
        /// Whether an argument is CultureInfo.InvariantCulture.
        /// </summary>
        bool IsInvariantCulture (Expression node)
        {
            return IsClientSide (node) &&
                Equals (Fold (node).Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        ServerExpression Argument (MethodCallExpression node, int position)
        {
            return ToExpression (
                CompileNode (node.Arguments [position]), node.Arguments [position].Type);
        }

        /// <summary>
        /// A collection of strings passed to string.Join or string.Concat, with a null element
        /// as an empty string, as in C#.
        /// </summary>
        /// <remarks>
        /// The server cannot describe the type of a lazily evaluated sequence, whose elements are
        /// taken as nullable.
        /// </remarks>
        ServerExpression StringElements (MethodCallExpression node, int position)
        {
            var strings = Argument (node, position);
            if (!IsALazySequence (node.Arguments [position].Type)) {
                var types = strings.ReturnType.Types;
                if (types.Count != 1 || !types [0].Nullable)
                    return strings;
            }
            var element = ServerExpression.Parameter (
                connection, "element", VariableType (typeof(string), node));
            return ServerExpression.Select (connection, strings, ServerExpression.Lambda (
                connection, new List<ServerExpression> { element },
                ServerExpression.Conditional (
                    connection, ServerExpression.IsNull (connection, element),
                    ServerExpression.ConstantString (connection, string.Empty),
                    ServerExpression.ConvertToString (connection, element))));
        }

        ServerExpression StringArgument (MethodCallExpression node, int position)
        {
            var argument = node.Arguments [position];
            var result = CompileNode (argument);
            if (argument.Type == typeof(char)) {
                if (!result.IsValue)
                    throw Error (node, "a character must be a client side value");
                return ServerExpression.ConstantString (connection, result.Value.ToString ());
            }
            return ToExpression (result, typeof(string));
        }

        /// <summary>
        /// Compile ToString of a value computed on the server.
        /// </summary>
        Result CompileToString (MethodCallExpression node)
        {
            var type = node.Object.Type;
            var invariant = node.Arguments.Count == 1 && IsInvariantCulture (node.Arguments [0]);
            if (node.Arguments.Count != (invariant ? 1 : 0))
                throw Error (node, "ToString takes no arguments, or CultureInfo.InvariantCulture");
            if (!invariant && IsNumeric (Nullable.GetUnderlyingType (type) ?? type))
                throw CultureError (node);
            var instance = ToExpression (CompileNode (node.Object), type);
            // ToString of a null reference throws, as the server does
            if (Nullable.GetUnderlyingType (type) == null)
                return Result.FromExpression (ServerExpression.ConvertToString (connection, instance));
            return Result.FromExpression (StringOf (node.Object, instance));
        }

        static FunctionCompilationException CultureError (Expression node)
        {
            return Error (node, "formatting a number depends on the current culture. Call " +
                          "ToString (CultureInfo.InvariantCulture), or format an interpolated " +
                          "string with FormattableString.Invariant");
        }

        /// <summary>
        /// Compile a format string, which an interpolated string becomes, as the concatenation of
        /// its text and its arguments.
        /// </summary>
        /// <remarks>
        /// A placeholder must name an argument by position, with no alignment or format specifier.
        /// The arguments are evaluated once each, in order, as in C#.
        /// </remarks>
        Result CompileFormat (
            Expression node, Expression format, IList<Expression> arguments, bool invariant)
        {
            if (format.Type != typeof(string) || !IsClientSide (format))
                throw Error (node, "the format string must be a client side value");
            if (arguments.Count == 1 && arguments [0] is NewArrayExpression array &&
                array.Type == typeof(object[]))
                arguments = array.Expressions;
            else if (arguments.Any (argument => argument.Type == typeof(object[])))
                throw Error (node, "the arguments of a format string must be listed");
            var parts = ParseFormat (node, (string)Fold (format).Value, arguments.Count);
            var results = arguments.Select (CompileNode).ToList ();
            var held = results.Where (result => !result.IsValue).Select (result => result.Expression);
            return Result.FromExpression (WithVariables (held.ToList (), values => {
                var operands = new List<Result> ();
                var next = 0;
                foreach (var result in results)
                    operands.Add (result.IsValue ? result : Result.FromExpression (values [next++]));
                return ServerExpression.StringConcat (connection, parts.Select (
                    part => part.Item2 < 0
                        ? ServerExpression.ConstantString (connection, part.Item1)
                        : StringPart (arguments [part.Item2], operands [part.Item2], invariant)).ToList ());
            }));
        }

        /// <summary>
        /// The parts of a format string: text, or the position of an argument with -1 for text.
        /// </summary>
        static IList<Tuple<string, int>> ParseFormat (Expression node, string text, int count)
        {
            if (text == null)
                throw Error (node, "the format string is null");
            var parts = new List<Tuple<string, int>> ();
            var literal = new System.Text.StringBuilder ();
            for (int i = 0; i < text.Length; i++) {
                var c = text [i];
                if ((c == '{' || c == '}') && i + 1 < text.Length && text [i + 1] == c) {
                    literal.Append (c);
                    i++;
                    continue;
                }
                if (c == '}')
                    throw Error (node, "a '}' in a format string must be doubled");
                if (c != '{') {
                    literal.Append (c);
                    continue;
                }
                var end = text.IndexOf ('}', i);
                int position;
                if (end < 0 || !int.TryParse (
                        text.Substring (i + 1, end - i - 1),
                        System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out position))
                    throw Error (node, "format specifiers are not supported");
                if (position >= count)
                    throw Error (node, "the format item {" + position + "} is out of range, " +
                                 "as there " + (count == 1 ? "is 1 argument" : "are " + count + " arguments"));
                if (literal.Length > 0) {
                    parts.Add (Tuple.Create (literal.ToString (), -1));
                    literal.Clear ();
                }
                parts.Add (Tuple.Create ((string)null, position));
                i = end;
            }
            if (literal.Length > 0)
                parts.Add (Tuple.Create (literal.ToString (), -1));
            return parts;
        }

        Result CompileStringConcat (MethodCallExpression node)
        {
            IList<Expression> arguments = node.Arguments;
            // string.Concat of more than four values takes them in an array
            if (arguments.Count == 1 && arguments [0] is NewArrayExpression array)
                arguments = array.Expressions;
            else if (arguments.Count == 1 && arguments [0].Type != typeof(string) &&
                     arguments [0].Type != typeof(object)) {
                if (!typeof(IEnumerable<string>).IsAssignableFrom (arguments [0].Type))
                    throw Error (node, "string.Concat takes a collection of strings. " +
                                 "Convert the values with Select");
                return Result.FromExpression (ServerExpression.StringJoin (
                    connection, ServerExpression.ConstantString (connection, string.Empty),
                    StringElements (node, 0)));
            }
            var parts = new List<ServerExpression> ();
            foreach (var argument in arguments)
                parts.Add (StringPart (argument));
            return Result.FromExpression (
                ServerExpression.StringConcat (connection, parts));
        }

        ServerExpression StringPart (Expression node)
        {
            return StringPart (node, CompileNode (node), false);
        }

        /// <summary>
        /// Compile a part of a concatenation to a string valued server expression.
        /// </summary>
        /// <remarks>
        /// A value the client computed is formatted on the client, as C# formats it.
        /// </remarks>
        ServerExpression StringPart (Expression node, Result result, bool invariant)
        {
            if (result.IsValue) {
                var value = result.Value;
                if (value == null)
                    return ServerExpression.ConstantString (connection, string.Empty);
                if (ClientFunctionFinder.HasClientMethods (value.GetType ()))
                    throw Error (node, "cannot format a " + value.GetType ().Name + ", whose " +
                                 "ToString the compiler cannot see into; format it outside the lambda");
                var formattable = value as IFormattable;
                return ServerExpression.ConstantString (connection, invariant && formattable != null
                    ? formattable.ToString (null, System.Globalization.CultureInfo.InvariantCulture)
                    : value.ToString ());
            }
            var type = node.Type;
            for (var source = node; IsBoxing (source); source = ((UnaryExpression)source).Operand)
                type = ((UnaryExpression)source).Operand.Type;
            if (!invariant && IsNumeric (Nullable.GetUnderlyingType (type) ?? type))
                throw CultureError (node);
            if (type == typeof(string) && !CanBeNull (node, result.Expression))
                return result.Expression;
            return StringOf (node, result.Expression);
        }

        /// <summary>
        /// The string representation of a value, which is empty for a null as in C#.
        /// </summary>
        ServerExpression StringOf (Expression node, ServerExpression value)
        {
            if (!CanBeNull (node, value))
                return ServerExpression.ConvertToString (connection, value);
            return WithVariables (
                new [] { value },
                values => ServerExpression.Conditional (
                    connection, ServerExpression.IsNull (connection, values [0]),
                    ServerExpression.ConstantString (connection, ""),
                    ServerExpression.ConvertToString (connection, values [0])));
        }

        Result CompileEnumerableCall (MethodCallExpression node)
        {
            if (IsClientSide (node))
                return Fold (node);
            var name = node.Method.Name;
            if ((name == "Select" || name == "Where" || name == "SelectMany") &&
                node.Arguments.Count == 2 && LambdaParameterCount (node.Arguments [1]) == 2)
                throw Error (node, "the overload of " + name + " taking an element's index " +
                             "is not supported");
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
            case "OrderBy" when node.Arguments.Count == 2 || node.Arguments.Count == 3:
                CheckOrdinalOrder (node);
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
            case "First" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.First (connection, sourceExpression));
            case "Last" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.Last (connection, sourceExpression));
            case "ElementAt" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.ElementAt (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type)));
            case "Distinct" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.Distinct (connection, sourceExpression));
            case "Reverse" when node.Arguments.Count == 1:
                return Result.FromExpression (ServerExpression.Reverse (connection, sourceExpression));
            case "Union" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Union (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type)));
            case "Intersect" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Intersect (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type)));
            case "Except" when node.Arguments.Count == 2:
                return Result.FromExpression (ServerExpression.Except (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type)));
            case "Zip" when node.Arguments.Count == 3:
                return Result.FromExpression (ServerExpression.Zip (
                    connection, sourceExpression,
                    ToExpression (CompileNode (node.Arguments [1]), node.Arguments [1].Type),
                    CompileFunction (node.Arguments [2])));
            default:
                if (collectionOperations.Contains (name))
                    throw Error (node, "unsupported overload of the collection operation '" + name + "'");
                throw Error (node, "unsupported collection operation '" + name + "'");
            }
        }

        // The names of the Enumerable methods with an overload that compiles
        static readonly ISet<string> collectionOperations = new HashSet<string> {
            "Select", "Where", "Any", "All", "OrderBy", "Contains", "Count", "Sum", "Min", "Max",
            "Average", "ToList", "ToHashSet", "Concat", "Skip", "Take", "SelectMany", "ToDictionary",
            "First", "Last", "ElementAt", "Distinct", "Reverse", "Union", "Intersect", "Except", "Zip"
        };

        /// <summary>
        /// Check that OrderBy orders strings by character code, as the server does.
        /// </summary>
        /// <remarks>
        /// The default order of strings depends on the current culture.
        /// </remarks>
        void CheckOrdinalOrder (MethodCallExpression node)
        {
            var key = node.Method.GetGenericArguments () [1];
            if (node.Arguments.Count == 3) {
                if (key != typeof(string) || !IsClientSide (node.Arguments [2]) ||
                    !ReferenceEquals (Fold (node.Arguments [2]).Value, StringComparer.Ordinal))
                    throw Error (node, "the comparer given to OrderBy must be StringComparer.Ordinal");
            } else if (HoldsAString (key))
                throw Error (node, "the order of strings depends on the current culture. " +
                             "Order a string key with StringComparer.Ordinal");
        }

        static bool HoldsAString (Type type)
        {
            return type == typeof(string) ||
                (IsATuple (type) && type.GetGenericArguments ().Any (HoldsAString));
        }

        /// <summary>
        /// Compile a call to a procedure of a service, with its arguments.
        /// </summary>
        Result CompileRPC (MethodCallExpression node, RPCAttribute attribute)
        {
            var arguments = new List<Result> ();
            var parameters = new List<ParameterInfo> ();
            var methodParameters = node.Method.GetParameters ();
            int position = 0;
            if (ExpressionUtils.IsAClassMethod (node)) {
                arguments.Add (CompileNode (node.Object));
                parameters.Add (null);
            } else if (ExpressionUtils.IsAClassStaticMethod (node))
                position = 1;  // Skip the connection argument
            for (; position < node.Arguments.Count; position++) {
                arguments.Add (CompileNode (node.Arguments [position]));
                parameters.Add (methodParameters [position]);
            }
            return CallNode (attribute, arguments, parameters);
        }

        static int LambdaParameterCount (Expression node)
        {
            if (node is UnaryExpression quote && quote.NodeType == ExpressionType.Quote)
                node = quote.Operand;
            var lambda = node as LambdaExpression;
            return lambda == null ? -1 : lambda.Parameters.Count;
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
                    connection, parameter.Name, VariableType (parameter.Type, parameter));
                parameters [parameter] = serverParameter;
                functionParameters.Add (serverParameter);
            }
            try {
                var body = ToExpression (CompileNode (lambda.Body), lambda.Body.Type);
                return ServerExpression.Lambda (connection, functionParameters, body);
            } finally {
                foreach (var parameter in lambda.Parameters)
                    parameters.Remove (parameter);
            }
        }

        delegate ServerExpression BinaryOp (ServerExpression left, ServerExpression right);

        /// <summary>
        /// Whether an expression is the constant null, which the compiler wraps in a
        /// conversion when comparing it with a nullable value type.
        /// </summary>
        static bool IsNullConstant (Expression node)
        {
            while (node.NodeType == ExpressionType.Convert)
                node = ((UnaryExpression)node).Operand;
            var constant = node as ConstantExpression;
            return constant != null && constant.Value == null;
        }

        Result CompileBinary (BinaryExpression node)
        {
            // A client side left operand that decides the result leaves the right
            // operand unevaluated, as in C#
            if ((node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse) &&
                node.Method == null && IsClientSide (node.Left)) {
                var left = Fold (node.Left);
                if ((bool)left.Value == (node.NodeType == ExpressionType.OrElse))
                    return left;
                return CompileNode (node.Right);
            }
            if ((node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse) &&
                node.Method == null)
                return CompileShortCircuit (node);
            if (node.NodeType == ExpressionType.Coalesce)
                return CompileCoalesce (node);
            if (node.NodeType == ExpressionType.ArrayIndex) {
                var array = CompileNode (node.Left);
                var index = CompileNode (node.Right);
                if (array.IsValue && index.IsValue)
                    return Fold (node, new [] { node.Left, node.Right }, new [] { array, index });
                return Result.FromExpression (ServerExpression.Get (
                    connection, ToExpression (array, node.Left.Type), ToExpression (index, node.Right.Type)));
            }
            if ((node.NodeType == ExpressionType.Add || node.NodeType == ExpressionType.AddChecked) &&
                node.Type == typeof(string)) {
                if (IsClientSide (node))
                    return Fold (node);
                return Result.FromExpression (ServerExpression.StringConcat (
                    connection,
                    new List<ServerExpression> {
                        StringPart (node.Left), StringPart (node.Right)
                    }));
            }
            if ((node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual) &&
                !IsClientSide (node)) {
                var operand = IsNullConstant (node.Right) ? node.Left :
                              IsNullConstant (node.Left) ? node.Right : null;
                if (operand != null) {
                    var value = CompileNode (operand);
                    if (value.IsValue)
                        return Result.FromValue (
                            (value.Value == null) == (node.NodeType == ExpressionType.Equal));
                    // A value that cannot be null is still evaluated, for its effects
                    if (!CanBeNull (operand, value.Expression))
                        return Result.FromExpression (ServerExpression.Block (
                            connection, new List<ServerExpression> {
                                value.Expression,
                                ServerExpression.ConstantBool (
                                    connection, node.NodeType == ExpressionType.NotEqual)
                            }));
                    var isNull = ServerExpression.IsNull (connection, value.Expression);
                    return Result.FromExpression (
                        node.NodeType == ExpressionType.Equal ? isNull : ServerExpression.Not (connection, isNull));
                }
            }
            if ((node.NodeType == ExpressionType.AddChecked ||
                 node.NodeType == ExpressionType.SubtractChecked ||
                 node.NodeType == ExpressionType.MultiplyChecked) && !IsClientSide (node))
                throw CheckedError (node);
            if ((node.NodeType == ExpressionType.LessThan ||
                 node.NodeType == ExpressionType.LessThanOrEqual ||
                 node.NodeType == ExpressionType.GreaterThan ||
                 node.NodeType == ExpressionType.GreaterThanOrEqual) &&
                IsAStruct (Nullable.GetUnderlyingType (node.Left.Type) ?? node.Left.Type) &&
                !IsClientSide (node))
                throw Error (node, "structures cannot be ordered on the server. Compare their fields");
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
            case ExpressionType.And:
                op = (a, b) => ServerExpression.And (connection, a, b);
                break;
            case ExpressionType.Or:
                op = (a, b) => ServerExpression.Or (connection, a, b);
                break;
            case ExpressionType.ExclusiveOr:
                op = (a, b) => ServerExpression.ExclusiveOr (connection, a, b);
                break;
            default:
                if (IsClientSide (node))
                    return Fold (node);
                throw Error (node, "unsupported operator (" + node.NodeType + ")");
            }
            if (IsALiftedComparison (node))
                return CompileLiftedComparison (node, op);
            return Lifted (node, CompileNumericBinary (node, node.Left, node.Right, op));
        }

        /// <summary>
        /// The result of a lifted operator, at the nullable type C# gives it.
        /// </summary>
        /// <remarks>
        /// The server unwraps a nullable operand, and throws when it is null.
        /// </remarks>
        Result Lifted (Expression node, Result result)
        {
            if (result.IsValue || Nullable.GetUnderlyingType (node.Type) == null)
                return result;
            return Result.FromExpression (ServerExpression.Cast (
                connection, result.Expression, RemoteType (node.Type, node)));
        }

        /// <summary>
        /// Compile a conditional and or a conditional or, with a left operand computed on the server.
        /// </summary>
        Result CompileShortCircuit (BinaryExpression node)
        {
            var left = ToExpression (CompileNode (node.Left), node.Left.Type);
            var orElse = node.NodeType == ExpressionType.OrElse;
            ServerExpression right;
            try {
                right = ToExpression (CompileNode (node.Right), node.Right.Type);
            } catch (FunctionCompilationException exn) when (IsAClientSideError (exn)) {
                // The error is raised only when the right operand is evaluated
                var decided = ServerExpression.ConstantBool (connection, orElse);
                var error = ClientSideError (exn);
                return Result.FromExpression (orElse
                    ? ServerExpression.Conditional (connection, left, decided, error)
                    : ServerExpression.Conditional (connection, left, error, decided));
            }
            return Result.FromExpression (orElse
                ? ServerExpression.ConditionalOr (connection, left, right)
                : ServerExpression.ConditionalAnd (connection, left, right));
        }

        /// <summary>
        /// Whether the value a node is compiled to can be null.
        /// </summary>
        /// <remarks>
        /// A lambda parameter of a reference type is nullable, and a new value is not. For
        /// other reference types we use the nullability of the server's type.
        /// </remarks>
        bool CanBeNull (Expression node, ServerExpression value)
        {
            while (IsBoxing (node))
                node = ((UnaryExpression)node).Operand;
            if (node.Type.IsValueType)
                return Nullable.GetUnderlyingType (node.Type) != null;
            if (node is ParameterExpression)
                return true;
            if (node is NewExpression || node is ListInitExpression || node is NewArrayExpression)
                return false;
            return value.ReturnType.Nullable;
        }

        /// <summary>
        /// Whether a conversion boxes a value to object, as the C# compiler does around the
        /// arguments of string.Concat.
        /// </summary>
        static bool IsBoxing (Expression node)
        {
            return (node.NodeType == ExpressionType.Convert ||
                    node.NodeType == ExpressionType.ConvertChecked) &&
                node.Type == typeof(object);
        }

        /// <summary>
        /// Compile a ?? b.
        /// </summary>
        /// <remarks>
        /// The right operand is evaluated only when the left one is null.
        /// </remarks>
        Result CompileCoalesce (BinaryExpression node)
        {
            if (node.Conversion != null)
                throw Error (node, "a ?? with a conversion is not supported");
            var left = CompileNode (node.Left);
            if (left.IsValue) {
                if (left.Value == null)
                    return CompileNode (node.Right);
                // The value takes the type of the ??, as in int? ?? long
                var leftType = Nullable.GetUnderlyingType (node.Left.Type) ?? node.Left.Type;
                if (leftType == node.Type)
                    return left;
                return Fold (Expression.Convert (Expression.Constant (left.Value, leftType), node.Type));
            }
            if (!CanBeNull (node.Left, left.Expression))
                return left;
            var right = ToExpression (CompileBranch (node.Right), node.Right.Type);
            return Result.FromExpression (WithVariables (
                new [] { left.Expression },
                values => ServerExpression.Conditional (
                    connection, ServerExpression.IsNull (connection, values [0]),
                    right, NullableValue (values [0], node.Left.Type, node.Type))));
        }

        /// <summary>
        /// Compile GetValueOrDefault of a nullable value.
        /// </summary>
        /// <remarks>
        /// A default computed on the server is evaluated whether or not it is used, as an argument
        /// is in C#.
        /// </remarks>
        Result CompileGetValueOrDefault (MethodCallExpression node)
        {
            var nullable = CompileNode (node.Object);
            var fallback = node.Arguments.Count == 1
                ? CompileNode (node.Arguments [0])
                : Result.FromValue (Activator.CreateInstance (node.Type));
            if (nullable.IsValue && nullable.Value == null)
                return fallback;
            if (fallback.IsValue) {
                if (nullable.IsValue)
                    return nullable;
                var fallbackExpression = ToExpression (fallback, node.Type);
                return Result.FromExpression (WithVariables (
                    new [] { nullable.Expression },
                    values => ServerExpression.Conditional (
                        connection, ServerExpression.IsNull (connection, values [0]),
                        fallbackExpression, NullableValue (values [0], node.Object.Type, node.Type))));
            }
            if (nullable.IsValue)
                return Result.FromExpression (WithVariables (
                    new [] { fallback.Expression }, values => ToExpression (nullable, node.Type)));
            return Result.FromExpression (WithVariables (
                new [] { nullable.Expression, fallback.Expression },
                values => ServerExpression.Conditional (
                    connection, ServerExpression.IsNull (connection, values [0]),
                    values [1], NullableValue (values [0], node.Object.Type, node.Type))));
        }

        /// <summary>
        /// The value a nullable holds, at the type a C# expression takes it at.
        /// </summary>
        ServerExpression NullableValue (ServerExpression nullable, Type nullableType, Type type)
        {
            if (Nullable.GetUnderlyingType (nullableType) != null && type.IsValueType &&
                type != nullableType)
                return ServerExpression.Cast (connection, nullable, RemoteType (type, null));
            return nullable;
        }

        /// <summary>
        /// An expression over values held in variables, so each is evaluated once, in order.
        /// </summary>
        ServerExpression WithVariables (
            IList<ServerExpression> values, Func<IList<ServerExpression>, ServerExpression> body,
            string name = "value")
        {
            if (values.Count == 0)
                return body (values);
            var variables = new List<ServerExpression> ();
            var statements = new List<ServerExpression> ();
            foreach (var value in values) {
                var variable = ServerExpression.Variable (
                    connection, name + variables.Count, value.ReturnType);
                variables.Add (variable);
                statements.Add (ServerExpression.Assign (connection, variable, value));
            }
            statements.Add (body (variables));
            return ServerExpression.BlockWithVariables (connection, variables, statements);
        }

        static bool IsALiftedComparison (BinaryExpression node)
        {
            return node.IsLifted && !node.IsLiftedToNull && node.Method == null && (
                node.NodeType == ExpressionType.GreaterThan ||
                node.NodeType == ExpressionType.GreaterThanOrEqual ||
                node.NodeType == ExpressionType.LessThan ||
                node.NodeType == ExpressionType.LessThanOrEqual);
        }

        /// <summary>
        /// Compile a comparison of nullable numbers, which is false when either is null, as in C#.
        /// </summary>
        /// <remarks>
        /// Both operands are evaluated once, before the comparison.
        /// </remarks>
        Result CompileLiftedComparison (BinaryExpression node, BinaryOp op)
        {
            var operands = new [] { node.Left, node.Right };
            var results = new [] { CompileNode (node.Left), CompileNode (node.Right) };
            if (results.All (result => result.IsValue))
                return Fold (node, operands, results);
            var held = results.Where (result => !result.IsValue).Select (result => result.Expression);
            return Result.FromExpression (WithVariables (held.ToList (), values => {
                if (results.Any (result => result.IsValue && result.Value == null))
                    return ServerExpression.ConstantBool (connection, false);
                var compared = new ServerExpression [2];
                var tests = new List<ServerExpression> ();
                var next = 0;
                for (int i = 0; i < 2; i++) {
                    if (results [i].IsValue) {
                        compared [i] = ToExpression (results [i], operands [i].Type);
                        continue;
                    }
                    compared [i] = values [next++];
                    if (Nullable.GetUnderlyingType (operands [i].Type) != null)
                        tests.Add (ServerExpression.Not (
                            connection, ServerExpression.IsNull (connection, compared [i])));
                }
                var expression = op (compared [0], compared [1]);
                for (int i = tests.Count - 1; i >= 0; i--)
                    expression = ServerExpression.ConditionalAnd (connection, tests [i], expression);
                return expression;
            }));
        }

        Result CompileNumericBinary (Expression node, Expression leftNode, Expression rightNode, BinaryOp op)
        {
            return CombineBinary (
                node, leftNode, rightNode, CompileNode (leftNode), CompileNode (rightNode), op);
        }

        Result CombineBinary (
            Expression node, Expression leftNode, Expression rightNode,
            Result left, Result right, BinaryOp op)
        {
            if (left.IsValue && right.IsValue)
                return Fold (node, new [] { leftNode, rightNode }, new [] { left, right });
            return Result.FromExpression (op (
                ToExpression (left, leftNode.Type), ToExpression (right, rightNode.Type)));
        }

        Result CompileUnary (UnaryExpression node)
        {
            switch (node.NodeType) {
            case ExpressionType.Quote:
            case ExpressionType.UnaryPlus:
                return CompileNode (node.Operand);
            case ExpressionType.Not: {
                    var operand = CompileNode (node.Operand);
                    if (operand.IsValue)
                        return Fold (node, new [] { node.Operand }, new [] { operand });
                    return Lifted (node, Result.FromExpression (ServerExpression.Not (
                        connection, ToExpression (operand, node.Operand.Type))));
                }
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked: {
                    var operand = CompileNode (node.Operand);
                    if (operand.IsValue)
                        return Fold (node, new [] { node.Operand }, new [] { operand });
                    if (node.NodeType == ExpressionType.NegateChecked)
                        throw CheckedError (node);
                    return Lifted (node, Result.FromExpression (ServerExpression.Negate (
                        connection, ToExpression (operand, node.Operand.Type))));
                }
            case ExpressionType.OnesComplement: {
                    var operand = CompileNode (node.Operand);
                    if (operand.IsValue)
                        return Fold (node, new [] { node.Operand }, new [] { operand });
                    return Lifted (node, Result.FromExpression (ServerExpression.Not (
                        connection, ToExpression (operand, node.Operand.Type))));
                }
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked: {
                    if (IsBoxing (node) || IsReferenceUpcast (node))
                        return CompileNode (node.Operand);
                    var operand = CompileNode (node.Operand);
                    if (operand.IsValue)
                        return Fold (node, new [] { node.Operand }, new [] { operand });
                    var target = Nullable.GetUnderlyingType (node.Type) ?? node.Type;
                    var source = Nullable.GetUnderlyingType (node.Operand.Type) ?? node.Operand.Type;
                    if (node.NodeType == ExpressionType.ConvertChecked && !IsWidening (source, target))
                        throw CheckedError (node);
                    // A cast to a nullable type keeps a null
                    return Result.FromExpression (ServerExpression.Cast (
                        connection,
                        ToExpression (operand, node.Operand.Type),
                        RemoteType (node.Type, node)));
                }
            default:
                if (IsClientSide (node))
                    return Fold (node);
                throw Error (node, "unsupported operator (" + node.NodeType + ")");
            }
        }

        /// <summary>
        /// Whether a conversion views a reference as one of its base types or interfaces, such as
        /// a set as an IEnumerable, which leaves the value as it is.
        /// </summary>
        static bool IsReferenceUpcast (UnaryExpression node)
        {
            return node.Method == null && !node.Type.IsValueType && !node.Operand.Type.IsValueType &&
                node.Type.IsAssignableFrom (node.Operand.Type);
        }

        // Server arithmetic wraps on overflow, so an overflow check cannot be kept
        static FunctionCompilationException CheckedError (Expression node)
        {
            return Error (node, "checked arithmetic is not supported on the server, " +
                          "where integer arithmetic wraps on overflow");
        }

        /// <summary>
        /// Whether a numeric conversion keeps every value, so it cannot overflow.
        /// </summary>
        /// <remarks>
        /// An enumeration converts as its underlying type.
        /// </remarks>
        static bool IsWidening (Type from, Type to)
        {
            if (from.IsEnum)
                from = Enum.GetUnderlyingType (from);
            if (to.IsEnum)
                to = Enum.GetUnderlyingType (to);
            if (from == to || to == typeof(double))
                return true;
            if (to == typeof(float))
                return from != typeof(double);
            if (to == typeof(long))
                return from == typeof(int) || from == typeof(uint);
            if (to == typeof(ulong))
                return from == typeof(uint);
            return false;
        }

        Result CompileConditional (ConditionalExpression node)
        {
            var condition = CompileNode (node.Test);
            if (condition.IsValue)
                return CompileNode ((bool)condition.Value ? node.IfTrue : node.IfFalse);
            var branches = new [] { node.IfTrue, node.IfFalse };
            var results = branches.Select (CompileBranch).ToList ();
            var sets = branches.Select ((branch, i) => IsASet (branch, results [i])).ToList ();
            var expressions = new List<ServerExpression> ();
            for (int i = 0; i < 2; i++) {
                var expression = ToExpression (results [i], branches [i].Type);
                // A set and a list viewed as one sequence type are both chosen as a list
                if (sets [i] && !sets [1 - i])
                    expression = ServerExpression.ToList (connection, expression);
                expressions.Add (expression);
            }
            return Result.FromExpression (ServerExpression.Conditional (
                connection, condition.Expression, expressions [0], expressions [1]));
        }

        /// <summary>
        /// Whether a node evaluates to a set, which a reference upcast leaves a set.
        /// </summary>
        static bool IsASet (Expression node, Result result)
        {
            Type type;
            if (result.IsValue) {
                if (result.Value == null)
                    return false;
                type = result.Value.GetType ();
            } else {
                while ((node.NodeType == ExpressionType.Convert ||
                        node.NodeType == ExpressionType.ConvertChecked) &&
                       IsReferenceUpcast ((UnaryExpression)node))
                    node = ((UnaryExpression)node).Operand;
                type = node.Type;
            }
            return new [] { type }.Concat (type.GetInterfaces ()).Any (
                candidate => IsGeneric (candidate, typeof(ISet<>)));
        }

        /// <summary>
        /// Compile a branch of a conditional the server chooses between.
        /// </summary>
        /// <remarks>
        /// A branch with a client side sub-expression that fails to evaluate raises its error
        /// only when it is chosen.
        /// </remarks>
        Result CompileBranch (Expression node)
        {
            try {
                return CompileNode (node);
            } catch (FunctionCompilationException exn) when (IsAClientSideError (exn)) {
                return Result.FromExpression (ClientSideError (exn));
            }
        }

        /// <summary>
        /// Whether an exception is an error evaluating a client side sub-expression, which
        /// carries the exception the evaluation raised.
        /// </summary>
        static bool IsAClientSideError (FunctionCompilationException exn)
        {
            return exn.InnerException != null;
        }

        /// <summary>
        /// A throw of the error a client side node raised, for the server to raise when the
        /// node is evaluated.
        /// </summary>
        ServerExpression ClientSideError (FunctionCompilationException exn)
        {
            var cause = exn.InnerException.InnerException ?? exn.InnerException;
            return ServerExpression.Throw (
                connection, "KRPC", "InvalidOperationException", ServerExpression.ConstantString (
                    connection, "Error evaluating client side sub-expression: " + cause.Message));
        }

        static bool IsATuple (Type type)
        {
            return type.Name.StartsWith ("Tuple`", StringComparison.Ordinal) &&
                type.Namespace == "System";
        }

        /// <summary>
        /// Compile the construction of a tuple, by its constructor or by Tuple.Create.
        /// </summary>
        Result CompileTuple (Expression node, IList<Expression> arguments)
        {
            if (arguments.Count > 7)
                throw Error (node, "a tuple of more than seven values is not supported");
            var results = arguments.Select (CompileNode).ToList ();
            if (results.All (result => result.IsValue))
                return Fold (node, arguments, results);
            var elements = new List<ServerExpression> ();
            for (int i = 0; i < results.Count; i++)
                elements.Add (ToExpression (results [i], arguments [i].Type));
            return Result.FromExpression (ServerExpression.CreateTuple (connection, elements));
        }

        /// <summary>
        /// Compile the construction of a structure.
        /// </summary>
        /// <remarks>
        /// The generated constructor takes the fields in the order the structure declares them,
        /// which is the order the server builds one from.
        /// </remarks>
        Result CompileStruct (NewExpression node)
        {
            var results = node.Arguments.Select (CompileNode).ToList ();
            if (results.All (result => result.IsValue))
                return Fold (node, node.Arguments, results);
            var fields = new List<ServerExpression> ();
            for (int i = 0; i < results.Count; i++)
                fields.Add (ToExpression (results [i], node.Arguments [i].Type));
            return Result.FromExpression (ServerExpression.CreateStruct (
                connection, RemoteType (node.Type, node), fields));
        }

        /// <summary>
        /// Compile the initializer of a List, a HashSet or a Dictionary.
        /// </summary>
        Result CompileListInit (ListInitExpression node)
        {
            var definition = node.Type.IsGenericType ? node.Type.GetGenericTypeDefinition () : null;
            if (definition != typeof(List<>) && definition != typeof(HashSet<>) &&
                definition != typeof(Dictionary<,>))
                throw Error (node, "only a List, a HashSet or a Dictionary can be initialized");
            if (node.NewExpression.Arguments.Count > 0)
                throw Error (node, "a collection initializer cannot pass constructor arguments");
            if (definition == typeof(Dictionary<,>))
                return CompileDictionaryInit (node);
            var elements = new List<Result> ();
            var types = new List<Type> ();
            var operands = new List<Expression> ();
            foreach (var initializer in node.Initializers) {
                if (initializer.Arguments.Count != 1)
                    throw Error (node, "unsupported collection initializer");
                elements.Add (CompileNode (initializer.Arguments [0]));
                types.Add (initializer.Arguments [0].Type);
                operands.Add (initializer.Arguments [0]);
            }
            if (elements.All (element => element.IsValue))
                return Fold (node, operands, elements);
            var expressions = new List<ServerExpression> ();
            for (int i = 0; i < elements.Count; i++)
                expressions.Add (ToExpression (elements [i], types [i]));
            if (definition == typeof(HashSet<>))
                return Result.FromExpression (ServerExpression.CreateSet (
                    connection, new HashSet<ServerExpression> (expressions)));
            return Result.FromExpression (ServerExpression.CreateList (connection, expressions));
        }

        /// <summary>
        /// Compile a dictionary initializer.
        /// </summary>
        /// <remarks>
        /// The keys and values are evaluated in order, and a repeated key throws, as Dictionary.Add
        /// does.
        /// </remarks>
        Result CompileDictionaryInit (ListInitExpression node)
        {
            var operands = new List<Expression> ();
            var results = new List<Result> ();
            foreach (var initializer in node.Initializers) {
                if (initializer.Arguments.Count != 2)
                    throw Error (node, "unsupported dictionary initializer");
                operands.AddRange (initializer.Arguments);
                results.Add (CompileNode (initializer.Arguments [0]));
                results.Add (CompileNode (initializer.Arguments [1]));
            }
            if (results.All (result => result.IsValue))
                return Fold (node, operands, results);
            var types = node.Type.GetGenericArguments ();
            var held = results.Where (result => !result.IsValue).Select (result => result.Expression);
            return Result.FromExpression (WithVariables (held.ToList (), values => {
                var keys = new List<ServerExpression> ();
                var entries = new List<ServerExpression> ();
                var next = 0;
                for (int i = 0; i < results.Count; i++) {
                    var value = results [i].IsValue
                        ? ToExpression (results [i], types [i % 2])
                        : values [next++];
                    (i % 2 == 0 ? keys : entries).Add (value);
                }
                var dictionary = ServerExpression.CreateDictionary (connection, keys, entries);
                return WithVariables (new [] { dictionary }, created => ServerExpression.Conditional (
                    connection,
                    ServerExpression.NotEqual (
                        connection, ServerExpression.Count (connection, created [0]),
                        ServerExpression.ConstantInt (connection, keys.Count)),
                    ServerExpression.Throw (
                        connection, "KRPC", "ArgumentException", ServerExpression.ConstantString (
                            connection, "An item with the same key has already been added.")),
                    created [0]), "dictionary");
            }));
        }

        Result CallNode (
            RPCAttribute attribute, IList<Result> arguments,
            IList<ParameterInfo> parameters = null)
        {
            var call = new Schema.KRPC.ProcedureCall ();
            call.Service = attribute.Service;
            call.Procedure = attribute.Procedure;
            var expressions = new Dictionary<int, ServerExpression> ();
            for (int position = 0; position < arguments.Count; position++) {
                var argument = arguments [position];
                if (argument.IsValue && argument.Value == null) {
                    // A null default stands for the server's default, so the argument is
                    // left out. Any other null is passed in the call itself.
                    var parameter = parameters == null ? null : parameters [position];
                    if (parameter == null || !parameter.HasDefaultValue || parameter.DefaultValue != null)
                        call.Arguments.Add (new Schema.KRPC.Argument {
                            Position = (uint)position,
                            IsNull = true
                        });
                    continue;
                }
                expressions [position] = ToExpression (argument, null);
            }
            return Result.FromExpression (
                ServerExpression.CallWithArguments (connection, call, expressions));
        }

        static RPCAttribute GetRPCAttribute (MemberInfo member)
        {
            var attributes = member.GetCustomAttributes (typeof(RPCAttribute), false);
            return attributes.Length == 1 ? (RPCAttribute)attributes [0] : null;
        }

        /// <summary>
        /// Evaluate a node on the client, over the values its operands were compiled to.
        /// </summary>
        static Result Fold (
            Expression node, IList<Expression> operands = null, IList<Result> values = null)
        {
            if (operands != null) {
                var replacements = new Dictionary<Expression, Expression> ();
                for (int i = 0; i < operands.Count; i++)
                    replacements [operands [i]] = Expression.Constant (
                        values [i].Value, operands [i].Type);
                node = new OperandReplacer (replacements).Visit (node);
            }
            // Such a call is evaluated once, here, so a remote call it makes would not run
            // on each evaluation of the function
            var finder = new ClientFunctionFinder ();
            finder.Visit (node);
            if (finder.Found != null)
                throw Error (finder.Found, finder.Message);
            try {
                return Result.FromValue (
                    Expression.Lambda (node).Compile ().DynamicInvoke ());
            } catch (Exception exception) {
                var message = exception.InnerException != null
                    ? exception.InnerException.Message : exception.Message;
                throw new FunctionCompilationException (
                    "Cannot compile expression: error evaluating client side sub-expression: " + message,
                    exception);
            }
        }

        /// <summary>
        /// Convert a compiled result to a server side expression, embedding values as constants.
        /// </summary>
        /// <remarks>
        /// The declared type takes precedence over the value's runtime type, when given.
        /// </remarks>
        ServerExpression ToExpression (Result result, Type type)
        {
            if (!result.IsValue)
                return result.Expression;
            var value = result.Value;
            if (value == null) {
                if (type == null)
                    throw new FunctionCompilationException (
                        "Cannot compile expression: cannot determine the type of a null value");
                return ServerExpression.ConstantNull (
                    connection, RemoteType (Nullable.GetUnderlyingType (type) ?? type, null));
            }
            type = type ?? value.GetType ();
            var underlying = Nullable.GetUnderlyingType (type);
            if (underlying != null)
                return ServerExpression.Cast (
                    connection, ToExpression (result, underlying), RemoteType (type, null));
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
            if (type == typeof(long))
                return ServerExpression.ConstantLong (connection, (long)value);
            if (type == typeof(uint))
                return ServerExpression.ConstantUInt (connection, (uint)value);
            if (type == typeof(ulong))
                return ServerExpression.ConstantULong (connection, (ulong)value);
            if (type == typeof(byte[]))
                return ServerExpression.ConstantBytes (connection, (byte[])value);
            if (value is RemoteObject remoteObject)
                return ServerExpression.ConstantObject (connection, RemoteObject.GetObjectId (remoteObject));
            if (type.IsEnum)
                return ServerExpression.Cast (
                    connection,
                    ServerExpression.ConstantInt (connection, Convert.ToInt32 (value, null)),
                    RemoteType (type, null));
            if (value is IDictionary dictionary) {
                var types = GenericArguments (value.GetType (), typeof(IDictionary<,>));
                var keys = new List<ServerExpression> ();
                var values = new List<ServerExpression> ();
                foreach (DictionaryEntry entry in dictionary) {
                    keys.Add (ToExpression (
                        Result.FromValue (entry.Key), ElementType (entry.Key, types [0])));
                    values.Add (ToExpression (
                        Result.FromValue (entry.Value), ElementType (entry.Value, types [1])));
                }
                if (keys.Count == 0)
                    return ServerExpression.CreateEmptyDictionary (
                        connection, RemoteType (types [0], null), RemoteType (types [1], null));
                return ServerExpression.CreateDictionary (connection, keys, values);
            }
            if (value is IEnumerable enumerable && !(value is string)) {
                var elementType = GenericArguments (value.GetType (), typeof(IEnumerable<>)) [0];
                var elements = new List<ServerExpression> ();
                foreach (var element in enumerable)
                    elements.Add (ToExpression (
                        Result.FromValue (element), ElementType (element, elementType)));
                // The declared type of a set can be an interface, such as ISet<T>
                var isASet = value.GetType ().GetInterfaces ().Any (
                    candidate => candidate.IsGenericType &&
                        candidate.GetGenericTypeDefinition () == typeof(ISet<>));
                if (elements.Count == 0) {
                    var element = RemoteType (elementType, null);
                    return isASet
                        ? ServerExpression.CreateEmptySet (connection, element)
                        : ServerExpression.CreateEmptyList (connection, element);
                }
                if (isASet)
                    return ServerExpression.CreateSet (
                        connection, new HashSet<ServerExpression> (elements));
                return ServerExpression.CreateList (connection, elements);
            }
            if (IsATuple (type)) {
                var types = TupleTypes (type, null);
                var elements = new List<ServerExpression> ();
                for (int i = 0; i < types.Length; i++)
                    elements.Add (ToExpression (
                        Result.FromValue (type.GetProperty ("Item" + (i + 1)).GetValue (value)),
                        types [i]));
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
            throw new FunctionCompilationException (
                "Cannot compile expression: cannot use a value of type " + type.Name + " in an expression");
        }

        /// <summary>
        /// The type a captured element is built at: the element type of its collection when
        /// that type is nullable or the element is null, and otherwise its own type.
        /// </summary>
        static Type ElementType (object element, Type elementType)
        {
            if (element == null || Nullable.GetUnderlyingType (elementType) != null)
                return elementType;
            return null;
        }

        /// <summary>
        /// The element types of a tuple type.
        /// </summary>
        /// <remarks>
        /// The server has no form of the eighth element, which C# nests in a tuple of its own.
        /// </remarks>
        static Type[] TupleTypes (Type type, Expression node)
        {
            var types = type.GetGenericArguments ();
            if (types.Length > 7)
                throw Error (node, "a tuple of more than seven values is not supported");
            return types;
        }

        /// <summary>
        /// The type arguments of the generic interface a type implements, such as the
        /// element type of an IEnumerable.
        /// </summary>
        static Type[] GenericArguments (Type type, Type definition)
        {
            var match = new [] { type }.Concat (type.GetInterfaces ()).First (
                candidate => candidate.IsGenericType &&
                    candidate.GetGenericTypeDefinition () == definition);
            return match.GetGenericArguments ();
        }

        /// <summary>
        /// The KRPC.Type object describing a CLR type, built once per connection.
        /// </summary>
        ServerType RemoteType (Type type, Expression node)
        {
            ServerType remote;
            if (remoteTypes.Types.TryGetValue (type, out remote))
                return remote;
            remote = BuildRemoteType (type, node);
            remoteTypes.Types [type] = remote;
            return remote;
        }

        /// <summary>
        /// The KRPC.Type object for a local or a parameter of a CLR type, which is nullable
        /// for a reference type as C# allows a null in one.
        /// </summary>
        ServerType VariableType (Type type, Expression node)
        {
            if (type.IsValueType)
                return RemoteType (type, node);
            ServerType remote;
            if (remoteTypes.NullableTypes.TryGetValue (type, out remote))
                return remote;
            remote = ServerType.NullableType (connection, RemoteType (type, node));
            remoteTypes.NullableTypes [type] = remote;
            return remote;
        }

        ServerType BuildRemoteType (Type type, Expression node)
        {
            var underlying = Nullable.GetUnderlyingType (type);
            if (underlying != null)
                return ServerType.NullableType (connection, RemoteType (underlying, node));
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
            if (IsATuple (type))
                return ServerType.TupleType (
                    connection, TupleTypes (type, node).Select (t => RemoteType (t, node)).ToList ());
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
        /// The properties carrying the fields of a structure, in the order the type declares them,
        /// which is the order the server builds one from.
        /// </summary>
        /// <remarks>
        /// Reflection does not promise an order for the properties of a type, so they are ordered
        /// by metadata token, which is assigned in declaration order.
        /// </remarks>
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

        /// <summary>
        /// Whether a C# type holds the values of a type the server describes.
        /// </summary>
        internal static bool Describes (Type type, Schema.KRPC.Type described)
        {
            type = Nullable.GetUnderlyingType (type) ?? type;
            var elements = described.Types_;
            switch ((Services.KRPC.TypeCode)(int)described.Code) {
            case Services.KRPC.TypeCode.Double:
                return type == typeof(double);
            case Services.KRPC.TypeCode.Float:
                return type == typeof(float);
            case Services.KRPC.TypeCode.SInt32:
                return type == typeof(int);
            case Services.KRPC.TypeCode.SInt64:
                return type == typeof(long);
            case Services.KRPC.TypeCode.UInt32:
                return type == typeof(uint);
            case Services.KRPC.TypeCode.UInt64:
                return type == typeof(ulong);
            case Services.KRPC.TypeCode.Bool:
                return type == typeof(bool);
            case Services.KRPC.TypeCode.String:
                return type == typeof(string);
            case Services.KRPC.TypeCode.Bytes:
                return type == typeof(byte[]);
            case Services.KRPC.TypeCode.Class:
                return typeof(RemoteObject).IsAssignableFrom (type) && IsNamed (type, described);
            case Services.KRPC.TypeCode.Enumeration:
                return type.IsEnum && IsNamed (type, described);
            case Services.KRPC.TypeCode.Struct:
                return IsAStruct (type) && IsNamed (type, described);
            case Services.KRPC.TypeCode.Tuple:
                return type.IsGenericType &&
                    type.Name.StartsWith ("Tuple`", StringComparison.Ordinal) &&
                    DescribesAll (type.GetGenericArguments (), elements);
            case Services.KRPC.TypeCode.List:
                return IsGeneric (type, typeof(IList<>), typeof(List<>)) &&
                    DescribesAll (type.GetGenericArguments (), elements);
            case Services.KRPC.TypeCode.Set:
                return IsGeneric (type, typeof(ISet<>), typeof(HashSet<>)) &&
                    DescribesAll (type.GetGenericArguments (), elements);
            case Services.KRPC.TypeCode.Dictionary:
                return IsGeneric (type, typeof(IDictionary<,>), typeof(Dictionary<,>)) &&
                    DescribesAll (type.GetGenericArguments (), elements);
            default:
                return false;
            }
        }

        /// <summary>
        /// The type a value the server describes is decoded at, for a requested type: the
        /// requested type when it describes the value, or a type it can hold, or null.
        /// </summary>
        internal static Type DecodedType (Type requested, Schema.KRPC.Type described)
        {
            if (Describes (requested, described))
                return requested;
            var type = NaturalType (described);
            return type != null && requested.IsAssignableFrom (type) ? type : null;
        }

        /// <summary>
        /// The C# type of a value the server describes, with interfaces for collections, or
        /// null for a type with no C# form.
        /// </summary>
        internal static Type NaturalType (Schema.KRPC.Type described)
        {
            var elements = described.Types_.Select (ElementNaturalType).ToArray ();
            if (elements.Any (element => element == null))
                return null;
            switch ((Services.KRPC.TypeCode)(int)described.Code) {
            case Services.KRPC.TypeCode.Double:
                return typeof(double);
            case Services.KRPC.TypeCode.Float:
                return typeof(float);
            case Services.KRPC.TypeCode.SInt32:
                return typeof(int);
            case Services.KRPC.TypeCode.SInt64:
                return typeof(long);
            case Services.KRPC.TypeCode.UInt32:
                return typeof(uint);
            case Services.KRPC.TypeCode.UInt64:
                return typeof(ulong);
            case Services.KRPC.TypeCode.Bool:
                return typeof(bool);
            case Services.KRPC.TypeCode.String:
                return typeof(string);
            case Services.KRPC.TypeCode.Bytes:
                return typeof(byte[]);
            case Services.KRPC.TypeCode.Class:
            case Services.KRPC.TypeCode.Enumeration:
            case Services.KRPC.TypeCode.Struct:
                return ServiceType (described);
            case Services.KRPC.TypeCode.Tuple:
                return elements.Length > 0 && elements.Length < 8
                    ? Type.GetType ("System.Tuple`" + elements.Length).MakeGenericType (elements)
                    : null;
            case Services.KRPC.TypeCode.List:
                return typeof(IList<>).MakeGenericType (elements);
            case Services.KRPC.TypeCode.Set:
                return typeof(ISet<>).MakeGenericType (elements);
            case Services.KRPC.TypeCode.Dictionary:
                return typeof(IDictionary<,>).MakeGenericType (elements);
            default:
                return null;
            }
        }

        static Type ElementNaturalType (Schema.KRPC.Type described)
        {
            var type = NaturalType (described);
            if (type != null && type.IsValueType && described.Nullable)
                return typeof(Nullable<>).MakeGenericType (type);
            return type;
        }

        /// <summary>
        /// The generated type of a class, enumeration or structure, from a loaded assembly.
        /// </summary>
        static Type ServiceType (Schema.KRPC.Type described)
        {
            var name = "KRPC.Client.Services." + described.Service + "." + described.Name;
            return AppDomain.CurrentDomain.GetAssemblies ()
                .Select (assembly => assembly.GetType (name))
                .FirstOrDefault (type => type != null);
        }

        static bool IsNamed (Type type, Schema.KRPC.Type described)
        {
            return ServiceName (type) == described.Service && type.Name == described.Name;
        }

        static bool IsGeneric (Type type, params Type[] definitions)
        {
            return type.IsGenericType && definitions.Contains (type.GetGenericTypeDefinition ());
        }

        static bool DescribesAll (Type[] types, IList<Schema.KRPC.Type> described)
        {
            if (types.Length != described.Count)
                return false;
            for (int i = 0; i < types.Length; i++) {
                // A nullable element is encoded with a flag saying whether it is null
                var nullable = Nullable.GetUnderlyingType (types [i]) != null;
                if (!Describes (types [i], described [i]) ||
                    (types [i].IsValueType && nullable != described [i].Nullable))
                    return false;
            }
            return true;
        }

        static FunctionCompilationException Error (Expression node, string message)
        {
            var location = node == null ? string.Empty : " in '" + node + "'";
            return new FunctionCompilationException (
                "Cannot compile expression: " + message + location);
        }
    }
}
