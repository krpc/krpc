" C++ build tools "

load("@bazel_skylib//lib:paths.bzl", "paths")
load("@bazel_skylib//rules:build_test.bzl", "build_test")
load("@rules_cc//cc/common:cc_common.bzl", "cc_common")
load("@rules_cc//cc/common:cc_info.bzl", "CcInfo")
load("@rules_python//python:defs.bzl", "py_test")

# The lint rules below run a hermetic LLVM tool (clang-tidy/clang-format) as a
# build action and emit a stamp file on success; a build_test wrapper turns
# "the stamp builds" into a test. The tool is invoked through the run_and_stamp
# python helper (not a shell `&& touch`) so the rules work on any host OS.

_TidyInfo = provider(
    doc = "The sources of a cc target to lint, and the flags it compiles them with.",
    fields = {
        "c_srcs": "the C sources",
        "cxx_srcs": "the C++ sources",
        "flags": "the compiler flags of the target, for both languages",
        "inputs": "a depset of the files the sources need",
    },
)

def _tidy_aspect_impl(target, ctx):
    rule = ctx.rule
    if rule.kind not in ("cc_binary", "cc_library", "cc_test"):
        fail("%s is not a cc_binary, cc_library or cc_test" % target.label)
    deps = [d[CcInfo] for d in rule.attr.deps if CcInfo in d]
    cc = cc_common.merge_cc_infos(cc_infos = deps).compilation_context
    copts = rule.attr.copts

    # The target's own include dirs are relative to its package, in the source
    # tree and in the output tree, which holds its generated headers
    package = paths.join(target.label.workspace_root, target.label.package)
    includes = []
    for include in rule.attr.includes:
        includes.append(paths.join(package, include))
        includes.append(paths.join(ctx.bin_dir.path, package, include))
    includes += cc.includes.to_list()
    defines = cc.defines.to_list() + rule.attr.defines + rule.attr.local_defines

    flags = list(ctx.fragments.cpp.copts) + copts
    flags += ["-I" + d for d in includes]
    flags += ["-iquote" + d for d in cc.quote_includes.to_list()]
    flags += ["-isystem" + d for d in cc.system_includes.to_list()]
    flags += ["-D" + d for d in defines]

    # Generated sources are left to the generator
    files = rule.files.srcs + getattr(rule.files, "hdrs", [])
    srcs = [f for f in rule.files.srcs if f.is_source and f.extension in ("c", "cc", "cpp")]
    as_cxx = "-xc++" in copts
    return [_TidyInfo(
        c_srcs = [f for f in srcs if f.extension == "c" and not as_cxx],
        cxx_srcs = [f for f in srcs if f.extension != "c" or as_cxx],
        flags = flags,
        inputs = depset(files, transitive = [cc.headers]),
    )]

_tidy_aspect = aspect(
    implementation = _tidy_aspect_impl,
    fragments = ["cpp"],
)

# The checks a test or example program leaves out. googletest registers each test
# in a static initializer, and an exception escaping a program ends it with the
# message a test needs. An example computes in double for float parameters, takes
# a nullable result with value(), which throws for null, and prints each line as
# it goes.
_PROGRAM_CHECKS = [
    "-bugprone-exception-escape",
    "-bugprone-narrowing-conversions",
    "-bugprone-throwing-static-initialization",
    "-bugprone-unchecked-optional-access",
    "-performance-avoid-endl",
]

def _clang_tidy_impl(ctx):
    tidy = ctx.files._clang_tidy[0]
    config = ctx.file.config

    # See the gcc-install-dir copt in .bazelrc
    common = ["-Wno-gcc-install-dir-libstdcxx"]
    languages = {
        "c": list(ctx.fragments.cpp.conlyopts) + ctx.attr.conlyopts,
        "cxx": ["-stdlib=libc++"] + list(ctx.fragments.cpp.cxxopts),
    }

    # One action for each language of each target, as the flags differ
    stamps = []
    targets = [(t, []) for t in ctx.attr.targets] + [(t, _PROGRAM_CHECKS) for t in ctx.attr.programs]
    for target, checks in targets:
        info = target[_TidyInfo]
        for language, language_flags in languages.items():
            srcs = getattr(info, language + "_srcs")
            if not srcs:
                continue
            stamp = ctx.actions.declare_file("%s/%s/%s.%s.stamp" % (
                ctx.label.name,
                target.label.package,
                target.label.name,
                language,
            ))
            stamps.append(stamp)

            # clang-tidy resolves the include dirs (exec-root relative) and the
            # source paths from the exec root, so this must run as a build action
            # rather than from a test's runfiles.
            args = ctx.actions.args()
            args.add(stamp.path)
            args.add(tidy.path)
            args.add("--config-file=" + config.path)
            args.add("--quiet")
            if checks:
                args.add("--checks=" + ",".join(checks))
            args.add("--warnings-as-errors=*")
            args.add_all(srcs)
            args.add("--")
            args.add_all(common + language_flags + info.flags)

            ctx.actions.run(
                executable = ctx.executable._runner,
                arguments = [args],
                inputs = depset(
                    direct = [config],
                    transitive = [info.inputs, depset(ctx.files._clang_tidy + ctx.files._llvm_includes)],
                ),
                outputs = [stamp],
                mnemonic = "ClangTidy",
                progress_message = "Running clang-tidy on %s" % target.label,
            )
    return [DefaultInfo(files = depset(stamps))]

