"""A check that a compiled function reads a local variable only once every
path to the read has assigned it."""

from __future__ import annotations

import ast
from typing import TYPE_CHECKING, cast

if TYPE_CHECKING:
    from krpc.functioncompiler import _Compiler


def assigned_names(function: ast.AST) -> set[str]:
    """The names a function assigns, which Python makes local to the whole body.
    A comprehension binds its own names, apart from an assignment expression."""
    names: set[str] = set()

    def visit(node: ast.AST) -> None:
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            names.add(node.name)
            return
        if isinstance(node, ast.Lambda):
            return
        if isinstance(
            node, (ast.ListComp, ast.SetComp, ast.DictComp, ast.GeneratorExp)
        ):
            for child in ast.walk(node):
                if isinstance(child, ast.NamedExpr):
                    visit(child.target)
            return
        if isinstance(node, ast.Name) and isinstance(node.ctx, (ast.Store, ast.Del)):
            names.add(node.id)
        elif isinstance(node, ast.ExceptHandler) and node.name:
            names.add(node.name)
        for child in ast.iter_child_nodes(node):
            visit(child)

    if isinstance(function, ast.Lambda):
        visit(function.body)
    else:
        for statement in cast(ast.FunctionDef, function).body:
            visit(statement)
    return names


def nonlocal_names(function: ast.AST) -> set[str]:
    """The names a function declares nonlocal, outside its local functions."""
    names: set[str] = set()

    def visit(node: ast.AST) -> None:
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            return
        if isinstance(node, ast.Nonlocal):
            names.update(node.names)
        for child in ast.iter_child_nodes(node):
            visit(child)

    if isinstance(function, ast.FunctionDef):
        for statement in function.body:
            visit(statement)
    return names


def local_function_nonlocal(function: ast.AST) -> ast.Nonlocal | None:
    """A nonlocal statement in one of a function's local functions, or None."""
    for node in ast.walk(function):
        if node is not function and isinstance(node, ast.FunctionDef):
            for child in ast.walk(node):
                if isinstance(child, ast.Nonlocal):
                    return child
    return None


# The names definitely assigned at a point of a function, or None where the
# point cannot be reached
_Assigned = frozenset[str] | None


def _merge(left: _Assigned, right: _Assigned) -> _Assigned:
    """The names assigned where two paths join: those assigned on both."""
    if left is None:
        return right
    if right is None:
        return left
    return left & right


