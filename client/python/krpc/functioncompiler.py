"""Compilation of python expressions into server side expression nodes.
Statement compilation lives in krpc.functionstatements."""

# One class compiles every kind of expression node. The parts that could be
# lifted out, the builtin functions and the comprehensions, each reach back
# into a dozen of the compiler's own members
# pylint: disable=too-many-lines

from __future__ import annotations
import ast
import inspect
import operator
import textwrap
from enum import Enum
from typing import Any, Callable, Dict, List, Optional, Tuple, TYPE_CHECKING, cast

from krpc.error import FunctionCompilationError
from krpc.expressionutils import (
    Metadata,
    Result as _Result,
    NUMERIC_CODES,
    NUMERIC_NAMES,
    build_ptype,
    promote,
    remote_type,
)
from krpc.functionstatements import _StatementCompiler
from krpc.service import _member_name
from krpc.types import ClassBase, ClassType, EnumerationType, StructType
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.client import Client


def compile_function(client: Client, func: Callable) -> Any:  # type: ignore[type-arg]
    """Compile a python function into a server side function.

    The function must take no arguments. Its result is a KRPC.Expression
    object that, when evaluated on the server, computes what the function
    would compute if it were run on the client."""
    return _Compiler(client, func).compile()[0]


def compile_function_with_type(
    client: Client, func: Callable  # type: ignore[type-arg]
) -> Tuple[Any, Optional[KRPC.Type]]:
    """Compile a python function into a server side function, and return it
    with the type of the values it evaluates to, as tracked by the compiler.
    The type is None for a function that evaluates to no value."""
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

# The range of values each integer type holds, as its smallest value and one past
# its largest
_INTEGER_RANGES = {
    KRPC.Type.SINT32: (-(2**31), 2**31),
    KRPC.Type.SINT64: (-(2**63), 2**63),
    KRPC.Type.UINT32: (0, 2**32),
    KRPC.Type.UINT64: (0, 2**64),
}


def _in_range(value: int, code: int) -> bool:
    low, high = _INTEGER_RANGES[code]
    return low <= value < high


# Names the place in a conversion error for the operations on a list or a set
_ELEMENT_OF_A_COLLECTION = "for an element of the collection"


