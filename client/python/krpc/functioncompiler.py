"""Compilation of Python expressions into server side expression nodes.
Statement compilation lives in krpc.functionstatements."""

# One class compiles every kind of expression node, as the builtin functions and
# the comprehensions share the compiler's state

from __future__ import annotations

import ast
import inspect
import operator
import textwrap
import threading
from collections import abc
from collections.abc import Callable, Iterable
from enum import Enum
from typing import (
    TYPE_CHECKING,
    Any,
    NamedTuple,
    TypeGuard,
    cast,
)

import krpc.schema.KRPC_pb2 as KRPC
from krpc.definiteassignment import (
    assigned_names,
    local_function_nonlocal,
    nonlocal_names,
)
from krpc.encoder import Encoder
from krpc.error import FunctionCompilationError
from krpc.expressionutils import (
    NUMERIC_CODES,
    NUMERIC_NAMES,
    Metadata,
    build_ptype,
    common_numeric_code,
    common_ptype,
    join_ptype,
    plain_ptype,
    promote,
    remote_type,
    same_shape,
    unwrap_class,
    with_nullable,
)
from krpc.expressionutils import (
    Result as _Result,
)
from krpc.functionstatements import (
    UNTYPED,
    Recompile,
    UntypedRead,
    _StatementCompiler,
    annotation_ptype,
    compile_lambda_body,
)
from krpc.service import _member_name
from krpc.stream import Stream
from krpc.types import ClassBase, ClassType, EnumerationType, StructType

if TYPE_CHECKING:
    from krpc.client import Client


class Parameters(NamedTuple):
    """The parameters of a compiled function: the Python signature that binds
    the arguments of a call to them, and their types, in the order the server
    side function takes them."""

    signature: inspect.Signature
    ptypes: list[KRPC.Type]

    def bind(self, args: tuple[object, ...], kwargs: dict[str, object]) -> list[object]:
        """The values of a call's arguments, one per parameter. Raises
        TypeError for arguments that do not bind, as Python does."""
        bound = self.signature.bind(*args, **kwargs)
        bound.apply_defaults()
        return list(bound.arguments.values())


def compile_function(
    client: Client,
    func: Callable,
    streamed: bool = False,
) -> Any:
    """Compile a Python function into a server side function.

    Its result is a KRPC.Expression object that, when evaluated on the
    server, computes what the function would compute if it were run on the
    client. A streamed function must make a remote procedure call or use a
    state variable, as one that does neither has the same value on every
    update. It cannot take parameters."""
    return compile_function_with_type(client, func, streamed)[0]


# The type compile_function_with_type gives a function that evaluates to no value
NO_VALUE = KRPC.Type()


def compile_function_with_type(
    client: Client,
    func: Callable,
    streamed: bool = False,
) -> tuple[Any, KRPC.Type | None, Parameters]:
    """Compile a Python function into a server side function, and return it
    with the type of the values it evaluates to, as tracked by the compiler,
    and its parameters. The type is NO_VALUE for a function that evaluates to
    no value, and None for one whose type the compiler does not know."""
    # A variable is declared with the type of the first value assigned to it. A
    # later value that can be None where the first cannot widens the type, and
    # the function is compiled again with the variable declared that way
    variable_types: dict[tuple[Any, str | None], KRPC.Type] = {}
    client_values: dict[tuple[int, ...], Any] = {}
    while True:
        compiler = _Compiler(client, func, variable_types, client_values)
        try:
            result = compiler.compile(streamed)
        except Recompile:
            continue
        if not compiler._variables_widened:
            return result


