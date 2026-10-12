using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// Prints an expression tree as indented text, for golden tests.
    /// </summary>
    /// <remarks>
    /// Parameters, variables, state variables and labels are numbered in order of first
    /// appearance, with a name that is not an identifier quoted. Types print with their
    /// namespace. Numbers print in the invariant culture. A collection, tuple or structure
    /// constant prints its values, and a type spec prints its type with a ? at each
    /// nullable position. A type prints as typeof, a procedure prints its name, and any
    /// other object constant prints as its type name.
    /// </remarks>
    static class ExpressionTreePrinter
    {
        /// <summary>
        /// The expression tree as indented text, one node per line.
        /// </summary>
        internal static string Print (LinqExpression expression)
        {
            if (ReferenceEquals (expression, null))
                throw new ArgumentNullException (nameof (expression));
            var printer = new Printer ();
            printer.PrintNode (expression, 0);
            return printer.ToString ();
        }

        sealed class Printer
        {
            readonly StringBuilder result = new StringBuilder ();
            readonly Dictionary<ParameterExpression, int> parameterIds =
                new Dictionary<ParameterExpression, int> ();
            readonly Dictionary<LabelTarget, int> labelIds =
                new Dictionary<LabelTarget, int> ();
            readonly Dictionary<Expression.IStateBox, int> stateIds =
                new Dictionary<Expression.IStateBox, int> ();

            public override string ToString ()
            {
                return result.ToString ();
            }

            public void PrintNode (LinqExpression expression, int depth)
            {
                result.Append (' ', depth * 2);
                var binary = expression as BinaryExpression;
                if (binary != null) {
                    if (binary.Conversion != null)
                        throw Unsupported ("the conversion of a " + expression.NodeType + " node");
                    Line (expression, MethodRepr (binary.Method));
                    PrintNode (binary.Left, depth + 1);
                    PrintNode (binary.Right, depth + 1);
                    return;
                }
                var unary = expression as UnaryExpression;
                if (unary != null) {
                    Line (expression, MethodRepr (unary.Method));
                    // A rethrow has no operand
                    if (unary.Operand != null)
                        PrintNode (unary.Operand, depth + 1);
                    return;
                }
                var constant = expression as ConstantExpression;
                if (constant != null) {
                    Line (expression, ConstantRepr (constant.Value));
                    return;
                }
                var parameter = expression as ParameterExpression;
                if (parameter != null) {
                    Line (expression, ParameterRepr (parameter));
                    return;
                }
                var call = expression as MethodCallExpression;
                if (call != null) {
                    Line (expression, (call.Object == null ? "static " : string.Empty) +
                        FormatType (call.Method.DeclaringType) + "." + call.Method.Name);
                    if (call.Object != null)
                        PrintNode (call.Object, depth + 1);
                    foreach (var argument in call.Arguments)
                        PrintNode (argument, depth + 1);
                    return;
                }
                var state = Expression.AsStateVariable (expression);
                if (state != null) {
                    Line (expression, "state " + StateRepr (state));
                    return;
                }
                var member = expression as MemberExpression;
                if (member != null) {
                    Line (expression, FormatType (member.Member.DeclaringType) + "." + member.Member.Name);
                    if (member.Expression != null)
                        PrintNode (member.Expression, depth + 1);
                    return;
                }
                var newExpr = expression as NewExpression;
                if (newExpr != null) {
                    Line (expression, string.Empty);
                    foreach (var argument in newExpr.Arguments)
                        PrintNode (argument, depth + 1);
                    return;
                }
                var memberInit = expression as MemberInitExpression;
                if (memberInit != null) {
                    Line (expression, string.Empty);
                    PrintNode (memberInit.NewExpression, depth + 1);
                    foreach (var binding in memberInit.Bindings) {
                        var assignment = binding as MemberAssignment;
                        if (assignment == null)
                            throw Unsupported ("a " + binding.BindingType + " binding");
                        result.Append (' ', (depth + 1) * 2);
                        result.Append (binding.BindingType);
                        result.Append (' ');
                        result.Append (FormatType (binding.Member.DeclaringType));
                        result.Append ('.');
                        result.Append (binding.Member.Name);
                        result.Append ('\n');
                        PrintNode (assignment.Expression, depth + 2);
                    }
                    return;
                }
                var newArray = expression as NewArrayExpression;
                if (newArray != null) {
                    Line (expression, string.Empty);
                    foreach (var element in newArray.Expressions)
                        PrintNode (element, depth + 1);
                    return;
                }
                var lambda = expression as LambdaExpression;
                if (lambda != null) {
                    Line (expression, "(" + string.Join (
                        ", ", lambda.Parameters.Select (ParameterRepr).ToArray ()) + ")");
                    PrintNode (lambda.Body, depth + 1);
                    return;
                }
                var invoke = expression as InvocationExpression;
                if (invoke != null) {
                    Line (expression, string.Empty);
                    PrintNode (invoke.Expression, depth + 1);
                    foreach (var argument in invoke.Arguments)
                        PrintNode (argument, depth + 1);
                    return;
                }
                var conditional = expression as ConditionalExpression;
                if (conditional != null) {
                    Line (expression, string.Empty);
                    PrintNode (conditional.Test, depth + 1);
                    PrintNode (conditional.IfTrue, depth + 1);
                    PrintNode (conditional.IfFalse, depth + 1);
                    return;
                }
                var block = expression as BlockExpression;
                if (block != null) {
                    Line (expression, block.Variables.Count == 0 ? string.Empty :
                        "(" + string.Join (", ", block.Variables.Select (
                            x => ParameterRepr (x) + ": " + FormatType (x.Type)).ToArray ()) + ")");
                    foreach (var statement in block.Expressions)
                        PrintNode (statement, depth + 1);
                    return;
                }
                var loop = expression as LoopExpression;
                if (loop != null) {
                    var labels = string.Empty;
                    if (loop.BreakLabel != null)
                        labels += " break:" + LabelRepr (loop.BreakLabel);
                    if (loop.ContinueLabel != null)
                        labels += " continue:" + LabelRepr (loop.ContinueLabel);
                    Line (expression, labels.TrimStart ());
                    PrintNode (loop.Body, depth + 1);
                    return;
                }
                var label = expression as LabelExpression;
                if (label != null) {
                    Line (expression, LabelRepr (label.Target));
                    if (label.DefaultValue != null)
                        PrintNode (label.DefaultValue, depth + 1);
                    return;
                }
                var gotoExpr = expression as GotoExpression;
                if (gotoExpr != null) {
                    Line (expression, gotoExpr.Kind + " " + LabelRepr (gotoExpr.Target));
                    if (gotoExpr.Value != null)
                        PrintNode (gotoExpr.Value, depth + 1);
                    return;
                }
                var tryExpr = expression as TryExpression;
                if (tryExpr != null) {
                    if (tryExpr.Fault != null)
                        throw Unsupported ("a fault block");
                    if (tryExpr.Handlers.Any (handler => handler.Filter != null))
                        throw Unsupported ("a catch filter");
                    Line (expression, string.Empty);
                    PrintNode (tryExpr.Body, depth + 1);
                    foreach (var handler in tryExpr.Handlers) {
                        result.Append (' ', (depth + 1) * 2);
                        result.Append ("Catch<");
                        result.Append (FormatType (handler.Test));
                        result.Append ('>');
                        if (handler.Variable != null) {
                            result.Append (' ');
                            result.Append (ParameterRepr (handler.Variable));
                        }
                        result.Append ('\n');
                        PrintNode (handler.Body, depth + 2);
                    }
                    if (tryExpr.Finally != null) {
                        result.Append (' ', (depth + 1) * 2);
                        result.Append ("Finally\n");
                        PrintNode (tryExpr.Finally, depth + 2);
                    }
                    return;
                }
                var index = expression as IndexExpression;
                if (index != null) {
                    Line (expression, index.Indexer == null ? string.Empty :
                        FormatType (index.Indexer.DeclaringType) + "." + index.Indexer.Name);
                    PrintNode (index.Object, depth + 1);
                    foreach (var argument in index.Arguments)
                        PrintNode (argument, depth + 1);
                    return;
                }
                var defaultExpr = expression as DefaultExpression;
                if (defaultExpr != null) {
                    Line (expression, string.Empty);
                    return;
                }
                throw Unsupported ("a " + expression.NodeType + " node");
            }

            static System.NotSupportedException Unsupported (string part)
            {
                return new System.NotSupportedException ("No rendering for " + part);
            }

            static string MethodRepr (System.Reflection.MethodInfo method)
            {
                return method == null ? string.Empty : FormatType (method.DeclaringType) + "." + method.Name;
            }

            void Line (LinqExpression expression, string detail)
            {
                result.Append (expression.NodeType);
                result.Append ('<');
                result.Append (FormatType (expression.Type));
                result.Append ('>');
                if (detail.Length > 0) {
                    result.Append (' ');
                    result.Append (detail);
                }
                result.Append ('\n');
            }

            string ParameterRepr (ParameterExpression parameter)
            {
                int id;
                if (!parameterIds.TryGetValue (parameter, out id)) {
                    id = parameterIds.Count;
                    parameterIds [parameter] = id;
                }
                return NameRepr (parameter.Name ?? "param") + "#" + id;
            }

            string StateRepr (Expression.IStateBox state)
            {
                int id;
                if (!stateIds.TryGetValue (state, out id)) {
                    id = stateIds.Count;
                    stateIds [state] = id;
                }
                return NameRepr (state.Name) + "#" + id;
            }

            string LabelRepr (LabelTarget target)
            {
                int id;
                if (!labelIds.TryGetValue (target, out id)) {
                    id = labelIds.Count;
                    labelIds [target] = id;
                }
                return NameRepr (string.IsNullOrEmpty (target.Name) ? "label" : target.Name) + "#" + id;
            }

            // A name that is not an identifier is quoted, so a dump reads one way
            static string NameRepr (string name)
            {
                if (!string.IsNullOrEmpty (name) && (char.IsLetter (name [0]) || name [0] == '_') &&
                    name.All (c => char.IsLetterOrDigit (c) || c == '_'))
                    return name;
                return StringRepr (name ?? string.Empty);
            }

            static string ConstantRepr (object value)
            {
                if (value == null)
                    return "null";
                var str = value as string;
                if (str != null)
                    return StringRepr (str);
                if (value is bool)
                    return (bool)value ? "true" : "false";
                // Mono prints a negative zero as 0
                if (value is double) {
                    var number = (double)value;
                    return IsNegativeZero (number) ? "-0" : number.ToString ("R", CultureInfo.InvariantCulture);
                }
                if (value is float) {
                    var number = (float)value;
                    return IsNegativeZero (number) ? "-0" : number.ToString ("R", CultureInfo.InvariantCulture);
                }
                if (value is int || value is long || value is uint || value is ulong)
                    return string.Format (CultureInfo.InvariantCulture, "{0}", value);
                var bytes = value as byte[];
                if (bytes != null)
                    return "0x" + BitConverter.ToString (bytes).Replace ("-", string.Empty);
                if (value.GetType ().IsEnum)
                    return FormatType (value.GetType ()) + "." + value;
                var type = value as System.Type;
                if (type != null)
                    return "typeof(" + FormatType (type) + ")";
                var procedure = value as Scanner.ProcedureSignature;
                if (procedure != null)
                    return procedure.FullyQualifiedName;
                var spec = value as TypeSpec;
                if (spec != null)
                    return SpecRepr (spec);
                var valueType = value.GetType ();
                var dictionary = value as IDictionary;
                if (dictionary != null) {
                    var entries = new List<string> ();
                    foreach (DictionaryEntry entry in dictionary)
                        entries.Add (ConstantRepr (entry.Key) + ": " + ConstantRepr (entry.Value));
                    return "{" + string.Join (", ", entries.ToArray ()) + "}";
                }
                if (TypeUtils.IsASetCollectionType (valueType))
                    return "{" + ValuesRepr ((IEnumerable)value) + "}";
                var list = value as IEnumerable;
                if (list != null)
                    return "[" + ValuesRepr (list) + "]";
                if (TypeUtils.IsATupleCollectionType (valueType))
                    return "(" + string.Join (", ", valueType.GetProperties ()
                        .Where (x => x.Name.StartsWith ("Item", StringComparison.Ordinal))
                        .OrderBy (x => x.Name, StringComparer.Ordinal)
                        .Select (x => ConstantRepr (x.GetValue (value, null))).ToArray ()) + ")";
                if (TypeUtils.IsAStructType (valueType))
                    return FormatType (valueType) + " {" + string.Join (", ", TypeUtils.GetStructFields (valueType)
                        .Select (x => x.Name + " = " + ConstantRepr (x.GetValue (value, null))).ToArray ()) + "}";
                // Any other object, such as a remote object or a comparer, prints as its type
                // name only, so the output does not depend on object identity
                return "<" + FormatType (valueType) + ">";
            }

            static bool IsNegativeZero (double value)
            {
                return value == 0 && BitConverter.DoubleToInt64Bits (value) < 0;
            }

            static string ValuesRepr (IEnumerable values)
            {
                var reprs = new List<string> ();
                foreach (var value in values)
                    reprs.Add (ConstantRepr (value));
                return string.Join (", ", reprs.ToArray ());
            }

            static string StringRepr (string value)
            {
                var repr = new StringBuilder ("\"");
                foreach (var c in value) {
                    if (c == '\\' || c == '"')
                        repr.Append ('\\').Append (c);
                    else if (c == '\n')
                        repr.Append ("\\n");
                    else if (c == '\r')
                        repr.Append ("\\r");
                    else if (c == '\t')
                        repr.Append ("\\t");
                    else if (char.IsControl (c))
                        repr.AppendFormat (CultureInfo.InvariantCulture, "\\u{0:x4}", (int)c);
                    else
                        repr.Append (c);
                }
                return repr.Append ('"').ToString ();
            }

            static string SpecRepr (TypeSpec spec)
            {
                var type = spec.Type;
                var arguments = type.IsGenericType ? type.GetGenericArguments () : new System.Type [0];
                string repr;
                if (spec.Types.Count > 0 && arguments.Length == spec.Types.Count)
                    repr = TypeName (type) + "<" + string.Join (
                        ", ", spec.Types.Select (SpecRepr).ToArray ()) + ">";
                else
                    repr = FormatType (type);
                return spec.Nullable ? repr + "?" : repr;
            }

            static string FormatType (System.Type type)
            {
                // A generic parameter is nested in the generic type that declares it
                if (type.IsGenericParameter)
                    return type.Name;
                if (type.IsArray)
                    return FormatType (type.GetElementType ()) +
                        "[" + new string (',', type.GetArrayRank () - 1) + "]";
                if (!type.IsGenericType)
                    return TypeName (type);
                return TypeName (type) + "<" + string.Join (
                    ", ", type.GetGenericArguments ().Select (FormatType).ToArray ()) + ">";
            }

            // The namespace qualified name of a type, without its generic arguments
            static string TypeName (System.Type type)
            {
                var name = type.IsNested ? FormatType (type.DeclaringType) + "." + type.Name :
                    string.IsNullOrEmpty (type.Namespace) ? type.Name : type.Namespace + "." + type.Name;
                var backtick = name.IndexOf ('`');
                return backtick >= 0 ? name.Substring (0, backtick) : name;
            }
        }
    }
}
