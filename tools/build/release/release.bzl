""" Whether the build is a release build, which Bazel needs as it loads the build files """

def _release_switch_impl(rctx):
    release = rctx.getenv("KRPC_RELEASE", "") == "1"
    rctx.file("BUILD.bazel", "")
    rctx.file("defs.bzl", "release = %s\n" % release)

# @krpc_release//:defs.bzl holds release, true when KRPC_RELEASE is 1 in the environment. The
# release scripts and the docs workflow's tag builds set it, and every other build is a dev build.
release_switch = repository_rule(implementation = _release_switch_impl)
