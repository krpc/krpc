" packaging tools "

load("@rules_pkg//pkg:mappings.bzl", "pkg_attributes", "pkg_files", "strip_prefix")
load("@rules_pkg//pkg:zip.bzl", _rules_pkg_zip = "pkg_zip")

# buildifier: disable=function-docstring-header
def _apply_path_map(path_map, path):
    """ Apply the path mappings to a path.
        Replaces the longest prefix match from the mapping. """
    matchlen = 0
    match = path
    for x, y in path_map.items():
        if path.startswith(x):
            if len(x) > matchlen:
                match = y + path[len(x):]
                matchlen = len(x)
    return match

def _apply_exclude(exclude, path):
    """ Apply wildcard exclusion patterns to the path. """

    # TODO: improve this
    for pattern in exclude:
        if "*" in pattern:
            if pattern[0] == "*" and path.endswith(pattern[1:]):
                return True
        else:
            return path == pattern
    return False

def _is_executable(mode_map, path):
    """ Whether the mode mapping marks the given archive path as executable. """
    return mode_map.get(path) == "755"

def _stage_files_impl(ctx):
    outs = []
    executable_outs = []
    for src in ctx.files.srcs:
        mapped = _apply_path_map(ctx.attr.path_map, src.short_path)
        if _apply_exclude(ctx.attr.exclude, src.short_path):
            continue
        if mapped.startswith(".."):
            fail(("File %s (from an external repository) has no path_map entry; " +
                  "add a mapping for its path. Note that these paths contain the " +
                  "repository's canonical name, which can change when bazel or " +
                  "MODULE.bazel dependencies are upgraded.") % src.short_path)
        path = ctx.label.name + "/" + mapped
        out = ctx.actions.declare_file(path)

        # A symlink rather than a copy keeps staging cheap and OS-independent
        # (no shell `cp`); rules_pkg follows it when archiving. The archive mode
        # is set by pkg_files below, not by the staged file, so the executable /
        # regular split is preserved via the output groups.
        ctx.actions.symlink(
            output = out,
            target_file = src,
        )
        if _is_executable(ctx.attr.mode_map, mapped):
            executable_outs.append(out)
        else:
            outs.append(out)

    return [
        DefaultInfo(files = depset(outs + executable_outs)),
        OutputGroupInfo(
            executable = depset(executable_outs),
            regular = depset(outs),
        ),
    ]

stage_files = rule(
    implementation = _stage_files_impl,
    attrs = {
        "srcs": attr.label_list(allow_files = True),
        "path_map": attr.string_dict(),
        "mode_map": attr.string_dict(),
        "exclude": attr.string_list(),
    },
)

# buildifier: disable=function-docstring
def pkg_zip(name, out, files, path_map = {}, mode_map = {}, exclude = [], visibility = None):
    stage_files(
        name = name + "-staged",
        srcs = files,
        exclude = exclude,
        mode_map = mode_map,
        path_map = path_map,
    )
    native.filegroup(
        name = name + "-staged-regular",
        srcs = [name + "-staged"],
        output_group = "regular",
    )
    native.filegroup(
        name = name + "-staged-executable",
        srcs = [name + "-staged"],
        output_group = "executable",
    )
    pkg_files(
        name = name + "-files",
        srcs = [name + "-staged-regular"],
        attributes = pkg_attributes(mode = "0644"),
        strip_prefix = strip_prefix.from_pkg(name + "-staged"),
    )
    pkg_files(
        name = name + "-executable-files",
        srcs = [name + "-staged-executable"],
        attributes = pkg_attributes(mode = "0755"),
        strip_prefix = strip_prefix.from_pkg(name + "-staged"),
    )
    _rules_pkg_zip(
        name = name,
        srcs = [
            name + "-files",
            name + "-executable-files",
        ],
        out = out,
        visibility = visibility,
    )
