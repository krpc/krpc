#!/bin/sh
# Wrapper so the clang_format lint rule can invoke the system LLVM (clang-20 from
# apt.llvm.org) baked into the buildenv image, when built with --config=system-llvm.
# Path-mode toolchains_llvm gives the compiler but no Bazel target for clang-format,
# so we exec the binary directly (matches MODULE.bazel's llvm.toolchain_root path).
exec /usr/lib/llvm-20/bin/clang-format "$@"
