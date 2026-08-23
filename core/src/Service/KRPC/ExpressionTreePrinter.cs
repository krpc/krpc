using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// Produces a deterministic, human-readable dump of an expression tree, for
    /// verifying the trees that the server side expression API generates.
    /// The output is independent of the runtime and culture, so it can be
    /// compared against golden strings in tests: parameters, variables and
    /// labels are identified by sequential ids in order of first appearance,
    /// numeric constants are formatted round-trip in the invariant culture, and
    /// object constants are printed as their type name only.
    /// </summary>
    public static class ExpressionTreePrinter
    {
        /// <summary>
        /// Render the expression tree as an indented multi-line string.
        /// </summary>
        public static string Print (LinqExpression expression)
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

            public override string ToString ()
            {
                return result.ToString ();
            }

            public void PrintNode (LinqExpression expression, int depth)
            {
                result.Append (' ', depth * 2);
                var binary = expression as BinaryExpression;
                if (binary != null) {
                    Line (expression, string.Empty);
                    PrintNode (binary.Left, depth + 1);
                    PrintNode (binary.Right, depth + 1);
                    return;
                }
                var unary = expression as UnaryExpression;
                if (unary != null) {
                    Line (expression, string.Empty);
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
                        result.Append (' ', (depth + 1) * 2);
                        result.Append (binding.BindingType);
                        result.Append (' ');
                        result.Append (FormatType (binding.Member.DeclaringType));
                        result.Append ('.');
                        result.Append (binding.Member.Name);
                        result.Append ('\n');
                        var assignment = binding as MemberAssignment;
                        if (assignment != null)
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
                        "(" + string.Join (
                            ", ", block.Variables.Select (ParameterRepr).ToArray ()) + ")");
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
                    Line (expression, string.Empty);
                    PrintNode (tryExpr.Body, depth + 1);
                    foreach (var handler in tryExpr.Handlers) {
                        result.Append (' ', (depth + 1) * 2);
                        result.Append ("Catch<");
                        result.Append (FormatType (handler.Test));
                        result.Append (">\n");
                        PrintNode (handler.Body, depth + 2);
                    }
                    if (tryExpr.Finally != null) {
                        result.Append (' ', (depth + 1) * 2);
                        result.Append ("Finally\n");
                        PrintNode (tryExpr.Finally, depth + 2);
                    }
                    return;
                }
                var defaultExpr = expression as DefaultExpression;
                if (defaultExpr != null) {
                    Line (expression, string.Empty);
                    return;
                }
                // Any node kind not given a specific rendering above is printed
                // without children, so unexpected nodes are visible in the dump
                // rather than causing a failure
                Line (expression, "unhandled");
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
                return (parameter.Name ?? "param") + "#" + id;
            }

            string LabelRepr (LabelTarget target)
            {
                int id;
                if (!labelIds.TryGetValue (target, out id)) {
                    id = labelIds.Count;
                    labelIds [target] = id;
                }
                return (string.IsNullOrEmpty (target.Name) ? "label" : target.Name) + "#" + id;
            }

            static string ConstantRepr (object value)
            {
                if (value == null)
                    return "null";
                var str = value as string;
                if (str != null)
                    return "\"" + str.Replace ("\\", "\\\\").Replace ("\"", "\\\"").Replace ("\n", "\\n") + "\"";
                if (value is bool)
                    return (bool)value ? "true" : "false";
                if (value is double)
                    return ((double)value).ToString ("R", CultureInfo.InvariantCulture);
                if (value is float)
                    return ((float)value).ToString ("R", CultureInfo.InvariantCulture);
                if (value is int || value is long || value is uint || value is ulong)
                    return string.Format (CultureInfo.InvariantCulture, "{0}", value);
                if (value.GetType ().IsEnum)
                    return value.GetType ().Name + "." + value;
                var procedure = value as Scanner.ProcedureSignature;
                if (procedure != null)
                    return procedure.FullyQualifiedName;
                // Other values (e.g. remote object instances) are printed as their
                // type name only, so the output does not depend on object identity
                return "<" + FormatType (value.GetType ()) + ">";
            }

            static string FormatType (System.Type type)
            {
                if (!type.IsGenericType)
                    return type.Name;
                var name = type.Name;
                var backtick = name.IndexOf ('`');
                if (backtick >= 0)
                    name = name.Substring (0, backtick);
                return name + "<" + string.Join (
                    ", ", type.GetGenericArguments ().Select (FormatType).ToArray ()) + ">";
            }
        }
    }
}
