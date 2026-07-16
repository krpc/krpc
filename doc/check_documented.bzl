" Test that the documented API members exactly match an expected list "

# Body of the generated test executable. It compares the set of members that
# were actually documented (the concatenation of the .documented.txt files
# emitted by the API doc build) against the expected list, and fails listing
# any difference in either direction. The two paths are baked in at analysis
# time as Python literals.
_TEST_TEMPLATE = """#!/usr/bin/env python3
import sys

expected_path = {expected}
actual_paths = {actuals}


def load(path):
    with open(path) as f:
        return set(line.strip() for line in f if line.strip())


expected = load(expected_path)
actual = set()
for path in actual_paths:
    actual |= load(path)

missing = expected - actual
extra = actual - expected
if missing or extra:
    if missing:
        print("Following were expected to be documented but were not:")
        for name in sorted(missing):
            print(name)
        print()
    if extra:
        print("Following were documented but were not expected to be:")
        for name in sorted(extra):
            print(name)
    sys.exit(1)

print("All members documented")
"""

def _check_documented_impl(ctx):
    # The API filegroups carry more than the member lists; only the
    # .documented.txt files list the members that were actually documented.
    documented = [
        src
        for src in ctx.files.srcs
        if src.short_path.endswith(".documented.txt")
    ]

    ctx.actions.write(
        output = ctx.outputs.executable,
        content = _TEST_TEMPLATE.format(
            expected = repr(ctx.file.members.short_path),
            actuals = repr([src.short_path for src in documented]),
        ),
        is_executable = True,
    )

    return DefaultInfo(
        executable = ctx.outputs.executable,
        runfiles = ctx.runfiles(files = [ctx.file.members] + documented),
    )

check_documented_test = rule(
    implementation = _check_documented_impl,
    attrs = {
        "members": attr.label(allow_single_file = True),
        "srcs": attr.label_list(allow_files = True),
    },
    test = True,
)