def _math_functions() -> Dict[Any, Tuple[str, int, int]]:
    """Functions from the math module with a server side equivalent, each with
    the smallest and largest number of arguments that equivalent takes."""
    import math  # pylint: disable=import-outside-toplevel

    return {
        # Raising to a power is an operator rather than a StdLib procedure
        math.pow: ("power", 2, 2),
        math.sqrt: ("sqrt", 1, 1),
        math.sin: ("sin", 1, 1),
        math.cos: ("cos", 1, 1),
        math.tan: ("tan", 1, 1),
        math.asin: ("asin", 1, 1),
        math.acos: ("acos", 1, 1),
        math.atan: ("atan", 1, 1),
        math.atan2: ("atan2", 2, 2),
        math.log: ("log", 1, 2),
        math.log10: ("log10", 1, 1),
        math.exp: ("exp", 1, 1),
        math.floor: ("floor", 1, 1),
        math.ceil: ("ceiling", 1, 1),
        math.fabs: ("abs", 1, 1),
        math.degrees: ("radians_to_degrees", 1, 1),
        math.radians: ("degrees_to_radians", 1, 1),
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


# The string operations, by the python method that reaches each one. A string is
# its own kind of value on the server, so these are named apart from the
# collection operations and dispatched on the static type of the value
_STRING_METHODS: Dict[str, Tuple[str, int]] = {
    "upper": ("string_to_upper", 0),
    "lower": ("string_to_lower", 0),
    "strip": ("string_trim", 0),
    "lstrip": ("string_trim_start", 0),
    "rstrip": ("string_trim_end", 0),
    "replace": ("string_replace", 2),
    "split": ("string_split", 1),
    "find": ("string_index_of", 1),
    "startswith": ("string_starts_with", 1),
    "endswith": ("string_ends_with", 1),
    "join": ("string_join", 1),
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

    def compile(self) -> Tuple[Any, Optional[KRPC.Type]]:
        node = self._parse()
        if isinstance(node, ast.Lambda):
            if node.args.args or node.args.posonlyargs or node.args.kwonlyargs:
                raise FunctionCompilationError(
                    "The function to compile must take no arguments"
                )
            result = self._compile(node.body)
        else:
            if node.args.args or node.args.posonlyargs or node.args.kwonlyargs:
                raise FunctionCompilationError(
                    "The function to compile must take no arguments"
                )
            result = _StatementCompiler(self).compile_body(node.body)
        result = self._to_expression(result)
        return result.expression, result.ptype

    def _parse(self) -> ast.AST:
        func = self._func
        try:
            lines, _ = inspect.getsourcelines(func)
        except (OSError, TypeError) as exc:
            raise FunctionCompilationError(
                "Cannot get the source code of the function to compile"
            ) from exc
        source = textwrap.dedent("".join(lines))
        # The source of a lambda embedded in a larger statement may not parse on
        # its own. It is tried as the statement itself, as a parenthesized
        # expression, and as the header of a compound statement, which is where a
        # lambda written inside a "with" or an "if" sits
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
            raise FunctionCompilationError(
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
                raise FunctionCompilationError(
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
            raise FunctionCompilationError(
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
        raise FunctionCompilationError("Cannot resolve the name '%s'" % name)

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
        return self._call_node(node, service, procedure, [base])

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
            if self._is_a_string(base) and func.attr in _STRING_METHODS:
                arguments = [self._compile(arg) for arg in node.args]
                if not base.is_value or any(
                    not argument.is_value for argument in arguments
                ):
                    return self._compile_string_method(node, func.attr, base, arguments)
                return self._compile_client_call(
                    node, getattr(base.value, func.attr), arguments
                )
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
            self._converted_expression(
                value,
                field_type.protobuf_type,
                node,
                "for field '%s' of %s.%s" % (name, ptype.service, ptype.name),
            )
            for name, value, field_type in zip(names, values, typ.field_types)
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
        when its arguments are known, and mapped to its server side equivalent
        when they are computed on the server."""
        if node.keywords:
            raise self._error(node, "keyword arguments are not supported here")
        member, least, most = _MATH_FUNCTIONS[func]
        if not least <= len(node.args) <= most:
            raise self._error(
                node,
                "math.%s with %d arguments has no server side equivalent"
                % (func.__name__, len(node.args)),
            )
        arguments = [self._compile(argument) for argument in node.args]
        if all(argument.is_value for argument in arguments):
            return self._compile_client_call(node, func)
        if member == "power":
            return self._compile_math_pow(arguments)
        return self._stdlib_call(node, member, arguments)

    def _compile_math_pow(self, arguments: List[_Result]) -> _Result:
        """Compile math.pow onto the power operator, converting the base to a
        double so the result is a float as it is in python."""
        left = self._ensure_double(self._to_expression(arguments[0]))
        right = self._to_expression(arguments[1])
        return _Result(
            expression=self._expr.power(left.expression, right.expression),
            ptype=promote(left.ptype, right.ptype),
        )

    def _compile_local_function_call(self, node: ast.Call, name: str) -> _Result:
        function, parameters, return_ptype = self._local_functions[name]
        if len(node.args) != len(parameters) or node.keywords:
            raise self._error(
                node,
                "'%s' takes %d positional arguments" % (name, len(parameters)),
            )
        arguments: Dict[str, Any] = {}
        for (parameter_name, parameter_ptype), argument in zip(parameters, node.args):
            arguments[parameter_name] = self._widened_expression(
                node,
                parameter_ptype,
                self._compile(argument),
                "for parameter '%s' of '%s'" % (parameter_name, name),
            )
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
            value = self._converted_expression(
                self._compile(node.args[0]),
                self._element_ptype(node, base),
                node,
                _ELEMENT_OF_A_COLLECTION,
            )
            return _Result(expression=self._expr.append(base.expression, value))
        if code == KRPC.Type.SET and func.attr == "add" and len(node.args) == 1:
            value = self._converted_expression(
                self._compile(node.args[0]),
                self._element_ptype(node, base),
                node,
                _ELEMENT_OF_A_COLLECTION,
            )
            return _Result(expression=self._expr.append(base.expression, value))
        return self._compile_collection_method(node, func.attr, base, code)

    # The collection operations, by the python method reaching each one, the type of
    # collection it is called on, and the number of arguments it takes
    _COLLECTION_METHODS = {
        ("remove", KRPC.Type.LIST): ("remove", 1),
        ("clear", KRPC.Type.LIST): ("clear", 0),
        ("remove", KRPC.Type.SET): ("remove", 1),
        ("discard", KRPC.Type.SET): ("remove", 1),
        ("clear", KRPC.Type.SET): ("clear", 0),
        ("clear", KRPC.Type.DICTIONARY): ("clear", 0),
        ("keys", KRPC.Type.DICTIONARY): ("dictionary_keys", 0),
        ("values", KRPC.Type.DICTIONARY): ("dictionary_values", 0),
    }

    def _compile_collection_method(
        self,
        node: ast.Call,
        name: str,
        base: _Result,
        code: Optional[int],
    ) -> Optional[_Result]:
        if name == "items" and code == KRPC.Type.DICTIONARY:
            if node.args or node.keywords:
                raise self._error(node, "'items' takes 0 arguments here")
            return self._compile_dictionary_items(node, base)
        entry = self._COLLECTION_METHODS.get((name, code))
        if entry is None:
            return None
        operation, arity = entry
        if len(node.args) != arity or node.keywords:
            raise self._error(
                node,
                "'%s' takes %d argument%s here"
                % (name, arity, "" if arity == 1 else "s"),
            )
        args = [
            self._converted_expression(
                self._compile(arg),
                self._element_ptype(node, base),
                node,
                _ELEMENT_OF_A_COLLECTION,
            )
            for arg in node.args
        ]
        expression = getattr(self._expr, operation)(base.expression, *args)
        ptype: Optional[KRPC.Type] = None
        if name == "remove":
            # Removing a value that is not there is an error, as in python
            kind = "list" if code == KRPC.Type.LIST else "set"
            return _Result(
                expression=self._expr.if_then(
                    self._expr.not_(expression),
                    self._expr.throw(
                        "KRPC",
                        "ArgumentException",
                        self._expr.constant_string("value not in " + kind),
                    ),
                )
            )
        if name == "keys":
            ptype = build_ptype(KRPC.Type.LIST, [base.ptype.types[0]])
        elif name == "values":
            ptype = build_ptype(KRPC.Type.LIST, [base.ptype.types[1]])
        elif name == "discard":
            ptype = build_ptype(KRPC.Type.BOOL)
        return _Result(expression=expression, ptype=ptype)

    def _compile_dictionary_items(self, node: ast.Call, dictionary: _Result) -> _Result:
        """The pairs of a dictionary, as a list of (key, value) tuples. The keys
        and the values of a dictionary are listed in the same order."""
        assert dictionary.ptype is not None
        key, value = dictionary.ptype.types
        expr = self._expr
        variable = expr.variable(
            "dictionary", self._remote_type(node, dictionary.ptype)
        )
        parameters = [
            expr.parameter("key", self._remote_type(node, key)),
            expr.parameter("value", self._remote_type(node, value)),
        ]
        pairs = expr.zip(
            expr.dictionary_keys(variable),
            expr.dictionary_values(variable),
            expr.lambda_(parameters, expr.create_tuple(parameters)),
        )
        return _Result(
            expression=expr.block_with_variables(
                [variable],
                [expr.assign(variable, dictionary.expression), expr.to_list(pairs)],
            ),
            ptype=build_ptype(
                KRPC.Type.LIST, [build_ptype(KRPC.Type.TUPLE, [key, value])]
            ),
        )

    @staticmethod
    def _is_an_integer(result: _Result) -> bool:
        return result.ptype is not None and result.ptype.code in _INTEGER_CODES

    @staticmethod
    def _is_a_string(result: _Result) -> bool:
        return (
            not result.is_value
            and result.ptype is not None
            and result.ptype.code == KRPC.Type.STRING
        ) or (result.is_value and isinstance(result.value, str))

    def _compile_string_method(
        self, node: ast.Call, name: str, base: _Result, arguments: List[_Result]
    ) -> _Result:
        """Compile a method call on a string, such as upper() or split()."""
        operation, arity = _STRING_METHODS[name]
        if len(node.args) != arity or node.keywords:
            raise self._error(
                node,
                "'%s' takes %d argument%s here"
                % (name, arity, "" if arity == 1 else "s"),
            )
        args = [self._to_expression(argument).expression for argument in arguments]
        expression = self._to_expression(base).expression
        result = getattr(self._expr, operation)(expression, *args)
        if name == "split":
            ptype = build_ptype(KRPC.Type.LIST, [build_ptype(KRPC.Type.STRING)])
        elif name == "find":
            ptype = build_ptype(KRPC.Type.SINT32)
        elif name in ("startswith", "endswith"):
            ptype = build_ptype(KRPC.Type.BOOL)
        else:
            ptype = build_ptype(KRPC.Type.STRING)
        return _Result(expression=result, ptype=ptype)

    def _compile_client_call(
        self,
        node: ast.Call,
        func: Callable,  # type: ignore[type-arg]
        args: Optional[List[_Result]] = None,
    ) -> _Result:
        """Call a client side function when compiling. The positional arguments
        are compiled here unless they are given."""
        if args is None:
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
        return self._call_node(node, service, procedure, args)

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
        if isinstance(node.op, ast.Mod):
            return self._compile_modulo(node, left, right)
        right_constant = (
            right.value
            if right.is_value and isinstance(right.value, (int, float))
            else None
        )
        left, right = self._operand_expressions(node, left, right)
        if isinstance(node.op, ast.Add) and (
            self._is_a_string(left) or self._is_a_string(right)
        ):
            return self._compile_string_addition(node, left, right)
        if isinstance(node.op, ast.Div) or (
            isinstance(node.op, ast.Pow)
            and right_constant is not None
            and right_constant < 0
        ):
            # Python division is true division, and a negative power of an
            # integer is a fraction; convert integer operands so the server does
            # not perform integer arithmetic
            left = self._ensure_double(left)
        op = getattr(self._expr, name)
        return _Result(
            expression=op(left.expression, right.expression),
            ptype=self._common_ptype(node, left, right),
        )

    def _compile_modulo(
        self, node: ast.BinOp, left: _Result, right: _Result
    ) -> _Result:
        """Python's remainder takes the sign of the divisor, where the server's
        takes the sign of the dividend. A nonzero remainder of the other sign
        has the divisor added to it."""
        dividend, divisor = self._operand_expressions(node, left, right)
        ptype = self._common_ptype(node, dividend, divisor)
        if ptype is None:
            return _Result(
                expression=self._expr.modulo(dividend.expression, divisor.expression)
            )
        expr = self._expr
        zero = self._converted_expression(_Result(value=0, is_value=True), ptype, node)

        def floored(dividend: Any, divisor: Any) -> Any:
            remainder = expr.variable("remainder", self._remote_type(node, ptype))
            adjust = expr.conditional_and(
                expr.not_equal(remainder, zero),
                expr.not_equal(
                    expr.less_than(remainder, zero), expr.less_than(divisor, zero)
                ),
            )
            return expr.block_with_variables(
                [remainder],
                [
                    expr.assign(remainder, expr.modulo(dividend, divisor)),
                    expr.conditional(adjust, expr.add(remainder, divisor), remainder),
                ],
            )

        return _Result(
            expression=self._with_temporaries(
                node, ptype, [("dividend", dividend), ("divisor", divisor)], floored
            ),
            ptype=ptype,
        )

    def _compile_string_addition(
        self, node: ast.BinOp, left: _Result, right: _Result
    ) -> _Result:
        """Adding strings concatenates them, which is a string operation rather
        than the arithmetic Add the operator otherwise maps to."""
        for operand in (left, right):
            if not self._is_a_string(operand):
                raise self._error(
                    node,
                    "a string can only be added to another string; "
                    "use str() to convert a value to one",
                )
        return _Result(
            expression=self._expr.string_concat([left.expression, right.expression]),
            ptype=build_ptype(KRPC.Type.STRING),
        )

    def _operand_expressions(
        self, node: ast.AST, left: _Result, right: _Result
    ) -> Tuple[_Result, _Result]:
        """The two operands of a binary operator as expressions. An integer
        constant beside an unsigned 64 bit integer is built at that type, as no
        signed type combines with it."""
        operands = [left, right]
        for i, other in ((0, right), (1, left)):
            operand = operands[i]
            beside_a_ulong = (
                not other.is_value
                and other.ptype is not None
                and other.ptype.code == KRPC.Type.UINT64
            )
            if (
                beside_a_ulong
                and operand.is_value
                and isinstance(operand.value, int)
                and not isinstance(operand.value, bool)
            ):
                operands[i] = _Result(
                    expression=self._converted_expression(operand, other.ptype, node),
                    ptype=other.ptype,
                )
        return self._to_expression(operands[0]), self._to_expression(operands[1])

    def _common_ptype(
        self, node: ast.AST, left: _Result, right: _Result
    ) -> Optional[KRPC.Type]:
        """The type two numeric operands are promoted to. None when either type
        is unknown or is not a number."""
        ptype = promote(left.ptype, right.ptype)
        if (
            ptype is None
            and left.ptype is not None
            and right.ptype is not None
            and left.ptype.code in NUMERIC_CODES
            and right.ptype.code in NUMERIC_CODES
        ):
            raise self._error(
                node,
                "%s and %s have no common type"
                % (NUMERIC_NAMES[left.ptype.code], NUMERIC_NAMES[right.ptype.code]),
            )
        return ptype

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
        left, right = self._operand_expressions(node, left, right)
        if self._is_an_integer(left) and self._is_an_integer(right):
            return self._compile_integer_floor_division(node, left, right)
        quotient = self._expr.divide(
            self._ensure_double(left).expression, right.expression
        )
        return self._stdlib_call(
            node,
            "floor",
            [_Result(expression=quotient, ptype=build_ptype(KRPC.Type.DOUBLE))],
        )

    def _compile_integer_floor_division(
        self, node: ast.BinOp, left: _Result, right: _Result
    ) -> _Result:
        """Integer division rounding toward negative infinity, as python's does.
        The server's integer division rounds toward zero, so a quotient with a
        remainder and operands of differing signs is one less."""
        ptype = self._common_ptype(node, left, right)
        assert ptype is not None
        expr = self._expr
        zero = self._integer_constant(0, ptype.code)
        one = self._integer_constant(1, ptype.code)

        def floored(dividend: Any, divisor: Any) -> Any:
            quotient = expr.divide(dividend, divisor)
            adjust = expr.conditional_and(
                expr.not_equal(expr.modulo(dividend, divisor), zero),
                expr.not_equal(
                    expr.less_than(dividend, zero), expr.less_than(divisor, zero)
                ),
            )
            return expr.conditional(adjust, expr.subtract(quotient, one), quotient)

        return _Result(
            expression=self._with_temporaries(
                node, ptype, [("dividend", left), ("divisor", right)], floored
            ),
            ptype=ptype,
        )

    def _with_temporaries(
        self,
        node: ast.AST,
        ptype: KRPC.Type,
        values: List[Tuple[str, _Result]],
        build: Callable[..., Any],
    ) -> Any:
        """Assign values that are used more than once to variables of one type,
        and build an expression on the variables. Each value is evaluated once,
        before the built expression."""
        expr = self._expr
        variables = [
            expr.variable(name, self._remote_type(node, ptype)) for name, _ in values
        ]
        assignments = [
            expr.assign(variable, self._converted_expression(value, ptype, node))
            for variable, (_, value) in zip(variables, values)
        ]
        return expr.block_with_variables(variables, assignments + [build(*variables)])

    def _stdlib_call(self, node: ast.AST, member: str, args: List[_Result]) -> _Result:
        """Embed a call to a StdLib procedure in the expression."""
        service, procedure = self._member(node, "StdLib", None, member, "method")
        arguments: List[Optional[_Result]] = list(args)
        return self._call_node(node, service, procedure, arguments)

    def _compile_boolop(self, node: ast.BoolOp) -> _Result:
        results = [self._compile(value) for value in node.values]
        if all(result.is_value for result in results):
            # The value of the first operand that decides the result, as python
            # gives, rather than a bool
            value: object = None
            for result in results:
                value = result.value
                if bool(value) != isinstance(node.op, ast.And):
                    break
            return _Result(value=value, is_value=True)
        is_and = isinstance(node.op, ast.And)
        operands = [self._to_expression(result) for result in results]
        if any(
            operand.ptype is not None and operand.ptype.code != KRPC.Type.BOOL
            for operand in operands
        ):
            combined = operands[0]
            for operand in operands[1:]:
                combined = self._compile_value_boolop(node, is_and, combined, operand)
            return combined
        op = self._expr.conditional_and if is_and else self._expr.conditional_or
        expression = operands[0].expression
        for operand in operands[1:]:
            expression = op(expression, operand.expression)
        return _Result(expression=expression, ptype=build_ptype(KRPC.Type.BOOL))

    def _compile_value_boolop(
        self, node: ast.BoolOp, is_and: bool, left: _Result, right: _Result
    ) -> _Result:
        """Compile a boolean operator on operands that are not booleans. It
        produces the operand that decides the result, as in python. The left
        operand is assigned to a temporary, as it is both tested and produced."""
        ptype = promote(left.ptype, right.ptype)
        if ptype is None:
            raise self._error(
                node,
                "the operands of %s must have a common type"
                % ("and" if is_and else "or"),
            )
        expr = self._expr
        variable = expr.variable("operand", self._remote_type(node, ptype))
        held = _Result(expression=variable, ptype=ptype)
        other = self._converted_expression(right, ptype, node)
        test = self._truth(node, held).expression
        if is_and:
            chosen = expr.conditional(test, other, variable)
        else:
            chosen = expr.conditional(test, variable, other)
        return _Result(
            expression=expr.block_with_variables(
                [variable],
                [
                    expr.assign(
                        variable, self._converted_expression(left, ptype, node)
                    ),
                    chosen,
                ],
            ),
            ptype=ptype,
        )

    def _compile_unaryop(self, node: ast.UnaryOp) -> _Result:
        if isinstance(node.op, ast.Not):
            result = self._compile_condition(node.operand)
            if result.is_value:
                return _Result(value=not result.value, is_value=True)
            return _Result(
                expression=self._expr.not_(self._to_expression(result).expression),
                ptype=build_ptype(KRPC.Type.BOOL),
            )
        result = self._compile(node.operand)
        if isinstance(node.op, ast.USub):
            if result.is_value:
                return _Result(value=-result.value, is_value=True)  # type: ignore[operator]
            result = self._to_expression(result)
            # Negating a uint gives a long, as in C#
            ptype = promote(build_ptype(KRPC.Type.SINT32), result.ptype)
            if result.ptype is not None and ptype is None:
                raise self._error(node, "cannot negate a value of this type")
            return _Result(expression=self._expr.negate(result.expression), ptype=ptype)
        if isinstance(node.op, ast.UAdd):
            return result
        if isinstance(node.op, ast.Invert):
            if result.is_value:
                return _Result(value=~result.value, is_value=True)  # type: ignore[operator]
            result = self._to_expression(result)
            if result.ptype is not None and not self._is_an_integer(result):
                raise self._error(
                    node, "~ is only supported on an integer; use not for a bool"
                )
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
                elif isinstance(op, ast.Is):
                    value = value and (left.value is right.value)
                elif isinstance(op, ast.IsNot):
                    value = value and (left.value is not right.value)
                elif type(op) in _COMPARE_OPS:
                    value = value and _COMPARE_OPS[type(op)][1](left.value, right.value)
                else:
                    raise self._error(
                        node, "unsupported comparison (%s)" % type(op).__name__
                    )
            return _Result(value=value, is_value=True)
        declared, preludes = self._hoist_chained_operands(node, operands)
        comparisons = []
        for left, op, right in zip(operands, node.ops, operands[1:]):
            if isinstance(op, (ast.In, ast.NotIn)):
                comparison = self._compile_membership(node, left, right)
                if isinstance(op, ast.NotIn):
                    comparison = self._expr.not_(comparison)
            elif isinstance(op, (ast.Is, ast.IsNot)):
                comparison = self._compile_is_none(node, left, right)
                if isinstance(op, ast.IsNot):
                    comparison = self._expr.not_(comparison)
            elif type(op) in _COMPARE_OPS:
                left, right = self._operand_expressions(node, left, right)
                comparison = getattr(self._expr, _COMPARE_OPS[type(op)][0])(
                    left.expression, right.expression
                )
            else:
                raise self._error(
                    node, "unsupported comparison (%s)" % type(op).__name__
                )
            prelude = preludes[len(comparisons)]
            if prelude and comparisons:
                comparison = self._expr.block(prelude + [comparison])
            comparisons.append(comparison)
        expression = comparisons[0]
        for comparison in comparisons[1:]:
            expression = self._expr.conditional_and(expression, comparison)
        if declared:
            # The first comparison is always made, so its operands are assigned
            # before the others
            expression = self._expr.block_with_variables(
                declared, preludes[0] + [expression]
            )
        return _Result(expression=expression, ptype=build_ptype(KRPC.Type.BOOL))

    def _compile_membership(
        self, node: ast.Compare, value: _Result, container: _Result
    ) -> Any:
        """A test of whether a value is in a string, a collection, or the keys of
        a dictionary."""
        container = self._to_expression(container)
        ptype = container.ptype
        if ptype is not None and ptype.code == KRPC.Type.DICTIONARY:
            return self._expr.contains_key(
                container.expression,
                self._converted_expression(
                    value, ptype.types[0], node, "for a key of the dictionary"
                ),
            )
        operation = (
            self._expr.string_contains
            if self._is_a_string(container)
            else self._expr.contains
        )
        return operation(container.expression, self._to_expression(value).expression)

    def _compile_is_none(self, node: ast.Compare, left: _Result, right: _Result) -> Any:
        """An identity comparison, which the algebra supports against None."""
        if right.is_value and right.value is None and not left.is_value:
            operand = left
        elif left.is_value and left.value is None and not right.is_value:
            operand = right
        else:
            raise self._error(node, "is and is not can only compare a value with None")
        return self._expr.is_null(self._to_expression(operand).expression)

    def _hoist_chained_operands(
        self, node: ast.Compare, operands: List[_Result]
    ) -> Tuple[List[Any], List[List[Any]]]:
        """Assign the operands of a chained comparison, except the last, to
        temporaries, and replace them in place with the temporary. A middle
        operand stands in two comparisons, and python evaluates each operand
        once, in order, and only while the comparisons so far hold. Returns the
        temporaries, and the assignments to make before each comparison."""
        declared: List[Any] = []
        preludes: List[List[Any]] = [[] for _ in node.ops]
        if len(operands) <= 2:
            return declared, preludes
        for index in range(len(operands) - 1):
            operand = operands[index]
            if operand.is_value or operand.ptype is None:
                continue
            variable = self._expr.variable(
                "chained comparison", self._remote_type(node, operand.ptype)
            )
            declared.append(variable)
            preludes[max(index - 1, 0)].append(
                self._expr.assign(variable, self._to_expression(operand).expression)
            )
            operands[index] = _Result(expression=variable, ptype=operand.ptype)
        return declared, preludes

    def _compile_ifexp(self, node: ast.IfExp) -> _Result:
        condition = self._compile_condition(node.test)
        if condition.is_value:
            return self._compile(node.body if condition.value else node.orelse)
        if_true = self._to_expression(self._compile(node.body))
        if_false = self._to_expression(self._compile(node.orelse))
        ptype = promote(if_true.ptype, if_false.ptype)
        if ptype is None and if_true.ptype is not None and if_false.ptype is not None:
            raise self._error(
                node, "both branches of a conditional must have the same type"
            )
        return _Result(
            expression=self._expr.conditional(
                condition.expression, if_true.expression, if_false.expression
            ),
            ptype=ptype,
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
        code = base.ptype.code if base.ptype is not None else None
        if (
            index.is_value
            and isinstance(index.value, int)
            and not isinstance(index.value, bool)
            and code != KRPC.Type.DICTIONARY
        ):
            if code == KRPC.Type.TUPLE:
                assert base.ptype is not None
                size = len(base.ptype.types)
                if not -size <= index.value < size:
                    raise self._error(node, "tuple index out of range")
                index = _Result(value=index.value % size, is_value=True)
            elif index.value < 0 and base.ptype is not None:
                return self._compile_index_from_end(node, base, index.value)
        if self._is_a_string(base):
            return _Result(
                expression=self._expr.string_get(
                    base.expression, self._to_expression(index).expression
                ),
                ptype=build_ptype(KRPC.Type.STRING),
            )
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

    def _compile_index_from_end(
        self, node: ast.Subscript, base: _Result, index: int
    ) -> _Result:
        """Index a list or a string by a negative constant, which counts from its
        end. The collection is assigned to a temporary, as it is both measured
        and indexed."""
        assert base.ptype is not None
        expr = self._expr
        offset = expr.constant_int(index)
        if self._is_a_string(base):
            ptype = build_ptype(KRPC.Type.STRING)

            def indexed(value: Any) -> Any:
                return expr.string_get(
                    value, expr.add(expr.string_length(value), offset)
                )

        else:
            ptype = self._element_ptype(node, base)

            def indexed(value: Any) -> Any:
                return expr.get(value, expr.add(expr.count(value), offset))

        return _Result(
            expression=self._with_temporaries(
                node, base.ptype, [("collection", base)], indexed
            ),
            ptype=ptype,
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
        if self._is_a_string(collection):
            return self._compile_string_slice(node, collection, lower, upper)
        element = self._element_ptype(node, collection)
        expr = self._expr
        variables: List[Any] = []
        statements: List[Any] = []
        if (
            lower is not None
            and upper is not None
            and not lower.is_value
            and collection.ptype is not None
            and lower.ptype is not None
        ):
            # The lower bound is used twice, so it and the collection evaluated
            # before it are assigned to temporaries
            for name, value in (("collection", collection), ("lower", lower)):
                assert value.ptype is not None
                variable = expr.variable(name, self._remote_type(node, value.ptype))
                variables.append(variable)
                statements.append(expr.assign(variable, value.expression))
            collection = _Result(expression=variables[0], ptype=collection.ptype)
            lower = _Result(expression=variables[1], ptype=lower.ptype)
        expression = collection.expression
        if lower is not None:
            lower = self._to_expression(lower)
            expression = expr.skip(expression, lower.expression)
        if upper is not None:
            upper = self._to_expression(upper)
            count = upper.expression
            if lower is not None:
                count = expr.subtract(count, lower.expression)
            expression = expr.take(expression, count)
        expression = expr.to_list(expression)
        if variables:
            expression = expr.block_with_variables(variables, statements + [expression])
        return _Result(
            expression=expression,
            ptype=build_ptype(KRPC.Type.LIST, [element]),
        )

    def _compile_string_slice(
        self,
        node: ast.Subscript,
        string: _Result,
        lower: Optional[_Result],
        upper: Optional[_Result],
    ) -> _Result:
        """Compile a slice of a string. A substring is taken by start and length,
        so a bound past the end of the string is clamped to it, as python does,
        and an absent bound is the start or the end of it."""
        expr = self._expr
        integer = build_ptype(KRPC.Type.SINT32)
        text = expr.variable(
            "string", self._remote_type(node, build_ptype(KRPC.Type.STRING))
        )
        length = expr.string_length(text)
        variables = [text]
        statements = [expr.assign(text, string.expression)]

        def clamped(name: str, bound: Optional[_Result], default: Any) -> Any:
            if bound is None:
                return default
            variable = expr.variable(name, self._remote_type(node, integer))
            variables.append(variable)
            statements.append(
                expr.assign(
                    variable,
                    self._widened_expression(
                        node, integer, bound, "for a bound of a slice"
                    ),
                )
            )
            return expr.conditional(expr.less_than(variable, length), variable, length)

        start = clamped("start", lower, expr.constant_int(0))
        end = clamped("end", upper, length)
        count = expr.conditional(
            expr.less_than(start, end), expr.subtract(end, start), expr.constant_int(0)
        )
        statements.append(expr.string_substring(text, start, count))
        return _Result(
            expression=expr.block_with_variables(variables, statements),
            ptype=build_ptype(KRPC.Type.STRING),
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
        if kind == "tuple":
            expressions = [self._to_expression(result) for result in results]
            return _Result(
                expression=self._expr.create_tuple(
                    [result.expression for result in expressions]
                ),
                ptype=self._collection_ptype(KRPC.Type.TUPLE, expressions),
            )
        expressions, element = self._collection_elements(results)
        code = KRPC.Type.LIST if kind == "list" else KRPC.Type.SET
        create = self._expr.create_list if kind == "list" else self._expr.create_set
        return _Result(
            expression=create([result.expression for result in expressions]),
            ptype=build_ptype(code, [element]) if element else None,
        )

    def _collection_elements(
        self, results: List[_Result]
    ) -> Tuple[List[_Result], Optional[KRPC.Type]]:
        """The elements of a list, a set, or the keys or values of a dictionary,
        as expressions, and the type the server widens them to. An integer
        constant among unsigned 64 bit integers is built at that type, as no
        signed type combines with it."""
        unsigned = any(
            not result.is_value
            and result.ptype is not None
            and result.ptype.code == KRPC.Type.UINT64
            for result in results
        )
        ulong = build_ptype(KRPC.Type.UINT64)
        expressions = []
        for result in results:
            if (
                unsigned
                and result.is_value
                and isinstance(result.value, int)
                and not isinstance(result.value, bool)
            ):
                result = _Result(
                    expression=self._converted_expression(result, ulong), ptype=ulong
                )
            expressions.append(self._to_expression(result))
        return expressions, self._widened_ptype(expressions)

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
        key_expressions, key_ptype = self._collection_elements(keys)
        value_expressions, value_ptype = self._collection_elements(values)
        return _Result(
            expression=self._expr.create_dictionary(
                [key.expression for key in key_expressions],
                [value.expression for value in value_expressions],
            ),
            ptype=(
                build_ptype(KRPC.Type.DICTIONARY, [key_ptype, value_ptype])
                if key_ptype and value_ptype
                else None
            ),
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
        the type of its elements. When predicate_only is set, elt is a
        condition and the result is the pair (collection, predicate function)
        instead."""
        if predicate_only and len(generators) != 1:
            raise self._error(node, "only a single 'for' clause is supported here")
        collection, parameter, scope = self._bind_generator(node, generators[0])
        self._scopes.append(scope)
        try:
            if len(generators) > 1:
                # The outer collection is flattened over a function computing the
                # inner comprehension for each of its values
                inner, inner_element = self._compile_comprehension(
                    node, generators[1:], elt
                )
                return (
                    self._expr.select_many(
                        collection, self._expr.lambda_([parameter], inner)
                    ),
                    inner_element,
                )
            if predicate_only:
                condition = self._to_expression(self._compile_condition(elt))
                return collection, self._expr.lambda_([parameter], condition.expression)
            body = self._to_expression(self._compile(elt))
            function = self._expr.lambda_([parameter], body.expression)
            return self._expr.select(collection, function), body.ptype
        finally:
            self._scopes.pop()

    def _bind_generator(
        self, node: ast.AST, generator: ast.comprehension
    ) -> Tuple[Any, Any, Dict[str, _Result]]:
        """Compile one 'for' clause of a comprehension. Returns its collection,
        filtered by the clause's conditions, the parameter each value is passed
        to, and the scope naming the value or the parts it unpacks into."""
        if generator.is_async:
            raise self._error(node, "async comprehensions are not supported")
        collection = self._iterable(self._compile(generator.iter))
        element = self._element_ptype(node, collection)
        target = generator.target
        name = target.id if isinstance(target, ast.Name) else "element"
        parameter = self._expr.parameter(name, self._remote_type(node, element))
        scope = {
            part: _Result(expression=self._unpacked(parameter, path), ptype=ptype)
            for part, path, ptype in self._unpack(node, target, element)
        }
        self._scopes.append(scope)
        try:
            expression = collection.expression
            for condition_node in generator.ifs:
                condition = self._to_expression(self._compile_condition(condition_node))
                expression = self._expr.where(
                    expression, self._expr.lambda_([parameter], condition.expression)
                )
        finally:
            self._scopes.pop()
        return expression, parameter, scope

    def _unpack(
        self,
        node: ast.AST,
        target: ast.expr,
        ptype: Optional[KRPC.Type],
        path: Tuple[int, ...] = (),
    ) -> List[Tuple[str, Tuple[int, ...], Optional[KRPC.Type]]]:
        """The names a target binds, each with the position of its value within
        the value being unpacked, and its type. A target is a name, or a tuple
        or list of targets that a tuple of as many values unpacks into."""
        if isinstance(target, ast.Name):
            return [(target.id, path, ptype)]
        if isinstance(target, (ast.Tuple, ast.List)):
            if (
                ptype is None
                or ptype.code != KRPC.Type.TUPLE
                or len(ptype.types) != len(target.elts)
            ):
                raise self._error(
                    node,
                    "only a tuple of %d values unpacks into this target"
                    % len(target.elts),
                )
            names = []
            for index, (part, part_ptype) in enumerate(zip(target.elts, ptype.types)):
                names.extend(self._unpack(node, part, part_ptype, path + (index,)))
            return names
        raise self._error(node, "unsupported target (%s)" % type(target).__name__)

    def _unpacked(self, value: Any, path: Tuple[int, ...]) -> Any:
        """The part of a tuple valued expression at a position found by _unpack."""
        for index in path:
            value = self._expr.get(value, self._expr.constant_int(index))
        return value

    def _compile_dictcomp(self, node: ast.DictComp) -> _Result:
        if len(node.generators) > 1:
            return self._compile_nested_dictcomp(node)
        collection, parameter, scope = self._bind_generator(node, node.generators[0])
        self._scopes.append(scope)
        try:
            key = self._to_expression(self._compile(node.key))
            value = self._to_expression(self._compile(node.value))
            return _Result(
                expression=self._expr.build_dictionary(
                    collection,
                    self._expr.lambda_([parameter], key.expression),
                    self._expr.lambda_([parameter], value.expression),
                ),
                ptype=build_ptype(
                    KRPC.Type.DICTIONARY,
                    [key.ptype, value.ptype] if key.ptype and value.ptype else None,
                ),
            )
        finally:
            self._scopes.pop()

    def _compile_nested_dictcomp(self, node: ast.DictComp) -> _Result:
        """A dictionary comprehension with several 'for' clauses, built from the
        flattened sequence of its (key, value) pairs."""
        pair = ast.copy_location(
            ast.Tuple(elts=[node.key, node.value], ctx=ast.Load()), node
        )
        pairs, ptype = self._compile_comprehension(node, node.generators, pair)
        expr = self._expr
        parameter = expr.parameter("pair", self._remote_type(node, ptype))
        return _Result(
            expression=expr.build_dictionary(
                pairs,
                expr.lambda_([parameter], expr.get(parameter, expr.constant_int(0))),
                expr.lambda_([parameter], expr.get(parameter, expr.constant_int(1))),
            ),
            ptype=build_ptype(
                KRPC.Type.DICTIONARY, list(ptype.types) if ptype else None
            ),
        )

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
            expression=self._expr.string_concat(expressions),
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
            reversed,
            abs,
            round,
            int,
            float,
            str,
            range,
            enumerate,
            zip,
        )

    def _compile_builtin(
        self, node: ast.Call, func: Callable  # type: ignore[type-arg]
    ) -> _Result:
        if func is range:
            return self._compile_range(node)
        if func is enumerate:
            return self._compile_enumerate(node)
        if func is zip:
            return self._compile_zip(node)
        if func in (min, max) and len(node.args) > 1:
            return self._compile_scalar_min_max(node, func)
        if func is round and len(node.args) == 2 and not node.keywords:
            return self._compile_round(node)
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
                value = func(result.value)  # type: ignore[arg-type]
            except Exception as exc:
                raise self._error(node, str(exc)) from exc
            # reversed() produces an iterator, which is not a value an expression
            # can hold, so it is read into a list
            return _Result(
                value=list(value) if func is reversed else value, is_value=True
            )
        result = self._to_expression(result)
        if func is len:
            if self._is_a_string(result):
                return _Result(
                    expression=self._expr.string_length(result.expression),
                    ptype=build_ptype(KRPC.Type.SINT32),
                )
            # Count requires a concrete collection
            expression = result.expression
            if isinstance(argument, ast.GeneratorExp):
                expression = self._expr.to_list(expression)
            return _Result(
                expression=self._expr.count(expression),
                ptype=build_ptype(KRPC.Type.SINT32),
            )
        result = self._iterable(result)
        element = self._element_ptype(node, result)
        if func is reversed:
            return _Result(
                expression=self._expr.to_list(self._expr.reverse(result.expression)),
                ptype=build_ptype(KRPC.Type.LIST, [element]),
            )
        if func is sum:
            return _Result(expression=self._expr.sum(result.expression), ptype=element)
        if func in (min, max):
            key = next(
                (keyword.value for keyword in node.keywords if keyword.arg == "key"),
                None,
            )
            if key is not None:
                # Selecting by a key gives back a value of the collection rather
                # than the smallest or largest key it produces
                selector = self._compile_key_function(node, key, element)
                operation = self._expr.min_by if func is min else self._expr.max_by
                return _Result(
                    expression=operation(result.expression, selector), ptype=element
                )
            operation = self._expr.min if func is min else self._expr.max
            return _Result(expression=operation(result.expression), ptype=element)
        if func is any or func is all:
            op = self._expr.any if func is any else self._expr.all
            identity = self._expr.parameter("x", self._remote_type(node, element))
            truth = self._truth(node, _Result(expression=identity, ptype=element))
            predicate = self._expr.lambda_([identity], truth.expression)
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
                function = self._expr.lambda_([parameter], parameter)
            else:
                function = self._compile_key_function(node, key, element)
            return _Result(
                expression=self._expr.to_list(
                    self._expr.order_by(result.expression, function)
                ),
                ptype=build_ptype(KRPC.Type.LIST, [element] if element else None),
            )
        raise self._error(node, "unsupported function %s()" % func.__name__)

    def _compile_range(self, node: ast.Call) -> _Result:
        """Compile range() into a list of its values, built on the server by a
        loop. The step is a constant, as its sign decides which way the loop
        counts."""
        if node.keywords or not 1 <= len(node.args) <= 3:
            raise self._error(node, "range() takes one to three arguments")
        arguments = [self._compile(argument) for argument in node.args]
        if len(arguments) == 1:
            arguments.insert(0, _Result(value=0, is_value=True))
        step = arguments[2] if len(arguments) == 3 else _Result(value=1, is_value=True)
        return self._range_loop(node, arguments[0], arguments[1], step)

    def _range_loop(
        self, node: Optional[ast.AST], start: _Result, end: _Result, step: _Result
    ) -> _Result:
        """A loop on the server building the list of values of a range."""
        integer = build_ptype(KRPC.Type.SINT32)
        numbers = build_ptype(KRPC.Type.LIST, [integer])
        expr = self._expr
        if not (
            step.is_value
            and isinstance(step.value, int)
            and not isinstance(step.value, bool)
        ):
            raise self._error(node, "the step of range() must be a constant integer")
        if step.value == 0:
            raise self._error(node, "the step of range() must not be zero")
        where = "for an argument of range()"
        result = expr.variable("range", self._remote_type(node, numbers))
        counter = expr.variable("counter", self._remote_type(node, integer))
        limit = expr.variable("end", self._remote_type(node, integer))
        compare = expr.less_than if step.value > 0 else expr.greater_than
        loop = expr.while_(
            compare(counter, limit),
            expr.block(
                [
                    expr.append(result, counter),
                    expr.assign(
                        counter,
                        expr.add(counter, self._to_expression(step).expression),
                    ),
                ]
            ),
        )
        return _Result(
            expression=expr.block_with_variables(
                [result, counter, limit],
                [
                    expr.assign(
                        result,
                        expr.create_empty_list(self._remote_type(node, integer)),
                    ),
                    expr.assign(
                        counter,
                        self._widened_expression(node, integer, start, where),
                    ),
                    expr.assign(
                        limit,
                        self._widened_expression(node, integer, end, where),
                    ),
                    loop,
                    result,
                ],
            ),
            ptype=numbers,
        )

    def _compile_enumerate(self, node: ast.Call) -> _Result:
        """Compile enumerate() into a list of (position, value) tuples, built on
        the server by a loop over the collection."""
        if (
            not 1 <= len(node.args) + len(node.keywords) <= 2
            or len(node.args) < 1
            or any(keyword.arg != "start" for keyword in node.keywords)
        ):
            raise self._error(
                node, "enumerate() takes a collection and an optional start"
            )
        collection = self._compile(node.args[0])
        if len(node.args) == 2:
            start = self._compile(node.args[1])
        elif node.keywords:
            start = self._compile(node.keywords[0].value)
        else:
            start = _Result(value=0, is_value=True)
        if collection.is_value and start.is_value:
            return self._folded_list(node, enumerate, [collection, start])
        collection = self._iterable(collection)
        element = self._element_ptype(node, collection)
        integer = build_ptype(KRPC.Type.SINT32)
        pair = build_ptype(KRPC.Type.TUPLE, [integer, element])
        pairs = build_ptype(KRPC.Type.LIST, [pair])
        expr = self._expr
        result = expr.variable("enumerate", self._remote_type(node, pairs))
        counter = expr.variable("counter", self._remote_type(node, integer))
        value = expr.variable("value", self._remote_type(node, element))
        loop = expr.for_each(
            value,
            collection.expression,
            expr.block(
                [
                    expr.append(result, expr.create_tuple([counter, value])),
                    expr.assign(counter, expr.add(counter, expr.constant_int(1))),
                ]
            ),
        )
        return _Result(
            expression=expr.block_with_variables(
                [result, counter, value],
                [
                    expr.assign(
                        result, expr.create_empty_list(self._remote_type(node, pair))
                    ),
                    expr.assign(
                        counter,
                        self._widened_expression(
                            node, integer, start, "for the start of enumerate()"
                        ),
                    ),
                    loop,
                    result,
                ],
            ),
            ptype=pairs,
        )

    def _compile_zip(self, node: ast.Call) -> _Result:
        """Compile zip() of two collections into a list of pairs."""
        if len(node.args) != 2 or node.keywords:
            raise self._error(
                node,
                "zip() takes two collections here, got %d arguments"
                % (len(node.args) + len(node.keywords)),
            )
        arguments = [self._compile(argument) for argument in node.args]
        if all(argument.is_value for argument in arguments):
            return self._folded_list(node, zip, arguments)
        collections = [self._iterable(argument) for argument in arguments]
        types = [self._element_ptype(node, collection) for collection in collections]
        expr = self._expr
        parameters = [
            expr.parameter(name, self._remote_type(node, ptype))
            for name, ptype in (("first", types[0]), ("second", types[1]))
        ]
        pairs = expr.zip(
            collections[0].expression,
            collections[1].expression,
            expr.lambda_(parameters, expr.create_tuple(parameters)),
        )
        return _Result(
            expression=expr.to_list(pairs),
            ptype=build_ptype(KRPC.Type.LIST, [build_ptype(KRPC.Type.TUPLE, types)]),
        )

    def _folded_list(
        self,
        node: ast.Call,
        func: Callable,  # type: ignore[type-arg]
        arguments: List[_Result],
    ) -> _Result:
        """Call a builtin producing a sequence on arguments known when compiling,
        and read the sequence into a list."""
        try:
            values = list(func(*[argument.value for argument in arguments]))
        except Exception as exc:
            raise self._error(node, str(exc)) from exc
        return _Result(value=values, is_value=True)

    def _compile_key_function(
        self, node: ast.Call, key: ast.expr, element: KRPC.Type
    ) -> Any:
        """Compile the lambda given as the key of a builtin, taking one value of the
        collection and producing the value to compare it by."""
        if not (isinstance(key, ast.Lambda) and len(key.args.args) == 1):
            raise self._error(node, "a key must be a lambda taking one argument")
        name = key.args.args[0].arg
        parameter = self._expr.parameter(name, self._remote_type(node, element))
        self._scopes.append({name: _Result(expression=parameter, ptype=element)})
        try:
            body = self._to_expression(self._compile(key.body))
        finally:
            self._scopes.pop()
        return self._expr.lambda_([parameter], body.expression)

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
        if (
            func is not str
            and result.ptype is not None
            and result.ptype.code not in NUMERIC_CODES
        ):
            raise self._error(
                node, "%s() is only supported on a number" % func.__name__
            )
        if func is abs:
            if self._is_an_integer(result):
                return self._compile_integer_abs(node, result)
            return self._stdlib_call(node, "abs", [result])
        if func in (round, int) and self._is_an_integer(result):
            return result
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

    def _compile_round(self, node: ast.Call) -> _Result:
        """Compile round(x, ndigits), which rounds to a number of decimal places.
        An integer rounded this way stays an integer, as it does in python."""
        value = self._compile(node.args[0])
        digits = self._compile(node.args[1])
        if value.is_value and digits.is_value:
            return _Result(
                value=round(value.value, digits.value),  # type: ignore[arg-type,call-overload]
                is_value=True,
            )
        value = self._to_expression(value)
        rounded = self._stdlib_call(node, "round", [value, self._to_expression(digits)])
        if self._is_an_integer(value):
            assert value.ptype is not None
            return _Result(
                expression=self._expr.cast(
                    rounded.expression, self._remote_type(node, value.ptype)
                ),
                ptype=value.ptype,
            )
        return rounded

    def _compile_integer_abs(self, node: ast.Call, value: _Result) -> _Result:
        """The absolute value of an integer, of the integer's own type."""
        ptype = value.ptype
        assert ptype is not None
        if ptype.code in (KRPC.Type.UINT32, KRPC.Type.UINT64):
            return value
        expr = self._expr
        zero = self._integer_constant(0, ptype.code)
        return _Result(
            expression=self._with_temporaries(
                node,
                ptype,
                [("value", value)],
                lambda x: expr.conditional(
                    expr.less_than(x, zero), expr.subtract(zero, x), x
                ),
            ),
            ptype=ptype,
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
        result = arguments[0]
        for argument in arguments[1:]:
            result, argument = self._operand_expressions(node, result, argument)
            if self._is_an_integer(result) and self._is_an_integer(argument):
                result = self._integer_min_max(node, func, result, argument)
            else:
                result = self._stdlib_call(
                    node, "min" if func is min else "max", [result, argument]
                )
        return result

    def _integer_min_max(
        self,
        node: ast.Call,
        func: Callable,  # type: ignore[type-arg]
        left: _Result,
        right: _Result,
    ) -> _Result:
        """The smaller or larger of two integers, of their common type. The
        first is chosen when they are equal, as python does."""
        ptype = self._common_ptype(node, left, right)
        assert ptype is not None
        expr = self._expr
        compare = expr.less_than if func is min else expr.greater_than
        return _Result(
            expression=self._with_temporaries(
                node,
                ptype,
                [("first", left), ("second", right)],
                lambda a, b: expr.conditional(compare(b, a), b, a),
            ),
            ptype=ptype,
        )

    # Conversions and type handling

    def _compile_condition(self, node: ast.expr) -> _Result:
        """Compile an expression used as a condition. The operands of and and
        or are each tested for truth, so they need not share a type."""
        if isinstance(node, ast.BoolOp):
            tests = [self._compile_condition(value) for value in node.values]
            is_and = isinstance(node.op, ast.And)
            if all(test.is_value for test in tests):
                values = [test.value for test in tests]
                return _Result(
                    value=all(values) if is_and else any(values), is_value=True
                )
            op = self._expr.conditional_and if is_and else self._expr.conditional_or
            expression = self._to_expression(tests[0]).expression
            for test in tests[1:]:
                expression = op(expression, self._to_expression(test).expression)
            return _Result(expression=expression, ptype=build_ptype(KRPC.Type.BOOL))
        return self._truth(node, self._compile(node))

    def _truth(self, node: ast.AST, result: _Result) -> _Result:
        """Whether a value is true, as python tests it in a condition: a number
        when it is not zero, a string or a collection when it is not empty, and
        an object when it is not None. A value of unknown type is left for the
        server to check."""
        if result.is_value:
            return _Result(value=bool(result.value), is_value=True)
        ptype = result.ptype
        if ptype is None or ptype.code == KRPC.Type.BOOL:
            return result
        expr = self._expr
        zero = expr.constant_int(0)
        if ptype.code in NUMERIC_CODES:
            test = expr.not_equal(
                result.expression,
                self._converted_expression(_Result(value=0, is_value=True), ptype),
            )
        elif ptype.code == KRPC.Type.STRING:
            test = expr.not_equal(expr.string_length(result.expression), zero)
        elif ptype.code in (KRPC.Type.LIST, KRPC.Type.SET, KRPC.Type.DICTIONARY):
            test = expr.not_equal(expr.count(result.expression), zero)
        elif ptype.code == KRPC.Type.CLASS:
            test = expr.not_(expr.is_null(result.expression))
        else:
            raise self._error(
                node, "cannot test whether this value is true; compare it explicitly"
            )
        return _Result(expression=test, ptype=build_ptype(KRPC.Type.BOOL))

    def _iterable(self, result: _Result) -> _Result:
        """A collection to iterate over. Iterating over a dictionary iterates
        over its keys, as in python."""
        result = self._to_expression(result)
        ptype = result.ptype
        if ptype is not None and ptype.code == KRPC.Type.DICTIONARY:
            return _Result(
                expression=self._expr.dictionary_keys(result.expression),
                ptype=build_ptype(KRPC.Type.LIST, [ptype.types[0]]),
            )
        return result

    def _to_expression(self, result: _Result) -> _Result:
        if not result.is_value:
            return result
        value = result.value
        expr = self._expr
        if isinstance(value, range):
            return self._range_loop(
                None,
                _Result(value=value.start, is_value=True),
                _Result(value=value.stop, is_value=True),
                _Result(value=value.step, is_value=True),
            )
        if isinstance(value, (abc.KeysView, abc.ValuesView, abc.ItemsView)):
            value = list(value)
        if isinstance(value, bool):
            return _Result(
                expression=expr.constant_bool(value), ptype=build_ptype(KRPC.Type.BOOL)
            )
        if isinstance(value, int):
            # The narrowest of int, long and ulong that holds the value
            for code in (KRPC.Type.SINT32, KRPC.Type.SINT64, KRPC.Type.UINT64):
                if _in_range(value, code):
                    return _Result(
                        expression=self._integer_constant(value, code),
                        ptype=build_ptype(code),
                    )
            raise FunctionCompilationError(
                "Integer constant %d is out of range" % value
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
        if isinstance(value, bytes):
            return _Result(
                expression=expr.constant_bytes(value),
                ptype=build_ptype(KRPC.Type.BYTES),
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
                    _Result(value=field, is_value=True),
                    field_type.protobuf_type,
                    None,
                    "for field '%s' of %s.%s"
                    % (name, typ.protobuf_type.service, typ.protobuf_type.name),
                )
                for name, field, field_type in zip(
                    typ.field_names, cast(Tuple[object, ...], value), typ.field_types
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
                raise FunctionCompilationError(
                    "Cannot use an empty collection in an expression"
                )
            expressions = [element.expression for element in elements]
            if isinstance(value, list):
                return _Result(
                    expression=expr.create_list(expressions),
                    ptype=build_ptype(KRPC.Type.LIST, [self._widened_ptype(elements)]),
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
                ptype=build_ptype(KRPC.Type.SET, [self._widened_ptype(elements)]),
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
                raise FunctionCompilationError(
                    "Cannot use an empty dictionary in an expression"
                )
            return _Result(
                expression=expr.create_dictionary(
                    [key.expression for key in keys],
                    [item.expression for item in values],
                ),
                ptype=build_ptype(
                    KRPC.Type.DICTIONARY,
                    [self._widened_ptype(keys), self._widened_ptype(values)],
                ),
            )
        raise FunctionCompilationError(
            "Cannot use a value of type %s in an expression" % type(value).__name__
        )

    def _call_node(
        self,
        node: Optional[ast.AST],
        service: str,
        procedure: KRPC.Procedure,
        args: List[Optional[_Result]],
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
            expressions[position] = self._argument_expression(
                node, arg, procedure, parameter
            )
        return _Result(
            expression=self._expr.call_with_arguments(call, expressions),
            ptype=(
                procedure.return_type if procedure.HasField("return_type") else None
            ),
        )

    def _argument_expression(
        self,
        node: Optional[ast.AST],
        result: _Result,
        procedure: KRPC.Procedure,
        parameter: Optional[KRPC.Parameter],
    ) -> Any:
        if parameter is None:
            return self._converted_expression(result, None, node)
        return self._converted_expression(
            result,
            parameter.type,
            node,
            "for parameter '%s' of '%s'"
            % (_member_name(parameter.name), procedure.name),
        )

    def _converted_expression(
        self,
        result: _Result,
        target: Optional[KRPC.Type],
        node: Optional[ast.AST] = None,
        where: str = "",
        hint: str = "",
    ) -> Any:
        """Convert a value to the type it is being used as. Python numbers do not
        distinguish the sizes of the server's numeric types, so numeric
        constants are built as the exact type wanted, and numeric expressions of
        a different type are converted with a cast. A float constant at an
        integer type is an error. The where and hint strings name the place in
        the error message."""
        if (
            target is not None
            and target.code in NUMERIC_CODES
            and result.is_value
            and isinstance(result.value, (int, float))
            and not isinstance(result.value, bool)
        ):
            value = result.value
            if isinstance(value, float) and target.code not in (
                KRPC.Type.DOUBLE,
                KRPC.Type.FLOAT,
            ):
                raise self._narrowing_error(node, KRPC.Type.DOUBLE, target, where, hint)
            if target.code == KRPC.Type.DOUBLE:
                return self._expr.constant_double(float(value))
            if target.code == KRPC.Type.FLOAT:
                return self._expr.constant_float(float(value))
            if not _in_range(value, target.code):
                message = "integer constant %d is out of range for %s" % (
                    value,
                    NUMERIC_NAMES[target.code],
                )
                raise self._error(node, message + (" " + where if where else ""))
            return self._integer_constant(value, target.code)
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

    def _integer_constant(self, value: int, code: int) -> Any:
        """A constant of one of the integer types."""
        return {
            KRPC.Type.SINT32: self._expr.constant_int,
            KRPC.Type.SINT64: self._expr.constant_long,
            KRPC.Type.UINT32: self._expr.constant_u_int,
            KRPC.Type.UINT64: self._expr.constant_u_long,
        }[code](value)

    def _widened_expression(
        self,
        node: Optional[ast.AST],
        target: Optional[KRPC.Type],
        value: _Result,
        where: str,
        hint: str = "",
    ) -> Any:
        """Convert a value to a type fixed in advance, by a variable's
        declaration or a local function's parameter. An implicit conversion
        widens, so a value of a wider type is an error. The where and hint
        strings name the place in the error message."""
        if target is None:
            return self._to_expression(value).expression
        if (
            target.code in NUMERIC_CODES
            and value.is_value
            and isinstance(value.value, (int, float))
            and not isinstance(value.value, bool)
        ):
            # A python numeric literal carries no type of its own and is
            # written at the target type
            return self._converted_expression(value, target, node, where, hint)
        converted = self._to_expression(value)
        if converted.ptype is None or converted.ptype == target:
            return converted.expression
        if target.code in NUMERIC_CODES and converted.ptype.code in NUMERIC_CODES:
            widened = promote(converted.ptype, target)
            if widened is None or widened.code != target.code:
                raise self._narrowing_error(
                    node, converted.ptype.code, target, where, hint
                )
            return self._expr.cast(
                converted.expression, self._remote_type(node, target)
            )
        raise self._error(node, "cannot use a value of a different type %s" % where)

    def _narrowing_error(
        self,
        node: Optional[ast.AST],
        source: int,
        target: KRPC.Type,
        where: str,
        hint: str,
    ) -> FunctionCompilationError:
        message = "no implicit conversion from %s to %s" % (
            NUMERIC_NAMES[source],
            NUMERIC_NAMES[target.code],
        )
        if where:
            message += " " + where
        return self._error(node, message + hint)

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
        raise FunctionCompilationError(
            "Cannot determine the remote class of %s" % python_type.__name__
        )

    def _enum_ptype(self, python_type: type) -> KRPC.Type:
        for typ in self._client._types._types.values():
            if isinstance(typ, EnumerationType) and typ.python_type is python_type:
                return typ.protobuf_type
        raise FunctionCompilationError(
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
    def _widened_ptype(elements: List[_Result]) -> Optional[KRPC.Type]:
        """The type the server widens the elements of a collection to."""
        ptype = elements[0].ptype
        for element in elements[1:]:
            ptype = promote(ptype, element.ptype)
        return ptype

    @staticmethod
    def _collection_ptype(code: int, elements: List[_Result]) -> Optional[KRPC.Type]:
        if any(element.ptype is None for element in elements):
            return None
        return build_ptype(
            code, [element.ptype for element in elements if element.ptype]
        )

    def _error(self, node: Optional[ast.AST], message: str) -> FunctionCompilationError:
        location = ""
        if node is not None and hasattr(node, "lineno"):
            location = " (line %d)" % node.lineno  # type: ignore[attr-defined]
        return FunctionCompilationError(
            "Cannot compile expression: " + message + location
        )
