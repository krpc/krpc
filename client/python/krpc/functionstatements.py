"""Compilation of Python statements (assignments, control flow and side
effects) into server side expression nodes. Used by the function compiler
for function bodies; expression compilation itself lives in
krpc.functioncompiler."""

# One class compiles every kind of statement, and each kind reaches back into
# the expression compiler
# pylint: disable=too-many-lines

from __future__ import annotations
import ast
import enum
from typing import (
    Any,
    Callable,
    Dict,
    List,
    NoReturn,
    Optional,
    Set,
    Tuple,
    TYPE_CHECKING,
    cast,
)

from krpc.definiteassignment import DefiniteAssignment
from krpc.error import FunctionCompilationError
from krpc.expressionutils import (
    Result as _Result,
    build_ptype,
    common_ptype,
    is_no_more_nullable,
    join_ptype,
    plain_ptype,
    same_shape,
    unwrap_class,
    with_nullable,
)
from krpc.types import EXCEPTION_TYPES, ClassBase
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.functioncompiler import _Compiler


# The value of a variable that has only been assigned None, so its type comes
# from a later assignment
UNTYPED = object()


class UntypedRead(Exception):
    """A read of a variable whose type is not known yet."""

    def __init__(self, name: str):
        super().__init__(name)
        self.name = name


class Recompile(Exception):
    """A type an earlier part of the function needed is now known, so the
    function is compiled again."""


_JUMP_NAMES = {ast.Return: "return", ast.Break: "break", ast.Continue: "continue"}


def _jump_out_of(
    statements: List[ast.stmt], in_loop: bool = False
) -> Optional[ast.stmt]:
    """The first return, break or continue that leaves the statements. A break
    or continue within a loop of the statements stays in it, and a local
    function's statements are its own."""
    for statement in statements:
        if isinstance(statement, ast.Return) or (
            isinstance(statement, (ast.Break, ast.Continue)) and not in_loop
        ):
            return statement
        if isinstance(statement, ast.FunctionDef):
            continue
        looped = in_loop or isinstance(statement, (ast.For, ast.While))
        nested = [getattr(statement, field, []) for field in ("body", "finalbody")]
        nested += [handler.body for handler in getattr(statement, "handlers", [])]
        for block in nested + [getattr(statement, "orelse", [])]:
            jump = _jump_out_of(block, looped)
            if jump is not None:
                return jump
    return None


def _breaks_out_of(statements: List[ast.stmt]) -> bool:
    """Whether a break in the statements leaves the loop they are the body of.
    A break in a nested loop leaves only that loop."""
    for statement in statements:
        if isinstance(statement, ast.Break):
            return True
        if isinstance(statement, (ast.For, ast.While, ast.FunctionDef)):
            continue
        blocks = [
            getattr(statement, field, []) for field in ("body", "orelse", "finalbody")
        ]
        blocks += [handler.body for handler in getattr(statement, "handlers", [])]
        if any(_breaks_out_of(block) for block in blocks):
            return True
    return False


def _raises_on_every_path(statements: List[ast.stmt]) -> bool:
    """Whether the statements end with a raise, an if and else whose branches
    both do, or a try whose body and every handler do, or whose finally
    clause does."""
    if not statements:
        return False
    final = statements[-1]
    if isinstance(final, ast.Raise):
        return True
    if isinstance(final, ast.If):
        return _raises_on_every_path(final.body) and _raises_on_every_path(final.orelse)
    if isinstance(final, ast.Try):
        handled = all(_raises_on_every_path(handler.body) for handler in final.handlers)
        return _raises_on_every_path(final.finalbody) or (
            _raises_on_every_path(final.body) and handled
        )
    return False


def _hidden(name: str) -> str:
    """A name no Python source can spell, for a temporary the statement compiler
    holds a value in."""
    return " " + name


def _is_none_node(node: Optional[ast.AST]) -> bool:
    return isinstance(node, ast.Constant) and node.value is None


def _is_plain(node: ast.expr) -> bool:
    """Whether evaluating an expression makes no call, as for a name or a
    constant."""
    if isinstance(node, ast.UnaryOp):
        return _is_plain(node.operand)
    return isinstance(node, (ast.Name, ast.Constant))


def _type_name(node: ast.expr) -> Optional[str]:
    """The name an annotation gives a type, as in list or typing.List."""
    if isinstance(node, ast.Name):
        return node.id
    if isinstance(node, ast.Attribute):
        return node.attr
    return None


# The kRPC exceptions each builtin Python exception stands for, as the Python client
# raises the KRPC service's general exceptions as builtins. Catching a builtin
# catches every exception it and its subclasses stand for. Raising one raises the
# first, and raising Exception raises the KRPC service's Error. A service's own
# exceptions are generated classes with their own names
_BUILTIN_EXCEPTIONS = {
    RuntimeError: [("KRPC", "InvalidOperationException")],
    ValueError: [
        ("KRPC", "ArgumentException"),
        ("KRPC", "ArgumentNullException"),
        ("KRPC", "ArgumentOutOfRangeException"),
    ],
    TypeError: [("KRPC", "NullReferenceException")],
    KeyError: [("KRPC", "KeyNotFoundException")],
    IndexError: [("KRPC", "IndexOutOfRangeException")],
    ZeroDivisionError: [("KRPC", "DivideByZeroException")],
    NotImplementedError: [("KRPC", "NotSupportedException")],
    TimeoutError: [("KRPC", "TimeoutException")],
    OverflowError: [("KRPC", "OverflowException")],
}


# The Python type and the literal of an empty collection, by its type code
_EMPTY_COLLECTIONS: Dict[int, Tuple[type, str]] = {
    KRPC.Type.LIST: (list, "[]"),
    KRPC.Type.SET: (set, "set()"),
    KRPC.Type.DICTIONARY: (dict, "{}"),
}


