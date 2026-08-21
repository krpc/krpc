"""Compilation of python expressions into server side expression nodes.
Statement compilation lives in krpc.expressionstatements."""

# One class compiles every kind of expression node, and the parts of it that
# could be lifted out - the builtin functions, the comprehensions - each reach
# back into a dozen of the compiler's own members, so moving them elsewhere
# would spread the class rather than divide it.
# pylint: disable=too-many-lines

from __future__ import annotations
import ast
import inspect
import operator
import textwrap
from enum import Enum
from typing import Any, Callable, Dict, List, Optional, Tuple, TYPE_CHECKING, cast

from krpc.error import ExpressionCompilationError
from krpc.expressionstatements import _StatementCompiler
from krpc.expressionutils import (
    Metadata,
    Result as _Result,
    NUMERIC_CODES,
    build_ptype,
    promote,
    remote_type,
)
from krpc.service import _member_name
from krpc.types import ClassBase, ClassType, EnumerationType, StructType
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


def compile_expression(client: Client, func: Callable) -> Any:  # type: ignore[type-arg]
    """Compile a python function into a server side expression.

    The function must take no arguments. Its result is a KRPC.Expression
    object that, when evaluated on the server, computes what the function
    would compute if it were run on the client."""
    return _Compiler(client, func).compile()


_BINARY_OPS: Dict[type, Tuple[str, Callable[[Any, Any], Any]]] = {
    ast.Add: ("add", operator.add),
    ast.Sub: ("subtract", operator.sub),
    ast.Mult: ("multiply", operator.mul),
    ast.Div: ("divide", operator.truediv),
    ast.Mod: ("modulo", operator.mod),
    ast.Pow: ("power", operator.pow),
    ast.LShift: ("left_shift", operator.lshift),
    ast.RShift: ("right_shift", operator.rshift),
    ast.BitAnd: ("and_", operator.and_),
    ast.BitOr: ("or_", operator.or_),
    ast.BitXor: ("exclusive_or", operator.xor),
}

_INTEGER_CODES = (
    KRPC.Type.SINT32,
    KRPC.Type.SINT64,
    KRPC.Type.UINT32,
    KRPC.Type.UINT64,
)


def _math_functions() -> Dict[Any, str]:
    """Functions from the math module that map to StdLib procedures."""
    import math  # pylint: disable=import-outside-toplevel

    return {
        math.sqrt: "sqrt",
        math.sin: "sin",
        math.cos: "cos",
        math.tan: "tan",
        math.asin: "asin",
        math.acos: "acos",
        math.atan: "atan",
        math.atan2: "atan2",
        math.log10: "log10",
        math.exp: "exp",
        math.floor: "floor",
        math.ceil: "ceiling",
        math.fabs: "abs",
        math.degrees: "radians_to_degrees",
        math.radians: "degrees_to_radians",
    }


_MATH_FUNCTIONS = _math_functions()

_COMPARE_OPS: Dict[type, Tuple[str, Callable[[Any, Any], Any]]] = {
    ast.Eq: ("equal", operator.eq),
    ast.NotEq: ("not_equal", operator.ne),
    ast.Gt: ("greater_than", operator.gt),
    ast.GtE: ("greater_than_or_equal", operator.ge),
    ast.Lt: ("less_than", operator.lt),
    ast.LtE: ("less_than_or_equal", operator.le),
}