class DefiniteAssignment:
    """Checks that a function reads a local variable only where every path to
    the read has assigned it. The check is conservative, as C#'s is: an
    assignment in a loop body, or in a try body before an exception, does not
    count after the loop or the try, and only a constant condition is known to
    choose a branch.

    A local function reads the function's variables when it is called, so a
    variable it reads can be assigned after its definition. The check of those
    reads moves to each call, and the definition is added to deferred, as it
    cannot be compiled before the variables are declared."""

    def __init__(
        self,
        compiler: _Compiler,
        local_names: set[str],
        deferred: set[ast.AST] | None = None,
    ):
        self._compiler = compiler
        self._locals = local_names
        self.deferred: set[ast.AST] = set() if deferred is None else deferred
        # The names assigned at each break out of the loops being checked
        self._breaks: list[list[_Assigned]] = []
        # The names each local function needs assigned where it is called: the
        # variables it reads that are not assigned at its definition, and the
        # local functions it calls that need any
        self._requires: dict[str, frozenset[str]] = {}

    def check(self, statements: list[ast.stmt], assigned: frozenset[str]) -> None:
        self._block(statements, assigned)

    def _block(self, statements: list[ast.stmt], assigned: _Assigned) -> _Assigned:
        for statement in statements:
            if assigned is None:
                break
            assigned = self._statement(statement, assigned)
        return assigned

    def _statement(self, statement: ast.stmt, assigned: frozenset[str]) -> _Assigned:
        if isinstance(statement, ast.Expr):
            return self._expression(statement.value, assigned)
        if isinstance(statement, ast.Assign):
            if (
                isinstance(statement.value, ast.Lambda)
                and len(statement.targets) == 1
                and isinstance(statement.targets[0], ast.Name)
            ):
                name = statement.targets[0].id
                lambda_ = statement.value
                parameters = {argument.arg for argument in lambda_.args.args}
                self._local_function(
                    name, lambda_, [lambda_.body], set(), parameters, assigned
                )
                return assigned | {name}
            assigned = self._expression(statement.value, assigned)
            for target in statement.targets:
                assigned = self._target(target, assigned)
            return assigned
        if isinstance(statement, ast.AnnAssign):
            if statement.value is not None:
                assigned = self._expression(statement.value, assigned)
            return self._target(statement.target, assigned)
        if isinstance(statement, ast.AugAssign):
            # The target is read before it is written
            assigned = self._expression(
                ast.copy_location(
                    ast.BinOp(
                        left=self._loaded(statement.target),
                        op=statement.op,
                        right=statement.value,
                    ),
                    statement,
                ),
                assigned,
            )
            return self._target(statement.target, assigned)
        if isinstance(statement, (ast.Return, ast.Raise)):
            for child in ast.iter_child_nodes(statement):
                if isinstance(child, ast.expr):
                    assigned = self._expression(child, assigned)
            return None
        if isinstance(statement, ast.If):
            assigned = self._expression(statement.test, assigned)
            if isinstance(statement.test, ast.Constant):
                branch = statement.body if statement.test.value else statement.orelse
                return self._block(branch, assigned)
            return _merge(
                self._block(statement.body, assigned),
                self._block(statement.orelse, assigned),
            )
        if isinstance(statement, ast.While):
            assigned = self._expression(statement.test, assigned)
            breaks = self._loop(statement.body, assigned)
            if isinstance(statement.test, ast.Constant) and statement.test.value:
                # Only a break leaves the loop
                after: _Assigned = None
                for state in breaks:
                    after = _merge(after, state)
                return after
            return assigned
        if isinstance(statement, ast.For):
            assigned = self._expression(statement.iter, assigned)
            self._loop(statement.body, self._target(statement.target, assigned))
            return assigned
        if isinstance(statement, ast.Break):
            if self._breaks:
                self._breaks[-1].append(assigned)
            return None
        if isinstance(statement, ast.Continue):
            return None
        if isinstance(statement, ast.Try):
            return self._try(statement, assigned)
        if isinstance(statement, ast.FunctionDef):
            parameters = {argument.arg for argument in statement.args.args}
            self._local_function(
                statement.name,
                statement,
                statement.body,
                assigned_names(statement),
                parameters,
                assigned,
            )
            return assigned | {statement.name}
        if isinstance(statement, ast.Delete):
            for target in statement.targets:
                assigned = self._expression(self._loaded(target), assigned)
            return assigned
        return assigned

    def _loop(self, body: list[ast.stmt], assigned: frozenset[str]) -> list[_Assigned]:
        """Check a loop body, and return the names assigned at each break."""
        self._breaks.append([])
        try:
            self._block(body, assigned)
        finally:
            breaks = self._breaks.pop()
        return breaks

    def _try(self, statement: ast.Try, assigned: frozenset[str]) -> _Assigned:
        # An exception can leave the body before any of its assignments, so a
        # handler starts with the names assigned before the try
        after = self._block(statement.body, assigned)
        for handler in statement.handlers:
            if handler.type is not None:
                self._expression(handler.type, assigned)
            name = {handler.name} if handler.name else set()
            handled = self._block(handler.body, assigned | name)
            # The name of the exception is unbound when the handler ends
            if handled is not None:
                handled = handled - name
            after = _merge(after, handled)
        if not statement.finalbody:
            return after
        finished = self._block(statement.finalbody, assigned)
        if finished is None or after is None:
            return None
        return after | finished

    def _local_function(
        self,
        name: str,
        node: ast.AST,
        body: list[ast.AST],
        own: set[str],
        parameters: set[str],
        assigned: frozenset[str],
    ) -> None:
        """Check a local function, a def or a lambda assigned to a name, whose
        own variables shadow the function's. It runs when it is called, so its
        reads of the function's variables are checked at each call."""
        nested = DefiniteAssignment(self._compiler, self._locals | own, self.deferred)
        outer = (self._locals | assigned | {name}) - own
        if isinstance(node, ast.Lambda):
            nested._expression(cast(ast.expr, body[0]), frozenset(outer | parameters))
        else:
            nested.check(cast(list[ast.stmt], body), frozenset(outer | parameters))
        reads = {
            child.id
            for statement in body
            for child in ast.walk(statement)
            if isinstance(child, ast.Name)
            and isinstance(child.ctx, ast.Load)
            and child.id in self._locals
            and child.id not in own | parameters
            and child.id != name
        }
        requires = frozenset(
            read for read in reads if read not in assigned or self._requires.get(read)
        )
        self._requires[name] = requires
        if requires:
            self.deferred.add(node)

    def _check_call(self, node: ast.Name, assigned: frozenset[str]) -> None:
        """Check that the variables a local function reads, and those of the
        local functions it calls, are assigned where it is called."""
        pending = [node.id]
        seen = set(pending)
        while pending:
            for name in sorted(self._requires.get(pending.pop(), frozenset())):
                if name not in assigned:
                    raise self._compiler._error(
                        node,
                        "local function '%s' reads the local variable '%s', which "
                        "is not assigned on every path to this call" % (node.id, name),
                    )
                if name not in seen:
                    seen.add(name)
                    pending.append(name)

    def _target(self, target: ast.expr, assigned: frozenset[str]) -> frozenset[str]:
        """Check the reads an assignment target makes, and add the names it
        assigns."""
        if isinstance(target, ast.Name):
            self._requires.pop(target.id, None)
            return assigned | {target.id}
        if isinstance(target, (ast.Tuple, ast.List)):
            for element in target.elts:
                assigned = self._target(element, assigned)
            return assigned
        result = self._expression(self._loaded(target), assigned)
        assert result is not None
        return result

    @staticmethod
    def _loaded(target: ast.expr) -> ast.expr:
        """An assignment target as the expression reading it."""
        if isinstance(target, ast.Name):
            return ast.copy_location(ast.Name(id=target.id, ctx=ast.Load()), target)
        return target

    def _expression(self, node: ast.expr, assigned: frozenset[str]) -> frozenset[str]:
        """Check the reads an expression makes, in the order it makes them, and
        return the names assigned after it."""
        if isinstance(node, ast.Name):
            if (
                isinstance(node.ctx, ast.Load)
                and node.id in self._locals
                and node.id not in assigned
            ):
                raise self._compiler._error(
                    node,
                    "the local variable '%s' is read before it is assigned on "
                    "every path" % node.id,
                )
            if isinstance(node.ctx, ast.Load) and node.id in self._requires:
                self._check_call(node, assigned)
            return assigned
        if isinstance(node, ast.NamedExpr):
            return self._expression(node.value, assigned) | {node.target.id}
        if isinstance(node, ast.BoolOp):
            # Only the first operand is always evaluated
            assigned = self._expression(node.values[0], assigned)
            for value in node.values[1:]:
                self._expression(value, assigned)
            return assigned
        if isinstance(node, ast.IfExp):
            assigned = self._expression(node.test, assigned)
            return self._expression(node.body, assigned) & self._expression(
                node.orelse, assigned
            )
        if isinstance(
            node, (ast.ListComp, ast.SetComp, ast.DictComp, ast.GeneratorExp)
        ):
            # The names a comprehension binds are its own, and its body may run
            # no times
            bound = set()
            for generator in node.generators:
                bound |= {
                    name.id
                    for name in ast.walk(generator.target)
                    if isinstance(name, ast.Name)
                }
            inner = assigned | bound
            for child in ast.iter_child_nodes(node):
                if isinstance(child, ast.comprehension):
                    self._expression(child.iter, inner)
                    for condition in child.ifs:
                        self._expression(condition, inner)
                elif isinstance(child, ast.expr):
                    self._expression(child, inner)
            return assigned
        if isinstance(node, ast.Lambda):
            parameters = {argument.arg for argument in node.args.args}
            self._expression(node.body, assigned | parameters)
            return assigned
        for child in ast.iter_child_nodes(node):
            if isinstance(child, ast.expr):
                assigned = self._expression(child, assigned)
            elif isinstance(child, ast.keyword):
                assigned = self._expression(child.value, assigned)
        return assigned