class _StatementCompiler:  # pylint: disable=too-many-instance-attributes
    """Compiles the statements of a function body, using the expression
    compiler for the expressions within them. Holds the per-function state:
    declared variables and the function's return type."""

    def __init__(self, compiler: _Compiler):
        self._compiler = compiler
        self._scope: Dict[str, _Result] = {}
        self._declared: List[Any] = []
        self._return_ptype: Optional[KRPC.Type] = None
        self._return_annotation: Optional[KRPC.Type] = None
        self._saw_valued_return = False
        # Whether a return gives a value, so a return None gives a null
        self._returns_values = False
        # Whether the function compiled by compile_body evaluates to no value
        self.valueless = False
        # Whether a return None was compiled before the return type was known
        self._null_return = False
        # The names declared by an annotation, whose type is not widened
        self._annotated: Set[str] = set()
        # The names of the state variables the function declares nonlocal
        self._state: Set[str] = set()
        # The names of the function's parameters, whose type is not widened
        self._parameters: Set[str] = set()
        # The assignments of None to variables whose type is not known yet
        self._untyped: Dict[str, ast.AST] = {}
        # Identifies the function body to the types its variables are widened to
        self._body: Any = None
        # The local functions compiled at their first call, as they read
        # variables assigned after their definition
        self._deferred: Set[ast.AST] = set()

    def compile_body(
        self,
        statements: List[ast.stmt],
        returns: Optional[ast.expr] = None,
        parameters: Optional[List[Any]] = None,
    ) -> _Result:
        """Compile a function body of statements into a server side function
        with the given parameters. A function with no parameters is invoked
        immediately, so the result is its value."""
        parameters = parameters or []
        assignment = DefiniteAssignment(self._compiler, self._compiler._local_names)
        assignment.check(statements, frozenset(name for name, _, _ in parameters))
        self._deferred = assignment.deferred
        state = self._state_variables()
        function, return_ptype = self.compile_function(
            statements, parameters, returns, state
        )
        self.valueless = not self._saw_valued_return and return_ptype is None
        if parameters:
            return _Result(expression=function, ptype=return_ptype)
        invoked = self._compiler._expr.invoke(function, {})
        return _Result(expression=invoked, ptype=return_ptype)

    def _state_variables(self) -> Dict[str, _Result]:
        """The state variables for the names the function declares nonlocal,
        each starting with the value its closure cell holds. A statement
        name = cast(T, name) in the function's body gives a state variable the
        type T, so it can start as None or an empty collection."""
        compiler = self._compiler
        casts = self._state_casts()
        state: Dict[str, _Result] = {}
        for name in sorted(compiler._state_names):
            value = compiler._lookup(name, compiler._node)
            if name in casts:
                ptype, statement = casts[name]
                initial = self._typed_initial_value(statement, value, ptype)
                state[name] = _Result(
                    expression=compiler._expr.state_variable(name, initial),
                    ptype=ptype,
                )
                continue
            hint = "; give it one with a cast, for example %s = cast(%s, %s)"
            if value.is_value and value.value is None:
                raise compiler._error(
                    compiler._node,
                    "the state variable '%s' is None when the function is compiled, "
                    "which gives it no type" % name
                    + hint % (name, "Optional[float]", name),
                )
            if value.is_value and self._is_empty_collection(value.value):
                raise compiler._error(
                    compiler._node,
                    "the state variable '%s' is empty when the function is compiled, "
                    "which gives it no type" % name + hint % (name, "List[int]", name),
                )
            initial = compiler._to_expression(value)
            if initial.ptype is None:
                raise compiler._error(
                    compiler._node,
                    "cannot determine the type of state variable '%s'" % name,
                )
            state[name] = _Result(
                expression=compiler._expr.state_variable(name, initial.expression),
                ptype=initial.ptype,
            )
        return state

    def _state_casts(self) -> Dict[str, Tuple[KRPC.Type, ast.stmt]]:
        """The types the statements name = cast(T, name) in the function's body
        give its state variables. Such a cast is a statement of the body itself,
        before any other use of the variable."""
        compiler = self._compiler
        casts: Dict[str, Tuple[KRPC.Type, ast.stmt]] = {}
        used: Set[str] = set()
        for statement in getattr(compiler._node, "body", []):
            name = self._state_cast(statement)
            if name is not None:
                if name in used:
                    raise compiler._error(
                        statement,
                        "the cast of state variable '%s' must come before any "
                        "other use of it" % name,
                    )
                value = cast(ast.Call, cast(ast.Assign, statement).value)
                casts[name] = (annotation_ptype(compiler, value.args[0]), statement)
                used.add(name)
                continue
            for node in ast.walk(statement):
                if isinstance(node, ast.Name):
                    used.add(node.id)
                elif isinstance(node, ast.stmt) and node is not statement:
                    nested = self._state_cast(node)
                    if nested is not None:
                        raise compiler._error(
                            node,
                            "the cast of state variable '%s' must be a statement "
                            "of the function's body, outside any nested block" % nested,
                        )
        return casts

    def _state_cast(self, statement: ast.AST) -> Optional[str]:
        """The state variable a statement name = cast(T, name) gives a type, or
        None for any other statement."""
        compiler = self._compiler
        if not (
            isinstance(statement, ast.Assign)
            and len(statement.targets) == 1
            and isinstance(statement.targets[0], ast.Name)
            and isinstance(statement.value, ast.Call)
        ):
            return None
        name = statement.targets[0].id
        arguments = statement.value.args
        if (
            name in compiler._state_names
            and len(arguments) == 2
            and isinstance(arguments[1], ast.Name)
            and arguments[1].id == name
            and compiler._is_typing_cast(statement.value.func)
        ):
            return name
        return None

    def _typed_initial_value(
        self, statement: ast.stmt, value: _Result, ptype: KRPC.Type
    ) -> Any:
        """The initial value of a state variable, converted to the type a cast
        gives it."""
        compiler = self._compiler
        if value.is_value and self._is_empty_collection(value.value):
            return self._empty_collection(statement, ptype, value.value)
        if compiler._is_none(value) and not ptype.nullable:
            assert isinstance(statement, ast.Assign)
            name = cast(ast.Name, statement.targets[0]).id
            raise compiler._error(
                statement,
                "the state variable '%s' is None when the function is compiled, "
                "and is cast to a type that cannot be None" % name,
            )
        initial = compiler._to_expression(value, ptype)
        if initial.ptype == ptype:
            return initial.expression
        return compiler._expr.cast(
            initial.expression, compiler._remote_type(statement, ptype)
        )

    def compile_function(
        self,
        statements: List[ast.stmt],
        parameters: List[Any],
        returns: Optional[ast.expr] = None,
        state: Optional[Dict[str, _Result]] = None,
    ) -> Any:
        """Compile statements into a server side function with the given
        parameters, each a (name, ptype, parameter expression) triple, return
        annotation and state variables. Returns the function expression and its
        return type."""
        scope: Dict[str, _Result] = dict(state or {})
        self._state = set(scope)
        self._parameters = {name for name, _, _ in parameters}
        for name, ptype, parameter in parameters:
            scope[name] = _Result(expression=parameter, ptype=ptype)
        self._scope = scope
        self._compiler._scopes.append(scope)
        previous = self._compiler._active_statements
        self._compiler._active_statements = self
        self._declared: List[Any] = []
        self._saw_valued_return = False
        self._null_return = False
        self._annotated = set()
        self._untyped = {}
        self._body = (
            (statements[0].lineno, statements[0].col_offset) if statements else None
        )
        self._return_annotation = self._return_annotation_ptype(returns)
        self._return_ptype = self._return_annotation or (
            self._compiler._variable_types.get((self._body, None))
        )
        self._returns_values = self._return_annotation is not None or (
            self._has_valued_return(statements)
        )
        local_functions = dict(self._compiler._local_functions)
        block_functions = set(self._compiler._block_functions)
        try:
            compiled = self._compile_statements(statements, top_level=True)
            for name, node in self._untyped.items():
                raise self._compiler._error(
                    node,
                    "cannot determine the type of variable '%s'; annotate it, "
                    "for example %s: Optional[int] = None" % (name, name),
                )
            raises = _raises_on_every_path(statements)
            if (
                self._return_annotation is not None
                and not self._saw_valued_return
                and not raises
            ):
                raise self._compiler._error(
                    returns,
                    "the function is annotated with a return type, but returns no "
                    "value",
                )
            if not self._saw_valued_return and self._return_annotation is not None:
                compiled.append(
                    self._unreachable_value(returns, self._return_annotation)
                )
            elif not self._saw_valued_return:
                # The function produces no value; end it with a bare return so
                # a trailing expression statement is not treated as a result
                compiled.append(self._compiler._expr.return_nothing())
            if self._declared:
                body = self._compiler._expr.block_with_variables(
                    self._declared, compiled
                )
            else:
                body = self._compiler._expr.block(compiled)
            function = self._compiler._expr.lambda_(
                [parameter for _, _, parameter in parameters], body
            )
            return function, self._return_ptype
        finally:
            self._compiler._active_statements = previous
            self._compiler._scopes.pop()
            self._compiler._local_functions.clear()
            self._compiler._local_functions.update(local_functions)
            self._compiler._block_functions.clear()
            self._compiler._block_functions.update(block_functions)

    def _unreachable_value(self, node: Any, ptype: KRPC.Type) -> Any:
        """A value of a type that ends a function whose every path raises. It is
        never computed, and gives the function the type of its annotation."""
        compiler = self._compiler
        nullable = with_nullable(ptype, True)
        null = compiler._expr.constant_null(compiler._remote_type(node, nullable))
        if ptype.nullable:
            return null
        return compiler._expr.cast(null, compiler._remote_type(node, ptype))

    def _compile_statements(
        self,
        statements: List[ast.stmt],
        top_level: bool = False,
        as_value: bool = False,
    ) -> List[Any]:
        """Compile a list of statements. When the list is the value of the
        function, or of a branch of the function's final if, its final return
        compiles to the value itself."""
        as_value = as_value or top_level
        compiled: List[Any] = []
        for position, statement in enumerate(statements):
            # An error converting a value names the statement, unless it is
            # within an expression of the statement
            self._compiler._node = statement
            last = position == len(statements) - 1
            try:
                compiled.extend(
                    self._compile_statement(statement, as_value=as_value and last)
                )
            except UntypedRead as read:
                # Compile the statements within, which can assign the variable
                # the value that gives its type
                if read.name not in self._untyped:
                    raise
                for nested in self._nested_statements(statement):
                    self._compile_statements(nested)
        if (
            top_level
            and self._saw_valued_return
            and not self._returns_a_value(statements)
        ):
            message = (
                "a function that returns a value must end with a return or raise "
                "statement, an if or try that returns or raises on every path, or a "
                "while True loop with no break"
            )
            if statements:
                raise self._compiler._error(statements[-1], message)
            raise FunctionCompilationError("Cannot compile expression: " + message)
        return compiled

    def _return_annotation_ptype(
        self, returns: Optional[ast.expr]
    ) -> Optional[KRPC.Type]:
        """The type a function's return annotation names. None for a function
        annotated as returning None, and for an annotation the compiler does
        not support, whose type is taken from the returns instead."""
        if returns is None or _is_none_node(returns):
            return None
        try:
            return annotation_ptype(self._compiler, returns)
        except FunctionCompilationError:
            return None

    def _compile_statement(self, statement: ast.stmt, as_value: bool) -> List[Any]:
        """Compile a statement into the expressions it consists of. A final
        return of a function's value becomes the value."""
        compiler = self._compiler
        if isinstance(statement, ast.Return):
            return [self._compile_return(statement, as_value=as_value)]
        if (
            isinstance(statement, ast.If)
            and as_value
            and self._branches_give_values([statement])
        ):
            return [self._compile_if_value(statement)]
        if isinstance(statement, ast.Expr):
            if isinstance(statement.value, ast.Constant):
                return []  # docstring or a constant with no effect
            result = compiler._compile_statement(statement.value, discarded=True)
            return [] if result.is_value else [result.expression]
        if isinstance(statement, ast.Pass):
            return []
        if isinstance(statement, ast.Nonlocal):
            return []
        if isinstance(statement, (ast.Assign, ast.AnnAssign, ast.AugAssign)):
            return [
                assignment
                for assignment in self._compile_assignment(statement)
                if assignment is not None
            ]
        if isinstance(statement, ast.FunctionDef):
            self._register_local_function(statement)
            return []
        if isinstance(statement, ast.If):
            compiled_if = self._compile_if(statement)
            return [] if compiled_if is None else [compiled_if]
        if isinstance(statement, ast.Delete):
            return self._compile_delete(statement)
        compilers: Dict[type, Callable[[Any], Any]] = {
            ast.While: self._compile_while,
            ast.For: self._compile_for,
            ast.Raise: self._compile_raise,
            ast.Try: self._compile_try,
            ast.Break: lambda _: compiler._expr.break_(),
            ast.Continue: lambda _: compiler._expr.continue_(),
        }
        compile_statement = compilers.get(type(statement))
        if compile_statement is None:
            raise compiler._error(
                statement, "unsupported statement (%s)" % type(statement).__name__
            )
        return [compile_statement(statement)]

    @staticmethod
    def _nested_statements(statement: ast.stmt) -> List[List[ast.stmt]]:
        if isinstance(statement, (ast.If, ast.While, ast.For)):
            return [statement.body, statement.orelse]
        if isinstance(statement, ast.Try):
            return [statement.body, statement.finalbody] + [
                handler.body for handler in statement.handlers
            ]
        return []

    @staticmethod
    def _has_valued_return(statements: List[ast.stmt]) -> bool:
        """Whether the function has a return with a value other than None,
        outside its local functions."""
        pending: List[ast.AST] = list(statements)
        while pending:
            node = pending.pop()
            if isinstance(node, (ast.FunctionDef, ast.Lambda)):
                continue
            if (
                isinstance(node, ast.Return)
                and node.value is not None
                and not _is_none_node(node.value)
            ):
                return True
            pending.extend(ast.iter_child_nodes(node))
        return False

    def _gives_a_value(self, statement: ast.Return) -> bool:
        """Whether a return gives a value. A return None gives a null in a
        function that returns values, and nothing otherwise."""
        if statement.value is None:
            return False
        return self._returns_values or not _is_none_node(statement.value)

    def _returns_a_value(self, statements: List[ast.stmt]) -> bool:
        """Whether the statements end by returning a value on every path, with a
        return statement, an if and else whose branches both do, a try whose
        body and every handler do, or a while True loop with no break. A raise
        ends a path without returning."""
        if not statements:
            return False
        final = statements[-1]
        if isinstance(final, ast.Raise):
            return True
        if isinstance(final, ast.Return):
            return self._gives_a_value(final)
        if isinstance(final, ast.If):
            return self._returns_a_value(final.body) and self._returns_a_value(
                final.orelse
            )
        if isinstance(final, ast.Try):
            returns = self._returns_a_value
            handled = all(returns(handler.body) for handler in final.handlers)
            return returns(final.finalbody) or (returns(final.body) and handled)
        if isinstance(final, ast.While):
            return (
                isinstance(final.test, ast.Constant)
                and bool(final.test.value)
                and not _breaks_out_of(final.body)
            )
        return False

    def _branches_give_values(self, statements: List[ast.stmt]) -> bool:
        """Whether the statements end with a return of a value, or with an if
        and else whose branches both do or are a single raise. Such a final if
        compiles to a conditional value. Any other final if is a statement,
        with its returns bound by the function."""
        if not statements:
            return False
        final = statements[-1]
        if isinstance(final, ast.Raise):
            return len(statements) == 1
        if isinstance(final, ast.Return):
            return self._gives_a_value(final)
        if isinstance(final, ast.If):
            return self._branches_give_values(
                final.body
            ) and self._branches_give_values(final.orelse)
        return False

    def _compile_if_value(self, statement: ast.If) -> Any:
        """Compile a final if and else that both return a value into a
        conditional value, each branch a block ending with its value."""
        condition = self._compiler._compile_condition(statement.test)
        if condition.is_value:
            branch = statement.body if condition.value else statement.orelse
            return self._value_block(branch)
        return self._compiler._expr.conditional(
            condition.expression,
            self._value_block(statement.body),
            self._value_block(statement.orelse),
        )

    def _value_block(self, statements: List[ast.stmt]) -> Any:
        compiled = self._compile_statements(statements, as_value=True)
        if len(compiled) == 1:
            return compiled[0]
        return self._compiler._expr.block(compiled)

    def _compile_return(self, statement: ast.Return, as_value: bool) -> Any:
        compiler = self._compiler
        if not self._gives_a_value(statement):
            if self._returns_values:
                raise compiler._error(
                    statement,
                    "return must have a value in a function that returns a value",
                )
            return compiler._expr.return_nothing()
        assert statement.value is not None
        value = compiler._compile(statement.value)
        annotation = self._return_annotation
        if compiler._is_none(value):
            if annotation is not None and not annotation.nullable:
                raise compiler._error(
                    statement, "the function's return annotation does not allow None"
                )
            if self._return_ptype is None:
                # The type comes from a later return, and the function is
                # compiled again once it is known
                self._null_return = True
                return compiler._expr.return_nothing()
            result = compiler._null(statement, self._return_ptype)
        elif annotation is not None:
            result = _Result(
                expression=compiler._widened_expression(
                    statement, annotation, value, "for the return value"
                ),
                ptype=annotation,
            )
        else:
            result = compiler._to_expression(value)
        if result.ptype is None:
            raise self._compiler._error(
                statement, "cannot determine the type of the return value"
            )
        if self._return_ptype is None:
            self._return_ptype = result.ptype
        else:
            # The server widens every returned value to their common type
            joined = common_ptype([self._return_ptype, result.ptype])
            if joined is None:
                raise self._compiler._error(
                    statement,
                    "the function returns values with no common type; convert "
                    "them to a common type",
                )
            self._return_ptype = joined
        if annotation is not None:
            self._return_ptype = annotation
        self._saw_valued_return = True
        if self._null_return:
            types = compiler._variable_types
            types[(self._body, None)] = with_nullable(self._return_ptype, True)
            raise Recompile()
        if as_value:
            return result.expression
        return self._compiler._expr.return_(result.expression)

    def _compile_assignment(self, statement: ast.stmt) -> List[Any]:
        annotation: Optional[KRPC.Type] = None
        if isinstance(statement, ast.Assign):
            if len(statement.targets) != 1:
                return self._compile_multiple_assignment(statement)
            target: ast.expr = statement.targets[0]
            if isinstance(target, (ast.Tuple, ast.List)):
                return self._compile_unpacking(statement, target)
            if isinstance(statement.value, ast.Lambda):
                if not isinstance(target, ast.Name):
                    raise self._compiler._error(
                        statement, "unsupported assignment target"
                    )
                self._register_lambda(statement, target.id, statement.value)
                return []
            value = self._compiler._compile(statement.value)
        elif isinstance(statement, ast.AnnAssign):
            if statement.value is None:
                raise self._compiler._error(
                    statement, "the assignment must have a value"
                )
            target = statement.target
            value = self._compiler._compile(statement.value)
            annotation = annotation_ptype(self._compiler, statement.annotation)
            if value.is_value and self._is_empty_collection(value.value):
                value = _Result(
                    expression=self._empty_collection(
                        statement, annotation, value.value
                    ),
                    ptype=annotation,
                )
        else:  # ast.AugAssign
            target = statement.target
            if isinstance(statement.op, ast.Add) and isinstance(
                target, (ast.Name, ast.Subscript)
            ):
                extended = self._compile_list_extend(statement, target)
                if extended is not None:
                    return [extended]
            if isinstance(target, ast.Subscript) and not (
                _is_plain(target.value) and _is_plain(target.slice)
            ):
                compiled = self._compile_subscript_augassign(statement, target)
                if compiled is not None:
                    return [compiled]
            if isinstance(target, ast.Attribute) and not _is_plain(target.value):
                compiled = self._compile_attribute_augassign(statement, target)
                if compiled is not None:
                    return [compiled]
            operator_node = ast.BinOp(
                left=statement.target, op=statement.op, right=statement.value
            )
            ast.copy_location(operator_node, statement)
            ast.fix_missing_locations(operator_node)
            value = self._compiler._compile(operator_node)
        if isinstance(target, ast.Name):
            return [self._assign_variable(statement, target.id, value, annotation)]
        held: List[Any] = []
        if not isinstance(statement, ast.AugAssign):
            held, value = self._value_before_target(statement, target, value)
        if isinstance(target, ast.Attribute):
            return held + [self._assign_attribute(statement, target, value)]
        if isinstance(target, ast.Subscript):
            return held + [self._assign_subscript(statement, target, value)]
        raise self._compiler._error(statement, "unsupported assignment target")

    def _value_before_target(
        self, statement: ast.stmt, target: ast.expr, value: _Result
    ) -> Tuple[List[Any], _Result]:
        """Hold a computed value in a variable when the target of its assignment
        is computed too, as Python evaluates the value before the target. Returns
        the assignment to the variable, if any, and the value to assign."""
        if isinstance(target, ast.Attribute):
            plain = _is_plain(target.value)
        elif isinstance(target, ast.Subscript):
            plain = _is_plain(target.value) and _is_plain(target.slice)
        else:
            return [], value
        if plain or value.is_value:
            return [], value
        value = self._compiler._to_expression(value)
        if value.ptype is None:
            return [], value
        variable = self._hidden_variable(statement, "value", value.ptype)
        return [self._compiler._expr.assign(variable, value.expression)], _Result(
            expression=variable, ptype=value.ptype
        )

    def _compile_list_extend(
        self, statement: ast.AugAssign, target: ast.expr
    ) -> Optional[Any]:
        """Compile += on a list, which appends each value to the list in place, as
        list.extend does. The list is evaluated once, before the values. The
        values are read into a list first, which copies a list extended by
        itself before it changes. None when the target is not a list."""
        compiler = self._compiler
        if isinstance(target, ast.Name) and target.id not in self._scope:
            return None
        collection = compiler._compile(target)
        if (
            collection.is_value
            or collection.ptype is None
            or collection.ptype.code != KRPC.Type.LIST
        ):
            return None
        values = compiler._to_expression(compiler._compile(statement.value))
        if values.ptype is None or values.ptype.code not in (
            KRPC.Type.LIST,
            KRPC.Type.SET,
        ):
            raise compiler._error(
                statement, "a list can only be extended by a list or a set"
            )
        expr = compiler._expr
        held = self._hidden_variable(statement, "list", collection.ptype)
        element = self._hidden_variable(statement, "element", values.ptype.types[0])
        appended = compiler._converted_expression(
            _Result(expression=element, ptype=values.ptype.types[0]),
            collection.ptype.types[0],
            statement,
            "for an element of the list",
        )
        return expr.block(
            [
                expr.assign(held, collection.expression),
                expr.for_each(
                    element,
                    expr.to_list(values.expression),
                    expr.append(held, appended),
                ),
            ]
        )

    def _compile_multiple_assignment(self, statement: ast.Assign) -> List[Any]:
        """Compile an assignment with several targets, such as a = b = 0.
        The value is evaluated once, into a hidden variable, and assigned to
        each target in turn, which takes the type of the value."""
        names = []
        for target in statement.targets:
            if not isinstance(target, ast.Name):
                raise self._compiler._error(
                    statement,
                    "assignments with several targets must assign to names",
                )
            names.append(target.id)
        value = self._compiler._compile(statement.value)
        statements = []
        if not value.is_value:
            value = self._compiler._to_expression(value)
            if value.ptype is None:
                raise self._compiler._error(
                    statement, "cannot determine the type of the value"
                )
            variable = self._hidden_variable(statement, "value", value.ptype)
            statements.append(self._compiler._expr.assign(variable, value.expression))
            value = _Result(expression=variable, ptype=value.ptype)
        return statements + [
            self._assign_variable(statement, name, value) for name in names
        ]

    def _compile_unpacking(self, statement: ast.Assign, target: ast.expr) -> List[Any]:
        """Compile an assignment that unpacks a tuple into several names, such as
        a, b = b, a. The whole value is evaluated before any name is assigned."""
        value = self._compiler._compile(statement.value)
        if value.is_value:
            return [
                self._assign_variable(
                    statement, name, _Result(value=part, is_value=True)
                )
                for name, part in self._unpack_constant(statement, target, value.value)
            ]
        value = self._compiler._to_expression(value)
        names = self._compiler._unpack(statement, target, value.ptype)
        variable = self._hidden_variable(statement, "unpacked", value.ptype)
        assign = self._compiler._expr.assign(variable, value.expression)
        return [assign] + self._assign_parts(statement, variable, names)

    def _unpack_constant(
        self, node: ast.stmt, target: ast.expr, value: object
    ) -> List[tuple]:
        """The names a target binds to the parts of a value known when compiling."""
        if isinstance(target, ast.Name):
            return [(target.id, value)]
        if (
            isinstance(target, (ast.Tuple, ast.List))
            and isinstance(value, (tuple, list))
            and len(value) == len(target.elts)
        ):
            return [
                pair
                for part_target, part in zip(target.elts, value)
                for pair in self._unpack_constant(node, part_target, part)
            ]
        raise self._compiler._error(node, "cannot unpack the value into this target")

    def _hidden_variable(
        self, node: ast.stmt, name: str, ptype: Optional[KRPC.Type]
    ) -> Any:
        """Declare a variable the function's own code cannot name."""
        variable = self._compiler._expr.variable(
            name, self._compiler._remote_type(node, ptype)
        )
        self._declared.append(variable)
        return variable

    def _assign_parts(
        self, node: ast.stmt, variable: Any, names: List[tuple]
    ) -> List[Any]:
        """Assign each part of the tuple a variable holds to the name _unpack
        found for it."""
        return [
            self._assign_variable(
                node,
                name,
                _Result(
                    expression=self._compiler._unpacked(variable, path), ptype=ptype
                ),
            )
            for name, path, ptype in names
        ]

    def _register_local_function(self, statement: ast.FunctionDef) -> None:
        """Compile a nested function definition into a server side function,
        registered so that calls to it become invocations. Parameters must
        have type annotations."""
        arguments = statement.args
        if (
            arguments.posonlyargs
            or arguments.kwonlyargs
            or arguments.vararg
            or arguments.kwarg
            or arguments.defaults
        ):
            raise self._compiler._error(
                statement,
                "local functions only support plain positional parameters",
            )
        if any(
            isinstance(node, ast.Name) and node.id == statement.name
            for body_statement in statement.body
            for node in ast.walk(body_statement)
        ):
            raise self._compiler._error(
                statement,
                "local function '%s' refers to itself; recursion is not supported"
                % statement.name,
            )
        parameters = []
        for argument in arguments.args:
            if argument.annotation is None:
                raise self._compiler._error(
                    statement,
                    "annotate the parameter types of local function '%s'"
                    % statement.name,
                )
            ptype = annotation_ptype(self._compiler, argument.annotation)
            parameter = self._compiler._expr.parameter(
                argument.arg, self._compiler._remote_type(statement, ptype)
            )
            parameters.append((argument.arg, ptype, parameter))

        def compile_() -> Tuple[Any, List[Tuple[str, KRPC.Type]], Any]:
            nested = _StatementCompiler(self._compiler)
            nested._deferred = self._deferred
            function, return_ptype = nested.compile_function(
                statement.body, parameters, statement.returns
            )
            return (
                function,
                [(name, ptype) for name, ptype, _ in parameters],
                return_ptype,
            )

        self._define_local_function(statement.name, statement, compile_)

    def _register_lambda(
        self, statement: ast.stmt, name: str, node: ast.Lambda
    ) -> None:
        """Register a lambda assigned to a name as a local function. Lambda
        parameters cannot be annotated, so only lambdas without parameters
        are supported; use a local def for functions with parameters."""
        if (
            node.args.args
            or node.args.posonlyargs
            or node.args.kwonlyargs
            or node.args.vararg
            or node.args.kwarg
        ):
            raise self._compiler._error(
                statement,
                "lambdas with parameters cannot be compiled; use a local "
                "function definition with annotated parameters",
            )

        def compile_() -> Tuple[Any, List[Tuple[str, KRPC.Type]], Any]:
            body = compile_lambda_body(self._compiler, node)
            return self._compiler._expr.lambda_([], body.expression), [], body.ptype

        self._define_local_function(name, node, compile_)

    def _define_local_function(
        self,
        name: str,
        node: ast.AST,
        compile_: Callable[[], Tuple[Any, List[Tuple[str, KRPC.Type]], Any]],
    ) -> None:
        """Register a local function, compiled now, or at its first call when
        it reads a variable assigned after its definition."""
        if node in self._deferred:
            function: Any = _DeferredFunction(self._compiler, compile_)
        else:
            function = compile_()
        self._compiler._local_functions[name] = function
        self._compiler._block_functions.discard(name)

    def assign_named(self, node: ast.AST, name: str, value: _Result) -> _Result:
        """Compile an assignment expression (the walrus operator): assigns to
        a variable and produces the assigned value."""
        expression = self._assign_variable(node, name, value)
        variable = self._scope[name]
        if expression is None:
            raise self._compiler._error(
                node, "cannot determine the type of variable '%s'" % name
            )
        return _Result(expression=expression, ptype=variable.ptype)

    def _assign_variable(
        self,
        node: ast.stmt,
        name: str,
        value: _Result,
        annotation: Optional[KRPC.Type] = None,
    ) -> Any:
        """Assign a value to a variable, declaring it on its first assignment.
        None when the variable has only been assigned None, and its type is not
        known yet."""
        compiler = self._compiler
        scope = self._scope
        is_none = compiler._is_none(value) or (
            value.is_value and value.value is UNTYPED
        )
        if name in scope:
            variable = scope[name]
            if variable.is_value and variable.value is UNTYPED:
                if is_none:
                    return None
                self._type_untyped(node, name, value)
            if annotation is not None and annotation != variable.ptype:
                raise compiler._error(
                    node,
                    "variable '%s' is already declared with a different type" % name,
                )
            if is_none and not variable.ptype.nullable:
                if name in self._state:
                    raise compiler._error(
                        node,
                        "state variable '%s' has a type that cannot be None" % name,
                    )
                if name in self._annotated:
                    raise compiler._error(
                        node,
                        "variable '%s' is annotated with a type that cannot be "
                        "None" % name,
                    )
                if name in self._parameters:
                    raise compiler._error(
                        node,
                        "parameter '%s' is annotated with a type that cannot be "
                        "None" % name,
                    )
                self._widen_to_nullable(name, variable.ptype)
            elif not value.is_value and name not in self._state:
                self._widen_variable(name, variable.ptype, value.ptype)
            return compiler._expr.assign(
                variable.expression,
                self._assigned_expression(node, name, variable.ptype, value),
            )
        if annotation is not None:
            if is_none and not annotation.nullable:
                raise compiler._error(
                    node,
                    "variable '%s' is annotated with a type that cannot be None" % name,
                )
            ptype = annotation
            self._annotated.add(name)
            expression = self._assigned_expression(node, name, annotation, value)
        elif is_none:
            stored = compiler._variable_types.get((self._body, name))
            if stored is None:
                scope[name] = _Result(value=UNTYPED, is_value=True)
                self._untyped[name] = node
                return None
            ptype = stored
            expression = self._assigned_expression(node, name, ptype, value)
        else:
            converted = (
                _Result()
                if value.is_value and self._is_empty_collection(value.value)
                else self._compiler._to_expression(value)
            )
            if converted.ptype is None:
                raise self._compiler._error(
                    node,
                    "cannot determine the type of variable '%s'; annotate it, "
                    "for example %s: list[int] = []" % (name, name),
                )
            ptype = self._declared_ptype(name, converted.ptype)
            expression = self._assigned_expression(node, name, ptype, converted)
        variable_expression = self._compiler._expr.variable(
            name, self._compiler._remote_type(node, ptype)
        )
        self._declared.append(variable_expression)
        scope[name] = _Result(expression=variable_expression, ptype=ptype)
        return self._compiler._expr.assign(variable_expression, expression)

    def _type_untyped(self, node: ast.stmt, name: str, value: _Result) -> None:
        """Note the type of a variable assigned None before, from a value
        assigned to it later, and compile the function again."""
        converted = self._compiler._to_expression(value)
        if converted.ptype is None:
            raise self._compiler._error(
                node, "cannot determine the type of variable '%s'" % name
            )
        self._note_type(name, converted.ptype)

    def _note_type(self, name: str, ptype: KRPC.Type) -> NoReturn:
        """Note the type of a variable assigned None before, and compile the
        function again."""
        self._compiler._variable_types[(self._body, name)] = with_nullable(ptype, True)
        raise Recompile()

    def _assigned_expression(
        self, node: ast.stmt, name: str, target: Optional[KRPC.Type], value: _Result
    ) -> Any:
        """The expression assigned to a variable, converted to the variable's
        type. A variable takes its type from its annotation or its first value,
        so every value assigned to it has to widen to that type."""
        if name in self._state:
            hint = "; give it a type with a cast, for example %s = cast(float, %s)" % (
                name,
                name,
            )
        elif name in self._annotated or name in self._parameters:
            hint = ""
        else:
            hint = "; annotate it, for example %s: float = 0" % name
        return self._compiler._widened_expression(
            node, target, value, "when assigning to variable '%s'" % name, hint
        )

    def _declared_ptype(self, name: str, ptype: KRPC.Type) -> KRPC.Type:
        """The type a variable is declared with, from the first value assigned
        to it. A later value can be None where the first is not, which an
        earlier compilation of the function found, and the type is widened to
        allow it. A single precision value declares a double, as Python holds
        it as one, so arithmetic on the variable assigns back."""
        if ptype.code == KRPC.Type.FLOAT:
            ptype = with_nullable(build_ptype(KRPC.Type.DOUBLE), ptype.nullable)
        widened = self._compiler._variable_types.get((self._body, name))
        if widened is None or not same_shape(widened, ptype):
            return ptype
        return join_ptype(widened, ptype) or ptype

    def _widen_variable(
        self, name: str, declared: KRPC.Type, value: Optional[KRPC.Type]
    ) -> None:
        """Note the type a variable must be declared with to hold a value that
        can be None where the variable cannot. The function is compiled again,
        declaring the variable with that type."""
        if (
            value is None
            or name in self._annotated
            or name in self._parameters
            or plain_ptype(value) != plain_ptype(declared)
            or is_no_more_nullable(value, declared)
        ):
            return
        self._note_widened(name, declared, value)

    def _widen_to_nullable(self, name: str, declared: KRPC.Type) -> None:
        """Note that a variable assigned None must be declared with the
        nullable form of its type, a number or a bool included, and compile
        the function again."""
        self._note_widened(name, declared, with_nullable(declared, True))

    def _note_widened(self, name: str, declared: KRPC.Type, value: KRPC.Type) -> None:
        key = (self._body, name)
        types = self._compiler._variable_types
        widened = join_ptype(types.get(key, declared), value)
        if widened is not None and widened != types.get(key):
            types[key] = widened
            self._compiler._variables_widened = True

    def _assign_attribute(
        self, node: ast.stmt, target: ast.Attribute, value: _Result
    ) -> Any:
        base = self._compiler._compile(target.value)
        if base.is_value and isinstance(base.value, ClassBase):
            protobuf_type = self._compiler._class_ptype(type(base.value))
            service, procedure = self._compiler._member(
                node,
                protobuf_type.service,
                protobuf_type.name,
                target.attr,
                "setter",
            )
            args: List[Optional[_Result]] = [base, value]
        elif base.is_value and self._compiler._is_service_object(base.value):
            service, procedure = self._compiler._member(
                node,
                self._compiler._service_name(base.value),
                None,
                target.attr,
                "setter",
            )
            args = [value]
        elif not base.is_value:
            base_service, class_name = self._compiler._class_of(node, base)
            service, procedure = self._compiler._member(
                node, base_service, class_name, target.attr, "setter"
            )
            args = [base, value]
        else:
            raise self._compiler._error(
                node, "cannot assign to '%s' of a client side value" % target.attr
            )
        return self._compiler._call_node(node, service, procedure, args).expression

    def _compile_subscript_augassign(
        self, statement: ast.AugAssign, target: ast.Subscript
    ) -> Optional[Any]:
        """Compile an augmented assignment to an element, such as d[k()] += 1.
        The collection and the key are assigned to temporaries, so each is
        evaluated once, as Python does. None when the type of either is
        unknown."""
        compiler = self._compiler
        expr = compiler._expr
        variables = []
        assignments = []
        scope = {}
        key = compiler._compile(target.slice)
        if key.is_value:
            # A constant key, such as a negative index, is kept as it is
            scope[_hidden("key")] = key
        for name in ("collection", "key"):
            if _hidden(name) in scope:
                continue
            result = (
                compiler._to_expression(key)
                if name == "key"
                else self._server_collection(statement, target, "assign to")
            )
            if result.ptype is None:
                return None
            variable = expr.variable(
                name, compiler._remote_type(statement, result.ptype)
            )
            variables.append(variable)
            assignments.append(expr.assign(variable, result.expression))
            scope[_hidden(name)] = _Result(expression=variable, ptype=result.ptype)
        held = ast.copy_location(
            ast.Subscript(
                value=ast.Name(id=_hidden("collection"), ctx=ast.Load()),
                slice=ast.Name(id=_hidden("key"), ctx=ast.Load()),
                ctx=ast.Load(),
            ),
            target,
        )
        operator_node = ast.copy_location(
            ast.BinOp(left=held, op=statement.op, right=statement.value), statement
        )
        ast.fix_missing_locations(operator_node)
        compiler._scopes.append(scope)
        try:
            value = compiler._compile(operator_node)
            store = self._assign_subscript(statement, held, value)
        finally:
            compiler._scopes.pop()
        return expr.block_with_variables(variables, assignments + [store])

    def _compile_attribute_augassign(
        self, statement: ast.AugAssign, target: ast.Attribute
    ) -> Optional[Any]:
        """Compile an augmented assignment to a property of a computed object,
        such as vessel.control.throttle += 0.1. The object is assigned to a
        temporary, so it is evaluated once, as Python does. None when the object
        is a client side value or its type is unknown."""
        compiler = self._compiler
        expr = compiler._expr
        base = compiler._compile(target.value)
        if base.is_value:
            return None
        base = compiler._to_expression(base)
        if base.ptype is None:
            return None
        variable = expr.variable("object", compiler._remote_type(statement, base.ptype))
        scope = {_hidden("object"): _Result(expression=variable, ptype=base.ptype)}
        held = ast.copy_location(
            ast.Attribute(
                value=ast.Name(id=_hidden("object"), ctx=ast.Load()),
                attr=target.attr,
                ctx=ast.Load(),
            ),
            target,
        )
        operator_node = ast.copy_location(
            ast.BinOp(left=held, op=statement.op, right=statement.value), statement
        )
        ast.fix_missing_locations(operator_node)
        compiler._scopes.append(scope)
        try:
            value = compiler._compile(operator_node)
            store = self._assign_attribute(statement, held, value)
        finally:
            compiler._scopes.pop()
        return expr.block_with_variables(
            [variable], [expr.assign(variable, base.expression), store]
        )

    def _server_collection(
        self, node: ast.AST, target: ast.Subscript, action: str
    ) -> _Result:
        """The collection an element is written to or removed from. A client side
        collection is copied to the server, so a change to it is an error."""
        collection = self._compiler._compile(target.value)
        if collection.is_value:
            raise self._compiler._error(
                node, "cannot %s an element of a client side value" % action
            )
        return self._compiler._to_expression(collection)

    def _assign_subscript(
        self, node: ast.stmt, target: ast.Subscript, value: _Result
    ) -> Any:
        collection = self._server_collection(node, target, "assign to")
        ptype = collection.ptype
        index = self._compiler._compile(target.slice)
        if ptype is not None and ptype.code == KRPC.Type.DICTIONARY:
            return self._compiler._expr.set(
                collection.expression,
                self._compiler._converted_expression(
                    index, ptype.types[0], node, "for a key of the dictionary"
                ),
                self._compiler._converted_expression(
                    value, ptype.types[1], node, "for a value of the dictionary"
                ),
            )
        if ptype is None or ptype.code == KRPC.Type.LIST:
            element = None if ptype is None else ptype.types[0]
            converted = self._compiler._converted_expression(
                value, element, node, "for an element of the list"
            )
            return self._at_list_index(
                node,
                collection,
                index,
                lambda base, position: self._compiler._expr.set(
                    base, position, converted
                ),
            )
        raise self._compiler._error(node, "unsupported assignment target")

    def _at_list_index(
        self,
        node: ast.AST,
        collection: _Result,
        index: _Result,
        operation: Callable[[Any, Any], Any],
    ) -> Any:
        """Apply an operation to a list and a position in it. A negative
        constant index counts from the end, as it does when reading, so the
        list is assigned to a temporary to be both measured and indexed."""
        if (
            collection.ptype is not None
            and index.is_value
            and isinstance(index.value, int)
            and not isinstance(index.value, bool)
            and index.value < 0
        ):
            expr = self._compiler._expr
            offset = expr.constant_int(index.value)
            return self._compiler._with_temporaries(
                node,
                collection.ptype,
                [("collection", collection)],
                lambda base: operation(base, expr.add(expr.count(base), offset)),
            )
        return operation(
            collection.expression, self._compiler._index_expression(node, index)
        )

    def _compile_if(self, statement: ast.If) -> Optional[Any]:
        condition = self._compiler._compile_condition(statement.test)
        if condition.is_value:
            branch = statement.body if condition.value else statement.orelse
            if not branch:
                return None
            return self._statement_block(branch, scoped=False)
        body = self._statement_block(statement.body)
        if not statement.orelse:
            return self._compiler._expr.if_then(condition.expression, body)
        return self._compiler._expr.if_then_else(
            condition.expression,
            body,
            self._statement_block(statement.orelse),
        )

    def _compile_while(self, statement: ast.While) -> Any:
        if statement.orelse:
            raise self._compiler._error(
                statement, "loop else clauses are not supported"
            )
        condition = self._compiler._to_expression(
            self._compiler._compile_condition(statement.test)
        )
        return self._compiler._expr.while_(
            condition.expression, self._statement_block(statement.body)
        )

    def _compile_for(self, statement: ast.For) -> Any:
        if statement.orelse:
            raise self._compiler._error(
                statement, "loop else clauses are not supported"
            )
        collection = self._compiler._iterable(self._compiler._compile(statement.iter))
        element = self._compiler._element_ptype(statement, collection)
        if not isinstance(statement.target, ast.Name):
            return self._compile_unpacking_for(statement, collection, element)
        name = statement.target.id
        scope = self._scope
        if name in self._state or element.code == KRPC.Type.FLOAT:
            # A state variable, or a double holding single precision values, is
            # not a loop variable, so it is assigned from one
            loop_variable = self._hidden_variable(statement, "element", element)
            assignment = self._assign_variable(
                statement, name, _Result(expression=loop_variable, ptype=element)
            )
            return self._compiler._expr.for_each(
                loop_variable,
                collection.expression,
                self._compiler._expr.block(
                    [assignment, self._statement_block(statement.body)]
                ),
            )
        if name in scope:
            variable = scope[name]
            if variable.is_value and variable.value is UNTYPED:
                self._note_type(name, element)
            if with_nullable(plain_ptype(variable.ptype), False) != with_nullable(
                plain_ptype(element), False
            ):
                raise self._compiler._error(
                    statement,
                    "the loop variable '%s' is already used with a "
                    "different type" % name,
                )
            self._widen_variable(name, variable.ptype, element)
        else:
            ptype = self._declared_ptype(name, element)
            variable = _Result(
                expression=self._compiler._expr.variable(
                    name, self._compiler._remote_type(statement, ptype)
                ),
                ptype=ptype,
            )
            self._declared.append(variable.expression)
            scope[name] = variable
        return self._compiler._expr.for_each(
            variable.expression,
            collection.expression,
            self._statement_block(statement.body),
        )

    def _compile_unpacking_for(
        self, statement: ast.For, collection: _Result, element: KRPC.Type
    ) -> Any:
        """Compile a for loop whose target unpacks each value, such as
        for key, value in d.items(). Each value is held in a hidden loop
        variable, and the names are assigned from it at the top of the body."""
        names = self._compiler._unpack(statement, statement.target, element)
        variable = self._hidden_variable(statement, "element", element)
        assignments = self._assign_parts(statement, variable, names)
        body = self._statement_block(statement.body)
        return self._compiler._expr.for_each(
            variable,
            collection.expression,
            self._compiler._expr.block(assignments + [body]),
        )

    def _compile_delete(self, statement: ast.Delete) -> List[Any]:
        """Compile a del statement, which removes an element of a list by position
        or a key of a dictionary."""
        compiled = []
        for target in statement.targets:
            if not isinstance(target, ast.Subscript) or isinstance(
                target.slice, ast.Slice
            ):
                raise self._compiler._error(
                    statement,
                    "del is only supported for an element of a list or a dictionary",
                )
            base = self._server_collection(statement, target, "delete")
            index = self._compiler._compile(target.slice)
            ptype = base.ptype
            if ptype is not None and ptype.code == KRPC.Type.DICTIONARY:
                key = self._compiler._converted_expression(
                    index, ptype.types[0], statement, "for a key of the dictionary"
                )
                compiled.append(
                    self._compiler._removed(
                        self._compiler._expr.remove(base.expression, key),
                        "KeyNotFoundException",
                        "key not in dictionary",
                    )
                )
            elif ptype is not None and ptype.code == KRPC.Type.LIST:
                compiled.append(
                    self._at_list_index(
                        statement, base, index, self._compiler._expr.remove_at
                    )
                )
            else:
                raise self._compiler._error(
                    statement,
                    "del is only supported for an element of a list or a dictionary",
                )
        return compiled

    def _exception_names(
        self, node: ast.AST, target: _Result, raising: bool = False
    ) -> List[tuple]:
        """The service and name of each kRPC exception a Python exception type
        stands for, or a tuple of them. A raised builtin stands for one exception,
        and a caught one for every exception its subclasses stand for."""
        value = target.value if target.is_value else None
        if isinstance(value, tuple):
            return [
                name
                for element in value
                for name in self._exception_names(
                    node, _Result(value=element, is_value=True)
                )
            ]
        if isinstance(value, type):
            service = getattr(value, "_service_name", None)
            name = getattr(value, "_class_name", None)
            if service is not None and name is not None:
                return [(service, name)]
            if raising:
                if value is Exception:
                    return [("KRPC", "Error")]
                if value in _BUILTIN_EXCEPTIONS:
                    return _BUILTIN_EXCEPTIONS[value]
            elif issubclass(value, BaseException):
                names = self._caught_names(value)
                if names:
                    return names
        raise self._compiler._error(node, "not an exception type the server defines")

    def _caught_names(self, value: type) -> List[tuple]:
        """The kRPC exceptions an except clause naming a builtin catches. The Python
        client raises a service's own exception as a subclass of RuntimeError, so
        catching RuntimeError catches every exception the services declare, apart
        from those the client represents as other builtins and KRPC.Error."""
        names = [
            name
            for builtin, builtin_names in _BUILTIN_EXCEPTIONS.items()
            if issubclass(builtin, value)
            for name in builtin_names
        ]
        if issubclass(RuntimeError, value):
            for service, name in self._compiler._metadata.exceptions:
                # KRPC.Error is the exception raise Exception(...) raises
                if service == "KRPC" and (name in EXCEPTION_TYPES or name == "Error"):
                    continue
                if (service, name) not in names:
                    names.append((service, name))
        return names

    def _compile_raise(self, statement: ast.Raise) -> Any:
        if statement.cause is not None:
            raise self._compiler._error(
                statement, "raise ... from ... is not supported"
            )
        exception = statement.exc
        if exception is None:
            raise self._compiler._error(statement, "a bare raise is not supported")
        if not isinstance(exception, ast.Call):
            raise self._compiler._error(
                statement,
                "an exception must be raised by calling it, "
                "for example raise RuntimeError('...')",
            )
        target = self._compiler._compile(exception.func)
        if target.is_value and isinstance(target.value, tuple):
            raise self._compiler._error(statement, "not an exception type")
        service, name = self._exception_names(statement, target, raising=True)[0]
        if len(exception.args) > 1 or exception.keywords:
            raise self._compiler._error(
                statement, "an exception is raised with a single message"
            )
        if exception.args:
            # The message is the argument as str() gives it, as Python's is
            argument = self._compiler._compile(exception.args[0])
            if argument.is_value:
                message = self._compiler._expr.constant_string(str(argument.value))
            else:
                message = self._compiler._string_of(statement, argument).expression
        else:
            message = self._compiler._expr.constant_string("")
        return self._compiler._expr.throw(service, name, message)

    def _compile_try(self, statement: ast.Try) -> Any:
        if statement.orelse:
            raise self._compiler._error(statement, "try else clauses are not supported")
        if not statement.handlers and not statement.finalbody:
            raise self._compiler._error(
                statement, "a try must have an except or a finally clause"
            )
        jump = _jump_out_of(statement.finalbody)
        if jump is not None:
            raise self._compiler._error(
                jump, "%s cannot leave a finally clause" % _JUMP_NAMES[type(jump)]
            )
        body = self._statement_block(statement.body)
        names = [self._handler_names(handler) for handler in statement.handlers]
        if len(names) == 1 and len(names[0]) <= 1:
            handler = statement.handlers[0]
            message, shadowed = self._bind_message(handler)
            compiled = self._handler_body(handler, shadowed)
            body = self._compile_handler(names[0], body, message, compiled)
        elif statement.handlers:
            body = self._compile_handlers(statement, names, body)
        if statement.finalbody:
            body = self._compiler._expr.try_finally(
                body, self._statement_block(statement.finalbody)
            )
        return body

    def _compile_handlers(
        self, statement: ast.Try, names: List[List[tuple]], body: Any
    ) -> Any:
        # Catches nest innermost first, so they are tried in the order written.
        # A catch only records which handler matched. The handlers run after the
        # nested catches, so the handlers after one do not catch its exception.
        # A handler naming several exceptions has a catch for each
        expr = self._compiler._expr
        ptype = build_ptype(KRPC.Type.SINT32)
        matched = expr.variable(
            "handler", self._compiler._remote_type(statement, ptype)
        )
        self._declared.append(matched)
        handlers = []
        for index, (handler, handler_names) in enumerate(
            zip(statement.handlers, names), 1
        ):
            record = expr.assign(matched, expr.constant_int(index))
            message, shadowed = self._bind_message(handler)
            handlers.append(self._handler_body(handler, shadowed))
            body = self._compile_handler(handler_names, body, message, record)
        dispatch = None
        for index in reversed(range(len(handlers))):
            condition = expr.equal(matched, expr.constant_int(index + 1))
            if dispatch is None:
                dispatch = expr.if_then(condition, handlers[index])
            else:
                dispatch = expr.if_then_else(condition, handlers[index], dispatch)
        return expr.block([expr.assign(matched, expr.constant_int(0)), body, dispatch])

    def _handler_body(
        self, handler: ast.ExceptHandler, shadowed: Optional[_Result]
    ) -> Any:
        """Compile a handler's statements. Its as name is bound only within
        them, so the binding of the name it shadows is restored after."""
        try:
            return self._statement_block(handler.body)
        finally:
            if handler.name:
                if shadowed is None:
                    self._scope.pop(handler.name, None)
                else:
                    self._scope[handler.name] = shadowed

    def _handler_names(self, handler: ast.ExceptHandler) -> List[tuple]:
        """The kRPC exceptions a handler catches, or none for every exception."""
        if handler.type is None:
            return []
        target = self._compiler._compile(handler.type)
        caught = target.value if target.is_value else None
        if not isinstance(caught, tuple):
            caught = (caught,)
        if any(value in (Exception, BaseException) for value in caught):
            return []
        return self._exception_names(handler, target)

    def _bind_message(
        self, handler: ast.ExceptHandler
    ) -> Tuple[Any, Optional[_Result]]:
        """Declare the variable a handler's as name binds, and return it with
        the binding of the name it shadows. Only the message is bound, never
        the exception itself, so the name holds a string."""
        if not handler.name:
            return None, None
        ptype = build_ptype(KRPC.Type.STRING)
        message = self._compiler._expr.variable(
            handler.name, self._compiler._remote_type(handler, ptype)
        )
        self._declared.append(message)
        shadowed = self._scope.get(handler.name)
        self._scope[handler.name] = _Result(expression=message, ptype=ptype)
        return message, shadowed

    def _compile_handler(
        self, names: List[tuple], body: Any, message: Any, compiled: Any
    ) -> Any:
        """Wrap the body in a catch for each of the handler's exceptions, or one
        for every exception when it names none. The catch binds the message
        variable, if any, and runs the compiled statement."""
        if not names:
            return self._compiler._expr.try_catch_all(body, message, compiled)
        for service, name in names:
            body = self._compiler._expr.try_catch(
                body, service, name, message, compiled
            )
        return body

    def _statement_block(self, statements: List[ast.stmt], scoped: bool = True) -> Any:
        """Compile a nested block of statements. Which of a branch's local
        function definitions runs is only known at run time, so a function a
        scoped block defines or redefines is not callable after it."""
        functions = self._compiler._local_functions
        local_functions = dict(functions)
        try:
            compiled = self._compile_statements(statements)
        finally:
            if scoped:
                self._compiler._block_functions.update(
                    name
                    for name, function in functions.items()
                    if local_functions.get(name) is not function
                )
                functions.clear()
                functions.update(local_functions)
                for name in self._compiler._block_functions:
                    functions.pop(name, None)
        if not compiled:
            # A block of pass statements and definitions still needs a statement,
            # and a constant has no effect
            return self._compiler._expr.constant_bool(False)
        if len(compiled) == 1:
            return compiled[0]
        return self._compiler._expr.block(compiled)

    @staticmethod
    def _is_empty_collection(value: object) -> bool:
        return isinstance(value, (list, tuple, set, dict)) and len(value) == 0

    def _empty_collection(self, node: ast.stmt, ptype: KRPC.Type, value: object) -> Any:
        """An empty collection of the type an annotation or a cast names. The
        empty value must be of the same kind, as in [] for a list."""
        collection = _EMPTY_COLLECTIONS.get(ptype.code)
        if collection is not None and not isinstance(value, collection[0]):
            raise self._compiler._error(
                node,
                "an empty %s cannot be given a %s type; use %s"
                % (type(value).__name__, collection[0].__name__, collection[1]),
            )
        if ptype.code == KRPC.Type.LIST:
            return self._compiler._expr.create_empty_list(
                self._compiler._remote_type(node, ptype.types[0])
            )
        if ptype.code == KRPC.Type.SET:
            return self._compiler._expr.create_empty_set(
                self._compiler._remote_type(node, ptype.types[0])
            )
        if ptype.code == KRPC.Type.DICTIONARY:
            return self._compiler._expr.create_empty_dictionary(
                self._compiler._remote_type(node, ptype.types[0]),
                self._compiler._remote_type(node, ptype.types[1]),
            )
        raise self._compiler._error(node, "the annotation must be a collection type")


