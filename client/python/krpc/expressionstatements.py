"""Compilation of python statements — assignments, control flow and side
effects — into server side expression nodes. Used by the expression compiler
for function bodies; expression compilation itself lives in
krpc.expressioncompiler."""

from __future__ import annotations
import ast
from typing import Any, Dict, List, Optional, TYPE_CHECKING

from krpc.error import ExpressionCompilationError
from krpc.expressionutils import NUMERIC_CODES, Result as _Result, build_ptype
from krpc.types import ClassBase
import krpc.schema.KRPC_pb2 as KRPC

if TYPE_CHECKING:
    from krpc.expressioncompiler import _Compiler


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
            function = self._compiler._expr.function(
                [parameter for _, _, parameter in parameters], body
            )
            return function, self._return_ptype
        finally:
            self._compiler._active_statements = previous
            self._compiler._scopes.pop()

    def _compile_statements(
        self, statements: List[ast.stmt], top_level: bool = False
    ) -> List[Any]:
        compiled: List[Any] = []
        for position, statement in enumerate(statements):
            last = position == len(statements) - 1
            if isinstance(statement, ast.Return):
                compiled.append(
                    self._compile_return(statement, as_value=top_level and last)
                )
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
            elif isinstance(statement, ast.Break):
                compiled.append(self._compiler._expr.break_())
            elif isinstance(statement, ast.Continue):
                compiled.append(self._compiler._expr.continue_())
            else:
                raise self._compiler._error(
                    statement,
                    "unsupported statement (%s)" % type(statement).__name__,
                )
        if top_level and self._saw_valued_return:
            final = statements[-1] if statements else None
            if not (isinstance(final, ast.Return) and final.value is not None):
                raise ExpressionCompilationError(
                    "A function that returns a value must end with a "
                    "return statement"
                )
        return compiled

    def _compile_return(self, statement: ast.Return, as_value: bool) -> Any:
        if statement.value is None:
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
        elif self._return_ptype.code != result.ptype.code:
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
        if isinstance(statement, ast.Assign):
            if len(statement.targets) != 1:
                return self._compile_multiple_assignment(statement)
            target: ast.expr = statement.targets[0]
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
            operator_node = ast.BinOp(
                left=statement.target, op=statement.op, right=statement.value
            )
            ast.copy_location(operator_node, statement)
            ast.fix_missing_locations(operator_node)
            value = self._compiler._compile(operator_node)
        if isinstance(target, ast.Name):
            return [self._assign_variable(statement, target.id, value)]
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
        function = self._compiler._expr.function([], body.expression)
        self._compiler._local_functions[name] = (function, [], body.ptype)

    def assign_named(self, node: ast.AST, name: str, value: _Result) -> _Result:
        """Compile an assignment expression (the walrus operator): assigns to
        a variable and produces the assigned value."""
        expression = self._assign_variable(node, name, value)
        variable = self._scope[name]
        return _Result(expression=expression, ptype=variable.ptype)

    def _assign_variable(self, node: ast.stmt, name: str, value: _Result) -> Any:
        scope = self._scope
        if name in scope:
            variable = scope[name]
            converted = self._compiler._to_expression(value)
            if (
                variable.ptype is not None
                and converted.ptype is not None
                and variable.ptype.code != converted.ptype.code
                and not (
                    variable.ptype.code in NUMERIC_CODES
                    and converted.ptype.code in NUMERIC_CODES
                )
            ):
                raise self._compiler._error(
                    node,
                    "cannot assign a value of a different type to "
                    "variable '%s'" % name,
                )
            return self._compiler._expr.assign(
                variable.expression, converted.expression
            )
        converted = self._compiler._to_expression(value)
        if converted.ptype is None:
            raise self._compiler._error(
                node,
                "cannot determine the type of variable '%s'; annotate it, "
                "for example x: list[int] = []" % name,
            )
        variable_expression = self._compiler._expr.variable(
            name, self._compiler._remote_type(node, converted.ptype)
        )
        self._declared.append(variable_expression)
        scope[name] = _Result(expression=variable_expression, ptype=converted.ptype)
        return self._compiler._expr.assign(variable_expression, converted.expression)

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
        return self._compiler._call_node(service, procedure, args).expression

    def _assign_subscript(
        self, node: ast.stmt, target: ast.Subscript, value: _Result
    ) -> Any:
        collection = self._compiler._to_expression(
            self._compiler._compile(target.value)
        )
        index = self._compiler._to_expression(self._compiler._compile(target.slice))
        converted = self._compiler._to_expression(value)
        if (
            collection.ptype is not None
            and collection.ptype.code == KRPC.Type.DICTIONARY
        ):
            return self._compiler._expr.dictionary_set(
                collection.expression, index.expression, converted.expression
            )
        if collection.ptype is None or collection.ptype.code == KRPC.Type.LIST:
            return self._compiler._expr.list_set(
                collection.expression, index.expression, converted.expression
            )
        raise self._compiler._error(node, "unsupported assignment target")

    def _compile_if(self, statement: ast.If) -> Optional[Any]:
        condition = self._compiler._compile(statement.test)
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
            self._compiler._compile(statement.test)
        )
        return self._compiler._expr.while_(
            condition.expression, self._statement_block(statement, statement.body)
        )

    def _compile_for(self, statement: ast.For) -> Any:
        if statement.orelse:
            raise self._compiler._error(
                statement, "loop else clauses are not supported"
            )
        if not isinstance(statement.target, ast.Name):
            raise self._compiler._error(
                statement, "the loop variable must be a single name"
            )
        collection = self._compiler._to_expression(
            self._compiler._compile(statement.iter)
        )
        element = self._compiler._element_ptype(statement, collection)
        name = statement.target.id
        scope = self._scope
        if name in scope:
            variable = scope[name]
            if variable.ptype is None or variable.ptype.code != element.code:
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

    def _statement_block(self, node: ast.stmt, statements: List[ast.stmt]) -> Any:
        compiled = self._compile_statements(statements)
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
