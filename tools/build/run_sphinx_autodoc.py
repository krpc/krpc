"""Documents the modules of a python package with sphinx, failing on any warning.

  run_sphinx_autodoc.py PACKAGE [--exclude PACKAGE...]

Each module of PACKAGE is documented with autodoc, along with its members, and
any reference in a docstring has to resolve. A package given to --exclude is
left out, along with the modules it holds.
"""

import argparse
import importlib
import os
import pkgutil
import sys
import tempfile

from sphinx.cmd.build import build_main

CONFIG = """
extensions = ["sphinx.ext.autodoc"]
autodoc_typehints = "none"
"""

DIRECTIVE = """
.. automodule:: {module}
   :members:
   :undoc-members:
"""


def modules(package, exclude):
    """The modules of the package, and the package itself, in name order"""
    root = importlib.import_module(package)
    names = [package] + [
        info.name for info in pkgutil.walk_packages(root.__path__, package + ".")
    ]
    return sorted(
        name
        for name in names
        if not any(name == x or name.startswith(x + ".") for x in exclude)
    )


def run(package, exclude):
    with tempfile.TemporaryDirectory(dir=os.environ.get("TEST_TMPDIR")) as root:
        source = os.path.join(root, "source")
        os.mkdir(source)
        with open(os.path.join(source, "conf.py"), "w", encoding="utf-8") as conf:
            conf.write(CONFIG)
        names = modules(package, exclude)
        with open(os.path.join(source, "index.rst"), "w", encoding="utf-8") as index:
            index.write(package + "\n" + "=" * len(package) + "\n")
            for name in names:
                index.write(DIRECTIVE.format(module=name))
        print("+ sphinx-build -W -n over the %d modules of %s" % (len(names), package))
        sys.stdout.flush()
        # -n reports a reference that does not resolve, and -W makes a warning fail
        output = os.path.join(root, "html")
        options = ["-b", "html", "-E", "-n", "-W", "--keep-going", "-q"]
        return build_main(options + [source, output])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("package")
    parser.add_argument("--exclude", nargs="*", default=[])
    options = parser.parse_args()
    return run(options.package, options.exclude)


if __name__ == "__main__":
    sys.exit(main())