class _DeferredFunction:  # pylint: disable=too-few-public-methods
    """A local function compiled at its first call, with the scopes of its
    definition. It unpacks to the triple a compiled local function is: the
    function, its parameters' names and types, and its return type."""

    def __init__(
        self,
        compiler: _Compiler,
        compile_: Callable[[], Tuple[Any, List[Tuple[str, KRPC.Type]], Any]],
    ):
        self._compiler = compiler
        self._compile = compile_
        # The scopes are the function's own, which gain the variables declared
        # after the definition
        self._scopes = list(compiler._scopes)
        self._compiled: Optional[Tuple[Any, List[Tuple[str, KRPC.Type]], Any]] = None

    def __iter__(self) -> Any:
        if self._compiled is None:
            compiler = self._compiler
            scopes, node = compiler._scopes, compiler._node
            compiler._scopes = list(self._scopes)
            try:
                self._compiled = self._compile()
            finally:
                compiler._scopes, compiler._node = scopes, node
        return iter(self._compiled)


def compile_lambda_body(compiler: _Compiler, node: ast.Lambda) -> _Result:
    """Compile the body of a lambda within the function. An assignment
    expression in it assigns a variable of the lambda's own, as in Python."""
    statements = _StatementCompiler(compiler)
    statements._body = (node.lineno, node.col_offset)
    compiler._scopes.append(statements._scope)
    previous = compiler._active_statements
    compiler._active_statements = statements
    try:
        body = compiler._to_expression(compiler._compile(node.body))
    finally:
        compiler._active_statements = previous
        compiler._scopes.pop()
    if not statements._declared:
        return body
    return _Result(
        expression=compiler._expr.block_with_variables(
            statements._declared, [body.expression]
        ),
        ptype=body.ptype,
    )


