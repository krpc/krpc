"""Compilation of python statements (assignments, control flow and side
effects) into server side expression nodes. Used by the function compiler
for function bodies; expression compilation itself lives in
krpc.functioncompiler."""

from __future__ import annotations
import ast
from typing import Any, Callable, Dict, List, Optional, TYPE_CHECKING

from krpc.error import FunctionCompilationError
from krpc.expressionutils import (
    Result as _Result,
    build_ptype,
)
from krpc.types import ClassBase
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.functioncompiler import _Compiler


# The kRPC exceptions each builtin python exception stands for. The python client
# represents the KRPC service's general exceptions as builtins, so catching a
# builtin catches every exception it stands for, and raising one raises the
# first. A service's own exceptions are generated classes and carry their own name
_BUILTIN_EXCEPTIONS = {
    RuntimeError: [("KRPC", "InvalidOperationException")],
    ValueError: [
        ("KRPC", "ArgumentException"),
        ("KRPC", "ArgumentNullException"),
        ("KRPC", "ArgumentOutOfRangeException"),
    ],
}


class _StatementCompiler:
    """Compiles the statements of a function body, using the expression
    compiler for the expressions within them. Holds the per-function state:
    declared variables and the function's return type."""

    def __init__(self, compiler: _Compiler):
        self._compiler = compiler
        self._scope: Dict[str, _Result] = {}
        self._declared: List[Any] = []
        self._return_ptype: Optional[KRPC.Type] = None
        self._saw_valued_return = False

    def compile_body(self, statements: List[ast.stmt]) -> _Result:
        """Compile a function body of statements into a value expression:
        a server side function with no parameters, invoked immediately."""
        function, return_ptype = self.compile_function(statements, [])
        invoked = self._compiler._expr.invoke(function, {})
        return _Result(expression=invoked, ptype=return_ptype)

    def compile_function(
        self,
        statements: List[ast.stmt],
        parameters: List[Any],
    ) -> Any:
        """Compile statements into a server side function with the given
        parameters, each a (name, ptype, parameter expression) triple.
        Returns the function expression and its return type."""
        scope: Dict[str, _Result] = {}
        for name, ptype, parameter in parameters:
            scope[name] = _Result(expression=parameter, ptype=ptype)
        self._scope = scope
        self._compiler._scopes.append(scope)
        previous = self._compiler._active_statements
        self._compiler._active_statements = self
        self._declared: List[Any] = []
        self._return_ptype: Optional[KRPC.Type] = None
        self._saw_valued_return = False
        try:
            compiled = self._compile_statements(statements, top_level=True)
            if not self._saw_valued_return:
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

    def _compile_statements(
        self,
        statements: List[ast.stmt],
        top_level: bool = False,
        as_value: bool = False,
    ) -> List[Any]:
        """Compile a list of statements. When the list is the value of the
        function, or of a branch of the function's final if, its final return
        becomes the value rather than a return statement."""
        as_value = as_value or top_level
        compiled: List[Any] = []
        for position, statement in enumerate(statements):
            last = position == len(statements) - 1
            if isinstance(statement, ast.Return):
                compiled.append(
                    self._compile_return(statement, as_value=as_value and last)
                )
            elif (
                isinstance(statement, ast.If)
                and as_value
                and last
                and self._returns_a_value([statement])
            ):
                compiled.append(self._compile_if_value(statement))
            elif isinstance(statement, ast.Expr):
                if isinstance(statement.value, ast.Constant):
                    continue  # docstring or a constant with no effect
                result = self._compiler._compile(statement.value)
                if not result.is_value:
                    compiled.append(result.expression)
            elif isinstance(statement, ast.Pass):
                continue
            elif isinstance(statement, (ast.Assign, ast.AnnAssign, ast.AugAssign)):
                compiled.extend(self._compile_assignment(statement))
            elif isinstance(statement, ast.FunctionDef):
                self._register_local_function(statement)
            elif isinstance(statement, ast.If):
                compiled_if = self._compile_if(statement)
                if compiled_if is not None:
                    compiled.append(compiled_if)
            elif isinstance(statement, ast.While):
                compiled.append(self._compile_while(statement))
            elif isinstance(statement, ast.For):
                compiled.append(self._compile_for(statement))
            elif isinstance(statement, ast.Delete):
                compiled.extend(self._compile_delete(statement))
            elif isinstance(statement, ast.Raise):
                compiled.append(self._compile_raise(statement))
            elif isinstance(statement, ast.Try):
                compiled.append(self._compile_try(statement))
            elif isinstance(statement, ast.Break):
                compiled.append(self._compiler._expr.break_())
            elif isinstance(statement, ast.Continue):
                compiled.append(self._compiler._expr.continue_())
            else:
                raise self._compiler._error(
                    statement,
                    "unsupported statement (%s)" % type(statement).__name__,
                )
        if (
            top_level
            and self._saw_valued_return
            and not self._returns_a_value(statements)
        ):
            message = (
                "a function that returns a value must end with a return "
                "statement, or an if and else that both do"
            )
            if statements:
                raise self._compiler._error(statements[-1], message)
            raise FunctionCompilationError("Cannot compile expression: " + message)
        return compiled

    @staticmethod
    def _return_value(statement: ast.Return) -> Optional[ast.expr]:
        """The value a return statement gives, or None for a bare return or
        return None."""
        value = statement.value
        if isinstance(value, ast.Constant) and value.value is None:
            return None
        return value

    def _returns_a_value(self, statements: List[ast.stmt]) -> bool:
        """Whether the statements end by returning a value on every path, with a
        return statement or an if and else whose branches both do."""
        if not statements:
            return False
        final = statements[-1]
        if isinstance(final, ast.Return):
            return self._return_value(final) is not None
        if isinstance(final, ast.If):
            return self._returns_a_value(final.body) and self._returns_a_value(
                final.orelse
            )
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
        if self._return_value(statement) is None:
            if self._saw_valued_return:
                raise self._compiler._error(
                    statement,
                    "return must have a value in a function that returns a value",
                )
            return self._compiler._expr.return_nothing()
        result = self._compiler._to_expression(self._compiler._compile(statement.value))
        if result.ptype is None:
            raise self._compiler._error(
                statement, "cannot determine the type of the return value"
            )
        if self._return_ptype is None:
            self._return_ptype = result.ptype
        elif self._return_ptype != result.ptype:
            raise self._compiler._error(
                statement,
                "the function returns values of differing types; convert "
                "them to a common type",
            )
        self._saw_valued_return = True
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
            annotation = self._annotation_ptype(statement.annotation)
            if value.is_value and self._is_empty_collection(value.value):
                value = _Result(
                    expression=self._empty_collection(statement, annotation),
                    ptype=annotation,
                )
        else:  # ast.AugAssign
            target = statement.target
            if isinstance(target, ast.Subscript) and not isinstance(
                target.slice, (ast.Constant, ast.Name)
            ):
                compiled = self._compile_subscript_augassign(statement, target)
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
        if isinstance(target, ast.Attribute):
            return [self._assign_attribute(statement, target, value)]
        if isinstance(target, ast.Subscript):
            return [self._assign_subscript(statement, target, value)]
        raise self._compiler._error(statement, "unsupported assignment target")

    def _compile_multiple_assignment(self, statement: ast.Assign) -> List[Any]:
        """Compile an assignment with several targets, such as a = b = 0.
        The value is evaluated once, into the first target, and the remaining
        targets read it back."""
        for target in statement.targets:
            if not isinstance(target, ast.Name):
                raise self._compiler._error(
                    statement,
                    "assignments with several targets must assign to names",
                )
        first = statement.targets[0]
        assert isinstance(first, ast.Name)
        statements = [
            self._assign_variable(
                statement, first.id, self._compiler._compile(statement.value)
            )
        ]
        for target in statement.targets[1:]:
            assert isinstance(target, ast.Name)
            statements.append(
                self._assign_variable(statement, target.id, self._scope[first.id])
            )
        return statements

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
        parameters = []
        for argument in arguments.args:
            if argument.annotation is None:
                raise self._compiler._error(
                    statement,
                    "annotate the parameter types of local function '%s'"
                    % statement.name,
                )
            ptype = self._annotation_ptype(argument.annotation)
            parameter = self._compiler._expr.parameter(
                argument.arg, self._compiler._remote_type(statement, ptype)
            )
            parameters.append((argument.arg, ptype, parameter))
        nested = _StatementCompiler(self._compiler)
        function, return_ptype = nested.compile_function(statement.body, parameters)
        self._compiler._local_functions[statement.name] = (
            function,
            [(name, ptype) for name, ptype, _ in parameters],
            return_ptype,
        )

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
        body = self._compiler._to_expression(self._compiler._compile(node.body))
        function = self._compiler._expr.lambda_([], body.expression)
        self._compiler._local_functions[name] = (function, [], body.ptype)

    def assign_named(self, node: ast.AST, name: str, value: _Result) -> _Result:
        """Compile an assignment expression (the walrus operator): assigns to
        a variable and produces the assigned value."""
        expression = self._assign_variable(node, name, value)
        variable = self._scope[name]
        return _Result(expression=expression, ptype=variable.ptype)

    def _assign_variable(
        self,
        node: ast.stmt,
        name: str,
        value: _Result,
        annotation: Optional[KRPC.Type] = None,
    ) -> Any:
        scope = self._scope
        if name in scope:
            variable = scope[name]
            if annotation is not None and annotation != variable.ptype:
                raise self._compiler._error(
                    node,
                    "variable '%s' is already declared with a different type" % name,
                )
            return self._compiler._expr.assign(
                variable.expression,
                self._assigned_expression(node, name, variable.ptype, value),
            )
        if annotation is not None:
            ptype = annotation
            expression = self._assigned_expression(node, name, annotation, value)
        else:
            converted = self._compiler._to_expression(value)
            if converted.ptype is None:
                raise self._compiler._error(
                    node,
                    "cannot determine the type of variable '%s'; annotate it, "
                    "for example x: list[int] = []" % name,
                )
            ptype = converted.ptype
            expression = converted.expression
        variable_expression = self._compiler._expr.variable(
            name, self._compiler._remote_type(node, ptype)
        )
        self._declared.append(variable_expression)
        scope[name] = _Result(expression=variable_expression, ptype=ptype)
        return self._compiler._expr.assign(variable_expression, expression)

    def _assigned_expression(
        self, node: ast.stmt, name: str, target: Optional[KRPC.Type], value: _Result
    ) -> Any:
        """The expression assigned to a variable, converted to the variable's
        type. A variable takes its type from its annotation or its first value,
        so every value assigned to it has to widen to that type."""
        return self._compiler._widened_expression(
            node,
            target,
            value,
            "when assigning to variable '%s'" % name,
            "; annotate it, for example %s: float = 0" % name,
        )

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
        evaluated once, as python does. None when the type of either is
        unknown."""
        compiler = self._compiler
        expr = compiler._expr
        variables = []
        assignments = []
        scope = {}
        key = compiler._compile(target.slice)
        if key.is_value:
            # A constant key, such as a negative index, is kept as it is
            scope[" key"] = key
        for name, part in (("collection", target.value), ("key", target.slice)):
            if " " + name in scope:
                continue
            result = compiler._to_expression(
                key if name == "key" else compiler._compile(part)
            )
            if result.ptype is None:
                return None
            variable = expr.variable(
                name, compiler._remote_type(statement, result.ptype)
            )
            variables.append(variable)
            assignments.append(expr.assign(variable, result.expression))
            # A name no python source can spell refers to the temporary
            scope[" " + name] = _Result(expression=variable, ptype=result.ptype)
        held = ast.copy_location(
            ast.Subscript(
                value=ast.Name(id=" collection", ctx=ast.Load()),
                slice=ast.Name(id=" key", ctx=ast.Load()),
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

    def _assign_subscript(
        self, node: ast.stmt, target: ast.Subscript, value: _Result
    ) -> Any:
        collection = self._compiler._to_expression(
            self._compiler._compile(target.value)
        )
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
            collection.expression, self._compiler._to_expression(index).expression
        )

    def _compile_if(self, statement: ast.If) -> Optional[Any]:
        condition = self._compiler._compile_condition(statement.test)
        if condition.is_value:
            branch = statement.body if condition.value else statement.orelse
            if not branch:
                return None
            return self._statement_block(statement, branch)
        body = self._statement_block(statement, statement.body)
        if not statement.orelse:
            return self._compiler._expr.if_then(condition.expression, body)
        return self._compiler._expr.if_then_else(
            condition.expression,
            body,
            self._statement_block(statement, statement.orelse),
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
            condition.expression, self._statement_block(statement, statement.body)
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
        if name in scope:
            variable = scope[name]
            if variable.ptype != element:
                raise self._compiler._error(
                    statement,
                    "the loop variable '%s' is already used with a "
                    "different type" % name,
                )
        else:
            variable = _Result(
                expression=self._compiler._expr.variable(
                    name, self._compiler._remote_type(statement, element)
                ),
                ptype=element,
            )
            self._declared.append(variable.expression)
            scope[name] = variable
        return self._compiler._expr.for_each(
            variable.expression,
            collection.expression,
            self._statement_block(statement, statement.body),
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
        body = self._statement_block(statement, statement.body)
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
            base = self._compiler._to_expression(self._compiler._compile(target.value))
            index = self._compiler._compile(target.slice)
            ptype = base.ptype
            if ptype is not None and ptype.code == KRPC.Type.DICTIONARY:
                operation = self._compiler._expr.remove
                argument = self._compiler._converted_expression(
                    index, ptype.types[0], statement, "for a key of the dictionary"
                )
            elif ptype is not None and ptype.code == KRPC.Type.LIST:
                compiled.append(
                    self._at_list_index(
                        statement, base, index, self._compiler._expr.remove_at
                    )
                )
                continue
            else:
                raise self._compiler._error(
                    statement,
                    "del is only supported for an element of a list or a dictionary",
                )
            compiled.append(operation(base.expression, argument))
        return compiled

    def _exception_names(self, node: ast.AST, target: _Result) -> List[tuple]:
        """The service and name of each kRPC exception a python exception type
        stands for, or a tuple of them."""
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
            if value in _BUILTIN_EXCEPTIONS:
                return _BUILTIN_EXCEPTIONS[value]
        raise self._compiler._error(node, "not an exception type the server defines")

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
        service, name = self._exception_names(statement, target)[0]
        if len(exception.args) > 1 or exception.keywords:
            raise self._compiler._error(
                statement, "an exception is raised with a single message"
            )
        if exception.args:
            message = self._compiler._to_expression(
                self._compiler._compile(exception.args[0])
            ).expression
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
        body = self._statement_block(statement, statement.body)
        names = [self._handler_names(handler) for handler in statement.handlers]
        if len(names) == 1 and len(names[0]) <= 1:
            body = self._compile_handler(statement.handlers[0], names[0], body)
        elif statement.handlers:
            body = self._compile_handlers(statement, names, body)
        if statement.finalbody:
            body = self._compiler._expr.try_finally(
                body, self._statement_block(statement, statement.finalbody)
            )
        return body

    def _compile_handlers(
        self, statement: ast.Try, names: List[List[tuple]], body: Any
    ) -> Any:
        # Catches nest innermost first, so they are tried in the order written.
        # A catch only records which handler matched, and the handlers run after
        # the nested catches, so an exception raised by one handler is not
        # caught by the handlers after it. A handler naming several exceptions
        # has a catch for each
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
            body = self._compile_handler(handler, handler_names, body, record)
            handlers.append(self._handler_body(handler))
        dispatch = None
        for index in reversed(range(len(handlers))):
            condition = expr.equal(matched, expr.constant_int(index + 1))
            if dispatch is None:
                dispatch = expr.if_then(condition, handlers[index])
            else:
                dispatch = expr.if_then_else(condition, handlers[index], dispatch)
        return expr.block([expr.assign(matched, expr.constant_int(0)), body, dispatch])

    def _handler_body(self, handler: ast.ExceptHandler) -> Any:
        return self._statement_block(handler, handler.body, allow_empty=True)

    def _handler_names(self, handler: ast.ExceptHandler) -> List[tuple]:
        """The kRPC exceptions a handler catches, or none for every exception."""
        if handler.type is None:
            return []
        target = self._compiler._compile(handler.type)
        if target.is_value and target.value in (Exception, BaseException):
            return []
        return self._exception_names(handler, target)

    def _compile_handler(
        self,
        handler: ast.ExceptHandler,
        names: List[tuple],
        body: Any,
        compiled: Any = None,
    ) -> Any:
        """Wrap the body in a catch for each of the handler's exceptions, or one
        for every exception when it names none. The catch runs the handler's
        statements, or the given statement in their place."""
        # Only the message is bound, never the exception itself, so the name of
        # a caught exception is a string variable holding its message
        message = None
        if handler.name:
            ptype = build_ptype(KRPC.Type.STRING)
            message = self._compiler._expr.variable(
                handler.name, self._compiler._remote_type(handler, ptype)
            )
            self._declared.append(message)
            self._scope[handler.name] = _Result(expression=message, ptype=ptype)
        if compiled is None:
            compiled = self._handler_body(handler)
        if not names:
            return self._compiler._expr.try_catch_all(body, message, compiled)
        for service, name in names:
            body = self._compiler._expr.try_catch(
                body, service, name, message, compiled
            )
        return body

    def _statement_block(
        self, node: ast.stmt, statements: List[ast.stmt], allow_empty: bool = False
    ) -> Any:
        compiled = self._compile_statements(statements)
        if not compiled and allow_empty:
            # An except clause that does nothing still needs a statement to be
            # the handler, and a constant has no effect
            return self._compiler._expr.constant_bool(False)
        if not compiled:
            raise self._compiler._error(node, "empty blocks are not supported")
        if len(compiled) == 1:
            return compiled[0]
        return self._compiler._expr.block(compiled)

    @staticmethod
    def _is_empty_collection(value: object) -> bool:
        return isinstance(value, (list, tuple, set, dict)) and len(value) == 0

    def _empty_collection(self, node: ast.stmt, ptype: KRPC.Type) -> Any:
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

    _ANNOTATION_VALUE_CODES = {
        "float": KRPC.Type.DOUBLE,
        "int": KRPC.Type.SINT32,
        "bool": KRPC.Type.BOOL,
        "str": KRPC.Type.STRING,
    }

    def _annotation_ptype(self, node: ast.expr) -> KRPC.Type:
        """The protocol buffer type described by a variable annotation, such
        as int, list[float] or dict[str, int]."""
        if isinstance(node, ast.Name):
            if node.id in self._ANNOTATION_VALUE_CODES:
                return build_ptype(self._ANNOTATION_VALUE_CODES[node.id])
            resolved = self._compiler._lookup(node.id)
            if (
                resolved.is_value
                and isinstance(resolved.value, type)
                and issubclass(resolved.value, ClassBase)
            ):
                return self._compiler._class_ptype(resolved.value)
        if isinstance(node, ast.Subscript) and isinstance(node.value, ast.Name):
            container = node.value.id
            if container == "list":
                return build_ptype(KRPC.Type.LIST, [self._annotation_ptype(node.slice)])
            if container == "set":
                return build_ptype(KRPC.Type.SET, [self._annotation_ptype(node.slice)])
            if container == "dict" and isinstance(node.slice, ast.Tuple):
                return build_ptype(
                    KRPC.Type.DICTIONARY,
                    [self._annotation_ptype(element) for element in node.slice.elts],
                )
        raise self._compiler._error(node, "unsupported type annotation")
