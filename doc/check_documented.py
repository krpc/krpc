"""Checks that the documented API members are exactly the expected ones.

  check_documented.py CONFIG

CONFIG is the json file written by the check_documented_test rule, naming the
list of members that ought to be documented and the .documented.txt files the
API documentation build emitted. Reports the difference either way.
"""

import argparse
import json
import sys


def load(path):
    with open(path, encoding="utf-8") as members:
        return set(line.strip() for line in members if line.strip())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("config")
    options = parser.parse_args()
    with open(options.config, encoding="utf-8") as config_file:
        config = json.load(config_file)

    expected = load(config["expected"])
    actual = set()
    for path in config["actual"]:
        actual |= load(path)

    missing = expected - actual
    extra = actual - expected
    if missing:
        print("Following were expected to be documented but were not:")
        for name in sorted(missing):
            print(name)
        print()
    if extra:
        print("Following were documented but were not expected to be:")
        for name in sorted(extra):
            print(name)
    if missing or extra:
        return 1

    print("All members documented")
    return 0


if __name__ == "__main__":
    sys.exit(main())