_BINARY_OPS: dict[type, tuple[str, Callable[[Any, Any], Any]]] = {
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

# The types len() measures. The server has no length of bytes
_SIZED_CODES = (
    KRPC.Type.STRING,
    KRPC.Type.TUPLE,
    KRPC.Type.LIST,
    KRPC.Type.SET,
    KRPC.Type.DICTIONARY,
)

# The types whose values are true unless None. A kRPC tuple is never empty
_ALWAYS_TRUE_CODES = (
    KRPC.Type.CLASS,
    KRPC.Type.ENUMERATION,
    KRPC.Type.STRUCT,
    KRPC.Type.TUPLE,
)

# The most values a tuple the server builds can hold
_MAX_TUPLE_SIZE = 7

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


def _annotation_types(func: Callable) -> dict[str, type]:
    """The types named in a function's evaluated annotations, by name,
    including those inside a typing form such as List[T]. A string annotation
    names no type."""
    types: dict[str, type] = {}
    pending = list(getattr(func, "__annotations__", {}).values())
    while pending:
        annotation = unwrap_class(pending.pop())
        if isinstance(annotation, type):
            types.setdefault(annotation.__name__, annotation)
        pending.extend(getattr(annotation, "__args__", ()))
    return types


def _in_range(value: int, code: KRPC.Type.TypeCode) -> bool:
    low, high = _INTEGER_RANGES[code]
    return low <= value < high


# The Python name of each type that is not a number, for errors
_TYPE_NAMES = {
    KRPC.Type.BOOL: "bool",
    KRPC.Type.STRING: "str",
    KRPC.Type.BYTES: "bytes",
    KRPC.Type.LIST: "list",
    KRPC.Type.SET: "set",
    KRPC.Type.DICTIONARY: "dict",
    KRPC.Type.TUPLE: "tuple",
}


def _type_name(ptype: KRPC.Type) -> str:
    """The name of a type, as Python writes it, for errors."""
    if ptype.code in NUMERIC_NAMES:
        return NUMERIC_NAMES[ptype.code]
    if ptype.code in (KRPC.Type.CLASS, KRPC.Type.ENUMERATION, KRPC.Type.STRUCT):
        return "%s.%s" % (ptype.service, ptype.name)
    name = _TYPE_NAMES.get(ptype.code, "unknown")
    if ptype.types:
        name += "[%s]" % ", ".join(_type_name(element) for element in ptype.types)
    return name


def _value_or_key_ptype(ptype: KRPC.Type) -> KRPC.Type:
    """The type of the values of a set, or the keys of a dictionary, built
    from values of the given type. The server unwraps a nullable one, and a null
    one is an error when it is added."""
    return with_nullable(plain_ptype(ptype), False)


def _is_empty_collection(value: object) -> bool:
    return isinstance(value, (list, set, dict)) and not value


# The Python type of the values of each collection type
_COLLECTION_KINDS: dict[int, type] = {
    KRPC.Type.LIST: list,
    KRPC.Type.SET: set,
    KRPC.Type.DICTIONARY: dict,
    KRPC.Type.TUPLE: tuple,
}

# How an empty list, set or dictionary is written
_EMPTY_FORMS = {list: "[]", set: "set()", dict: "{}"}


def _orderable(ptype: KRPC.Type) -> bool:
    """Whether Python orders values of a type: numbers, bools, strings, and
    tuples of them."""
    if ptype.code == KRPC.Type.TUPLE:
        return all(_orderable(element) for element in ptype.types)
    return ptype.code in NUMERIC_CODES + (KRPC.Type.BOOL, KRPC.Type.STRING)


def _convertible(source: KRPC.Type, target: KRPC.Type) -> bool:
    """Whether a value of one type can be used as a value of another, apart
    from the numbers it holds, whose conversions are checked separately."""
    if source.code in NUMERIC_CODES and target.code in NUMERIC_CODES:
        return True
    if source.code != target.code:
        return False
    if source.code in (KRPC.Type.CLASS, KRPC.Type.ENUMERATION, KRPC.Type.STRUCT):
        return (source.service, source.name) == (target.service, target.name)
    return len(source.types) == len(target.types) and all(
        _convertible(a, b) for a, b in zip(source.types, target.types)
    )


def _comparable(left: KRPC.Type, right: KRPC.Type) -> bool:
    """Whether the server can test values of two types for equality. Numbers
    widen to a common type, as do the numbers within collections."""
    if left.code in NUMERIC_CODES and right.code in NUMERIC_CODES:
        return common_numeric_code({left.code, right.code}) is not None
    if left.code != right.code:
        return False
    if left.code in (KRPC.Type.CLASS, KRPC.Type.ENUMERATION, KRPC.Type.STRUCT):
        return (left.service, left.name) == (right.service, right.name)
    if len(left.types) != len(right.types):
        return False
    return all(_comparable(a, b) for a, b in zip(left.types, right.types))


def _math_functions() -> dict[Any, tuple[str, int, int]]:
    """Functions from the math module with a server side equivalent, each with
    the smallest and largest number of arguments that equivalent takes."""
    import math

    return {
        # math.pow maps onto the power operator
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
        math.copysign: ("copy_sign", 2, 2),
        math.hypot: ("hypot", 2, 2),
        math.dist: ("vector_distance", 2, 2),
        math.isnan: ("is_nan", 1, 1),
        math.isfinite: ("is_finite", 1, 1),
        math.degrees: ("radians_to_degrees", 1, 1),
        math.radians: ("degrees_to_radians", 1, 1),
    }


_MATH_FUNCTIONS = _math_functions()


def _is_a_math_function(value: object) -> TypeGuard[Callable[..., Any]]:
    """Whether a value is a math module function with a server side
    equivalent. A value that cannot be hashed is not."""
    try:
        return value in _MATH_FUNCTIONS
    except TypeError:
        return False


# The types of the keys a dictionary on the server can have
_KEY_CODES = (
    KRPC.Type.SINT32,
    KRPC.Type.SINT64,
    KRPC.Type.UINT32,
    KRPC.Type.UINT64,
    KRPC.Type.BOOL,
    KRPC.Type.STRING,
)

_COMPARE_OPS: dict[type, tuple[str, Callable[[Any, Any], Any]]] = {
    ast.Eq: ("equal", operator.eq),
    ast.NotEq: ("not_equal", operator.ne),
    ast.Gt: ("greater_than", operator.gt),
    ast.GtE: ("greater_than_or_equal", operator.ge),
    ast.Lt: ("less_than", operator.lt),
    ast.LtE: ("less_than_or_equal", operator.le),
}


# The string operations, by the Python method that reaches each one. A string is
# its own kind of value on the server, so these are named apart from the
# collection operations and dispatched on the static type of the value
_STRING_METHODS: dict[str, tuple[str, int]] = {
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


# The number of remote procedure calls the current thread has sent, through any
# connection a function has been compiled for
_SENT_CALLS = threading.local()
_SENT_CALLS_LOCK = threading.Lock()


def _count_sent_calls(client: Client) -> None:
    """Count the remote procedure calls each thread sends through a client's
    connection, so a client side function called when compiling can be checked
    for one."""
    connection = client._rpc_connection
    with _SENT_CALLS_LOCK:
        if getattr(connection, "_counts_sent_calls", False):
            return
        send = connection.send_message

        def counted(message: Any) -> None:
            _SENT_CALLS.count = _sent_calls() + 1
            send(message)

        connection.send_message = counted
        connection._counts_sent_calls = True  # pyright: ignore[reportAttributeAccessIssue]


def _sent_calls() -> int:
    return cast(int, getattr(_SENT_CALLS, "count", 0))


def _is_a_plain_read(node: ast.AST) -> bool:
    """Whether an expression only reads names and constants, so computing it
    can neither have an effect nor raise."""
    if isinstance(node, (ast.Name, ast.Constant)):
        return True
    if isinstance(node, (ast.Tuple, ast.List, ast.Set)):
        return all(_is_a_plain_read(element) for element in node.elts)
    return False


def _position(node: ast.AST) -> tuple[int, ...]:
    """The span of the source a node was parsed from."""
    return (
        getattr(node, "lineno", 0),
        getattr(node, "col_offset", 0),
        getattr(node, "end_lineno", 0) or 0,
        getattr(node, "end_col_offset", 0) or 0,
    )


class _Untyped(FunctionCompilationError):
    """A None or an empty collection with no type to give it."""


class _Compiler:
    def __init__(
        self,
        client: Client,
        func: Callable,
        variable_types: dict[tuple[Any, str | None], KRPC.Type] | None = None,
        client_values: dict[tuple[int, ...], Any] | None = None,
    ):
        self._client = client
        self._expr = client.krpc.Expression
        self._type = client.krpc.Type
        self._func = func
        # The types the function's parameter and return annotations name, by
        # name, as Python evaluated them where the function is defined
        self._annotation_types: dict[str, type] = _annotation_types(func)
        self._scopes: list[dict[str, _Result]] = []
        # The names the function assigns, which never resolve to a global
        self._local_names: set[str] = set()
        # The names the function declares nonlocal, which are state variables
        self._state_names: set[str] = set()
        # name -> (function expression, [(parameter name, ptype)], return ptype)
        self._local_functions: dict[str, Any] = {}
        # The local functions defined in a nested block, which are not callable
        # after it
        self._block_functions: set[str] = set()
        # The statement compiler for the function body being compiled, when
        # there is one; used by assignment expressions
        self._active_statements: Any = None
        # The call written as a statement being compiled, whose value is discarded
        self._statement_call: ast.AST | None = None
        # The number of remote procedure calls the function makes
        self._remote_calls = 0
        # Calls of procedures that return no value
        self._valueless_calls: list[Any] = []
        # The node being compiled, which an error converting a value names
        self._node: ast.AST | None = None
        if client._expression_metadata is None:
            client._expression_metadata = Metadata(client)
        _count_sent_calls(client)
        self._metadata: Metadata = client._expression_metadata
        # Naming a type is a round trip, so the objects naming them are shared
        # by every function compiled for this connection
        self._remote_types: dict[bytes, Any] = client._expression_remote_types
        # (function body, variable name) -> the type a variable is declared with,
        # where a value assigned to it after the first can be None. A name of None
        # is the return type
        self._variable_types: dict[tuple[Any, str | None], KRPC.Type] = (
            {} if variable_types is None else variable_types
        )
        # Whether compiling the function widened the type of a variable
        self._variables_widened = False
        # The values of the client side calls, by their position in the source,
        # so each call runs once however many times the function is compiled
        self._client_values: dict[tuple[int, ...], Any] = (
            {} if client_values is None else client_values
        )

    def compile(
        self, streamed: bool = False
    ) -> tuple[Any, KRPC.Type | None, Parameters]:
        # The compiler recurses over the syntax tree, so a deep enough expression
        # exhausts the interpreter's stack
        try:
            return self._compile_function(streamed)
        except RecursionError as exc:
            raise FunctionCompilationError(
                "Cannot compile expression: expression too deeply nested"
            ) from exc

    def _compile_function(
        self, streamed: bool
    ) -> tuple[Any, KRPC.Type | None, Parameters]:
        node = self._parse()
        self._node = node
        nested = local_function_nonlocal(node)
        if nested is not None:
            raise self._error(
                nested, "nonlocal is supported only in the function being compiled"
            )
        self._state_names = nonlocal_names(node)
        self._local_names = assigned_names(node) - self._state_names
        parameters = self._parameters(node, streamed)
        if isinstance(node, ast.Lambda):
            result = self._compile_statement(node.body)
            valueless = any(result.expression is call for call in self._valueless_calls)
        else:
            statements = _StatementCompiler(self)
            result = statements.compile_body(node.body, node.returns, parameters)
            valueless = statements.valueless
        if streamed and self._remote_calls == 0 and not self._state_names:
            raise self._error(
                node,
                "the function makes no remote procedure call, so its value is the "
                "same on every update",
            )
        result = self._to_expression(result)
        return (
            result.expression,
            NO_VALUE if valueless else result.ptype,
            Parameters(
                inspect.signature(self._func), [ptype for _, ptype, _ in parameters]
            ),
        )

    def _parameters(self, node: Any, streamed: bool) -> list[Any]:
        """The parameters of the function to compile, each a (name, ptype,
        parameter expression) triple. Only a def takes parameters, each with a
        type annotation, and a default the parameter's type can hold."""
        arguments = node.args
        names = arguments.posonlyargs + arguments.args + arguments.kwonlyargs
        if isinstance(node, ast.Lambda):
            if names or arguments.vararg or arguments.kwarg:
                raise self._error(
                    node,
                    "a lambda with parameters cannot be compiled; use a def with "
                    "type annotations",
                )
            return []
        if arguments.vararg or arguments.kwarg:
            raise self._error(node, "*args and **kwargs parameters are not supported")
        if names and streamed:
            raise self._error(
                node,
                "a function that is streamed, or checked for an event, cannot take "
                "parameters",
            )
        defaults = {
            name: parameter.default
            for name, parameter in inspect.signature(self._func).parameters.items()
            if parameter.default is not inspect.Parameter.empty
        }
        parameters = []
        for argument in names:
            if argument.annotation is None:
                raise self._error(
                    argument,
                    "annotate the type of parameter '%s'" % argument.arg,
                )
            ptype = annotation_ptype(self, argument.annotation)
            if argument.arg in defaults:
                self._check_default(argument, ptype, defaults[argument.arg])
            parameter = self._expr.parameter(
                argument.arg, self._remote_type(argument, ptype)
            )
            parameters.append((argument.arg, ptype, parameter))
        return parameters

    def _check_default(self, node: ast.arg, ptype: KRPC.Type, value: object) -> None:
        """Check that a parameter's default value is one its type can hold. A
        collection must be of the kind the type names, as the encoder takes any
        iterable."""
        valid = ptype.nullable
        kind = _COLLECTION_KINDS.get(ptype.code)
        if value is not None and kind is not None and not isinstance(value, kind):
            if _is_empty_collection(value) and kind in _EMPTY_FORMS:
                raise self._error(node, self._empty_kind_message(value, kind))
        elif value is not None:
            try:
                Encoder.encode(value, self._client._types.as_type(ptype))
                # The encoder writes any int as a varint, whatever its size
                bounds = _INTEGER_RANGES.get(ptype.code)
                valid = (
                    bounds is None
                    or not isinstance(value, int)
                    or bounds[0] <= value < bounds[1]
                )
            except Exception:
                valid = False
        if not valid:
            raise self._error(
                node,
                "the default value of parameter '%s' is not a value of type %s"
                % (node.arg, _type_name(ptype)),
            )

    def _parse(self) -> ast.Lambda | ast.FunctionDef:
        func = self._func
        if not inspect.isfunction(func):
            raise FunctionCompilationError(
                "Cannot compile %s; only a lambda or a def can be compiled"
                % type(func).__name__
            )
        if func.__name__ == "<lambda>":
            return self._parse_lambda()
        try:
            lines, first_line = inspect.getsourcelines(func)
        except (OSError, TypeError) as exc:
            raise FunctionCompilationError(
                "Cannot get the source code of the function to compile"
            ) from exc
        try:
            tree = ast.parse(textwrap.dedent("".join(lines)))
        except SyntaxError as exc:
            raise FunctionCompilationError(
                "Cannot parse the source code of the function to compile"
            ) from exc
        # Line numbers in errors count from the start of the file
        ast.increment_lineno(tree, max(first_line, 1) - 1)
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

    def _parse_lambda(self) -> ast.Lambda:
        """The lambda node of the function to compile. A lambda can sit anywhere
        in a statement spanning several lines, so we parse the whole source
        file. The lambda is the one at the source positions its code object
        records."""
        code = self._func.__code__
        try:
            lines, _ = inspect.findsource(code)
            tree = ast.parse("".join(lines))
        except (OSError, TypeError) as exc:
            raise FunctionCompilationError(
                "Cannot get the source code of the function to compile"
            ) from exc
        except SyntaxError as exc:
            raise FunctionCompilationError(
                "Cannot parse the source code of the function to compile"
            ) from exc
        arity = code.co_argcount
        candidates = [
            node
            for node in ast.walk(tree)
            if isinstance(node, ast.Lambda)
            and node.lineno == code.co_firstlineno
            and len(node.args.args) + len(node.args.posonlyargs) == arity
        ]
        start, end = self._code_span(code)
        if end is not None:
            # The body ends where the last of its instructions ends. A lambda
            # nested at the end of another ends there too, so the start of the
            # body decides between them
            candidates = [
                node
                for node in candidates
                if (node.body.end_lineno, node.body.end_col_offset) == end
            ]
            if len(candidates) > 1:
                candidates = [
                    node
                    for node in candidates
                    if (node.body.lineno, node.body.col_offset) == start
                ]
        if len(candidates) != 1:
            raise FunctionCompilationError(
                "Cannot identify the source code of the lambda to compile"
            )
        return candidates[0]

    @staticmethod
    def _code_span(code: Any) -> tuple[Any, Any]:
        """The source positions at which the instructions of a code object start
        and end, as (line, column) pairs. None when the interpreter does not
        record columns."""
        positions = getattr(code, "co_positions", None)
        if positions is None:
            return None, None
        # An instruction of no width, such as the one starting the function,
        # marks no source
        spans = [
            ((line, column), (end_line, end_column))
            for line, end_line, column, end_column in positions()
            if None not in (line, end_line, column, end_column)
            and (line, column) != (end_line, end_column)
        ]
        if not spans:
            return None, None
        return min(span[0] for span in spans), max(span[1] for span in spans)

    # Name resolution

    def _lookup(self, name: str, node: ast.AST | None = None) -> _Result:
        for scope in reversed(self._scopes):
            if name in scope:
                result = scope[name]
                if result.is_value and result.value is UNTYPED:
                    raise UntypedRead(name)
                return result
        if name in self._local_functions or name in self._block_functions:
            raise self._error(
                node,
                "local function '%s' can only be called" % name,
            )
        if name in self._local_names:
            raise self._error(
                node, "the local variable '%s' is read before it is assigned" % name
            )
        func = self._func
        freevars = func.__code__.co_freevars
        if name in freevars and func.__closure__ is not None:
            try:
                value = func.__closure__[freevars.index(name)].cell_contents
            except ValueError as exc:
                raise self._error(
                    node,
                    "the captured variable '%s' is not assigned when the function "
                    "is compiled" % name,
                ) from exc
            return _Result(value=value, is_value=True)
        if name in func.__globals__:
            return _Result(value=func.__globals__[name], is_value=True)
        builtins = func.__globals__.get("__builtins__", {})
        if not isinstance(builtins, dict):
            builtins = builtins.__dict__
        if name in builtins:
            return _Result(value=builtins[name], is_value=True)
        raise self._error(node, "cannot resolve the name '%s'" % name)

    # Compilation of AST nodes

    def _compile(self, node: ast.AST) -> _Result:
        method = getattr(self, "_compile_" + type(node).__name__.lower(), None)
        # A slice and a comprehension are compiled as part of the node holding them
        if method is None or isinstance(node, (ast.Slice, ast.comprehension)):
            raise self._error(node, "unsupported syntax (%s)" % type(node).__name__)
        previous = self._node
        self._node = node
        try:
            return cast(_Result, method(node))
        finally:
            self._node = previous

    def _compile_statement(self, node: ast.AST, discarded: bool = False) -> _Result:
        """Compile an expression that appears as a statement. A discarded
        statement is one whose value is not used, which a client side call
        cannot be."""
        if not discarded:
            return self._compile(node)
        previous = self._statement_call
        self._statement_call = node
        try:
            return self._compile(node)
        finally:
            self._statement_call = previous

    def _compile_constant(self, node: ast.Constant) -> _Result:
        return _Result(value=node.value, is_value=True)

    def _compile_name(self, node: ast.Name) -> _Result:
        return self._lookup(node.id, node)

    def _compile_attribute(self, node: ast.Attribute) -> _Result:
        base = self._compile(node.value)
        if base.is_value:
            value = base.value
            if isinstance(value, ClassBase) or self._is_service_object(value):
                if not hasattr(value, "_build_call_" + node.attr):
                    member = getattr(value, node.attr, None)
                    # The class, enumeration and structure types a service defines
                    # are attributes of it alongside its remote members
                    if self._is_service_object(value) and isinstance(
                        unwrap_class(member), type
                    ):
                        return _Result(value=member, is_value=True)
                    raise self._error(
                        node,
                        "'%s' is not a remote member of %s"
                        % (node.attr, type(value).__name__),
                    )
                if not isinstance(
                    inspect.getattr_static(value, node.attr, None), property
                ):
                    raise self._error(
                        node, "'%s' is a remote method, which needs a call" % node.attr
                    )
                call = self._client.get_call(getattr, value, node.attr)
                # The service definitions say where the value can be None, which
                # the pre-generated stubs do not
                procedure = self._metadata.procedures.get(
                    (call.service, call.procedure)
                )
                if procedure is not None and procedure.HasField("return_type"):
                    ptype = procedure.return_type
                else:
                    ptype = getattr(value, "_return_type_" + node.attr)().protobuf_type
                # A StdLib property has the same value on every evaluation
                if call.service != "StdLib":
                    self._remote_calls += 1
                return _Result(expression=self._expr.call(call), ptype=ptype)
            return self._fold(node, getattr, value, node.attr)
        # The base is a server side expression; a field of a structure is read
        # directly, and a member of an object is resolved from its class type
        if base.ptype is not None and base.ptype.code == KRPC.Type.STRUCT:
            return self._compile_struct_field(node, base)
        service, class_name = self._class_of(node, base)
        if (service, class_name, node.attr, "method") in self._metadata.members:
            raise self._error(
                node, "'%s' is a remote method, which needs a call" % node.attr
            )
        service, procedure = self._member(
            node, service, class_name, node.attr, "getter"
        )
        return self._call_node(node, service, procedure, [base])

    def _check_unpacking(self, node: ast.Call) -> None:
        """Raise for a call that unpacks its arguments."""
        if any(isinstance(argument, ast.Starred) for argument in node.args):
            raise self._error(node, "argument unpacking (*args) is not supported")
        if any(keyword.arg is None for keyword in node.keywords):
            raise self._error(
                node, "keyword argument unpacking (**kwargs) is not supported"
            )

    def _compile_call(self, node: ast.Call) -> _Result:
        self._check_unpacking(node)
        func = node.func
        # Calls of local functions defined within the compiled function
        if isinstance(func, ast.Name) and func.id in self._local_functions:
            return self._compile_local_function_call(node, func.id)
        if isinstance(func, ast.Name) and func.id in self._block_functions:
            raise self._error(
                node,
                "local function '%s' is defined in a nested block, so it can only "
                "be called within that block" % func.id,
            )
        if self._is_typing_cast(func):
            return self._compile_typing_cast(node)
        # Calls of supported builtin functions, and of captured bound methods
        # of remote objects and services
        if isinstance(func, ast.Name):
            target = self._lookup(func.id, func)
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
            target = self._lookup(func.id, func)
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
                if base.ptype is not None and base.ptype.code != KRPC.Type.CLASS:
                    raise self._error(
                        node,
                        "'%s' is not supported on a value of type %s"
                        % (func.attr, _type_name(base.ptype)),
                    )
                return self._compile_remote_call(node, func.attr, base)
            # A method on a plain client-side value
            method = getattr(base.value, func.attr, None)
            if method is None:
                raise self._error(node, "cannot resolve method '%s'" % func.attr)
            if _is_a_math_function(method):
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
            if _is_a_math_function(target.value):
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
        values: list[_Result | None] = [None] * len(names)
        if len(node.args) > len(names):
            raise self._error(
                node,
                "%s.%s has %d fields, got %d positional field values"
                % (ptype.service, ptype.name, len(names), len(node.args)),
            )
        for index, argument in enumerate(node.args):
            values[index] = self._compile(argument)
        given = [value for value in values if value is not None and not value.is_value]
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
            field_value = self._compile(keyword.value)
            values[index] = field_value
            if not field_value.is_value:
                given.append(field_value)
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
        # The server evaluates fields in declaration order, and Python in the
        # order given
        variables, assignments, values = self._in_source_order(node, given, values)
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
        structure = self._expr.create_struct(
            self._remote_type(node, ptype), expressions
        )
        if variables:
            structure = self._expr.block_with_variables(
                variables, assignments + [structure]
            )
        return _Result(expression=structure, ptype=ptype)

    def _compile_math_call(
        self,
        node: ast.Call,
        func: Callable,
    ) -> _Result:
        """Compile a call to a math module function: evaluated on the client
        when its arguments are known, and mapped to its server side equivalent
        when they are computed on the server."""
        if node.keywords:
            raise self._error(node, "keyword arguments are not supported here")
        member, least, most = _MATH_FUNCTIONS[func]
        arguments = [self._compile(argument) for argument in node.args]
        if all(argument.is_value for argument in arguments):
            return self._compile_client_call(node, func, arguments)
        if not least <= len(node.args) <= most:
            raise self._error(
                node,
                "math.%s with %d arguments has no server side equivalent"
                % (func.__name__, len(node.args)),
            )
        if member == "power":
            return self._compile_math_pow(arguments)
        if member in ("floor", "ceiling"):
            return self._compile_math_rounding(node, member, arguments[0])
        if member == "vector_distance":
            return self._compile_math_dist(node, arguments)
        return self._stdlib_call(node, member, arguments)

    def _compile_math_dist(self, node: ast.Call, arguments: list[_Result]) -> _Result:
        """Compile math.dist of two points given as tuples, of any number of
        coordinates. Three coordinates map onto StdLib.VectorDistance, and any
        other number onto StdLib.Hypot of the differences."""
        points = [self._to_expression(argument) for argument in arguments]
        sizes = {
            len(point.ptype.types)
            for point in points
            if point.ptype is not None and point.ptype.code == KRPC.Type.TUPLE
        }
        if len(sizes) != 1 or any(
            point.ptype is None
            or point.ptype.code != KRPC.Type.TUPLE
            or any(
                coordinate.code not in NUMERIC_CODES for coordinate in point.ptype.types
            )
            for point in points
        ):
            raise self._error(
                node,
                "math.dist takes two tuples of numbers with the same number of "
                "coordinates",
            )
        size = sizes.pop()
        if size == 3:
            return self._stdlib_call(node, "vector_distance", points)
        expr = self._expr
        ptypes = [cast(KRPC.Type, point.ptype) for point in points]
        variables = [
            expr.variable(name, self._remote_type(node, ptype))
            for name, ptype in zip(("p", "q"), ptypes)
        ]
        double = build_ptype(KRPC.Type.DOUBLE)
        distance: _Result = _Result(expression=expr.constant_double(0), ptype=double)
        for i in range(size):
            # Each coordinate is a double, as in Python
            first, second = [
                self._ensure_double(
                    _Result(
                        expression=expr.get(variable, expr.constant_int(i)),
                        ptype=ptype.types[i],
                    )
                )
                for variable, ptype in zip(variables, ptypes)
            ]
            difference = _Result(
                expression=expr.subtract(first.expression, second.expression),
                ptype=double,
            )
            distance = self._stdlib_call(node, "hypot", [distance, difference])
        return _Result(
            expression=expr.block_with_variables(
                variables,
                [
                    expr.assign(variable, point.expression)
                    for variable, point in zip(variables, points)
                ]
                + [distance.expression],
            ),
            ptype=double,
        )

    def _compile_math_rounding(
        self, node: ast.Call, member: str, argument: _Result
    ) -> _Result:
        """Compile math.floor or math.ceil, which give an integer as round()
        does."""
        argument = self._to_expression(argument)
        if self._is_an_integer(argument):
            return argument
        rounded = self._stdlib_call(node, member, [argument])
        return _Result(
            expression=self._expr.cast(rounded.expression, self._type.int()),
            ptype=build_ptype(KRPC.Type.SINT32),
        )

    def _compile_math_pow(self, arguments: list[_Result]) -> _Result:
        """Compile math.pow onto the power operator, converting the base to a
        double so the result is a float as it is in Python."""
        left = self._ensure_double(self._to_expression(arguments[0]))
        right = self._widen_single(self._to_expression(arguments[1]))
        return _Result(
            expression=self._expr.power(left.expression, right.expression),
            ptype=promote(left.ptype, right.ptype),
        )

    def _compile_local_function_call(self, node: ast.Call, name: str) -> _Result:
        function, parameters, return_ptype = self._local_functions[name]
        if node.keywords:
            raise self._error(
                node,
                "'%s' takes positional arguments only" % name,
            )
        if len(node.args) != len(parameters):
            raise self._error(
                node,
                "'%s' takes %d positional argument%s"
                % (name, len(parameters), "" if len(parameters) == 1 else "s"),
            )
        arguments: dict[str, Any] = {}
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

    def _remote_bound_method(self, value: object) -> tuple[object, str] | None:
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

    # The operations changing a collection, by the Python method reaching each one
    # and the type of collection it is called on, with the number of arguments it
    # takes
    _COLLECTION_MUTATIONS = {
        ("append", KRPC.Type.LIST): ("append", 1),
        ("add", KRPC.Type.SET): ("append", 1),
        ("remove", KRPC.Type.LIST): ("remove", 1),
        ("clear", KRPC.Type.LIST): ("clear", 0),
        ("remove", KRPC.Type.SET): ("remove", 1),
        ("discard", KRPC.Type.SET): ("remove", 1),
        ("clear", KRPC.Type.SET): ("clear", 0),
        ("clear", KRPC.Type.DICTIONARY): ("clear", 0),
    }

    def _compile_collection_mutation(
        self, node: ast.Call, func: ast.Attribute, base: _Result
    ) -> _Result | None:
        """Compile a method call that changes a server side collection, such as
        appending to a list variable, or one that views a dictionary."""
        name = func.attr
        code = base.ptype.code if base.ptype is not None else None
        entry = None if code is None else self._COLLECTION_MUTATIONS.get((name, code))
        if entry is None:
            return self._compile_dictionary_view(node, name, base)
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
                "for an element of the collection",
            )
            for arg in node.args
        ]
        expression = getattr(self._expr, operation)(base.expression, *args)
        if name == "remove":
            # Removing a value that is not there is an error, as in Python
            if code == KRPC.Type.LIST:
                return _Result(
                    expression=self._removed(
                        expression, "ArgumentException", "value not in list"
                    )
                )
            return _Result(
                expression=self._removed(
                    expression, "KeyNotFoundException", "value not in set"
                )
            )
        if name == "discard":
            # Whether a value was removed is not part of Python's discard, so the
            # result is dropped by a test whose body has no effect
            return _Result(
                expression=self._expr.if_then(
                    expression, self._expr.constant_bool(False)
                )
            )
        return _Result(expression=expression)

    def _removed(self, removal: Any, exception: str, message: str) -> Any:
        """A removal that raises the given kRPC exception when the value is not
        in the collection."""
        return self._expr.if_then(
            self._expr.not_(removal),
            self._expr.throw("KRPC", exception, self._expr.constant_string(message)),
        )

    def _compile_dictionary_view(
        self, node: ast.Call, name: str, base: _Result
    ) -> _Result | None:
        """Compile items(), keys() or values() of a server side dictionary. None
        for any other method."""
        ptype = base.ptype
        if (
            ptype is None
            or ptype.code != KRPC.Type.DICTIONARY
            or name not in ("items", "keys", "values")
        ):
            return None
        if node.args or node.keywords:
            raise self._error(node, "'%s' takes 0 arguments here" % name)
        if name == "items":
            return self._compile_dictionary_items(node, base)
        if name == "keys":
            return _Result(
                expression=self._expr.dictionary_keys(base.expression),
                ptype=build_ptype(KRPC.Type.LIST, [ptype.types[0]]),
            )
        return _Result(
            expression=self._expr.dictionary_values(base.expression),
            ptype=build_ptype(KRPC.Type.LIST, [ptype.types[1]]),
        )

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
    def _is_a_bool(result: _Result) -> bool:
        return result.ptype is not None and result.ptype.code == KRPC.Type.BOOL

    def _check_numbers(self, node: ast.AST, results: list[_Result]) -> None:
        """Raise for an operand of a numeric operation whose type is known and is
        not a number."""
        for result in results:
            if result.ptype is None or result.ptype.code in NUMERIC_CODES:
                continue
            hint = ""
            if self._is_a_bool(result):
                hint = "; use 1 if ... else 0 to convert a bool to a number"
            raise self._error(node, "the operation needs numbers" + hint)

    @staticmethod
    def _is_a_string(result: _Result) -> bool:
        return (
            not result.is_value
            and result.ptype is not None
            and result.ptype.code == KRPC.Type.STRING
        ) or (result.is_value and isinstance(result.value, str))

    def _compile_string_method(
        self, node: ast.Call, name: str, base: _Result, arguments: list[_Result]
    ) -> _Result:
        """Compile a method call on a string, such as upper() or split()."""
        operation, arity = _STRING_METHODS[name]
        if len(node.args) != arity or node.keywords:
            raise self._error(
                node,
                "'%s' takes %d argument%s here"
                % (name, arity, "" if arity == 1 else "s"),
            )
        args = []
        for argument in arguments:
            argument = self._to_expression(argument)
            ptype = argument.ptype
            if name == "join":
                valid = (
                    ptype is None
                    or ptype.code in (KRPC.Type.LIST, KRPC.Type.SET)
                    and ptype.types[0].code == KRPC.Type.STRING
                )
                wanted = "a list or set of str"
            else:
                valid = ptype is None or ptype.code == KRPC.Type.STRING
                wanted = "a str"
            if not valid:
                raise self._error(
                    node,
                    "str.%s takes %s here, not a value of type %s"
                    % (name, wanted, _type_name(cast(KRPC.Type, ptype))),
                )
            args.append(argument.expression)
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
        func: Any,
        args: list[_Result] | None = None,
    ) -> _Result:
        """Call a client side function when compiling. The positional arguments
        are compiled here unless they are given."""
        if isinstance(func, Stream) or isinstance(
            getattr(func, "__self__", None), Stream
        ):
            raise self._error(
                node,
                "a stream is read once, on the client, when the function is "
                "compiled; make the stream's remote procedure call in the function "
                "instead",
            )
        if node is self._statement_call:
            raise self._error(
                node,
                "a client side function called as a statement would run once, "
                "when the function is compiled",
            )
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
        position = _position(node)
        if position in self._client_values:
            return _Result(value=self._client_values[position], is_value=True)
        value = self._fold(
            node,
            lambda: func(
                *[arg.value for arg in args],
                **{name: arg.value for name, arg in keywords.items()},  # pyright: ignore[reportCallIssue]
            ),
            prefix="error calling client side function: ",
        ).value
        self._client_values[position] = value
        return _Result(value=value, is_value=True)

    def _compile_remote_call(
        self, node: ast.Call, member: str, base: _Result
    ) -> _Result:
        if base.is_value and self._is_remote_class(base.value):
            # A static class method, called on the class itself
            protobuf_type = self._class_ptype(cast(type, unwrap_class(base.value)))
            service, procedure = self._member(
                node, protobuf_type.service, protobuf_type.name, member, "static"
            )
            instance_args: list[_Result | None] = []
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
        args: list[_Result | None] = list(instance_args)
        if len(node.args) > len(parameters):
            raise self._error(
                node,
                "too many arguments for %s: expected at most %d, got %d"
                % (procedure.name, len(parameters), len(node.args)),
            )
        args.extend(self._compile(arg) for arg in node.args)
        given = [arg for arg in args if arg is not None and not arg.is_value]
        if node.keywords:
            names = [_member_name(parameter.name) for parameter in parameters]
            values: dict[int, _Result] = {}
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
                if not values[position].is_value:
                    given.append(values[position])
            for position in sorted(values):
                while len(args) < position:
                    args.append(None)
                args.append(values[position])
        missing = [
            _member_name(parameter.name)
            for position, parameter in enumerate(parameters, len(instance_args))
            if not parameter.has_default_value
            and (position >= len(args) or args[position] is None)
        ]
        if missing:
            raise self._error(
                node,
                "%s is missing the argument%s %s"
                % (
                    procedure.name,
                    "" if len(missing) == 1 else "s",
                    ", ".join("'%s'" % name for name in missing),
                ),
            )
        # The server evaluates arguments by position, and Python in the order given
        variables, assignments, args = self._in_source_order(node, given, args)
        call = self._call_node(node, service, procedure, args)
        if not variables:
            return call
        return _Result(
            expression=self._expr.block_with_variables(
                variables, assignments + [call.expression]
            ),
            ptype=call.ptype,
        )

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
            return self._fold(node, fold, left.value, right.value)
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
        if isinstance(node.op, ast.Add) and all(
            operand.ptype is not None and operand.ptype.code == KRPC.Type.LIST
            for operand in (left, right)
        ):
            ptype = join_ptype(left.ptype, right.ptype)
            if ptype is None:
                assert left.ptype is not None and right.ptype is not None
                raise self._error(
                    node,
                    "cannot add a value of type %s to a value of type %s"
                    % (_type_name(right.ptype), _type_name(left.ptype)),
                )
            return _Result(
                expression=self._expr.to_list(
                    self._expr.concat(left.expression, right.expression)
                ),
                ptype=ptype,
            )
        bitwise = isinstance(node.op, (ast.BitAnd, ast.BitOr, ast.BitXor))
        if not (bitwise and all(self._is_a_bool(x) for x in (left, right))):
            self._check_numbers(node, [left, right])
            if bitwise and any(
                x.ptype is not None and not self._is_an_integer(x)
                for x in (left, right)
            ):
                raise self._error(node, "&, | and ^ need integers or bools")
        # Python computes with a single precision value as a double
        left, right = self._widen_single(left), self._widen_single(right)
        if isinstance(node.op, ast.Mod):
            return self._compile_modulo(node, left, right)
        if isinstance(node.op, (ast.LShift, ast.RShift)):
            return self._compile_shift(node, left, right)
        if isinstance(node.op, ast.Div) or (
            isinstance(node.op, ast.Pow)
            and right_constant is not None
            and right_constant < 0
        ):
            # True division and a negative power of an integer give a fraction,
            # so an integer left operand is made a double
            left = self._ensure_double(left)
        ptype = self._common_ptype(node, left, right)
        op = getattr(self._expr, name)
        return _Result(expression=op(left.expression, right.expression), ptype=ptype)

    def _compile_shift(self, node: ast.BinOp, left: _Result, right: _Result) -> _Result:
        """The server uses only the low bits of a shift count, where Python shifts
        by the whole count. A count of at least the width shifts every bit out, and
        a negative count is an error. The count keeps its own type. The value is
        widened to the common type of the two, or keeps its own type when they
        have none."""
        value, count = self._operand_expressions(node, left, right)
        if not (self._is_an_integer(value) and self._is_an_integer(count)):
            raise self._error(node, "a shift needs integer operands")
        assert value.ptype is not None and count.ptype is not None
        ptype = promote(value.ptype, count.ptype) or build_ptype(value.ptype.code)
        count_ptype = build_ptype(count.ptype.code)
        expr = self._expr
        zero = self._integer_constant(0, ptype.code)
        no_count = self._integer_constant(0, count_ptype.code)
        width = self._integer_constant(
            64 if ptype.code in (KRPC.Type.SINT64, KRPC.Type.UINT64) else 32,
            count_ptype.code,
        )
        negative = expr.throw(
            "KRPC", "ArgumentException", expr.constant_string("negative shift count")
        )
        left_shift = isinstance(node.op, ast.LShift)

        signed = ptype.code in (KRPC.Type.SINT32, KRPC.Type.SINT64)

        def shifted(value: Any, count: Any) -> Any:
            if left_shift:
                result = expr.left_shift(value, count)
            else:
                result = expr.right_shift(value, count)
            overflow = zero
            if signed and not left_shift:
                overflow = expr.conditional(
                    expr.less_than(value, zero),
                    self._integer_constant(-1, ptype.code),
                    zero,
                )
            return expr.conditional(
                expr.less_than(count, no_count),
                negative,
                expr.conditional(expr.less_than(count, width), result, overflow),
            )

        held_value = expr.variable("value", self._remote_type(node, ptype))
        held_count = expr.variable("count", self._remote_type(node, count_ptype))
        return _Result(
            expression=expr.block_with_variables(
                [held_value, held_count],
                [
                    expr.assign(
                        held_value, self._converted_expression(value, ptype, node)
                    ),
                    expr.assign(
                        held_count,
                        self._converted_expression(count, count_ptype, node),
                    ),
                    shifted(held_value, held_count),
                ],
            ),
            ptype=ptype,
        )

    def _compile_modulo(
        self, node: ast.BinOp, left: _Result, right: _Result
    ) -> _Result:
        """Python's remainder takes the sign of the divisor, where the server's
        takes the sign of the dividend. A nonzero remainder of the other sign
        has the divisor added to it, and a float remainder of zero takes the
        sign of the divisor."""
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
            result = expr.conditional(adjust, expr.add(remainder, divisor), remainder)
            if ptype.code in (KRPC.Type.DOUBLE, KRPC.Type.FLOAT):
                negative_zero = self._converted_expression(
                    _Result(value=-0.0, is_value=True), ptype, node
                )
                result = expr.conditional(
                    expr.equal(remainder, zero),
                    expr.conditional(
                        expr.less_than(divisor, zero), negative_zero, zero
                    ),
                    result,
                )
            return expr.block_with_variables(
                [remainder],
                [expr.assign(remainder, expr.modulo(dividend, divisor)), result],
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
        """Adding strings concatenates them, with the server's string
        concatenation."""
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
    ) -> tuple[_Result, _Result]:
        """The two operands of a binary operator as expressions. An integer
        constant beside an unsigned 64 bit integer is built at that type, as no
        signed type combines with it."""
        operands = [left, right]
        # A None or an empty collection takes the type of the other operand
        for i, other in ((0, right), (1, left)):
            if other.is_value or other.ptype is None or not operands[i].is_value:
                continue
            if operands[i].value is None:
                operands[i] = self._to_expression(
                    operands[i], with_nullable(other.ptype, True)
                )
            elif _is_empty_collection(operands[i].value):
                operands[i] = self._to_expression(operands[i], other.ptype)
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
    ) -> KRPC.Type | None:
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
        """Convert an integer or single precision expression to a double."""
        if result.ptype is None or result.ptype.code not in _INTEGER_CODES + (
            KRPC.Type.FLOAT,
        ):
            return result
        return _Result(
            expression=self._expr.cast(result.expression, self._type.double()),
            ptype=build_ptype(KRPC.Type.DOUBLE),
        )

    def _widen_single(self, result: _Result) -> _Result:
        """Convert a single precision expression to a double, keeping whether
        it can be None."""
        if result.ptype is None or result.ptype.code != KRPC.Type.FLOAT:
            return result
        ptype = with_nullable(build_ptype(KRPC.Type.DOUBLE), result.ptype.nullable)
        return _Result(
            expression=self._expr.cast(
                result.expression, self._remote_type(None, ptype)
            ),
            ptype=ptype,
        )

    def _compile_floor_division(self, node: ast.BinOp) -> _Result:
        left = self._compile(node.left)
        right = self._compile(node.right)
        if left.is_value and right.is_value:
            return self._fold(node, operator.floordiv, left.value, right.value)
        left, right = self._operand_expressions(node, left, right)
        self._check_numbers(node, [left, right])
        if self._is_an_integer(left) and self._is_an_integer(right):
            return self._compile_integer_floor_division(node, left, right)
        return self._compile_float_floor_division(node, left, right)

    def _compile_float_floor_division(
        self, node: ast.BinOp, left: _Result, right: _Result
    ) -> _Result:
        """Floor division of floats as Python computes it, from the remainder,
        which differs from the floor of the quotient when the quotient rounds up
        to an integer. A quotient of zero takes the sign of the true quotient."""
        expr = self._expr
        ptype = build_ptype(KRPC.Type.DOUBLE)
        double = self._remote_type(node, ptype)
        zero = expr.constant_double(0)
        one = expr.constant_double(1)

        def floored(dividend: Any, divisor: Any) -> Any:
            remainder = expr.variable("remainder", double)
            quotient = expr.variable("quotient", double)
            floor = expr.variable("floor", double)
            # A nonzero remainder of the other sign to the divisor is one fewer
            adjust = expr.conditional_and(
                expr.not_equal(remainder, zero),
                expr.not_equal(
                    expr.less_than(remainder, zero), expr.less_than(divisor, zero)
                ),
            )
            floor_call = self._stdlib_call(
                node, "floor", [_Result(expression=quotient, ptype=ptype)]
            ).expression
            signed_zero = self._stdlib_call(
                node,
                "copy_sign",
                [
                    _Result(expression=zero, ptype=ptype),
                    _Result(expression=expr.divide(dividend, divisor), ptype=ptype),
                ],
            ).expression
            return expr.block_with_variables(
                [remainder, quotient, floor],
                [
                    expr.assign(remainder, expr.modulo(dividend, divisor)),
                    expr.assign(
                        quotient,
                        expr.divide(expr.subtract(dividend, remainder), divisor),
                    ),
                    expr.assign(
                        quotient,
                        expr.conditional(
                            adjust, expr.subtract(quotient, one), quotient
                        ),
                    ),
                    expr.assign(floor, floor_call),
                    expr.conditional(
                        expr.equal(quotient, zero),
                        signed_zero,
                        expr.conditional(
                            expr.greater_than(
                                expr.subtract(quotient, floor),
                                expr.constant_double(0.5),
                            ),
                            expr.add(floor, one),
                            floor,
                        ),
                    ),
                ],
            )

        return _Result(
            expression=self._with_temporaries(
                node, ptype, [("dividend", left), ("divisor", right)], floored
            ),
            ptype=ptype,
        )

    def _compile_integer_floor_division(
        self, node: ast.BinOp, left: _Result, right: _Result
    ) -> _Result:
        """Integer division rounding toward negative infinity, as Python's does.
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

    def _in_source_order(
        self, node: ast.AST, given: list[_Result], used: list[_Result | None]
    ) -> tuple[list[Any], list[Any], list[_Result | None]]:
        """Hold computed values in variables, assigned in the order they are
        given, when the server would evaluate them in the order they are used.
        Returns the variables, their assignments and the values as used."""
        computed = given
        ids = {id(result) for result in computed}
        in_use = [result for result in used if result is not None and id(result) in ids]
        if [id(x) for x in computed] == [id(x) for x in in_use]:
            return [], [], used
        expr = self._expr
        variables: list[Any] = []
        assignments: list[Any] = []
        held: dict[int, _Result] = {}
        for result in computed:
            if result.ptype is None:
                raise self._error(node, "cannot determine the type of a value")
            variable = expr.variable("value", self._remote_type(node, result.ptype))
            variables.append(variable)
            assignments.append(expr.assign(variable, result.expression))
            held[id(result)] = _Result(expression=variable, ptype=result.ptype)
        return (
            variables,
            assignments,
            [
                None if result is None else held.get(id(result), result)
                for result in used
            ],
        )

    def _with_temporaries(
        self,
        node: ast.AST,
        ptype: KRPC.Type,
        values: list[tuple[str, _Result]],
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

    def _stdlib_call(self, node: ast.AST, member: str, args: list[_Result]) -> _Result:
        """Embed a call to a StdLib procedure in the expression."""
        service, procedure = self._member(node, "StdLib", None, member, "method")
        arguments: list[_Result | None] = list(args)
        return self._call_node(node, service, procedure, arguments)

    def _compile_boolop(self, node: ast.BoolOp) -> _Result:
        is_and = isinstance(node.op, ast.And)
        # Operands are compiled left to right, stopping at a constant that decides
        # the result, so later operands are not folded. A constant that does not
        # decide it is skipped, unless it is the last operand.
        results: list[_Result] = []
        for position, value in enumerate(node.values):
            result = self._compile(value)
            if (
                result.is_value
                and self._fold(value, bool, result.value).value == is_and
            ):
                if position == len(node.values) - 1:
                    results.append(result)
                continue
            results.append(result)
            if result.is_value:
                break
        if len(results) == 1:
            return results[0]
        operands = self._typed_elements(results)
        # A nullable bool decides the result as Python does, where None is false
        if any(
            operand.ptype is not None
            and (operand.ptype.code != KRPC.Type.BOOL or operand.ptype.nullable)
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
        produces the operand that decides the result, as in Python. The left
        operand is assigned to a temporary, as it is both tested and produced."""
        ptype = join_ptype(left.ptype, right.ptype)
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
                return self._fold(node, operator.neg, result.value)
            result = self._to_expression(result)
            # Negating a uint gives a long, as in C#
            ptype = promote(build_ptype(KRPC.Type.SINT32), result.ptype)
            if result.ptype is not None and ptype is None:
                raise self._error(node, "cannot negate a value of this type")
            return _Result(expression=self._expr.negate(result.expression), ptype=ptype)
        if isinstance(node.op, ast.UAdd):
            if result.is_value:
                return self._fold(node, operator.pos, result.value)
            # Python gives an integer for a bool, which the server has no
            # operator for
            self._check_numbers(node, [result])
            return self._not_none(node, self._to_expression(result))
        if isinstance(node.op, ast.Invert):
            if result.is_value:
                return self._fold(node, operator.invert, result.value)
            result = self._to_expression(result)
            if result.ptype is not None and not self._is_an_integer(result):
                raise self._error(
                    node, "~ is only supported on an integer; use not for a bool"
                )
            # Inverting a uint gives a long, as negating it does, so the result
            # is negative as in Python
            ptype = promote(build_ptype(KRPC.Type.SINT32), result.ptype)
            if result.ptype is not None and ptype is None:
                raise self._error(node, "cannot invert a value of this type")
            expression = result.expression
            if (
                ptype is not None
                and result.ptype is not None
                and ptype.code != result.ptype.code
            ):
                expression = self._expr.cast(expression, self._remote_type(node, ptype))
            return _Result(expression=self._expr.not_(expression), ptype=ptype)
        raise self._error(node, "unsupported operator (%s)" % type(node.op).__name__)

    def _compile_compare(self, node: ast.Compare) -> _Result:
        operands = [self._compile(value) for value in [node.left] + node.comparators]
        if all(operand.is_value for operand in operands):
            value = True
            for left, op, right in zip(operands, node.ops, operands[1:]):
                if isinstance(op, ast.In):
                    value = (
                        value
                        and self._fold(
                            node, operator.contains, right.value, left.value
                        ).value
                    )
                elif isinstance(op, ast.NotIn):
                    value = (
                        value
                        and not self._fold(
                            node, operator.contains, right.value, left.value
                        ).value
                    )
                elif isinstance(op, ast.Is):
                    value = value and (left.value is right.value)
                elif isinstance(op, ast.IsNot):
                    value = value and (left.value is not right.value)
                elif type(op) in _COMPARE_OPS:
                    value = (
                        value
                        and self._fold(
                            node, _COMPARE_OPS[type(op)][1], left.value, right.value
                        ).value
                    )
                else:
                    raise self._error(
                        node, "unsupported comparison (%s)" % type(op).__name__
                    )
            return _Result(value=value, is_value=True)
        folded = self._folded_is_none(node, operands)
        if folded is not None:
            return folded
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
                if isinstance(op, (ast.Eq, ast.NotEq)):
                    self._check_comparable(node, left.ptype, right.ptype)
                else:
                    self._check_numbers(node, [left, right])
                    self._common_ptype(node, left, right)
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
        if container.is_value and _is_empty_collection(container.value):
            # The value is computed for its effects
            return self._expr.block(
                [
                    self._to_expression(value).expression,
                    self._expr.constant_bool(False),
                ]
            )
        if (container.is_value and isinstance(container.value, tuple)) or (
            not container.is_value
            and container.ptype is not None
            and container.ptype.code == KRPC.Type.TUPLE
        ):
            return self._compile_tuple_membership(node, value, container)
        if not value.is_value and not container.is_value and value.ptype is not None:
            # The server evaluates the container first, so the value is held to
            # evaluate it first, as Python does
            expr = self._expr
            variable = expr.variable("value", self._remote_type(node, value.ptype))
            test = self._membership(
                node, _Result(expression=variable, ptype=value.ptype), container
            )
            return expr.block_with_variables(
                [variable], [expr.assign(variable, value.expression), test]
            )
        return self._membership(node, value, container)

    def _membership(self, node: ast.Compare, value: _Result, container: _Result) -> Any:
        """A test of whether a value is in a string, a list, a set, or the keys of
        a dictionary, evaluating the container first."""
        container = self._to_expression(container)
        ptype = container.ptype
        if ptype is not None and ptype.code == KRPC.Type.DICTIONARY:
            return self._key_membership(node, container, value)
        if self._is_a_string(container):
            value = self._to_expression(value)
            self._check_comparable(node, value.ptype, container.ptype)
            return self._expr.string_contains(container.expression, value.expression)
        if ptype is not None and ptype.code not in (KRPC.Type.LIST, KRPC.Type.SET):
            raise self._error(
                node, "cannot test membership in a value of type %s" % _type_name(ptype)
            )
        element = None
        if ptype is not None:
            element = ptype.types[0]
        value = self._to_expression(
            self._operand_expressions(node, value, _Result(ptype=element))[0]
        )
        self._check_comparable(node, value.ptype, element)
        if (
            element is not None
            and value.ptype is not None
            and value.ptype.code in NUMERIC_CODES
            and value.ptype.code != element.code
        ):
            return self._numeric_membership(node, value, container, element)
        return self._expr.contains(container.expression, value.expression)

    def _numeric_membership(
        self, node: ast.AST, value: _Result, container: _Result, element: KRPC.Type
    ) -> Any:
        """A test of whether a number is in a collection of numbers of another
        type. Both are widened to their common type, as they are for =="""
        assert value.ptype is not None
        wider = self._common_ptype(node, value, _Result(ptype=element))
        assert wider is not None
        # A None is equal only to a None
        nullable = value.ptype.nullable or element.nullable
        if wider.code == element.code:
            return self._expr.contains(
                container.expression,
                self._converted_expression(
                    value, with_nullable(element, value.ptype.nullable), node
                ),
            )
        wider = with_nullable(wider, nullable)
        expr = self._expr
        parameter = expr.parameter("element", self._remote_type(node, element))
        widened = expr.cast(parameter, self._remote_type(node, wider))
        cast_held = value.ptype.code != wider.code

        def contains(held: Any) -> Any:
            if cast_held:
                held = expr.cast(held, self._remote_type(node, wider))
            return expr.any(
                container.expression,
                expr.lambda_([parameter], expr.equal(widened, held)),
            )

        return self._with_temporaries(node, value.ptype, [("value", value)], contains)

    def _key_membership(self, node: ast.AST, dictionary: _Result, key: _Result) -> Any:
        """A test of whether a key is in a dictionary. A None is in no
        dictionary, and a number of a wider type than the keys is compared with
        each key widened, as for =="""
        assert dictionary.ptype is not None
        target = dictionary.ptype.types[0]
        keys = dictionary.expression
        if self._is_none(key):
            key = self._null(node, target)
        elif (
            key.is_value
            and isinstance(key.value, int)
            and not isinstance(key.value, bool)
            and target.code in _INTEGER_CODES
            and _in_range(key.value, target.code)
        ):
            # A number given in the source is built at the type of the keys
            return self._expr.contains_key(
                keys, self._integer_constant(key.value, target.code)
            )
        key = self._to_expression(key)
        self._check_comparable(node, key.ptype, target)
        if (
            key.ptype is not None
            and key.ptype.code in NUMERIC_CODES
            and promote(key.ptype, target) != target
        ):
            return self._numeric_membership(
                node,
                key,
                _Result(
                    expression=self._expr.dictionary_keys(keys),
                    ptype=build_ptype(KRPC.Type.LIST, [target]),
                ),
                target,
            )
        nullable = key.ptype is not None and key.ptype.nullable
        return self._expr.contains_key(
            keys,
            self._converted_expression(
                key,
                with_nullable(target, nullable),
                node,
                "for a key of the dictionary",
            ),
        )

    def _key_expression(
        self, node: ast.AST, dictionary: KRPC.Type, key: _Result
    ) -> Any:
        """A key of a dictionary, converted to the type of its keys."""
        target = dictionary.types[0]
        where = "for a key of the dictionary"
        if (
            key.is_value
            and isinstance(key.value, (int, float))
            and not isinstance(key.value, bool)
            and target.code in NUMERIC_CODES
        ):
            # A number given in the source is built at the type of the keys
            return self._converted_expression(key, target, node, where)
        key = self._to_expression(key)
        self._check_comparable(node, key.ptype, target)
        return self._converted_expression(key, target, node, where)

    def _check_comparable(
        self, node: ast.AST, left: KRPC.Type | None, right: KRPC.Type | None
    ) -> None:
        """Raise for two values of known types the server cannot test for
        equality."""
        if left is None or right is None or _comparable(left, right):
            return
        raise self._error(
            node,
            "cannot compare a value of type %s with a value of type %s"
            % (_type_name(left), _type_name(right)),
        )

    def _folded_is_none(
        self, node: ast.Compare, operands: list[_Result]
    ) -> _Result | None:
        """The result of a test of whether a value is None, when the value is of
        a type that is never None and computing it can neither have an effect
        nor raise."""
        if len(node.ops) != 1 or not isinstance(node.ops[0], (ast.Is, ast.IsNot)):
            return None
        index = 1 if operands[0].is_value else 0
        other = operands[1 - index]
        if (
            other.is_value
            and other.value is None
            and self._is_never_none(operands[index])
            and _is_a_plain_read([node.left, *node.comparators][index])
        ):
            return _Result(value=isinstance(node.ops[0], ast.IsNot), is_value=True)
        return None

    def _compile_tuple_membership(
        self, node: ast.Compare, value: _Result, container: _Result
    ) -> Any:
        """A test of whether a value is in a tuple, made as Python does, by
        comparing it with each element in turn until one is equal. The value and
        the tuple are each evaluated once, in that order."""
        expr = self._expr
        variables: list[Any] = []
        statements: list[Any] = []

        def held(name: str, result: _Result) -> _Result:
            result = self._to_expression(result)
            if result.ptype is None:
                raise self._error(node, "cannot determine the type of the value")
            variable = expr.variable(name, self._remote_type(node, result.ptype))
            variables.append(variable)
            statements.append(expr.assign(variable, result.expression))
            return _Result(expression=variable, ptype=result.ptype)

        if value.is_value and value.value is None:
            # A None takes the type of the first element with one
            if container.is_value:
                ptypes = [
                    self._to_expression(_Result(value=element, is_value=True)).ptype
                    for element in cast(tuple[object, ...], container.value)
                    if element is not None
                ]
            else:
                ptypes = list(container.ptype.types) if container.ptype else []
            if ptypes and ptypes[0] is not None:
                value = self._to_expression(value, with_nullable(ptypes[0], True))
        value = held("value", value)
        if container.is_value:
            elements = [
                _Result(value=element, is_value=True)
                for element in cast(tuple[object, ...], container.value)
            ]
        else:
            held_tuple = held("tuple", container)
            assert held_tuple.ptype is not None
            elements = [
                _Result(
                    expression=expr.get(held_tuple.expression, expr.constant_int(i)),
                    ptype=ptype,
                )
                for i, ptype in enumerate(held_tuple.ptype.types)
            ]
        test = expr.constant_bool(False)
        for position, element in enumerate(elements):
            left, right = self._operand_expressions(node, value, element)
            self._check_comparable(node, left.ptype, right.ptype)
            equal = expr.equal(left.expression, right.expression)
            test = equal if position == 0 else expr.conditional_or(test, equal)
        return expr.block_with_variables(variables, statements + [test])

    def _compile_is_none(self, node: ast.Compare, left: _Result, right: _Result) -> Any:
        """An identity comparison, which the algebra supports against None."""
        if right.is_value and right.value is None and not left.is_value:
            operand = left
        elif left.is_value and left.value is None and not right.is_value:
            operand = right
        else:
            raise self._error(node, "is and is not can only compare a value with None")
        expression = self._to_expression(operand).expression
        if self._is_never_none(operand):
            # The value is computed for its effects
            return self._expr.block([expression, self._expr.constant_bool(False)])
        return self._expr.is_null(expression)

    @staticmethod
    def _is_never_none(result: _Result) -> bool:
        """Whether a server side value is of a type that never holds None."""
        return (
            not result.is_value
            and result.ptype is not None
            and not result.ptype.nullable
        )

    def _hoist_chained_operands(
        self, node: ast.Compare, operands: list[_Result]
    ) -> tuple[list[Any], list[list[Any]]]:
        """Assign the operands of a chained comparison, except the last, to
        temporaries, and replace them in place with the temporary. A middle
        operand stands in two comparisons, and Python evaluates each operand
        once, in order, and only while the comparisons so far hold. Returns the
        temporaries, and the assignments to make before each comparison."""
        declared: list[Any] = []
        preludes: list[list[Any]] = [[] for _ in node.ops]
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
        if_true, if_false = self._typed_elements(
            [self._compile(node.body), self._compile(node.orelse)]
        )
        ptype = join_ptype(if_true.ptype, if_false.ptype)
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
            return self._fold(node, operator.getitem, base.value, index.value)
        base = self._to_expression(base)
        code = base.ptype.code if base.ptype is not None else None
        constant = (
            index.value
            if index.is_value
            and isinstance(index.value, int)
            and not isinstance(index.value, bool)
            else None
        )
        if code is not None and code not in (
            KRPC.Type.STRING,
            KRPC.Type.LIST,
            KRPC.Type.DICTIONARY,
            KRPC.Type.TUPLE,
        ):
            assert base.ptype is not None
            raise self._error(
                node, "cannot index a value of type %s" % _type_name(base.ptype)
            )
        if code == KRPC.Type.TUPLE and constant is None:
            raise self._error(node, "a tuple can only be indexed by a constant integer")
        if constant is not None and code != KRPC.Type.DICTIONARY:
            if code == KRPC.Type.TUPLE:
                assert base.ptype is not None
                size = len(base.ptype.types)
                if not -size <= constant < size:
                    raise self._error(node, "tuple index out of range")
                index = _Result(value=constant % size, is_value=True)
            elif constant < 0 and base.ptype is not None:
                return self._compile_index_from_end(node, base, constant)
        if self._is_a_string(base):
            return _Result(
                expression=self._expr.string_get(
                    base.expression, self._index_expression(node, index)
                ),
                ptype=build_ptype(KRPC.Type.STRING),
            )
        if base.ptype is not None and base.ptype.code == KRPC.Type.DICTIONARY:
            return _Result(
                expression=self._expr.get(
                    base.expression, self._key_expression(node, base.ptype, index)
                ),
                ptype=base.ptype.types[1],
            )
        element: KRPC.Type | None = None
        if code == KRPC.Type.TUPLE:
            assert base.ptype is not None
            element = base.ptype.types[index.value]  # pyright: ignore[reportArgumentType, reportCallIssue]
            position = self._expr.constant_int(index.value)
        else:
            if code == KRPC.Type.LIST:
                assert base.ptype is not None
                element = base.ptype.types[0]
            position = self._index_expression(node, index)
        return _Result(
            expression=self._expr.get(base.expression, position), ptype=element
        )

    def _index_expression(self, node: ast.AST, index: _Result) -> Any:
        """A position in a list or a string, which the server takes as an int."""
        if (
            index.is_value
            and isinstance(index.value, int)
            and not isinstance(index.value, bool)
        ):
            if not _in_range(index.value, KRPC.Type.SINT32):
                raise self._error(node, "index %d is out of range" % index.value)
            return self._expr.constant_int(index.value)
        index = self._to_expression(index)
        if index.ptype is not None and index.ptype.code != KRPC.Type.SINT32:
            raise self._error(
                node,
                "an index must be an int, not a value of type %s"
                % _type_name(index.ptype),
            )
        return index.expression

    def _compile_index_from_end(
        self, node: ast.Subscript, base: _Result, index: int
    ) -> _Result:
        """Index a list or a string by a negative constant, which counts from its
        end. The collection is assigned to a temporary, as it is both measured
        and indexed."""
        assert base.ptype is not None
        expr = self._expr
        offset = expr.constant_int(index)
        # A position before the start is out of range, which the error reports
        # as the index given
        out_of_range = expr.throw(
            "KRPC",
            "IndexOutOfRangeException",
            expr.constant_string("index %d is out of range" % index),
        )
        if self._is_a_string(base):
            ptype = build_ptype(KRPC.Type.STRING)

            def indexed(value: Any) -> Any:
                position = expr.add(expr.string_length(value), offset)
                return expr.conditional(
                    expr.less_than(position, expr.constant_int(0)),
                    out_of_range,
                    expr.string_get(value, position),
                )

        else:
            ptype = self._element_ptype(node, base, "index", ordered=True)

            def indexed(value: Any) -> Any:
                position = expr.add(expr.count(value), offset)
                return expr.conditional(
                    expr.less_than(position, expr.constant_int(0)),
                    out_of_range,
                    expr.get(value, position),
                )

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
        # A None bound is no bound, as in Python
        lower, upper = [
            None if bound is None else self._compile(bound)
            for bound in (piece.lower, piece.upper)
        ]
        lower, upper = [
            None if bound is None or self._is_none(bound) else bound
            for bound in (lower, upper)
        ]
        if (
            base.is_value
            and (lower is None or lower.is_value)
            and (upper is None or upper.is_value)
        ):
            return self._fold(
                node,
                operator.getitem,
                base.value,
                slice(
                    None if lower is None else lower.value,
                    None if upper is None else upper.value,
                ),
            )
        collection = self._to_expression(base)
        if collection.ptype is not None and collection.ptype.code == KRPC.Type.TUPLE:
            return self._compile_tuple_slice(node, collection, lower, upper)
        for bound in (lower, upper):
            if (
                bound is not None
                and bound.is_value
                and isinstance(bound.value, int)
                and bound.value < 0
            ):
                raise self._error(node, "negative slice bounds are not supported")
        if self._is_a_string(collection):
            return self._compile_string_slice(node, collection, lower, upper)
        element = self._element_ptype(node, collection, "slice", ordered=True)
        expr = self._expr
        variables: list[Any] = []
        statements: list[Any] = []

        def held(name: str, value: _Result) -> Any:
            variable = expr.variable(name, self._remote_type(node, value.ptype))
            variables.append(variable)
            statements.append(expr.assign(variable, value.expression))
            return variable

        items = collection.expression
        if any(bound is not None and not bound.is_value for bound in (lower, upper)):
            # The collection and the bounds are each evaluated once, in order
            items = held("collection", collection)
        expression = items
        start = None
        if lower is not None:
            start = self._slice_bound(node, "lower", lower, expr.constant_int(0), held)
            expression = expr.skip(expression, start)
        if upper is not None:
            count = self._slice_bound(node, "upper", upper, expr.count(items), held)
            if start is not None:
                count = expr.subtract(count, start)
            expression = expr.take(expression, count)
        expression = expr.to_list(expression)
        if variables:
            expression = expr.block_with_variables(variables, statements + [expression])
        return _Result(
            expression=expression,
            ptype=build_ptype(KRPC.Type.LIST, [element]),
        )

    def _slice_bound(
        self,
        node: ast.Subscript,
        name: str,
        bound: _Result,
        default: Any,
        held: Callable[[str, _Result], Any],
    ) -> Any:
        """A bound of a slice, as an int. A computed bound is held in a
        variable, and a None it holds stands for the default."""
        where = "for a bound of a slice"
        integer = build_ptype(KRPC.Type.SINT32)
        if bound.is_value:
            return self._widened_expression(node, integer, bound, where)
        nullable = bound.ptype is not None and bound.ptype.nullable
        target = with_nullable(integer, nullable)
        variable = held(
            name,
            _Result(
                expression=self._widened_expression(node, target, bound, where),
                ptype=target,
            ),
        )
        if not nullable:
            return variable
        expr = self._expr
        return expr.conditional(
            expr.is_null(variable),
            default,
            expr.cast(variable, self._remote_type(node, integer)),
        )

    def _compile_tuple_slice(
        self,
        node: ast.Subscript,
        values: _Result,
        lower: _Result | None,
        upper: _Result | None,
    ) -> _Result:
        """Compile a slice of a tuple, by constant bounds, into a tuple of the
        values it holds. The type of each value is known only for a constant
        position."""
        bounds = []
        for bound in (lower, upper):
            if bound is not None and not (
                bound.is_value
                and isinstance(bound.value, int)
                and not isinstance(bound.value, bool)
            ):
                raise self._error(node, "a tuple can only be sliced by constant bounds")
            bounds.append(None if bound is None else bound.value)
        assert values.ptype is not None
        types = values.ptype.types
        positions = range(len(types))[slice(*bounds)]
        if not positions:
            raise self._error(
                node, "a slice of a tuple must hold a value, as a tuple cannot be empty"
            )
        expr = self._expr
        variable = expr.variable("tuple", self._remote_type(node, values.ptype))
        return _Result(
            expression=expr.block_with_variables(
                [variable],
                [
                    expr.assign(variable, values.expression),
                    expr.create_tuple(
                        [expr.get(variable, expr.constant_int(i)) for i in positions]
                    ),
                ],
            ),
            ptype=build_ptype(KRPC.Type.TUPLE, [types[i] for i in positions]),
        )

    def _compile_string_slice(
        self,
        node: ast.Subscript,
        string: _Result,
        lower: _Result | None,
        upper: _Result | None,
    ) -> _Result:
        """Compile a slice of a string, as a substring taken by start and length.
        A bound past the end of the string is clamped to it, as Python does. An
        absent bound is the start or the end of it."""
        expr = self._expr
        text = expr.variable(
            "string", self._remote_type(node, build_ptype(KRPC.Type.STRING))
        )
        length = expr.string_length(text)
        variables = [text]
        statements = [expr.assign(text, string.expression)]

        def held(name: str, value: _Result) -> Any:
            variable = expr.variable(name, self._remote_type(node, value.ptype))
            variables.append(variable)
            statements.append(expr.assign(variable, value.expression))
            return variable

        def clamped(name: str, bound: _Result | None, default: Any) -> Any:
            if bound is None:
                return default
            value = self._slice_bound(node, name, bound, default, held)
            return expr.conditional(expr.less_than(value, length), value, length)

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

    def _compile_elements(self, elts: list[ast.expr], kind: str) -> _Result:
        results = [self._compile(elt) for elt in elts]
        if all(result.is_value for result in results):
            values = [result.value for result in results]
            if kind == "list":
                return _Result(value=values, is_value=True)
            if kind == "tuple":
                return _Result(value=tuple(values), is_value=True)
            return self._fold(self._node, set, values)
        if kind == "tuple":
            self._check_tuple_size(len(results))
            expressions = [self._to_expression(result) for result in results]
            return _Result(
                expression=self._expr.create_tuple(
                    [result.expression for result in expressions]
                ),
                ptype=self._collection_ptype(KRPC.Type.TUPLE, expressions),
            )
        if kind == "set":
            self._check_not_none(results, "the values of a set cannot be None")
        expressions, element = self._collection_elements(results)
        code = KRPC.Type.LIST if kind == "list" else KRPC.Type.SET
        if element is not None and code == KRPC.Type.SET:
            element = _value_or_key_ptype(element)
        values = [result.expression for result in expressions]
        return _Result(
            expression=(
                self._expr.create_list(values)
                if kind == "list"
                else self._expr.create_set(set(values))
            ),
            ptype=build_ptype(code, [element]) if element else None,
        )

    def _collection_elements(
        self, results: list[_Result]
    ) -> tuple[list[_Result], KRPC.Type | None]:
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
        elements = []
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
            elements.append(result)
        expressions = self._typed_elements(elements)
        return expressions, self._widened_ptype(expressions)

    def _compile_dict(self, node: ast.Dict) -> _Result:
        if any(key is None for key in node.keys):
            raise self._error(node, "dictionary unpacking is not supported")
        keys = [self._compile(key) for key in node.keys if key is not None]
        values = [self._compile(value) for value in node.values]
        if all(result.is_value for result in keys + values):
            return self._fold(
                node,
                lambda: dict(
                    zip((key.value for key in keys), (value.value for value in values))
                ),
            )
        self._check_not_none(keys, "the keys of a dictionary cannot be None")
        key_expressions, key_ptype = self._collection_elements(keys)
        if key_ptype is not None:
            self._check_keys(node, build_ptype(KRPC.Type.DICTIONARY, [key_ptype]))
        value_expressions, value_ptype = self._collection_elements(values)
        # The server evaluates every key and then every value, and Python each
        # key and then its value
        pairs = [
            expression
            for parts in zip(keys, key_expressions, values, value_expressions)
            for result, expression in (parts[:2], parts[2:])
            if not result.is_value
        ]
        variables, assignments, held = self._in_source_order(
            node, pairs, list(key_expressions + value_expressions)
        )
        count = len(key_expressions)
        dictionary = self._expr.create_dictionary(
            [cast(_Result, key).expression for key in held[:count]],
            [cast(_Result, value).expression for value in held[count:]],
        )
        if variables:
            dictionary = self._expr.block_with_variables(
                variables, assignments + [dictionary]
            )
        return _Result(
            expression=dictionary,
            ptype=(
                build_ptype(
                    KRPC.Type.DICTIONARY, [_value_or_key_ptype(key_ptype), value_ptype]
                )
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
            ptype=build_ptype(
                KRPC.Type.SET, [_value_or_key_ptype(element)] if element else None
            ),
        )

    def _compile_generatorexp(self, node: ast.GeneratorExp) -> _Result:
        selected, element = self._compile_comprehension(node, node.generators, node.elt)
        # A list, so the generator can be held in a variable or passed to a
        # function as well as consumed
        return _Result(
            expression=self._expr.to_list(selected),
            ptype=build_ptype(KRPC.Type.LIST, [element] if element else None),
        )

    def _compile_comprehension(
        self,
        node: ast.AST,
        generators: list[ast.comprehension],
        elt: ast.expr,
        quantifier: Callable[[Any, Any], Any] | None = None,
    ) -> tuple[Any, KRPC.Type | None]:
        """Compile a comprehension to select/where over its collection.
        Comprehensions with multiple 'for' clauses flatten via the server's
        select-many operation. Returns the resulting collection expression and
        the type of its elements. Given a quantifier, such as the server's any,
        elt is a condition and the result is the pair (collection, predicate
        function) instead. The predicate of a clause applies the quantifier to
        the next clause, so the test stops as soon as its result is known."""
        collection, parameter, scope = self._bind_generator(node, generators[0])
        self._scopes.append(scope)
        try:
            if quantifier is not None and len(generators) > 1:
                inner, predicate = self._compile_comprehension(
                    node, generators[1:], elt, quantifier
                )
                return collection, self._expr.lambda_(
                    [parameter], quantifier(inner, predicate)
                )
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
            if quantifier is not None:
                condition = self._to_expression(self._compile_condition(elt))
                return collection, self._expr.lambda_([parameter], condition.expression)
            body = self._to_expression(self._compile(elt))
            function = self._expr.lambda_([parameter], body.expression)
            return self._expr.select(collection, function), body.ptype
        finally:
            self._scopes.pop()

    def _bind_generator(
        self, node: ast.AST, generator: ast.comprehension
    ) -> tuple[Any, Any, dict[str, _Result]]:
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
        ptype: KRPC.Type | None,
        path: tuple[int, ...] = (),
    ) -> list[tuple[str, tuple[int, ...], KRPC.Type | None]]:
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

    def _unpacked(self, value: Any, path: tuple[int, ...]) -> Any:
        """The part of a tuple valued expression at a position found by _unpack."""
        for index in path:
            value = self._expr.get(value, self._expr.constant_int(index))
        return value

    def _compile_dictcomp(self, node: ast.DictComp) -> _Result:
        """A dictionary comprehension, built by a loop over the sequence of its
        (key, value) pairs. A repeated key takes the last value, as Python does."""
        pair = ast.copy_location(
            ast.Tuple(elts=[node.key, node.value], ctx=ast.Load()), node
        )
        pairs, ptype = self._compile_comprehension(node, node.generators, pair)
        if ptype is None:
            raise self._error(node, "cannot determine the type of the dictionary")
        key, value = ptype.types
        dictionary = build_ptype(
            KRPC.Type.DICTIONARY, [_value_or_key_ptype(key), value]
        )
        expr = self._expr
        result = expr.variable("dictionary", self._remote_type(node, dictionary))
        variable = expr.variable("pair", self._remote_type(node, ptype))
        loop = expr.for_each(
            variable,
            pairs,
            expr.set(
                result,
                expr.get(variable, expr.constant_int(0)),
                expr.get(variable, expr.constant_int(1)),
            ),
        )
        return _Result(
            expression=expr.block_with_variables(
                [result, variable],
                [
                    expr.assign(
                        result,
                        expr.create_empty_dictionary(
                            self._remote_type(node, dictionary.types[0]),
                            self._remote_type(node, dictionary.types[1]),
                        ),
                    ),
                    loop,
                    result,
                ],
            ),
            ptype=dictionary,
        )

    def _compile_joinedstr(self, node: ast.JoinedStr) -> _Result:
        parts: list[_Result] = []
        for piece in node.values:
            if isinstance(piece, ast.Constant):
                parts.append(_Result(value=piece.value, is_value=True))
                continue
            assert isinstance(piece, ast.FormattedValue)
            if piece.format_spec is not None:
                raise self._error(node, "format specifiers are not supported")
            if piece.conversion not in (-1, 115):  # none or !s
                # f"{x=}" is the text "x=" and then x with the !r conversion
                previous = parts[-1].value if parts and parts[-1].is_value else ""
                if piece.conversion == 114 and str(previous).rstrip().endswith("="):
                    raise self._error(node, "the = specifier is not supported")
                raise self._error(node, "the !r and !a conversions are not supported")
            parts.append(self._compile(piece.value))
        if all(part.is_value for part in parts):
            return _Result(
                value="".join(str(part.value) for part in parts), is_value=True
            )
        expressions = []
        for part in parts:
            if part.is_value:
                expressions.append(self._expr.constant_string(str(part.value)))
            else:
                expressions.append(self._string_of(node, part).expression)
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

    def _is_typing_cast(self, func: ast.expr) -> bool:
        if isinstance(func, ast.Name):
            target = self._lookup(func.id, func)
            return target.is_value and target.value is cast
        if isinstance(func, ast.Attribute) and isinstance(func.value, ast.Name):
            base = self._lookup(func.value.id, func.value)
            return base.is_value and getattr(base.value, func.attr, None) is cast
        return False

    def _compile_typing_cast(self, node: ast.Call) -> _Result:
        """typing.cast(T, value), which gives a value the type T. A None takes
        the type, and any other value is converted to it, including a float to
        an integer type, which truncates it."""
        if len(node.args) != 2 or node.keywords:
            raise self._error(node, "cast takes a type and a value")
        ptype = annotation_ptype(self, node.args[0])
        value = self._to_expression(self._compile(node.args[1]), ptype)
        if value.ptype == ptype:
            return value
        if value.ptype is not None and not _convertible(value.ptype, ptype):
            raise self._error(
                node,
                "cannot cast a value of type %s to %s"
                % (_type_name(value.ptype), _type_name(ptype)),
            )
        return _Result(
            expression=self._expr.cast(
                value.expression, self._remote_type(node, ptype)
            ),
            ptype=ptype,
        )

    @staticmethod
    def _is_supported_builtin(value: object) -> TypeGuard[Callable[..., Any]]:
        # Compared by identity, as a value can define equality to be anything
        return any(
            value is builtin
            for builtin in (
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
                print,
            )
        )

    def _compile_builtin(
        self,
        node: ast.Call,
        func: Callable,
    ) -> _Result:
        if func is print:
            return self._compile_print(node)
        if func is range:
            return self._compile_range(node)
        if func is enumerate:
            return self._compile_enumerate(node)
        if func is zip:
            return self._compile_zip(node)
        if func in (min, max) and len(node.args) > 1:
            if not node.keywords:
                return self._compile_scalar_min_max(node, func)
            node = self._min_max_of_a_list(node)
        if func is round and len(node.args) == 2 and not node.keywords:
            return self._compile_round(node)
        self._check_builtin_keywords(node, func)
        if len(node.args) != 1:
            raise self._error(
                node,
                "%s() must be called with a single argument" % func.__name__,
            )
        argument = node.args[0]
        if func in (abs, round, int, float, str):
            return self._compile_scalar_builtin(node, func, argument)
        if func in (any, all) and isinstance(argument, ast.GeneratorExp):
            op = self._expr.any if func is any else self._expr.all
            collection, predicate = self._compile_comprehension(
                argument, argument.generators, argument.elt, op
            )
            return _Result(
                expression=op(collection, predicate), ptype=build_ptype(KRPC.Type.BOOL)
            )
        result = self._compile(argument)
        if result.is_value and not node.keywords:
            value = self._fold(node, func, result.value).value
            # reversed() gives an iterator, which is read into a list
            return _Result(
                value=list(cast(Iterable[Any], value)) if func is reversed else value,
                is_value=True,
            )
        result = self._to_expression(result)
        if func is len:
            if result.ptype is not None and result.ptype.code not in _SIZED_CODES:
                raise self._error(
                    node,
                    "cannot call len() on a value of type %s"
                    % _type_name(result.ptype),
                )
            if result.ptype is not None and result.ptype.code == KRPC.Type.TUPLE:
                # A tuple's length is fixed by its type. Its elements are still
                # computed, as computing one can have an effect or raise
                size = len(result.ptype.types)
                if _is_a_plain_read(argument):
                    return _Result(value=size, is_value=True)
                return _Result(
                    expression=self._expr.block(
                        [result.expression, self._expr.constant_int(size)]
                    ),
                    ptype=build_ptype(KRPC.Type.SINT32),
                )
            if self._is_a_string(result):
                return _Result(
                    expression=self._expr.string_length(result.expression),
                    ptype=build_ptype(KRPC.Type.SINT32),
                )
            return _Result(
                expression=self._expr.count(result.expression),
                ptype=build_ptype(KRPC.Type.SINT32),
            )
        result = self._iterable(result)
        element = self._element_ptype(
            node, result, "call %s() on" % func.__name__, ordered=func is reversed
        )
        if func is reversed:
            return _Result(
                expression=self._expr.to_list(self._expr.reverse(result.expression)),
                ptype=build_ptype(KRPC.Type.LIST, [element]),
            )
        if func in (sum, min, max):
            return self._compile_aggregate(node, func, result, element)
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
                self._check_orderable(node, element, "sorted()")
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

    def _check_builtin_keywords(
        self,
        node: ast.Call,
        func: Callable,
    ) -> None:
        """Raise for a keyword argument other than the key of sorted, min or max."""
        keys = ("key",) if func in (sorted, min, max) else ()
        for keyword in node.keywords:
            if keyword.arg not in keys:
                raise self._error(
                    node,
                    "%s() does not support the keyword argument '%s'"
                    % (func.__name__, keyword.arg),
                )

    def _compile_print(self, node: ast.Call) -> _Result:
        """Compile print() into a call of StdLib.print, which writes the line to the
        game's log. The separator is a constant."""
        separator = " "
        for keyword in node.keywords:
            if keyword.arg != "sep":
                raise self._error(
                    node,
                    "print() does not support the keyword argument '%s'" % keyword.arg,
                )
            sep = self._compile(keyword.value)
            if not sep.is_value or not isinstance(sep.value, (str, type(None))):
                raise self._error(node, "the separator of print() must be a constant")
            if sep.value is not None:
                separator = sep.value
        # The message's parts, with adjacent constants joined into one string
        parts: list[_Result] = []
        for position, argument in enumerate(node.args):
            texts = [separator] if position > 0 else []
            part = self._compile(argument)
            if part.is_value:
                texts.append(str(part.value))
            if texts:
                if parts and parts[-1].is_value:
                    texts.insert(0, cast(str, parts.pop().value))
                parts.append(_Result(value="".join(texts), is_value=True))
            if not part.is_value:
                parts.append(part)
        if not parts:
            message = _Result(value="", is_value=True)
        elif len(parts) == 1:
            message = parts[0]
            if not message.is_value:
                message = self._string_of(node, message)
        else:
            message = _Result(
                expression=self._expr.string_concat(
                    [
                        (
                            self._expr.constant_string(part.value)
                            if part.is_value
                            else self._string_of(node, part).expression
                        )
                        for part in parts
                    ]
                ),
                ptype=build_ptype(KRPC.Type.STRING),
            )
        return self._stdlib_call(node, "print", [message])

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
        self, node: ast.AST | None, start: _Result, end: _Result, step: _Result
    ) -> _Result:
        """A loop on the server building the list of values of a range. The
        loop counts in a long, so a step past the end of an int range does not
        wrap around."""
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
        if not _in_range(step.value, KRPC.Type.SINT32):
            raise self._error(
                node,
                "integer constant %d is out of range for int %s" % (step.value, where),
            )
        long = self._remote_type(node, build_ptype(KRPC.Type.SINT64))
        result = expr.variable("range", self._remote_type(node, numbers))
        counter = expr.variable("counter", long)
        limit = expr.variable("end", long)
        compare = expr.less_than if step.value > 0 else expr.greater_than

        def counted(value: _Result) -> Any:
            if (
                value.is_value
                and isinstance(value.value, int)
                and not isinstance(value.value, bool)
                and _in_range(value.value, KRPC.Type.SINT32)
            ):
                return expr.constant_long(value.value)
            return expr.cast(
                self._widened_expression(node, integer, value, where), long
            )

        loop = expr.while_(
            compare(counter, limit),
            expr.block(
                [
                    expr.append(
                        result, expr.cast(counter, self._remote_type(node, integer))
                    ),
                    expr.assign(
                        counter, expr.add(counter, expr.constant_long(step.value))
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
                    expr.assign(counter, counted(start)),
                    expr.assign(limit, counted(end)),
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
        element = self._element_ptype(node, collection, "call enumerate() on")
        integer = build_ptype(KRPC.Type.SINT32)
        pair = build_ptype(KRPC.Type.TUPLE, [integer, element])
        pairs = build_ptype(KRPC.Type.LIST, [pair])
        expr = self._expr
        result = expr.variable("enumerate", self._remote_type(node, pairs))
        counter = expr.variable("counter", self._remote_type(node, integer))
        value = expr.variable("value", self._remote_type(node, element))
        variables = [result, counter, value]
        statements = [
            expr.assign(result, expr.create_empty_list(self._remote_type(node, pair)))
        ]
        items = collection.expression
        # The collection is evaluated before a computed start, as in Python
        if not start.is_value:
            items = expr.variable("items", self._remote_type(node, collection.ptype))
            variables.append(items)
            statements.append(expr.assign(items, collection.expression))
        statements.append(
            expr.assign(
                counter,
                self._widened_expression(
                    node, integer, start, "for the start of enumerate()"
                ),
            )
        )
        statements.append(
            expr.for_each(
                value,
                items,
                expr.block(
                    [
                        expr.append(result, expr.create_tuple([counter, value])),
                        expr.assign(counter, expr.add(counter, expr.constant_int(1))),
                    ]
                ),
            )
        )
        statements.append(result)
        return _Result(
            expression=expr.block_with_variables(variables, statements),
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
        types = [
            self._element_ptype(node, collection, "call zip() on")
            for collection in collections
        ]
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
        func: Callable,
        arguments: list[_Result],
    ) -> _Result:
        """Call a builtin producing a sequence on arguments known when compiling,
        and read the sequence into a list."""
        return self._fold(
            node, lambda: list(func(*[argument.value for argument in arguments]))
        )

    def _compile_key_function(
        self, node: ast.Call, key: ast.expr, element: KRPC.Type | None
    ) -> Any:
        """Compile the lambda given as the key of a builtin, taking one value of the
        collection and producing the value to compare it by."""
        if not (isinstance(key, ast.Lambda) and len(key.args.args) == 1):
            raise self._error(node, "a key must be a lambda taking one argument")
        name = key.args.args[0].arg
        parameter = self._expr.parameter(name, self._remote_type(node, element))
        self._scopes.append({name: _Result(expression=parameter, ptype=element)})
        try:
            body = compile_lambda_body(self, key)
        finally:
            self._scopes.pop()
        self._check_orderable(
            node, body.ptype, "the key of %s()" % self._builtin_name(node)
        )
        return self._expr.lambda_([parameter], body.expression)

    @staticmethod
    def _builtin_name(node: ast.Call) -> str:
        return node.func.id if isinstance(node.func, ast.Name) else "the function"

    def _check_orderable(
        self, node: ast.AST, ptype: KRPC.Type | None, what: str
    ) -> None:
        """Raise for values of a known type that Python cannot order, such as
        objects and enumeration values."""
        if ptype is None or _orderable(ptype):
            return
        raise self._error(
            node, "%s cannot order values of type %s" % (what, _type_name(ptype))
        )

    def _compile_scalar_builtin(
        self,
        node: ast.Call,
        func: Callable,
        argument: ast.expr,
    ) -> _Result:
        result = self._compile(argument)
        if result.is_value:
            return self._fold(node, func, result.value)
        result = self._to_expression(result)
        if (
            func is not str
            and result.ptype is not None
            and result.ptype.code not in NUMERIC_CODES
        ):
            raise self._error(
                node, "%s() is only supported on a number" % func.__name__
            )
        if func is not str:
            # Python raises TypeError for a None
            result = self._not_none(node, result)
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
        return self._string_of(node, result)

    def _not_none(self, node: ast.AST, result: _Result) -> _Result:
        """A nullable value converted to its type that is not, with a cast that
        raises for a None, as the operators do."""
        if result.ptype is None or not result.ptype.nullable:
            return result
        plain = with_nullable(result.ptype, False)
        return _Result(
            expression=self._expr.cast(
                result.expression, self._remote_type(node, plain)
            ),
            ptype=plain,
        )

    def _string_of(self, node: ast.AST, result: _Result) -> _Result:
        """The text of a string, a number or a bool, as str() gives it. Raises a
        compile error for a value of any other known type."""
        ptype = result.ptype
        if ptype is not None and ptype.code not in NUMERIC_CODES + (
            KRPC.Type.BOOL,
            KRPC.Type.STRING,
        ):
            raise self._error(
                node, "only a string, a number or a bool can be converted to a string"
            )
        string = build_ptype(KRPC.Type.STRING)
        if ptype is not None and ptype.code == KRPC.Type.STRING and not ptype.nullable:
            return result
        expr = self._expr
        result = self._to_expression(result)
        if ptype is None or not ptype.nullable:
            return _Result(
                expression=expr.convert_to_string(result.expression), ptype=string
            )
        # The server raises for a null, which Python writes as None
        value = expr.variable("value", self._remote_type(node, ptype))
        text = expr.conditional(
            expr.is_null(value),
            expr.constant_string("None"),
            expr.convert_to_string(value),
        )
        return _Result(
            expression=expr.block_with_variables(
                [value], [expr.assign(value, result.expression), text]
            ),
            ptype=string,
        )

    def _compile_round(self, node: ast.Call) -> _Result:
        """Compile round(x, ndigits), which rounds to a number of decimal places.
        An integer rounded this way stays an integer, as it does in Python."""
        value = self._compile(node.args[0])
        digits = self._compile(node.args[1])
        if value.is_value and digits.is_value:
            return self._fold(node, round, value.value, digits.value)
        value = self._not_none(node, self._to_expression(value))
        if not self._is_an_integer(value):
            return self._stdlib_call(
                node, "round", [value, self._to_expression(digits)]
            )
        ptype = value.ptype
        assert ptype is not None
        # An integer rounded to a place at or after the point is unchanged
        if digits.is_value and isinstance(digits.value, int) and digits.value >= 0:
            return value
        expr = self._expr
        integer = build_ptype(KRPC.Type.SINT32)

        def rounded(held_value: Any) -> Any:
            def by_digits(held_digits: Any) -> Any:
                return expr.conditional(
                    expr.greater_than_or_equal(held_digits, expr.constant_int(0)),
                    held_value,
                    self._integer_rounded(node, ptype, held_value, held_digits),
                )

            return self._with_temporaries(
                node, integer, [("digits", self._to_expression(digits))], by_digits
            )

        return _Result(
            expression=self._with_temporaries(node, ptype, [("value", value)], rounded),
            ptype=ptype,
        )

    def _integer_rounded(
        self, node: ast.AST, ptype: KRPC.Type, value: Any, digits: Any
    ) -> Any:
        """An integer rounded to a negative number of decimal places, half to
        even as Python rounds. The arithmetic is in the integer's own type, so a
        long keeps every digit. The power of ten the value is rounded to is
        found by a loop, and when it exceeds the type the value rounds to 0."""
        expr = self._expr
        code = ptype.code
        typ = self._remote_type(node, ptype)
        integer = self._remote_type(node, build_ptype(KRPC.Type.SINT32))
        zero = self._integer_constant(0, code)
        one = self._integer_constant(1, code)
        power = expr.variable("power", typ)
        places = expr.variable("places", integer)
        quotient = expr.variable("quotient", typ)
        remainder = expr.variable("remainder", typ)
        rest = expr.subtract(power, remainder)
        # The remainder takes the sign of the value, and its size decides
        if code in (KRPC.Type.SINT32, KRPC.Type.SINT64):
            size = expr.conditional(
                expr.less_than(remainder, zero), expr.negate(remainder), remainder
            )
            rest = expr.subtract(power, size)
            away = expr.conditional(
                expr.less_than(value, zero),
                expr.subtract(quotient, one),
                expr.add(quotient, one),
            )
        else:
            size = remainder
            away = expr.add(quotient, one)
        half_or_more = expr.conditional_or(
            expr.greater_than(size, rest),
            expr.conditional_and(
                expr.equal(size, rest),
                expr.not_equal(
                    expr.modulo(quotient, self._integer_constant(2, code)), zero
                ),
            ),
        )
        return expr.block_with_variables(
            [power, places, quotient, remainder],
            [
                expr.assign(power, one),
                expr.assign(places, digits),
                expr.while_(
                    expr.conditional_and(
                        expr.less_than(places, expr.constant_int(0)),
                        expr.less_than_or_equal(
                            power,
                            self._integer_constant(
                                (_INTEGER_RANGES[code][1] - 1) // 10, code
                            ),
                        ),
                    ),
                    expr.block(
                        [
                            expr.assign(
                                power,
                                expr.multiply(power, self._integer_constant(10, code)),
                            ),
                            expr.assign(places, expr.add(places, expr.constant_int(1))),
                        ]
                    ),
                ),
                expr.conditional(
                    expr.less_than(places, expr.constant_int(0)),
                    zero,
                    expr.block(
                        [
                            expr.assign(quotient, expr.divide(value, power)),
                            expr.assign(remainder, expr.modulo(value, power)),
                            expr.multiply(
                                expr.conditional(half_or_more, away, quotient), power
                            ),
                        ]
                    ),
                ),
            ],
        )

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

    @staticmethod
    def _min_max_of_a_list(node: ast.Call) -> ast.Call:
        """A call to min or max with several arguments, rewritten to take a list of
        them, the form a key applies to."""
        elements = ast.copy_location(ast.List(elts=node.args, ctx=ast.Load()), node)
        return ast.copy_location(
            ast.Call(func=node.func, args=[elements], keywords=node.keywords), node
        )

    def _compile_aggregate(
        self,
        node: ast.Call,
        func: Callable,
        result: _Result,
        element: KRPC.Type | None,
    ) -> _Result:
        """sum, min or max of a collection, with min and max taking an optional
        key."""
        key = next(
            (keyword.value for keyword in node.keywords if keyword.arg == "key"),
            None,
        )
        if key is None:
            self._check_numbers(node, [_Result(ptype=element)])
        # The value is computed, so only a nullable number in it can be None
        computed = None if element is None else plain_ptype(element)
        expr = self._expr
        if func is sum:
            values = result.expression
            if computed is not None and computed.code == KRPC.Type.FLOAT:
                # Python adds single precision values as doubles
                value = expr.parameter("x", self._remote_type(node, computed))
                widened = self._widen_single(_Result(expression=value, ptype=computed))
                values = expr.select(values, expr.lambda_([value], widened.expression))
                computed = widened.ptype
            return _Result(expression=expr.sum(values), ptype=computed)
        ptype = computed
        if key is not None:
            # The value of the collection with the smallest or largest key
            selector = self._compile_key_function(node, key, element)
            operation = expr.min_by if func is min else expr.max_by
            ptype = element

            def aggregate(values: Any) -> Any:
                return operation(values, selector)

        elif (
            computed is not None
            and computed.code in (KRPC.Type.DOUBLE, KRPC.Type.FLOAT)
            and not computed.nullable
        ):
            # Compared pairwise, as Python does. The server's Min and Max give
            # NaN for a NaN anywhere
            first = expr.parameter("first", self._remote_type(node, computed))
            second = expr.parameter("second", self._remote_type(node, computed))
            compare = expr.less_than if func is min else expr.greater_than
            pairwise = expr.lambda_(
                [first, second], expr.conditional(compare(second, first), second, first)
            )

            def aggregate(values: Any) -> Any:
                return expr.aggregate(values, pairwise)

        else:
            operation = expr.min if func is min else expr.max

            def aggregate(values: Any) -> Any:
                return operation(values)

        # Python raises ValueError for an empty collection
        empty = expr.throw(
            "KRPC",
            "ArgumentException",
            expr.constant_string("%s() arg is an empty sequence" % func.__name__),
        )
        return _Result(
            expression=self._with_temporaries(
                node,
                cast(KRPC.Type, result.ptype),
                [("collection", result)],
                lambda values: expr.conditional(
                    expr.equal(expr.count(values), expr.constant_int(0)),
                    empty,
                    aggregate(values),
                ),
            ),
            ptype=ptype,
        )

    def _compile_scalar_min_max(
        self,
        node: ast.Call,
        func: Callable,
    ) -> _Result:
        arguments = [self._compile(argument) for argument in node.args]
        if all(argument.is_value for argument in arguments):
            return self._fold(
                node, lambda: func(argument.value for argument in arguments)
            )
        result = arguments[0]
        for argument in arguments[1:]:
            result, argument = self._operand_expressions(node, result, argument)
            self._check_numbers(node, [result, argument])
            result = self._min_max_pair(node, func, result, argument)
        return result

    def _min_max_pair(
        self,
        node: ast.Call,
        func: Callable,
        left: _Result,
        right: _Result,
    ) -> _Result:
        """The smaller or larger of two numbers, of their common type. The second
        is chosen only when it compares smaller or larger, as Python does, so the
        first is chosen when they are equal or either is NaN."""
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
            is_and = isinstance(node.op, ast.And)
            # As for a value, stopping at a constant that decides the result
            tests: list[_Result] = []
            for value in node.values:
                test = self._compile_condition(value)
                if test.is_value and bool(test.value) == is_and:
                    continue
                tests.append(test)
                if test.is_value:
                    break
            if not tests:
                return _Result(value=is_and, is_value=True)
            if len(tests) == 1:
                return tests[0]
            op = self._expr.conditional_and if is_and else self._expr.conditional_or
            expression = self._to_expression(tests[0]).expression
            for test in tests[1:]:
                expression = op(expression, self._to_expression(test).expression)
            return _Result(expression=expression, ptype=build_ptype(KRPC.Type.BOOL))
        return self._truth(node, self._compile(node))

    def _truth(self, node: ast.AST, result: _Result) -> _Result:
        """Whether a value is true, as Python tests it in a condition: a number
        when it is not zero, a string, bytes or a collection when it is not
        empty, and any other value when it is not None. None is false. A value
        of unknown type is left for the server to check."""
        if result.is_value:
            return self._fold(node, bool, result.value)
        ptype = result.ptype
        if ptype is None or (ptype.code == KRPC.Type.BOOL and not ptype.nullable):
            return result
        expr = self._expr
        zero = expr.constant_int(0)
        if ptype.nullable and ptype.code in NUMERIC_CODES + (
            KRPC.Type.BOOL,
            KRPC.Type.STRING,
            KRPC.Type.BYTES,
            KRPC.Type.LIST,
            KRPC.Type.SET,
            KRPC.Type.DICTIONARY,
        ):
            # None is false, and the value is held to test it for None and for truth
            present = with_nullable(ptype, False)
            test = self._with_temporaries(
                node,
                ptype,
                [("value", result)],
                lambda value: expr.conditional_and(
                    expr.not_(expr.is_null(value)),
                    self._truth(
                        node, _Result(expression=value, ptype=present)
                    ).expression,
                ),
            )
        elif ptype.code in NUMERIC_CODES:
            test = expr.not_equal(
                result.expression,
                self._converted_expression(_Result(value=0, is_value=True), ptype),
            )
        elif ptype.code == KRPC.Type.STRING:
            test = expr.not_equal(expr.string_length(result.expression), zero)
        elif ptype.code == KRPC.Type.BYTES:
            test = expr.not_equal(result.expression, expr.constant_bytes(b""))
        elif ptype.code in (KRPC.Type.LIST, KRPC.Type.SET, KRPC.Type.DICTIONARY):
            test = expr.not_equal(expr.count(result.expression), zero)
        elif ptype.code in _ALWAYS_TRUE_CODES and not ptype.nullable:
            # The value is computed for its effects
            test = expr.block([result.expression, expr.constant_bool(True)])
        elif ptype.code in _ALWAYS_TRUE_CODES:
            test = expr.not_(expr.is_null(result.expression))
        else:
            raise self._error(
                node, "cannot test whether this value is true; compare it explicitly"
            )
        return _Result(expression=test, ptype=build_ptype(KRPC.Type.BOOL))

    def _iterable(self, result: _Result) -> _Result:
        """A collection to iterate over. Iterating over a dictionary iterates
        over its keys, as in Python."""
        result = self._to_expression(result)
        ptype = result.ptype
        if ptype is not None and ptype.code == KRPC.Type.DICTIONARY:
            return _Result(
                expression=self._expr.dictionary_keys(result.expression),
                ptype=build_ptype(KRPC.Type.LIST, [ptype.types[0]]),
            )
        return result

    def _to_expression(
        self, result: _Result, ptype: KRPC.Type | None = None
    ) -> _Result:
        """A value as an expression. The type is that of a None or an empty
        collection, and of any None or empty collection within the value."""
        if not result.is_value:
            return result
        value = result.value
        expr = self._expr
        if value is None:
            if ptype is None:
                raise self._error(
                    self._node,
                    "cannot determine the type of None; give it one with an "
                    "annotation or typing.cast",
                    _Untyped,
                )
            return self._null(self._node, ptype)
        if _is_empty_collection(value) and ptype is not None:
            empty = self._empty_collection(value, ptype)
            if empty is not None:
                return empty
            kind = _COLLECTION_KINDS.get(ptype.code)
            if kind is not None and kind in _EMPTY_FORMS:
                raise self._error(self._node, self._empty_kind_message(value, kind))
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
            raise self._error(self._node, "integer constant %d is out of range" % value)
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
                    typ.field_names, cast(tuple[object, ...], value), typ.field_types
                )
            ]
            return _Result(
                expression=expr.create_struct(
                    self._remote_type(None, typ.protobuf_type), fields
                ),
                ptype=typ.protobuf_type,
            )
        if isinstance(value, (list, tuple, set, dict)):
            return self._collection_expression(value, ptype)
        raise self._error(
            self._node,
            "cannot use a value of type %s in an expression" % type(value).__name__,
        )

    def _collection_expression(self, value: Any, ptype: KRPC.Type | None) -> _Result:
        """A list, tuple, set or dictionary as an expression. The type is that of
        any None or empty collection within the value."""
        expr = self._expr
        if isinstance(value, (list, tuple, set)):
            values = [_Result(value=element, is_value=True) for element in value]
            if isinstance(value, tuple):
                self._check_tuple_size(len(values))
                elements = [
                    self._to_expression(
                        element, self._position_ptype(ptype, KRPC.Type.TUPLE, i)
                    )
                    for i, element in enumerate(values)
                ]
            else:
                code = KRPC.Type.LIST if isinstance(value, list) else KRPC.Type.SET
                if code == KRPC.Type.SET:
                    self._check_not_none(values, "the values of a set cannot be None")
                elements = self._typed_elements(
                    values, self._position_ptype(ptype, code, 0)
                )
            if not elements:
                raise self._error(
                    self._node,
                    "cannot determine the type of an empty collection; give it one "
                    "with an annotation or typing.cast, for example "
                    "cast(List[int], [])",
                    _Untyped,
                )
            expressions = [element.expression for element in elements]
            if isinstance(value, list):
                ptype = build_ptype(KRPC.Type.LIST, [self._joined_ptype(elements)])
                return _Result(expression=expr.create_list(expressions), ptype=ptype)
            if isinstance(value, tuple):
                return _Result(
                    expression=expr.create_tuple(expressions),
                    ptype=build_ptype(
                        KRPC.Type.TUPLE,
                        cast(list[KRPC.Type], [element.ptype for element in elements]),
                    ),
                )
            ptype = build_ptype(KRPC.Type.SET, [self._joined_ptype(elements)])
            return _Result(expression=expr.create_set(set(expressions)), ptype=ptype)
        key_values = [_Result(value=key, is_value=True) for key in value.keys()]
        self._check_not_none(key_values, "the keys of a dictionary cannot be None")
        keys = self._typed_elements(
            key_values, self._position_ptype(ptype, KRPC.Type.DICTIONARY, 0)
        )
        values = self._typed_elements(
            [_Result(value=item, is_value=True) for item in value.values()],
            self._position_ptype(ptype, KRPC.Type.DICTIONARY, 1),
        )
        if not keys:
            raise self._error(
                self._node,
                "cannot determine the type of an empty dictionary; give it one "
                "with an annotation or typing.cast, for example "
                "cast(Dict[str, int], {})",
                _Untyped,
            )
        ptype = build_ptype(
            KRPC.Type.DICTIONARY,
            [self._joined_ptype(keys), self._joined_ptype(values)],
        )
        self._check_keys(self._node, ptype)
        return _Result(
            expression=expr.create_dictionary(
                [key.expression for key in keys],
                [item.expression for item in values],
            ),
            ptype=ptype,
        )

    def _empty_collection(self, value: object, ptype: KRPC.Type) -> _Result | None:
        """An empty list, set or dictionary of a collection type of the same
        kind. None for a type of another kind."""
        expr = self._expr
        node = self._node
        if isinstance(value, list) and ptype.code == KRPC.Type.LIST:
            expression = expr.create_empty_list(self._remote_type(node, ptype.types[0]))
        elif isinstance(value, set) and ptype.code == KRPC.Type.SET:
            expression = expr.create_empty_set(self._remote_type(node, ptype.types[0]))
        elif isinstance(value, dict) and ptype.code == KRPC.Type.DICTIONARY:
            expression = expr.create_empty_dictionary(
                self._remote_type(node, ptype.types[0]),
                self._remote_type(node, ptype.types[1]),
            )
        else:
            return None
        return _Result(expression=expression, ptype=with_nullable(ptype, False))

    @staticmethod
    def _empty_kind_message(value: object, kind: type) -> str:
        """The error for an empty collection given a collection type of
        another kind."""
        return "an empty %s cannot be given a %s type; use %s" % (
            type(value).__name__,
            kind.__name__,
            _EMPTY_FORMS[kind],
        )

    def _check_tuple_size(self, size: int) -> None:
        if size > _MAX_TUPLE_SIZE:
            raise self._error(
                self._node, "a tuple can hold at most %d values" % _MAX_TUPLE_SIZE
            )

    def _null(self, node: ast.AST | None, ptype: KRPC.Type) -> _Result:
        """A null constant of a type, made nullable."""
        nullable = with_nullable(ptype, True)
        return _Result(
            expression=self._expr.constant_null(self._remote_type(node, nullable)),
            ptype=nullable,
        )

    @staticmethod
    def _is_none(result: _Result) -> bool:
        return result.is_value and result.value is None

    def _check_not_none(self, results: list[_Result], message: str) -> None:
        if any(self._is_none(result) for result in results):
            raise self._error(self._node, message)

    @staticmethod
    def _position_ptype(
        ptype: KRPC.Type | None, code: int, position: int
    ) -> KRPC.Type | None:
        """The type at a position of a collection type, if it is of that kind."""
        if ptype is None or ptype.code != code or position >= len(ptype.types):
            return None
        return ptype.types[position]

    def _typed_elements(
        self, results: list[_Result], ptype: KRPC.Type | None = None
    ) -> list[_Result]:
        """Values that are combined into one type, such as the elements of a list
        or the branches of a conditional, as expressions. A None or an empty
        collection takes the given type, or else the type of the other values, as
        does one within a value such as a tuple."""
        converted = [
            None if self._is_none(result) else self._typed_or_none(result, ptype)
            for result in results
        ]
        if ptype is None:
            known = [result for result in converted if result is not None]
            ptype = self._widened_ptype(known) if known else None
        return [
            self._to_expression(result, ptype) if typed is None else typed
            for result, typed in zip(results, converted)
        ]

    def _typed_or_none(
        self, result: _Result, ptype: KRPC.Type | None
    ) -> _Result | None:
        """A value as an expression, or None when it holds a None or an empty
        collection with no type."""
        try:
            return self._to_expression(result, ptype)
        except _Untyped:
            return None

    def _call_node(
        self,
        node: ast.AST | None,
        service: str,
        procedure: KRPC.Procedure,
        args: list[_Result | None],
    ) -> _Result:
        # A StdLib call computes the same value from the same arguments
        if service != "StdLib":
            self._remote_calls += 1
        call = KRPC.ProcedureCall()
        call.service = service
        call.procedure = procedure.name
        parameters = list(procedure.parameters)
        # Arguments are keyed by position. The server gives a position a keyword
        # argument skips the parameter's default
        expressions: dict[int, Any] = {}
        for position, arg in enumerate(args):
            if arg is None:
                continue
            parameter = parameters[position] if position < len(parameters) else None
            if arg.is_value and arg.value is None:
                # A null argument is a constant of the call
                if parameter is None or not parameter.type.nullable:
                    raise self._error(node, "'%s' does not take None" % procedure.name)
                call.arguments.add(position=position, is_null=True)
                continue
            expressions[position] = self._argument_expression(
                node, arg, procedure, parameter
            )
        expression = self._expr.call_with_arguments(call, expressions)
        if not procedure.HasField("return_type"):
            self._valueless_calls.append(expression)
            return _Result(expression=expression)
        return _Result(expression=expression, ptype=procedure.return_type)

    def _argument_expression(
        self,
        node: ast.AST | None,
        result: _Result,
        procedure: KRPC.Procedure,
        parameter: KRPC.Parameter | None,
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
        target: KRPC.Type | None,
        node: ast.AST | None = None,
        where: str = "",
        hint: str = "",
    ) -> Any:
        """Convert a value to the type it is being used as. Python numbers have
        no sizes, so a numeric constant is built as the exact type wanted. A
        numeric expression of a different type is converted with a cast. A
        conversion that narrows to an integer type is an error. The where and
        hint strings name the place in the error message."""
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
            if isinstance(value, float):
                raise self._narrowing_error(node, KRPC.Type.DOUBLE, target, where, hint)
            if not _in_range(value, target.code):
                message = "integer constant %d is out of range for %s" % (
                    value,
                    NUMERIC_NAMES[target.code],
                )
                raise self._error(node, message + (" " + where if where else ""))
            return self._integer_constant(value, target.code)
        converted = self._to_expression(result, target)
        if (
            target is not None
            and converted.ptype is not None
            and not _convertible(converted.ptype, target)
        ):
            raise self._error(
                node,
                "cannot use a value of type %s %s"
                % (
                    _type_name(converted.ptype),
                    where or "as a value of type %s" % _type_name(target),
                ),
            )
        if (
            target is not None
            and target.code in NUMERIC_CODES
            and converted.ptype is not None
            and converted.ptype.code in NUMERIC_CODES
            and converted.ptype.code != target.code
        ):
            # A Python float is a double, so a narrowing to a float is allowed
            widened = promote(converted.ptype, target)
            if target.code != KRPC.Type.FLOAT and (
                widened is None or widened.code != target.code
            ):
                raise self._narrowing_error(
                    node, converted.ptype.code, target, where, hint
                )
            return self._expr.cast(
                converted.expression, self._remote_type(None, target)
            )
        return converted.expression

    def _integer_constant(self, value: int, code: KRPC.Type.TypeCode) -> Any:
        """A constant of one of the integer types."""
        return {
            KRPC.Type.SINT32: self._expr.constant_int,
            KRPC.Type.SINT64: self._expr.constant_long,
            KRPC.Type.UINT32: self._expr.constant_u_int,
            KRPC.Type.UINT64: self._expr.constant_u_long,
        }[code](value)

    def _widened_expression(
        self,
        node: ast.AST | None,
        target: KRPC.Type | None,
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
            # A Python numeric literal carries no type of its own and is
            # written at the target type
            return self._converted_expression(value, target, node, where, hint)
        converted = self._to_expression(value, target)
        if converted.ptype is None or converted.ptype == target:
            return converted.expression
        # The server checks for a null that the target does not allow
        if target.code not in NUMERIC_CODES and same_shape(converted.ptype, target):
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
        node: ast.AST | None,
        source: KRPC.Type.TypeCode,
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
        class_name: str | None,
        member: str,
        kind: str,
    ) -> tuple[str, KRPC.Procedure]:
        entry = self._metadata.members.get((service, class_name, member, kind))
        if entry is None:
            target = service if class_name is None else service + "." + class_name
            raise self._error(
                node, "'%s' is not a remote member of %s" % (member, target)
            )
        return entry

    def _class_of(self, node: ast.AST, base: _Result) -> tuple[str, str]:
        if base.ptype is None or base.ptype.code != KRPC.Type.CLASS:
            raise self._error(
                node,
                "cannot access a member of a value that is not an object "
                "with a known class",
            )
        return base.ptype.service, base.ptype.name

    def _class_ptype(self, python_type: type) -> KRPC.Type:
        # A class accessed through a service is a subclass of the registered one
        for base in python_type.__mro__:
            for typ in self._client._types._types.values():
                if isinstance(typ, ClassType) and typ.python_type is base:
                    return typ.protobuf_type
        raise self._error(
            self._node, "cannot determine the remote class of %s" % python_type.__name__
        )

    def _enum_ptype(self, python_type: type) -> KRPC.Type:
        for typ in self._client._types._types.values():
            if isinstance(typ, EnumerationType) and typ.python_type is python_type:
                return typ.protobuf_type
        raise self._error(
            self._node,
            "cannot determine the remote enumeration of %s" % python_type.__name__,
        )

    def _struct_type_of(self, node: ast.AST | None, python_type: type) -> StructType:
        """The structure type a value of the given Python type belongs to."""
        for typ in self._client._types._types.values():
            if isinstance(typ, StructType) and typ.python_type is python_type:
                return self._known_struct_type(node, typ)
        raise self._error(
            node, "cannot determine the remote structure of %s" % python_type.__name__
        )

    def _struct_type_named(
        self, node: ast.AST | None, service: str, name: str
    ) -> StructType:
        return self._known_struct_type(
            node, self._client._types.struct_type(service, name)
        )

    def _known_struct_type(self, node: ast.AST | None, typ: StructType) -> StructType:
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

    def _is_remote_struct(self, value: object) -> TypeGuard[type]:
        return isinstance(value, type) and any(
            isinstance(typ, StructType) and typ.python_type is value
            for typ in self._client._types._types.values()
        )

    def _is_remote_class(self, value: object) -> bool:
        value = unwrap_class(value)
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

    def _element_ptype(
        self,
        node: ast.AST,
        collection: _Result,
        action: str = "iterate over",
        ordered: bool = False,
    ) -> KRPC.Type:
        """The type of the elements of a list, or of a set unless the action
        needs an order. The action names what is done with the collection in an
        error."""
        ptype = collection.ptype
        if ptype is None:
            raise self._error(
                node, "cannot determine the type of the elements of the collection"
            )
        if ptype.code == KRPC.Type.LIST or (
            ptype.code == KRPC.Type.SET and not ordered
        ):
            return ptype.types[0]
        raise self._error(
            node, "cannot %s a value of type %s" % (action, _type_name(ptype))
        )

    def _check_keys(self, node: ast.AST | None, ptype: KRPC.Type | None) -> None:
        """Raise for a type holding a dictionary whose keys the server does not
        take."""
        if ptype is None:
            return
        if ptype.code == KRPC.Type.DICTIONARY and ptype.types[0].code not in _KEY_CODES:
            raise self._error(
                node,
                "the keys of a dictionary must be integers, bools or strings, not "
                "values of type %s" % _type_name(ptype.types[0]),
            )
        for element in ptype.types:
            self._check_keys(node, element)

    def _remote_type(self, node: ast.AST | None, ptype: KRPC.Type | None) -> Any:
        self._check_keys(node, ptype)
        try:
            return remote_type(self._type, ptype, self._remote_types)
        except ValueError as exc:
            raise self._error(node, str(exc)) from exc

    @staticmethod
    def _widened_ptype(elements: list[_Result]) -> KRPC.Type | None:
        """The type the server widens the elements of a collection to."""
        return common_ptype([element.ptype for element in elements])

    def _joined_ptype(self, elements: list[_Result]) -> KRPC.Type:
        """The type the server widens the values of a captured collection to.
        Throws an error if they have no common type."""
        ptype = self._widened_ptype(elements)
        if ptype is None:
            raise self._error(
                self._node, "the values of a collection have no common type"
            )
        return ptype

    @staticmethod
    def _collection_ptype(code: int, elements: list[_Result]) -> KRPC.Type | None:
        if any(element.ptype is None for element in elements):
            return None
        return build_ptype(
            code, [element.ptype for element in elements if element.ptype]
        )

    def _fold(
        self,
        node: ast.AST | None,
        func: Callable,
        *args: Any,
        prefix: str = "",
    ) -> _Result:
        """A value computed when compiling, by calling a function on values
        known when compiling. An error the call raises is a compile error, with
        the prefix before its message. So is a remote procedure call it makes,
        which would be made once, when compiling, and not on each evaluation."""
        sent = _sent_calls()
        try:
            value = func(*args)
        except Exception as exc:
            raise self._error(node, prefix + str(exc)) from exc
        if _sent_calls() != sent:
            raise self._error(
                node,
                "a client side function called here makes a remote procedure "
                "call, which would run once, when the function is compiled; "
                "make the call in the function instead",
            )
        return _Result(value=value, is_value=True)

    def _error(
        self,
        node: ast.AST | None,
        message: str,
        error: type[FunctionCompilationError] = FunctionCompilationError,
    ) -> FunctionCompilationError:
        location = ""
        if node is not None and hasattr(node, "lineno"):
            location = " (line %d)" % node.lineno  # pyright: ignore[reportAttributeAccessIssue]
        return error("Cannot compile expression: " + message + location)