class _Compiler:
    def __init__(self, client: Client, func: Callable):  # type: ignore[type-arg]
        self._client = client
        self._expr = client.krpc.Expression
        self._type = client.krpc.Type
        self._func = func
        self._scopes: List[Dict[str, _Result]] = []
        # name -> (function expression, [(parameter name, ptype)], return ptype)
        self._local_functions: Dict[str, Any] = {}
        # The statement compiler for the function body being compiled, when
        # there is one; used by assignment expressions
        self._active_statements: Any = None
        if client._expression_metadata is None:
            client._expression_metadata = Metadata(client)
        self._metadata: Metadata = client._expression_metadata
        # Naming a type is a round trip, so the objects naming them are shared
        # by every function compiled for this connection
        self._remote_types: Dict[bytes, Any] = client._expression_remote_types

    def compile(self) -> Any:
        node = self._parse()
        if isinstance(node, ast.Lambda):
            if node.args.args or node.args.posonlyargs or node.args.kwonlyargs:
                raise ExpressionCompilationError(
                    "The function to compile must take no arguments"
                )
            result = self._compile(node.body)
        else:
            if node.args.args or node.args.posonlyargs or node.args.kwonlyargs:
                raise ExpressionCompilationError(
                    "The function to compile must take no arguments"
                )
            result = _StatementCompiler(self).compile_body(node.body)
        return self._to_expression(result).expression

    def _parse(self) -> ast.AST:
        func = self._func
        try:
            lines, _ = inspect.getsourcelines(func)
        except (OSError, TypeError) as exc:
            raise ExpressionCompilationError(
                "Cannot get the source code of the function to compile"
            ) from exc
        source = textwrap.dedent("".join(lines))
        # The source of a lambda embedded in a larger statement may not parse on
        # its own. As well as the statement itself, try it as a parenthesized
        # expression, and as the header of a compound statement, which is what a
        # lambda written inside a "with" or an "if" belongs to.
        tree = None
        error: Optional[SyntaxError] = None
        for candidate in (
            source,
            "(" + source.strip().rstrip(",") + ")",
            source.rstrip() + "\n    pass",
        ):
            try:
                tree = ast.parse(candidate)
                break
            except SyntaxError as exc:
                error = exc
        if tree is None:
            raise ExpressionCompilationError(
                "Cannot parse the source code of the function to compile"
            ) from error
        if func.__name__ == "<lambda>":
            # The parsed source may contain other lambdas, e.g. as the key of a
            # sorted() call within the target; identify the target by its arity
            arity = func.__code__.co_argcount
            lambdas = [
                n
                for n in ast.walk(tree)
                if isinstance(n, ast.Lambda)
                and len(n.args.args) + len(n.args.posonlyargs) == arity
            ]
            if len(lambdas) != 1:
                raise ExpressionCompilationError(
                    "The source containing the lambda to compile must contain "
                    "exactly one lambda; define it on its own line"
                )
            return lambdas[0]
        functions = [
            n
            for n in ast.walk(tree)
            if isinstance(n, ast.FunctionDef) and n.name == func.__name__
        ]
        if len(functions) != 1:
            raise ExpressionCompilationError(
                "Cannot identify the definition of the function to compile"
            )
        return functions[0]

    # Name resolution

    def _lookup(self, name: str) -> _Result:
        for scope in reversed(self._scopes):
            if name in scope:
                return scope[name]
        func = self._func
        freevars = func.__code__.co_freevars
        if name in freevars and func.__closure__ is not None:
            return _Result(
                value=func.__closure__[freevars.index(name)].cell_contents,
                is_value=True,
            )
        if name in func.__globals__:
            return _Result(value=func.__globals__[name], is_value=True)
        builtins = func.__globals__.get("__builtins__", {})
        if not isinstance(builtins, dict):
            builtins = builtins.__dict__
        if name in builtins:
            return _Result(value=builtins[name], is_value=True)
        raise ExpressionCompilationError("Cannot resolve the name '%s'" % name)

    # Compilation of AST nodes

    def _compile(self, node: ast.AST) -> _Result:
        method = getattr(self, "_compile_" + type(node).__name__.lower(), None)
        if method is None:
            raise self._error(node, "unsupported syntax (%s)" % type(node).__name__)
        return method(node)

    def _compile_constant(self, node: ast.Constant) -> _Result:
        return _Result(value=node.value, is_value=True)

    def _compile_name(self, node: ast.Name) -> _Result:
        return self._lookup(node.id)

    def _compile_attribute(self, node: ast.Attribute) -> _Result:
        base = self._compile(node.value)
        if base.is_value:
            value = base.value
            if isinstance(value, ClassBase) or self._is_service_object(value):
                if not hasattr(value, "_build_call_" + node.attr):
                    member = getattr(value, node.attr, None)
                    # The class, enumeration and structure types a service defines
                    # are attributes of it alongside its remote members
                    if self._is_service_object(value) and isinstance(member, type):
                        return _Result(value=member, is_value=True)
                    raise self._error(
                        node,
                        "'%s' is not a remote member of %s"
                        % (node.attr, type(value).__name__),
                    )
                call = self._client.get_call(getattr, value, node.attr)
                return_type = getattr(value, "_return_type_" + node.attr)()
                return _Result(
                    expression=self._expr.call(call),
                    ptype=return_type.protobuf_type,
                )
            try:
                return _Result(value=getattr(value, node.attr), is_value=True)
            except AttributeError as exc:
                raise self._error(node, str(exc)) from exc
        # The base is a server side expression; a field of a structure is read
        # directly, and a member of an object is resolved from its class type
        if base.ptype is not None and base.ptype.code == KRPC.Type.STRUCT:
            return self._compile_struct_field(node, base)
        service, class_name = self._class_of(node, base)
        service, procedure = self._member(
            node, service, class_name, node.attr, "getter"
        )
        return self._call_node(service, procedure, [base])

    def _compile_call(self, node: ast.Call) -> _Result:
        func = node.func
        # Calls of local functions defined within the compiled function
        if isinstance(func, ast.Name) and func.id in self._local_functions:
            return self._compile_local_function_call(node, func.id)
        # Calls of supported builtin functions, and of captured bound methods
        # of remote objects and services
        if isinstance(func, ast.Name):
            target = self._lookup(func.id)
            if target.is_value:
                if self._is_supported_builtin(target.value):
                    return self._compile_builtin(node, target.value)
                bound = self._remote_bound_method(target.value)
                if bound is not None:
                    owner, member = bound
                    return self._compile_remote_call(
                        node, member, _Result(value=owner, is_value=True)
                    )
        # Construction of a structure the server defines
        if isinstance(func, ast.Name):
            target = self._lookup(func.id)
            if target.is_value and self._is_remote_struct(target.value):
                return self._compile_struct_construction(node, target.value)
        # Method calls on remote objects and services
        if isinstance(func, ast.Attribute):
            base = self._compile(func.value)
            if base.is_value:
                member = getattr(base.value, func.attr, None)
                if self._is_remote_struct(member):
                    return self._compile_struct_construction(node, member)
            if not base.is_value and base.ptype is not None:
                mutation = self._compile_collection_mutation(node, func, base)
                if mutation is not None:
                    return mutation
            if base.is_value and (
                isinstance(base.value, ClassBase)
                or self._is_service_object(base.value)
                or self._is_remote_class(base.value)
            ):
                return self._compile_remote_call(node, func.attr, base)
            if not base.is_value:
                return self._compile_remote_call(node, func.attr, base)
            # A method on a plain client-side value
            method = getattr(base.value, func.attr, None)
            if method is None:
                raise self._error(node, "cannot resolve method '%s'" % func.attr)
            if method in _MATH_FUNCTIONS:
                return self._compile_math_call(node, method)
            return self._compile_client_call(node, method)
        # A call of a plain client-side function
        target = self._compile(func)
        if target.is_value:
            bound = self._remote_bound_method(target.value)
            if bound is not None:
                owner, member = bound
                return self._compile_remote_call(
                    node, member, _Result(value=owner, is_value=True)
                )
            if target.value in _MATH_FUNCTIONS:
                return self._compile_math_call(node, target.value)
            return self._compile_client_call(node, target.value)
        raise self._error(node, "unsupported function call")

    def _compile_struct_field(self, node: ast.Attribute, base: _Result) -> _Result:
        """Compile a read of a field of a structure valued expression."""
        ptype = base.ptype
        assert ptype is not None
        typ = self._struct_type_named(node, ptype.service, ptype.name)
        if node.attr not in typ.field_names:
            raise self._error(
                node,
                "'%s' is not a field of %s.%s" % (node.attr, ptype.service, ptype.name),
            )
        index = typ.field_names.index(node.attr)
        declared = self._metadata.struct_fields[(ptype.service, ptype.name)]
        return _Result(
            expression=self._expr.get_field(base.expression, declared[index]),
            ptype=typ.field_types[index].protobuf_type,
        )

    def _compile_struct_construction(
        self, node: ast.Call, python_type: type
    ) -> _Result:
        """Compile the construction of a structure from its field values, given
        positionally, by field name, or both."""
        typ = self._struct_type_of(node, python_type)
        ptype = typ.protobuf_type
        names = typ.field_names
        values: List[Optional[_Result]] = [None] * len(names)
        if len(node.args) > len(names):
            raise self._error(
                node,
                "%s.%s has %d fields, got %d positional field values"
                % (ptype.service, ptype.name, len(names), len(node.args)),
            )
        for index, argument in enumerate(node.args):
            values[index] = self._compile(argument)
        for keyword in node.keywords:
            if keyword.arg is None or keyword.arg not in names:
                raise self._error(
                    node,
                    "'%s' is not a field of %s.%s"
                    % (keyword.arg, ptype.service, ptype.name),
                )
            index = names.index(keyword.arg)
            if values[index] is not None:
                raise self._error(
                    node, "field '%s' is given more than one value" % keyword.arg
                )
            values[index] = self._compile(keyword.value)
        missing = [name for name, value in zip(names, values) if value is None]
        if missing:
            raise self._error(
                node,
                "no value for the field %s of %s.%s"
                % (
                    ", ".join("'%s'" % name for name in missing),
                    ptype.service,
                    ptype.name,
                ),
            )
        expressions = [
            self._converted_expression(value, field_type.protobuf_type)
            for value, field_type in zip(values, typ.field_types)
            if value is not None
        ]
        return _Result(
            expression=self._expr.create_struct(
                self._remote_type(node, ptype), expressions
            ),
            ptype=ptype,
        )

    def _compile_math_call(
        self, node: ast.Call, func: Callable  # type: ignore[type-arg]
    ) -> _Result:
        """Compile a call to a math module function: evaluated on the client
        when its arguments are known, and mapped to the equivalent StdLib
        procedure when they are computed on the server."""
        if node.keywords:
            raise self._error(node, "keyword arguments are not supported here")
        arguments = [self._compile(argument) for argument in node.args]
        if all(argument.is_value for argument in arguments):
            return self._compile_client_call(node, func)
        return self._stdlib_call(node, _MATH_FUNCTIONS[func], arguments)

    def _compile_local_function_call(self, node: ast.Call, name: str) -> _Result:
        function, parameters, return_ptype = self._local_functions[name]
        if len(node.args) != len(parameters) or node.keywords:
            raise self._error(
                node,
                "'%s' takes %d positional arguments" % (name, len(parameters)),
            )
        arguments: Dict[str, Any] = {}
        for (parameter_name, parameter_ptype), argument in zip(parameters, node.args):
            compiled = self._to_expression(self._compile(argument))
            expression = compiled.expression
            if (
                parameter_ptype is not None
                and compiled.ptype is not None
                and compiled.ptype.code != parameter_ptype.code
                and parameter_ptype.code in NUMERIC_CODES
                and compiled.ptype.code in NUMERIC_CODES
            ):
                expression = self._expr.cast(
                    expression, self._remote_type(node, parameter_ptype)
                )
            arguments[parameter_name] = expression
        return _Result(
            expression=self._expr.invoke(function, arguments),
            ptype=return_ptype,
        )

    def _remote_bound_method(self, value: object) -> Optional[Tuple[object, str]]:
        """When the value is a bound method of a remote object, service or
        remote class, return the object it is bound to and the member name."""
        if not callable(value):
            return None
        owner = getattr(value, "__self__", None)
        name = getattr(value, "__name__", None)
        if owner is None or name is None:
            return None
        if (
            isinstance(owner, ClassBase)
            or self._is_service_object(owner)
            or self._is_remote_class(owner)
        ):
            return owner, name
        return None

    def _compile_collection_mutation(
        self, node: ast.Call, func: ast.Attribute, base: _Result
    ) -> Optional[_Result]:
        """Compile mutating method calls on server side collections, such as
        appending to a list variable."""
        code = base.ptype.code if base.ptype is not None else None
        if code == KRPC.Type.LIST and func.attr == "append" and len(node.args) == 1:
            value = self._to_expression(self._compile(node.args[0]))
            return _Result(
                expression=self._expr.list_add(base.expression, value.expression)
            )
        if code == KRPC.Type.SET and func.attr == "add" and len(node.args) == 1:
            value = self._to_expression(self._compile(node.args[0]))
            return _Result(
                expression=self._expr.set_add(base.expression, value.expression)
            )
        return None

    def _compile_client_call(
        self, node: ast.Call, func: Callable  # type: ignore[type-arg]
    ) -> _Result:
        args = [self._compile(arg) for arg in node.args]
        keywords = {
            keyword.arg: self._compile(keyword.value) for keyword in node.keywords
        }
        if any(not arg.is_value for arg in args) or any(
            not arg.is_value for arg in keywords.values()
        ):
            raise self._error(
                node,
                "cannot call a client side function with an argument "
                "computed on the server",
            )
        try:
            value = func(
                *[arg.value for arg in args],
                **{name: arg.value for name, arg in keywords.items()},  # type: ignore[misc]
            )
        except Exception as exc:
            raise self._error(
                node, "error calling client side function: %s" % exc
            ) from exc
        return _Result(value=value, is_value=True)

    def _compile_remote_call(
        self, node: ast.Call, member: str, base: _Result
    ) -> _Result:
        if base.is_value and self._is_remote_class(base.value):
            # A static class method, called on the class itself
            protobuf_type = self._class_ptype(base.value)
            service, procedure = self._member(
                node, protobuf_type.service, protobuf_type.name, member, "static"
            )
            instance_args: List[Optional[_Result]] = []
        elif base.is_value and self._is_service_object(base.value):
            service, procedure = self._member(
                node, self._service_name(base.value), None, member, "method"
            )
            instance_args = []
        else:
            if base.is_value:
                base = self._to_expression(base)
            service, class_name = self._class_of(node, base)
            service, procedure = self._member(
                node, service, class_name, member, "method"
            )
            instance_args = [base]

        parameters = list(procedure.parameters)[len(instance_args) :]
        args: List[Optional[_Result]] = list(instance_args)
        if len(node.args) > len(parameters):
            raise self._error(
                node,
                "too many arguments for %s: expected at most %d, got %d"
                % (procedure.name, len(parameters), len(node.args)),
            )
        args.extend(self._compile(arg) for arg in node.args)
        if node.keywords:
            names = [_member_name(parameter.name) for parameter in parameters]
            values: Dict[int, _Result] = {}
            for keyword in node.keywords:
                if keyword.arg not in names:
                    raise self._error(
                        node,
                        "unknown keyword argument '%s' for %s"
                        % (keyword.arg, procedure.name),
                    )
                position = len(instance_args) + names.index(keyword.arg)
                if position < len(args):
                    raise self._error(node, "argument '%s' given twice" % keyword.arg)
                values[position] = self._compile(keyword.value)
            for position in sorted(values):
                while len(args) < position:
                    args.append(None)
                args.append(values[position])
        return self._call_node(service, procedure, args)

    def _compile_binop(self, node: ast.BinOp) -> _Result:
        if isinstance(node.op, ast.FloorDiv):
            return self._compile_floor_division(node)
        try:
            name, fold = _BINARY_OPS[type(node.op)]
        except KeyError:
            raise self._error(
                node, "unsupported operator (%s)" % type(node.op).__name__
            ) from None
        left = self._compile(node.left)
        right = self._compile(node.right)
        if left.is_value and right.is_value:
            return _Result(value=fold(left.value, right.value), is_value=True)
        left = self._to_expression(left)
        right = self._to_expression(right)
        if isinstance(node.op, ast.Div):
            # Python division is true division; convert integer operands so
            # the server does not perform integer division
            left = self._ensure_double(left)
        op = getattr(self._expr, name)
        return _Result(
            expression=op(left.expression, right.expression),
            ptype=promote(left.ptype, right.ptype),
        )

    def _ensure_double(self, result: _Result) -> _Result:
        """Convert an integer typed expression to a double."""
        if result.ptype is None or result.ptype.code not in _INTEGER_CODES:
            return result
        return _Result(
            expression=self._expr.cast(result.expression, self._type.double()),
            ptype=build_ptype(KRPC.Type.DOUBLE),
        )

    def _compile_floor_division(self, node: ast.BinOp) -> _Result:
        left = self._compile(node.left)
        right = self._compile(node.right)
        if left.is_value and right.is_value:
            return _Result(
                value=operator.floordiv(left.value, right.value), is_value=True
            )
        left = self._to_expression(left)
        right = self._to_expression(right)
        both_integers = (
            left.ptype is not None
            and right.ptype is not None
            and left.ptype.code in _INTEGER_CODES
            and right.ptype.code in _INTEGER_CODES
        )
        quotient = self._expr.divide(
            self._ensure_double(left).expression, right.expression
        )
        floored = self._stdlib_call(
            node,
            "floor",
            [_Result(expression=quotient, ptype=build_ptype(KRPC.Type.DOUBLE))],
        )
        if both_integers:
            return _Result(
                expression=self._expr.cast(floored.expression, self._type.int()),
                ptype=build_ptype(KRPC.Type.SINT32),
            )
        return floored

    def _stdlib_call(self, node: ast.AST, member: str, args: List[_Result]) -> _Result:
        """Embed a call to a StdLib procedure in the expression."""
        service, procedure = self._member(node, "StdLib", None, member, "method")
        arguments: List[Optional[_Result]] = list(args)
        return self._call_node(service, procedure, arguments)

    def _stdlib_scalar_call(
        self, node: ast.AST, member: str, args: List[_Result]
    ) -> _Result:
        """Embed a StdLib call on scalar arguments, preserving integer typing:
        when every argument is an integer, the double result is converted back
        to an integer, matching python's behavior for functions such as abs."""
        all_integers = all(
            arg.ptype is not None and arg.ptype.code in _INTEGER_CODES for arg in args
        )
        result = self._stdlib_call(node, member, args)
        if all_integers:
            return _Result(
                expression=self._expr.cast(result.expression, self._type.int()),
                ptype=build_ptype(KRPC.Type.SINT32),
            )
        return result

    def _compile_boolop(self, node: ast.BoolOp) -> _Result:
        results = [self._compile(value) for value in node.values]
        if all(result.is_value for result in results):
            values = [result.value for result in results]
            if isinstance(node.op, ast.And):
                value: object = all(values)
            else:
                value = any(values)
            return _Result(value=value, is_value=True)
        op = self._expr.and_ if isinstance(node.op, ast.And) else self._expr.or_
        expression = self._to_expression(results[0]).expression
        for result in results[1:]:
            expression = op(expression, self._to_expression(result).expression)
        return _Result(expression=expression, ptype=build_ptype(KRPC.Type.BOOL))

    def _compile_unaryop(self, node: ast.UnaryOp) -> _Result:
        result = self._compile(node.operand)
        if isinstance(node.op, ast.Not):
            if result.is_value:
                return _Result(value=not result.value, is_value=True)
            return _Result(
                expression=self._expr.not_(self._to_expression(result).expression),
                ptype=build_ptype(KRPC.Type.BOOL),
            )
        if isinstance(node.op, ast.USub):
            if result.is_value:
                return _Result(value=-result.value, is_value=True)  # type: ignore[operator]
            result = self._to_expression(result)
            return _Result(
                expression=self._expr.multiply(
                    self._expr.constant_int(-1), result.expression
                ),
                ptype=result.ptype,
            )
        if isinstance(node.op, ast.UAdd):
            return result
        if isinstance(node.op, ast.Invert):
            if result.is_value:
                return _Result(value=~result.value, is_value=True)  # type: ignore[operator]
            result = self._to_expression(result)
            return _Result(
                expression=self._expr.not_(result.expression), ptype=result.ptype
            )
        raise self._error(node, "unsupported operator (%s)" % type(node.op).__name__)

    def _compile_compare(self, node: ast.Compare) -> _Result:
        operands = [self._compile(value) for value in [node.left] + node.comparators]
        if all(operand.is_value for operand in operands):
            value = True
            for left, op, right in zip(operands, node.ops, operands[1:]):
                if isinstance(op, ast.In):
                    value = value and (left.value in right.value)  # type: ignore[operator]
                elif isinstance(op, ast.NotIn):
                    value = value and (left.value not in right.value)  # type: ignore[operator]
                elif type(op) in _COMPARE_OPS:
                    value = value and _COMPARE_OPS[type(op)][1](left.value, right.value)
                else:
                    raise self._error(
                        node, "unsupported comparison (%s)" % type(op).__name__
                    )
            return _Result(value=value, is_value=True)
        comparisons = []
        for left, op, right in zip(operands, node.ops, operands[1:]):
            if isinstance(op, (ast.In, ast.NotIn)):
                comparison = self._expr.contains(
                    self._to_expression(right).expression,
                    self._to_expression(left).expression,
                )
                if isinstance(op, ast.NotIn):
                    comparison = self._expr.not_(comparison)
            elif type(op) in _COMPARE_OPS:
                comparison = getattr(self._expr, _COMPARE_OPS[type(op)][0])(
                    self._to_expression(left).expression,
                    self._to_expression(right).expression,
                )
            else:
                raise self._error(
                    node, "unsupported comparison (%s)" % type(op).__name__
                )
            comparisons.append(comparison)
        expression = comparisons[0]
        for comparison in comparisons[1:]:
            expression = self._expr.and_(expression, comparison)
        return _Result(expression=expression, ptype=build_ptype(KRPC.Type.BOOL))

    def _compile_ifexp(self, node: ast.IfExp) -> _Result:
        condition = self._compile(node.test)
        if condition.is_value:
            return self._compile(node.body if condition.value else node.orelse)
        if_true = self._to_expression(self._compile(node.body))
        if_false = self._to_expression(self._compile(node.orelse))
        return _Result(
            expression=self._expr.conditional(
                condition.expression, if_true.expression, if_false.expression
            ),
            ptype=promote(if_true.ptype, if_false.ptype),
        )

    def _compile_subscript(self, node: ast.Subscript) -> _Result:
        if isinstance(node.slice, ast.Slice):
            return self._compile_slice(node)
        base = self._compile(node.value)
        index = self._compile(node.slice)
        if base.is_value and index.is_value:
            return _Result(
                value=base.value[index.value], is_value=True  # type: ignore[index]
            )
        base = self._to_expression(base)
        element: Optional[KRPC.Type] = None
        if base.ptype is not None:
            if base.ptype.code == KRPC.Type.LIST:
                element = base.ptype.types[0]
            elif base.ptype.code == KRPC.Type.DICTIONARY:
                element = base.ptype.types[1]
            elif base.ptype.code == KRPC.Type.TUPLE and index.is_value:
                element = base.ptype.types[index.value]  # type: ignore[index]
        return _Result(
            expression=self._expr.get(
                base.expression, self._to_expression(index).expression
            ),
            ptype=element,
        )

    def _compile_slice(self, node: ast.Subscript) -> _Result:
        piece = node.slice
        assert isinstance(piece, ast.Slice)
        if piece.step is not None:
            raise self._error(node, "slices with a step are not supported")
        base = self._compile(node.value)
        lower = None if piece.lower is None else self._compile(piece.lower)
        upper = None if piece.upper is None else self._compile(piece.upper)
        if (
            base.is_value
            and (lower is None or lower.is_value)
            and (upper is None or upper.is_value)
        ):
            return _Result(
                value=base.value[  # type: ignore[index]
                    slice(
                        None if lower is None else lower.value,
                        None if upper is None else upper.value,
                    )
                ],
                is_value=True,
            )
        for bound in (lower, upper):
            if (
                bound is not None
                and bound.is_value
                and isinstance(bound.value, int)
                and bound.value < 0
            ):
                raise self._error(node, "negative slice bounds are not supported")
        collection = self._to_expression(base)
        element = self._element_ptype(node, collection)
        expression = collection.expression
        if lower is not None:
            lower = self._to_expression(lower)
            expression = self._expr.skip(expression, lower.expression)
        if upper is not None:
            upper = self._to_expression(upper)
            count = upper.expression
            if lower is not None:
                count = self._expr.subtract(count, lower.expression)
            expression = self._expr.take(expression, count)
        return _Result(
            expression=self._expr.to_list(expression),
            ptype=build_ptype(KRPC.Type.LIST, [element]),
        )

    def _compile_list(self, node: ast.List) -> _Result:
        return self._compile_elements(node.elts, "list")

    def _compile_tuple(self, node: ast.Tuple) -> _Result:
        return self._compile_elements(node.elts, "tuple")

    def _compile_set(self, node: ast.Set) -> _Result:
        return self._compile_elements(node.elts, "set")

    def _compile_elements(self, elts: List[ast.expr], kind: str) -> _Result:
        results = [self._compile(elt) for elt in elts]
        if all(result.is_value for result in results):
            values = [result.value for result in results]
            if kind == "list":
                return _Result(value=values, is_value=True)
            if kind == "tuple":
                return _Result(value=tuple(values), is_value=True)
            return _Result(value=set(values), is_value=True)
        expressions = [self._to_expression(result) for result in results]
        if kind == "list":
            return _Result(
                expression=self._expr.create_list(
                    [result.expression for result in expressions]
                ),
                ptype=self._collection_ptype(KRPC.Type.LIST, expressions[:1]),
            )
        if kind == "tuple":
            return _Result(
                expression=self._expr.create_tuple(
                    [result.expression for result in expressions]
                ),
                ptype=self._collection_ptype(KRPC.Type.TUPLE, expressions),
            )
        return _Result(
            expression=self._expr.create_set(
                [result.expression for result in expressions]
            ),
            ptype=self._collection_ptype(KRPC.Type.SET, expressions[:1]),
        )

    def _compile_dict(self, node: ast.Dict) -> _Result:
        if any(key is None for key in node.keys):
            raise self._error(node, "dictionary unpacking is not supported")
        keys = [self._compile(key) for key in node.keys if key is not None]
        values = [self._compile(value) for value in node.values]
        if all(result.is_value for result in keys + values):
            return _Result(
                value=dict(
                    zip((key.value for key in keys), (value.value for value in values))
                ),
                is_value=True,
            )
        key_expressions = [self._to_expression(key) for key in keys]
        value_expressions = [self._to_expression(value) for value in values]
        return _Result(
            expression=self._expr.create_dictionary(
                [key.expression for key in key_expressions],
                [value.expression for value in value_expressions],
            )
        )

    def _compile_listcomp(self, node: ast.ListComp) -> _Result:
        selected, element = self._compile_comprehension(node, node.generators, node.elt)
        return _Result(
            expression=self._expr.to_list(selected),
            ptype=build_ptype(KRPC.Type.LIST, [element] if element else None),
        )

    def _compile_setcomp(self, node: ast.SetComp) -> _Result:
        selected, element = self._compile_comprehension(node, node.generators, node.elt)
        return _Result(
            expression=self._expr.to_set(selected),
            ptype=build_ptype(KRPC.Type.SET, [element] if element else None),
        )

    def _compile_generatorexp(self, node: ast.GeneratorExp) -> _Result:
        selected, element = self._compile_comprehension(node, node.generators, node.elt)
        # A lazily evaluated sequence; consumed by aggregations such as sum,
        # min and max
        return _Result(
            expression=selected,
            ptype=build_ptype(KRPC.Type.LIST, [element] if element else None),
        )

    def _compile_comprehension(
        self,
        node: ast.AST,
        generators: List[ast.comprehension],
        elt: ast.expr,
        predicate_only: bool = False,
    ) -> Tuple[Any, Optional[KRPC.Type]]:
        """Compile a comprehension to select/where over its collection.
        Comprehensions with multiple 'for' clauses flatten via the server's
        select-many operation. Returns the resulting collection expression and
        the type of its elements. When predicate_only is set, elt must be
        boolean and the result is the pair (collection, predicate function)
        instead."""
        if predicate_only and len(generators) != 1:
            raise self._error(node, "only a single 'for' clause is supported here")
        if len(generators) > 1:
            return self._compile_nested_comprehension(node, generators, elt)
        generator = generators[0]
        if not isinstance(generator.target, ast.Name):
            raise self._error(node, "the loop variable must be a single name")
        if generator.is_async:
            raise self._error(node, "async comprehensions are not supported")
        collection = self._to_expression(self._compile(generator.iter))
        element = self._element_ptype(node, collection)
        parameter = self._expr.parameter(
            generator.target.id, self._remote_type(node, element)
        )
        scope = {generator.target.id: _Result(expression=parameter, ptype=element)}
        self._scopes.append(scope)
        try:
            collection_expression = collection.expression
            if generator.ifs:
                condition = self._compile(generator.ifs[0])
                for extra in generator.ifs[1:]:
                    condition_extra = self._compile(extra)
                    condition = _Result(
                        expression=self._expr.and_(
                            self._to_expression(condition).expression,
                            self._to_expression(condition_extra).expression,
                        )
                    )
                collection_expression = self._expr.where(
                    collection_expression,
                    self._expr.function(
                        [parameter], self._to_expression(condition).expression
                    ),
                )
            body = self._to_expression(self._compile(elt))
            function = self._expr.function([parameter], body.expression)
            if predicate_only:
                return collection_expression, function
            return (
                self._expr.select(collection_expression, function),
                body.ptype,
            )
        finally:
            self._scopes.pop()

    def _compile_nested_comprehension(
        self,
        node: ast.AST,
        generators: List[ast.comprehension],
        elt: ast.expr,
    ) -> Tuple[Any, Optional[KRPC.Type]]:
        """Compile a comprehension with several 'for' clauses: the outer
        collection is flattened over a function computing the inner
        comprehension for each of its values."""
        generator = generators[0]
        if not isinstance(generator.target, ast.Name):
            raise self._error(node, "the loop variable must be a single name")
        collection = self._to_expression(self._compile(generator.iter))
        element = self._element_ptype(node, collection)
        parameter = self._expr.parameter(
            generator.target.id, self._remote_type(node, element)
        )
        scope = {generator.target.id: _Result(expression=parameter, ptype=element)}
        self._scopes.append(scope)
        try:
            collection_expression = collection.expression
            for condition_node in generator.ifs:
                condition = self._to_expression(self._compile(condition_node))
                collection_expression = self._expr.where(
                    collection_expression,
                    self._expr.function([parameter], condition.expression),
                )
            inner, inner_element = self._compile_comprehension(
                node, generators[1:], elt
            )
            return (
                self._expr.select_many(
                    collection_expression,
                    self._expr.function([parameter], inner),
                ),
                inner_element,
            )
        finally:
            self._scopes.pop()

    def _compile_dictcomp(self, node: ast.DictComp) -> _Result:
        if len(node.generators) != 1:
            raise self._error(node, "only a single 'for' clause is supported here")
        generator = node.generators[0]
        if not isinstance(generator.target, ast.Name):
            raise self._error(node, "the loop variable must be a single name")
        collection = self._to_expression(self._compile(generator.iter))
        element = self._element_ptype(node, collection)
        parameter = self._expr.parameter(
            generator.target.id, self._remote_type(node, element)
        )
        scope = {generator.target.id: _Result(expression=parameter, ptype=element)}
        self._scopes.append(scope)
        try:
            collection_expression = collection.expression
            for condition_node in generator.ifs:
                condition = self._to_expression(self._compile(condition_node))
                collection_expression = self._expr.where(
                    collection_expression,
                    self._expr.function([parameter], condition.expression),
                )
            key = self._to_expression(self._compile(node.key))
            value = self._to_expression(self._compile(node.value))
            return _Result(
                expression=self._expr.build_dictionary(
                    collection_expression,
                    self._expr.function([parameter], key.expression),
                    self._expr.function([parameter], value.expression),
                ),
                ptype=build_ptype(
                    KRPC.Type.DICTIONARY,
                    [key.ptype, value.ptype] if key.ptype and value.ptype else None,
                ),
            )
        finally:
            self._scopes.pop()

    def _compile_joinedstr(self, node: ast.JoinedStr) -> _Result:
        parts: List[_Result] = []
        for piece in node.values:
            if isinstance(piece, ast.Constant):
                parts.append(_Result(value=piece.value, is_value=True))
                continue
            assert isinstance(piece, ast.FormattedValue)
            if piece.format_spec is not None:
                raise self._error(node, "format specifiers are not supported")
            if piece.conversion not in (-1, 115):  # none or !s
                raise self._error(node, "conversion specifiers are not supported")
            parts.append(self._compile(piece.value))
        if all(part.is_value for part in parts):
            return _Result(
                value="".join(str(part.value) for part in parts), is_value=True
            )
        expressions = []
        for part in parts:
            if part.is_value:
                expressions.append(self._expr.constant_string(str(part.value)))
            elif part.ptype is not None and part.ptype.code == KRPC.Type.STRING:
                expressions.append(part.expression)
            else:
                expressions.append(
                    self._expr.convert_to_string(self._to_expression(part).expression)
                )
        return _Result(
            expression=self._expr.concat_strings(expressions),
            ptype=build_ptype(KRPC.Type.STRING),
        )

    def _compile_namedexpr(self, node: ast.NamedExpr) -> _Result:
        if self._active_statements is None:
            raise self._error(
                node,
                "assignment expressions are only supported within a function body",
            )
        if not isinstance(node.target, ast.Name):
            raise self._error(node, "unsupported assignment target")
        value = self._compile(node.value)
        return self._active_statements.assign_named(node, node.target.id, value)

    # Builtin functions

    @staticmethod
    def _is_supported_builtin(value: object) -> bool:
        return value in (
            len,
            sum,
            min,
            max,
            any,
            all,
            sorted,
            abs,
            round,
            int,
            float,
            str,
        )

    def _compile_builtin(
        self, node: ast.Call, func: Callable  # type: ignore[type-arg]
    ) -> _Result:
        if func in (min, max) and len(node.args) > 1:
            return self._compile_scalar_min_max(node, func)
        if len(node.args) != 1 or any(
            keyword.arg != "key" for keyword in node.keywords
        ):
            raise self._error(
                node,
                "%s() must be called with a single argument" % func.__name__,
            )
        argument = node.args[0]
        if func in (abs, round, int, float, str):
            return self._compile_scalar_builtin(node, func, argument)
        if (
            func in (any, all)
            and isinstance(argument, ast.GeneratorExp)
            and len(argument.generators) == 1
        ):
            collection, predicate = self._compile_comprehension(
                argument, argument.generators, argument.elt, predicate_only=True
            )
            op = self._expr.any if func is any else self._expr.all
            return _Result(
                expression=op(collection, predicate), ptype=build_ptype(KRPC.Type.BOOL)
            )
        result = self._compile(argument)
        if result.is_value and not node.keywords:
            try:
                return _Result(value=func(result.value), is_value=True)  # type: ignore[arg-type]
            except Exception as exc:
                raise self._error(node, str(exc)) from exc
        result = self._to_expression(result)
        if func is len:
            # Count requires a concrete collection
            expression = result.expression
            if isinstance(argument, ast.GeneratorExp):
                expression = self._expr.to_list(expression)
            return _Result(
                expression=self._expr.count(expression),
                ptype=build_ptype(KRPC.Type.SINT32),
            )
        element = self._element_ptype(node, result)
        if func is sum:
            return _Result(expression=self._expr.sum(result.expression), ptype=element)
        if func is min:
            return _Result(expression=self._expr.min(result.expression), ptype=element)
        if func is max:
            return _Result(expression=self._expr.max(result.expression), ptype=element)
        if func is any or func is all:
            op = self._expr.any if func is any else self._expr.all
            identity = self._expr.parameter("x", self._remote_type(node, element))
            predicate = self._expr.function([identity], identity)
            return _Result(
                expression=op(result.expression, predicate),
                ptype=build_ptype(KRPC.Type.BOOL),
            )
        if func is sorted:
            key = next(
                (keyword.value for keyword in node.keywords if keyword.arg == "key"),
                None,
            )
            if key is None:
                parameter = self._expr.parameter("x", self._remote_type(node, element))
                function = self._expr.function([parameter], parameter)
            elif isinstance(key, ast.Lambda) and len(key.args.args) == 1:
                parameter = self._expr.parameter(
                    key.args.args[0].arg, self._remote_type(node, element)
                )
                self._scopes.append(
                    {key.args.args[0].arg: _Result(expression=parameter, ptype=element)}
                )
                try:
                    body = self._to_expression(self._compile(key.body))
                finally:
                    self._scopes.pop()
                function = self._expr.function([parameter], body.expression)
            else:
                raise self._error(
                    node, "the key for sorted() must be a lambda taking one argument"
                )
            return _Result(
                expression=self._expr.to_list(
                    self._expr.order_by(result.expression, function)
                ),
                ptype=build_ptype(KRPC.Type.LIST, [element] if element else None),
            )
        raise self._error(node, "unsupported function %s()" % func.__name__)

    def _compile_scalar_builtin(
        self,
        node: ast.Call,
        func: Callable,  # type: ignore[type-arg]
        argument: ast.expr,
    ) -> _Result:
        result = self._compile(argument)
        if result.is_value:
            return _Result(value=func(result.value), is_value=True)  # type: ignore[arg-type]
        result = self._to_expression(result)
        if func is abs:
            return self._stdlib_scalar_call(node, "abs", [result])
        if func is round:
            rounded = self._stdlib_call(node, "round", [result])
            return _Result(
                expression=self._expr.cast(rounded.expression, self._type.int()),
                ptype=build_ptype(KRPC.Type.SINT32),
            )
        if func is int:
            return _Result(
                expression=self._expr.cast(result.expression, self._type.int()),
                ptype=build_ptype(KRPC.Type.SINT32),
            )
        if func is float:
            return self._ensure_double(result)
        # str
        return _Result(
            expression=self._expr.convert_to_string(result.expression),
            ptype=build_ptype(KRPC.Type.STRING),
        )

    def _compile_scalar_min_max(
        self, node: ast.Call, func: Callable  # type: ignore[type-arg]
    ) -> _Result:
        arguments = [self._compile(argument) for argument in node.args]
        if all(argument.is_value for argument in arguments):
            return _Result(
                value=func(argument.value for argument in arguments),  # type: ignore[arg-type]
                is_value=True,
            )
        member = "min" if func is min else "max"
        result = arguments[0]
        for argument in arguments[1:]:
            result = self._stdlib_scalar_call(node, member, [result, argument])
        return result

    # Conversions and type handling

    def _to_expression(self, result: _Result) -> _Result:
        if not result.is_value:
            return result
        value = result.value
        expr = self._expr
        if isinstance(value, bool):
            return _Result(
                expression=expr.constant_bool(value), ptype=build_ptype(KRPC.Type.BOOL)
            )
        if isinstance(value, int):
            if not -(2**31) <= value < 2**31:
                raise ExpressionCompilationError(
                    "Integer constant %d is out of range" % value
                )
            return _Result(
                expression=expr.constant_int(value), ptype=build_ptype(KRPC.Type.SINT32)
            )
        if isinstance(value, float):
            return _Result(
                expression=expr.constant_double(value),
                ptype=build_ptype(KRPC.Type.DOUBLE),
            )
        if isinstance(value, str):
            return _Result(
                expression=expr.constant_string(value),
                ptype=build_ptype(KRPC.Type.STRING),
            )
        if isinstance(value, ClassBase):
            ptype = self._class_ptype(type(value))
            return _Result(
                expression=expr.constant_object(value._object_id), ptype=ptype
            )
        if isinstance(value, Enum):
            ptype = self._enum_ptype(type(value))
            return _Result(
                expression=expr.cast(
                    expr.constant_int(value.value),
                    self._remote_type(None, ptype),
                ),
                ptype=ptype,
            )
        if self._is_remote_struct(type(value)):
            # A structure value is a tuple of its field values, so it has to be
            # recognized before the collection types below
            typ = self._struct_type_of(None, type(value))
            fields = [
                self._converted_expression(
                    _Result(value=field, is_value=True), field_type.protobuf_type
                )
                for field, field_type in zip(
                    cast(Tuple[object, ...], value), typ.field_types
                )
            ]
            return _Result(
                expression=expr.create_struct(
                    self._remote_type(None, typ.protobuf_type), fields
                ),
                ptype=typ.protobuf_type,
            )
        if isinstance(value, (list, tuple, set)):
            elements = [
                self._to_expression(_Result(value=element, is_value=True))
                for element in value
            ]
            if not elements:
                raise ExpressionCompilationError(
                    "Cannot use an empty collection in an expression"
                )
            expressions = [element.expression for element in elements]
            if isinstance(value, list):
                return _Result(
                    expression=expr.create_list(expressions),
                    ptype=build_ptype(KRPC.Type.LIST, [elements[0].ptype]),
                )
            if isinstance(value, tuple):
                return _Result(
                    expression=expr.create_tuple(expressions),
                    ptype=build_ptype(
                        KRPC.Type.TUPLE, [element.ptype for element in elements]
                    ),
                )
            return _Result(
                expression=expr.create_set(expressions),
                ptype=build_ptype(KRPC.Type.SET, [elements[0].ptype]),
            )
        if isinstance(value, dict):
            keys = [
                self._to_expression(_Result(value=key, is_value=True))
                for key in value.keys()
            ]
            values = [
                self._to_expression(_Result(value=item, is_value=True))
                for item in value.values()
            ]
            if not keys:
                raise ExpressionCompilationError(
                    "Cannot use an empty dictionary in an expression"
                )
            return _Result(
                expression=expr.create_dictionary(
                    [key.expression for key in keys],
                    [item.expression for item in values],
                ),
                ptype=build_ptype(
                    KRPC.Type.DICTIONARY, [keys[0].ptype, values[0].ptype]
                ),
            )
        raise ExpressionCompilationError(
            "Cannot use a value of type %s in an expression" % type(value).__name__
        )

    def _call_node(
        self, service: str, procedure: KRPC.Procedure, args: List[Optional[_Result]]
    ) -> _Result:
        call = KRPC.ProcedureCall()
        call.service = service
        call.procedure = procedure.name
        parameters = list(procedure.parameters)
        # Arguments are keyed by position, so a position skipped by a keyword
        # argument is simply absent and the server uses the parameter's default
        expressions: Dict[int, Any] = {}
        for position, arg in enumerate(args):
            if arg is None:
                continue
            parameter = parameters[position] if position < len(parameters) else None
            expressions[position] = self._argument_expression(arg, parameter)
        return _Result(
            expression=self._expr.call_with_arguments(call, expressions),
            ptype=(
                procedure.return_type if procedure.HasField("return_type") else None
            ),
        )

    def _argument_expression(
        self, result: _Result, parameter: Optional[KRPC.Parameter]
    ) -> Any:
        return self._converted_expression(
            result, parameter.type if parameter is not None else None
        )

    def _converted_expression(
        self, result: _Result, target: Optional[KRPC.Type]
    ) -> Any:
        """Convert a value to the type it is being used as. Python numbers do not
        distinguish the sizes of the server's numeric types, so numeric
        constants are built as the exact type wanted, and numeric expressions of
        a different type are converted with a cast."""
        if (
            target is not None
            and target.code in NUMERIC_CODES
            and result.is_value
            and isinstance(result.value, (int, float))
            and not isinstance(result.value, bool)
        ):
            value = result.value
            if target.code == KRPC.Type.DOUBLE:
                return self._expr.constant_double(float(value))
            if target.code == KRPC.Type.FLOAT:
                return self._expr.constant_float(float(value))
            if target.code == KRPC.Type.SINT32 and isinstance(value, int):
                return self._to_expression(result).expression
            # 64-bit and unsigned parameters: convert an int constant
            converted = self._to_expression(result)
            return self._expr.cast(
                converted.expression, self._remote_type(None, target)
            )
        converted = self._to_expression(result)
        if (
            target is not None
            and target.code in NUMERIC_CODES
            and converted.ptype is not None
            and converted.ptype.code in NUMERIC_CODES
            and converted.ptype.code != target.code
        ):
            return self._expr.cast(
                converted.expression, self._remote_type(None, target)
            )
        return converted.expression

    def _member(
        self,
        node: ast.AST,
        service: str,
        class_name: Optional[str],
        member: str,
        kind: str,
    ) -> Tuple[str, KRPC.Procedure]:
        entry = self._metadata.members.get((service, class_name, member, kind))
        if entry is None:
            target = service if class_name is None else service + "." + class_name
            raise self._error(
                node, "'%s' is not a remote member of %s" % (member, target)
            )
        return entry

    def _class_of(self, node: ast.AST, base: _Result) -> Tuple[str, str]:
        if base.ptype is None or base.ptype.code != KRPC.Type.CLASS:
            raise self._error(
                node,
                "cannot access a member of a value that is not an object "
                "with a known class",
            )
        return base.ptype.service, base.ptype.name

    def _class_ptype(self, python_type: type) -> KRPC.Type:
        for typ in self._client._types._types.values():
            if isinstance(typ, ClassType) and typ.python_type is python_type:
                return typ.protobuf_type
        raise ExpressionCompilationError(
            "Cannot determine the remote class of %s" % python_type.__name__
        )

    def _enum_ptype(self, python_type: type) -> KRPC.Type:
        for typ in self._client._types._types.values():
            if isinstance(typ, EnumerationType) and typ.python_type is python_type:
                return typ.protobuf_type
        raise ExpressionCompilationError(
            "Cannot determine the remote enumeration of %s" % python_type.__name__
        )

    def _struct_type_of(self, node: Optional[ast.AST], python_type: type) -> StructType:
        """The structure type a value of the given python type belongs to."""
        for typ in self._client._types._types.values():
            if isinstance(typ, StructType) and typ.python_type is python_type:
                return self._known_struct_type(node, typ)
        raise self._error(
            node, "cannot determine the remote structure of %s" % python_type.__name__
        )

    def _struct_type_named(
        self, node: Optional[ast.AST], service: str, name: str
    ) -> StructType:
        return self._known_struct_type(
            node, self._client._types.struct_type(service, name)
        )

    def _known_struct_type(
        self, node: Optional[ast.AST], typ: StructType
    ) -> StructType:
        """A structure type, checked to be one the server described the fields of.

        A structure whose fields name a type this client does not know is skipped
        when the service definitions are read, so it has no fields to work with."""
        ptype = typ.protobuf_type
        if not typ.has_fields or (ptype.service, ptype.name) not in (
            self._metadata.struct_fields
        ):
            raise self._error(
                node, "the fields of %s.%s are not known" % (ptype.service, ptype.name)
            )
        return typ

    def _is_remote_struct(self, value: object) -> bool:
        return isinstance(value, type) and any(
            isinstance(typ, StructType) and typ.python_type is value
            for typ in self._client._types._types.values()
        )

    def _is_remote_class(self, value: object) -> bool:
        return isinstance(value, type) and issubclass(value, ClassBase)

    @staticmethod
    def _is_service_object(value: object) -> bool:
        """Whether the value is a service object, or a service class (remote
        members of dynamically created services are bound to the class)."""
        if isinstance(value, ClassBase):
            return False
        if isinstance(value, type) and issubclass(value, ClassBase):
            return False
        cls = value if isinstance(value, type) else type(value)
        return any(name.startswith("_build_call_") for name in dir(cls))

    @staticmethod
    def _service_name(value: object) -> str:
        return value.__name__ if isinstance(value, type) else type(value).__name__

    def _element_ptype(self, node: ast.AST, collection: _Result) -> KRPC.Type:
        ptype = collection.ptype
        if ptype is not None and ptype.code in (KRPC.Type.LIST, KRPC.Type.SET):
            return ptype.types[0]
        raise self._error(
            node, "cannot determine the type of the elements of the collection"
        )

    def _remote_type(self, node: Optional[ast.AST], ptype: Optional[KRPC.Type]) -> Any:
        try:
            return remote_type(self._type, ptype, self._remote_types)
        except ValueError as exc:
            raise self._error(node, str(exc)) from exc

    @staticmethod
    def _collection_ptype(code: int, elements: List[_Result]) -> Optional[KRPC.Type]:
        if any(element.ptype is None for element in elements):
            return None
        return build_ptype(
            code, [element.ptype for element in elements if element.ptype]
        )

    def _error(
        self, node: Optional[ast.AST], message: str
    ) -> ExpressionCompilationError:
        location = ""
        if node is not None and hasattr(node, "lineno"):
            location = " (line %d)" % node.lineno  # type: ignore[attr-defined]
        return ExpressionCompilationError(
            "Cannot compile expression: " + message + location
        )