_ANNOTATION_VALUE_CODES = {
    "float": KRPC.Type.DOUBLE,
    "int": KRPC.Type.SINT32,
    "bool": KRPC.Type.BOOL,
    "str": KRPC.Type.STRING,
}

# The collection types an annotation can name, by their builtin and their
# typing module names
_ANNOTATION_COLLECTION_CODES = {
    "list": KRPC.Type.LIST,
    "List": KRPC.Type.LIST,
    "set": KRPC.Type.SET,
    "Set": KRPC.Type.SET,
    "dict": KRPC.Type.DICTIONARY,
    "Dict": KRPC.Type.DICTIONARY,
    "tuple": KRPC.Type.TUPLE,
    "Tuple": KRPC.Type.TUPLE,
}


def annotation_ptype(compiler: _Compiler, node: ast.expr) -> KRPC.Type:
    """The protocol buffer type described by a variable annotation, such
    as int, list[float], dict[str, int], tuple[str, int] or Optional[str]."""
    optional = _optional_annotation(node)
    if optional is not None:
        return with_nullable(annotation_ptype(compiler, optional), True)
    if isinstance(node, ast.Name) and node.id in _ANNOTATION_VALUE_CODES:
        return build_ptype(_ANNOTATION_VALUE_CODES[node.id])
    if isinstance(node, ast.Subscript):
        code = _ANNOTATION_COLLECTION_CODES.get(_type_name(node.value))
        if code is not None:
            return _collection_annotation_ptype(compiler, node, code)
    remote = _remote_annotation_ptype(compiler, node, _annotation_value(compiler, node))
    if remote is not None:
        return remote
    raise compiler._error(node, "unsupported type annotation")


