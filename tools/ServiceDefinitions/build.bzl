" service definitions tool "

# DotnetAssemblyRuntimeInfo is only available from rules_dotnet's private module.
load("@rules_dotnet//dotnet/private:providers.bzl", "DotnetAssemblyRuntimeInfo")  # buildifier: disable=bzl-visibility
load("//tools/build/ksp:build.bzl", "ksp_unity_libs")

def _impl(ctx):
    # The service assemblies reference UnityEngine and KSP assemblies, which
    # the tool must be able to resolve while scanning them
    reference_libs = [lib for x in ctx.attr._references for lib in x[DotnetAssemblyRuntimeInfo].libs]
    reference_dirs = {lib.dirname: None for lib in reference_libs}.keys()
    args = ["--output=%s" % ctx.outputs.out.path]
    args += ["--reference-dir=%s" % d for d in reference_dirs]
    args += [ctx.attr.service] + [lib.path for x in ctx.attr.assemblies for lib in x[DotnetAssemblyRuntimeInfo].libs]
    ctx.actions.run(
        inputs = ctx.files.assemblies + reference_libs,
        outputs = [ctx.outputs.out],
        arguments = args,
        progress_message = "Generating service definitions for %s" % ctx.outputs.out.short_path,
        executable = ctx.executable._service_definitions_tool,
    )

service_definitions = rule(
    implementation = _impl,
    attrs = {
        "assemblies": attr.label_list(allow_files = True),
        "service": attr.string(mandatory = True),
        "out": attr.output(mandatory = True),
        "_references": attr.label_list(default = [Label(x) for x in ksp_unity_libs]),
        "_service_definitions_tool": attr.label(
            executable = True,
            cfg = "exec",
            default = Label("//tools/ServiceDefinitions:ServiceDefinitions"),
        ),
    },
)
