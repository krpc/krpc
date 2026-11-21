"""Running an example from a test case: the mixin a game's example test case takes, which runs
an example and checks how it ended. The game's own test case brings the game to the example's
setup."""

from __future__ import annotations

import math
import re
import tempfile
import unittest
from typing import TYPE_CHECKING, Any, ClassVar

from tools.doc_examples import runner
from tools.doc_examples.languages import Command, Languages
from tools.doc_examples.monitor import Monitor
from tools.doc_examples.spec import (
    Example,
    Exit,
    Interactive,
    Loop,
    Values,
    example_path,
)

# The time an example has to connect before a hook acts on the game, in seconds
CONNECT_TIMEOUT = 60

# The base the mixin's methods are checked against: the game's test case, a unittest.TestCase
# with a kRPC connection
_Base: Any = unittest.TestCase if TYPE_CHECKING else object


class ExampleRunner(_Base):
    # The languages of the game's examples
    languages: ClassVar[Languages]

    if TYPE_CHECKING:
        # The game's connection, from the game's test case
        @classmethod
        def connect(cls) -> Any: ...

    def run_example(self, example: Example, language: str):
        example = example.for_language(language)
        if example.prepare:
            example.prepare(self)
        command = self.languages.command(language, example_path(example.key, language))
        monitor = example.monitor(self) if example.monitor else None
        if monitor:
            monitor.start()
        try:
            with tempfile.TemporaryDirectory(prefix="krpc-example-") as cwd:
                result = self.run_command(
                    example, command, str(command.cwd or cwd), monitor
                )
            if monitor and result.exited and result.returncode == 0:
                monitor.settle()
        finally:
            if monitor:
                monitor.stop()
        message = result.describe()
        run = example.run.then if isinstance(example.run, Interactive) else example.run
        if isinstance(run, Exit):
            self.assertTrue(result.exited, "Timed out. " + message)
            self.assertEqual(0, result.returncode, message)
        else:
            assert isinstance(run, Loop)
            self.assertFalse(result.exited, "Exited before being stopped. " + message)
            if run.prints:
                self.assertTrue(result.stdout.strip(), "Printed nothing. " + message)
        if not example.expect_errors:
            self.assertEqual("", result.stderr, message)
        for pattern in example.expect_errors:
            self.assertTrue(
                re.search(pattern, result.stderr),
                "Error output does not match %r. %s" % (pattern, message),
            )
        for pattern in example.expect_output:
            self.assertTrue(
                re.search(pattern, result.stdout),
                "Output does not match %r. %s" % (pattern, message),
            )
        if example.values:
            self.check_values(example.values, result.stdout, message)
        if example.lines:
            expected = sorted(_normalize(line) for line in example.lines(self))
            printed = sorted(
                _normalize(line) for line in result.stdout.splitlines() if line.strip()
            )
            self.assertEqual(expected, printed, message)
        if example.check:
            example.check(self)
        if monitor:
            print(monitor.describe())
            monitor.check()

    def check_values(self, values: Values, stdout: str, message: str) -> None:
        printed = [float(match) for match in re.findall(values.pattern, stdout)]
        expected = list(values.expected(self))
        self.assertEqual(
            len(expected), len(printed), "Printed %s. %s" % (printed, message)
        )
        for want, got in zip(expected, printed, strict=True):
            self.assertTrue(
                math.isclose(
                    want, got, rel_tol=values.relative, abs_tol=values.absolute
                ),
                "Printed %s, the game gives %s. %s" % (got, want, message),
            )

    def run_command(
        self,
        example: Example,
        command: Command,
        cwd: str,
        monitor: Monitor | None = None,
    ) -> runner.Result:
        run = example.run
        mode = run.then if isinstance(run, Interactive) else run
        # The clients connected before the example starts, by id. The last example's client
        # stays in the list until the server drops its connection.
        clients = {client.id for client in self.connect().krpc.clients}
        # Whether the example runs to its end, for a runner that has to stop the program then
        env = dict(
            command.env, KRPC_EXAMPLE_MODE="exit" if isinstance(mode, Exit) else "loop"
        )
        process = runner.Process(command.argv, env, cwd)
        connected = True
        try:
            if isinstance(run, Interactive):
                ready = run.ready
                if ready is None:
                    connected = runner.wait_for(
                        lambda: any(
                            client.id not in clients
                            for client in self.connect().krpc.clients
                        ),
                        process,
                        CONNECT_TIMEOUT,
                    )
                else:
                    connected = runner.wait_for(
                        lambda: re.search(ready, process.stdout()) is not None,
                        process,
                        CONNECT_TIMEOUT,
                    )
                if connected:
                    run.during(self)
            if connected:
                timeout = mode.timeout if isinstance(mode, Exit) else mode.seconds
                if monitor:
                    # Stop the example early when the monitor fails
                    runner.wait_for(monitor.failed.is_set, process, timeout)
                else:
                    process.wait(timeout)
        finally:
            result = process.finish()
        if not connected:
            self.fail("Not ready for the hook. " + result.describe())
        return result


def _normalize(line: str) -> str:
    return " ".join(line.split())