_clang_tidy = rule(
    implementation = _clang_tidy_impl,
    fragments = ["cpp"],
    attrs = {
        "conlyopts": attr.string_list(),
        "config": attr.label(
            allow_single_file = True,
            default = Label("//:.clang-tidy"),
        ),
        "programs": attr.label_list(aspects = [_tidy_aspect]),
        "targets": attr.label_list(aspects = [_tidy_aspect]),
        "_clang_tidy": attr.label(
            default = Label("//tools/build/llvm:clang-tidy"),
            allow_files = True,
        ),
        "_llvm_includes": attr.label(
            default = Label("//tools/build/llvm:include"),
            allow_files = True,
        ),
        "_runner": attr.label(
            default = Label("//tools/build:run_and_stamp"),
            executable = True,
            cfg = "exec",
        ),
    },
)

# clang-tidy takes libc++ and the toolchain's :include headers, which only the
# LLVM toolchain's Bazel targets supply, so it runs on Linux alone. clang-format
# reads nothing but the sources and the style file, and //tools/build/llvm has a
# binary for it on Windows as well.
_TIDY_ONLY_ON = ["@platforms//os:linux"]

_FORMAT_ONLY_ON = select({
    "@platforms//os:linux": [],
    "@platforms//os:windows": [],
    "//conditions:default": ["@platforms//:incompatible"],
})

def clang_tidy_test(name, targets = [], programs = [], conlyopts = [], config = None, **kwargs):
    """A test that clang-tidy reports nothing in the sources of cc targets.

    Each target's sources are checked with the copts, defines and include dirs the target
    compiles them with.

    Args:
        name: the target name.
        targets: the cc_binary, cc_library and cc_test targets.
        programs: the targets of test and example programs, checked without the checks
            that only scaffolding reports.
        conlyopts: extra flags for the C sources.
        config: the clang-tidy configuration file.
        **kwargs: passed to the test.
    """
    check = name + ".check"
    _clang_tidy(
        name = check,
        targets = targets,
        programs = programs,
        conlyopts = conlyopts,
        config = config or Label("//:.clang-tidy"),
        tags = ["manual"],
        testonly = True,
        target_compatible_with = _TIDY_ONLY_ON,
    )
    build_test(
        name = name,
        targets = [":" + check],
        target_compatible_with = _TIDY_ONLY_ON,
        **kwargs
    )

def _clang_format_impl(ctx):
    fmt = ctx.files._clang_format[0]
    config = ctx.file.config
    srcs = ctx.files.srcs
    stamp = ctx.actions.declare_file(ctx.label.name + ".stamp")

    # --dry-run --Werror exits non-zero (and prints the diff) if any file is not
    # already formatted.
    args = ctx.actions.args()
    args.add(stamp.path)
    args.add(fmt.path)
    args.add("--dry-run")
    args.add("--Werror")
    args.add("--style=file:" + config.path)
    args.add_all([s.path for s in srcs])

    ctx.actions.run(
        executable = ctx.executable._runner,
        arguments = [args],
        inputs = depset(direct = srcs + [config] + ctx.files._clang_format),
        outputs = [stamp],
        mnemonic = "ClangFormat",
        progress_message = "Checking clang-format on %s" % ctx.label,
    )
    return [DefaultInfo(files = depset([stamp]))]

_clang_format = rule(
    implementation = _clang_format_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = [".c", ".cc", ".cpp", ".h", ".hpp"]),
        "config": attr.label(
            allow_single_file = True,
            default = Label("//:.clang-format"),
        ),
        "_runner": attr.label(
            default = Label("//tools/build:run_and_stamp"),
            executable = True,
            cfg = "exec",
        ),
        "_clang_format": attr.label(
            default = Label("//tools/build/llvm:clang-format"),
            allow_files = True,
        ),
    },
)

# buildifier: disable=function-docstring
def clang_format_test(name, srcs, config = None, **kwargs):
    check = name + ".check"
    _clang_format(
        name = check,
        srcs = srcs,
        config = config or Label("//:.clang-format"),
        tags = ["manual"],
        testonly = True,
        target_compatible_with = _FORMAT_ONLY_ON,
    )
    build_test(
        name = name,
        targets = [":" + check],
        target_compatible_with = _FORMAT_ONLY_ON,
        **kwargs
    )

def doxygen_test(name, srcs, c = False, unlinked = [], **kwargs):
    """A test that Doxygen documents the given headers without a warning, linking each name.

    Args:
        name: the target name.
        srcs: the headers.
        c: whether the headers are C rather than C++.
        unlinked: the names the documentation refers to that Doxygen may leave unlinked.
        **kwargs: passed to the test.
    """
    doxygen = Label("//tools/build/doxygen")
    args = ["--c"] if c else []
    if unlinked:
        args += ["--unlinked"] + unlinked
    py_test(
        name = name,
        srcs = [Label("//tools/build:run_doxygen.py")],
        main = Label("//tools/build:run_doxygen.py"),
        args = args + ["--doxygen", "$(rootpaths %s)" % doxygen, "--"] +
               ["$(rootpaths %s)" % src for src in srcs],
        data = [doxygen] + srcs,
        **kwargs
    )
