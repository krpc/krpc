" clientgen tool "

def _impl(ctx):
    language = ctx.attr.language
    service = ctx.attr.service
    defs = ctx.file.defs
    output = ctx.outputs.out

    ctx.actions.run(
        inputs = [defs],
        outputs = [output],
        progress_message = "Generating %s code for %s service" % (language, service),
        executable = ctx.executable._clientgen,
        arguments = [language, service, defs.path, "--output=%s" % output.path],
    )

clientgen = rule(
    implementation = _impl,
    attrs = {
        "service": attr.string(mandatory = True),
        "defs": attr.label(allow_single_file = True),
        "out": attr.output(mandatory = True),
        "language": attr.string(mandatory = True),
        "_clientgen": attr.label(
            default = Label("//tools/krpctools:clientgen"),
            executable = True,
            cfg = "exec",
        ),
    },
)

def _snake_case(name):
    """Converts a camel case name to snake case, as krpc.utils.snake_case does."""
    result = ""
    for i in range(len(name)):
        before = name[i - 1] if i > 0 else ""
        after = name[i + 1] if i + 1 < len(name) else ""
        if name[i] == "_" and i > 0:
            result += "_"
        elif name[i].isupper() and (before.islower() or before.isdigit() or
                                    (before.isupper() and (after.islower() or after.isdigit()))):
            result += "_"
        result += name[i]
    return result.lower()

# buildifier: disable=function-docstring
def clientgen_csharp(name, service, defs, visibility = [], out = None):
    if out == None:
        out = "Services/%s.cs" % service
    clientgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = "csharp",
        visibility = visibility,
    )

# buildifier: disable=function-docstring
def clientgen_cpp(name, service, defs, out = None):
    if out == None:
        out = "include/krpc/services/%s.hpp" % _snake_case(service)
    clientgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = "cpp",
    )

# buildifier: disable=function-docstring
def clientgen_java(name, service, defs, out = None):
    if out == None:
        out = "services/%s.java" % service
    clientgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = "java",
    )

# buildifier: disable=function-docstring
def clientgen_cnano(name, service, defs, out = None):
    if out == None:
        out = "include/krpc_cnano/services/%s.h" % _snake_case(service)
    clientgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = "cnano",
    )

# buildifier: disable=function-docstring
def clientgen_python(name, service, defs, out = None):
    if out == None:
        out = "krpc/services/%s.py" % service.lower()
    clientgen(
        name = name,
        service = service,
        defs = defs,
        out = out,
        language = "python",
    )