def _collection_annotation_ptype(
    compiler: _Compiler, node: ast.Subscript, code: int
) -> KRPC.Type:
    elements = node.slice.elts if isinstance(node.slice, ast.Tuple) else [node.slice]
    types = [annotation_ptype(compiler, element) for element in elements]
    if code in (KRPC.Type.LIST, KRPC.Type.SET) and len(types) != 1:
        raise compiler._error(node, "unsupported type annotation")
    if code == KRPC.Type.DICTIONARY and len(types) != 2:
        raise compiler._error(node, "unsupported type annotation")
    if code == KRPC.Type.SET and types[0].nullable:
        raise compiler._error(node, "the values of a set cannot be None")
    if code == KRPC.Type.DICTIONARY and types[0].nullable:
        raise compiler._error(node, "the keys of a dictionary cannot be None")
    return build_ptype(code, types)


def _remote_annotation_ptype(
    compiler: _Compiler, node: ast.expr, value: object
) -> Optional[KRPC.Type]:
    """The type of a class, enumeration or structure a service defines,
    named by an annotation."""
    value = unwrap_class(value)
    if not isinstance(value, type):
        return None
    if issubclass(value, ClassBase):
        return compiler._class_ptype(value)
    if issubclass(value, enum.Enum):
        return compiler._enum_ptype(value)
    if compiler._is_remote_struct(value):
        return cast(KRPC.Type, compiler._struct_type_of(node, value).protobuf_type)
    return None


def _annotation_value(compiler: _Compiler, node: ast.expr) -> object:
    """The value a name or attribute in an annotation refers to, or None."""
    if isinstance(node, ast.Name):
        if node.id in compiler._annotation_types:
            return compiler._annotation_types[node.id]
        resolved = compiler._lookup(node.id, node)
        return resolved.value if resolved.is_value else None
    if isinstance(node, ast.Attribute):
        base = _annotation_value(compiler, node.value)
        return None if base is None else getattr(base, node.attr, None)
    return None


def _optional_annotation(node: ast.expr) -> Optional[ast.expr]:
    """The annotation an annotation of a value that can be None is written
    around, as Optional[T] or T | None. None for any other annotation."""
    if isinstance(node, ast.Subscript) and _type_name(node.value) == "Optional":
        return node.slice
    if isinstance(node, ast.BinOp) and isinstance(node.op, ast.BitOr):
        for value, other in ((node.left, node.right), (node.right, node.left)):
            if isinstance(other, ast.Constant) and other.value is None:
                return value
    return None
