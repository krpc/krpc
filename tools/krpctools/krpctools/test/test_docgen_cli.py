import contextlib
import io
import json
import os
import tempfile
import unittest
from unittest import mock

from krpctools.docgen import main

DEFINITIONS = {
    "Demo": {
        "id": 1,
        "documentation": "<doc><summary>A service.</summary></doc>",
        "procedures": {
            "Hello": {
                "id": 1,
                "parameters": [],
                "documentation": "<doc><summary>Say hello.</summary></doc>",
            },
            "Goodbye": {
                "id": 2,
                "parameters": [],
                "documentation": "<doc><summary>Say goodbye.</summary></doc>",
            },
        },
        "classes": {},
        "enumerations": {},
        "exceptions": {},
    }
}

TEMPLATE = """{{ domain.currentmodule('Demo') }}
Page for {{ services['Demo'].name }}
{{ mark_documented(services['Demo'].members['%s']) }}
"""


class TestDocGenCli(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.dir = directory.name
        self.definitions = self.write("Demo.json", json.dumps(DEFINITIONS))
        self.order = self.write("order.txt", "Demo.Hello\nDemo.Goodbye\n")
        self.hello = self.write("hello.tmpl", TEMPLATE % "Hello")
        self.goodbye = self.write("goodbye.tmpl", TEMPLATE % "Goodbye")

    def path(self, name):
        return os.path.join(self.dir, name)

    def write(self, name, content):
        with open(self.path(name), "w", encoding="utf-8") as fp:
            fp.write(content)
        return self.path(name)

    def read(self, name):
        with open(self.path(name), encoding="utf-8") as fp:
            return fp.read()

    def run_docgen(self, *args):
        stderr = io.StringIO()
        with (
            mock.patch("sys.argv", ["krpc-docgen", *args]),
            contextlib.redirect_stderr(stderr),
        ):
            main()
        return stderr.getvalue()

    def test_pages(self):
        stderr = self.run_docgen(
            "python",
            self.order,
            self.definitions,
            "--page",
            self.hello,
            self.path("out/hello.rst"),
            "--page",
            self.goodbye,
            self.path("out/goodbye.rst"),
            "--documented",
            self.path("documented.txt"),
        )
        self.assertEqual("", stderr)
        self.assertIn("Page for Demo", self.read("out/hello.rst"))
        self.assertIn("Page for Demo", self.read("out/goodbye.rst"))
        self.assertEqual("Demo.Goodbye\nDemo.Hello\n", self.read("documented.txt"))

    def test_pages_from_file(self):
        arguments = [
            "python",
            self.order,
            self.definitions,
            "--page",
            self.hello,
            self.path("hello.rst"),
        ]
        self.run_docgen("@" + self.write("arguments.txt", "\n".join(arguments)))
        self.assertIn("Page for Demo", self.read("hello.rst"))

    def test_unordered_members(self):
        self.write("order.txt", "Demo.Hello\n")
        stderr = io.StringIO()
        with (
            mock.patch(
                "sys.argv",
                [
                    "krpc-docgen",
                    "python",
                    self.order,
                    self.definitions,
                    "--page",
                    self.hello,
                    self.path("hello.rst"),
                ],
            ),
            contextlib.redirect_stderr(stderr),
        ):
            self.assertEqual(1, main())
        self.assertEqual(
            "Error: Don't know how to order:\nDemo.Goodbye\n", stderr.getvalue()
        )

    def test_page_without_order_file(self):
        with self.assertRaises(SystemExit):
            self.run_docgen("python", "--page", self.hello, self.path("hello.rst"))

    def test_positional_page(self):
        with self.assertRaises(SystemExit):
            self.run_docgen(
                "python",
                self.hello,
                self.order,
                self.path("hello.rst"),
                self.definitions,
            )


if __name__ == "__main__":
    unittest.main()
