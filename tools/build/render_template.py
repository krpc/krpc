"""Instantiates a template, substituting strings and whole files.

  render_template.py --out OUT --template TEMPLATE
                     [--string NAME=VALUE]... [--file NAME=PATH]...

Each NAME is a placeholder replaced wherever it appears in the template, with
VALUE or with the contents of PATH. Only a rule reading a file as it runs can
carry one file inside another, which is what --file is for; --string is here so
that a template needing both takes one action rather than two.
"""

import argparse
import sys


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    parser.add_argument("--template", required=True)
    parser.add_argument("--string", action="append", default=[])
    parser.add_argument("--file", action="append", default=[])
    opts = parser.parse_args()

    substitutions = []
    for spec in opts.string:
        name, value = spec.split("=", 1)
        substitutions.append((name, value))
    for spec in opts.file:
        name, path = spec.split("=", 1)
        with open(path, encoding="utf-8") as handle:
            substitutions.append((name, handle.read()))

    with open(opts.template, encoding="utf-8") as handle:
        content = handle.read()
    for name, value in substitutions:
        content = content.replace(name, value)

    # newline="" so the output is byte for byte the same on every platform:
    # python would otherwise write each line ending as the one the platform uses.
    with open(opts.out, "w", encoding="utf-8", newline="") as handle:
        handle.write(content)
    return 0


if __name__ == "__main__":
    sys.exit(main())
