" clientgen tool "

load("@bazel_skylib//rules:expand_template.bzl", "expand_template")
load(":services.bzl", "SERVICES")

ClientgenInfo = provider(
    doc = "The generated code of a service, the path it is included by, and the " +
          "definitions it was generated from",
    fields = ["service", "include", "defs"],
)

def _impl(ctx):
    language = ctx.attr.language
    service = ctx.attr.service
    defs = depset(
        [ctx.file.defs],
        transitive = [dep[ClientgenInfo].defs for dep in ctx.attr.deps],
        order = "preorder",
    )
    output = ctx.outputs.out
    includes = [
        "--include=%s=%s" % (dep[ClientgenInfo].service, dep[ClientgenInfo].include)
        for dep in ctx.attr.deps
    ]

    ctx.actions.run(
        inputs = defs,
        outputs = [output],
        progress_message = "Generating %s code for %s service" % (language, service),
        executable = ctx.executable._clientgen,
        arguments = [language, service] + [f.path for f in defs.to_list()] +
                    ["--output=%s" % output.path] + includes,
    )

    # The include path is relative to the package's include directory
    include = ctx.attr.out.name
    if include.startswith("include/"):
        include = include[len("include/"):]
    return [ClientgenInfo(service = service, include = include, defs = defs)]

clientgen = rule(
    implementation = _impl,
    attrs = {
        "service": attr.string(mandatory = True),
        "defs": attr.label(allow_single_file = True),
        # The generated code of the services whose types the service uses. The generated
        # code includes it, and is generated from its definitions too.
        "deps": attr.label_list(providers = [ClientgenInfo]),
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
def clientgen_csharp(name, service, defs, out = None, deps = [], visibility = []):
    if out == None:
        out = "Services/%s.cs" % service
    clientgen(
        name = name,
        service = service,
        defs = defs,
        deps = deps,
        out = out,
        language = "csharp",
        visibility = visibility,
    )

# buildifier: disable=function-docstring
def clientgen_cpp(name, service, defs, out = None, deps = []):
    if out == None:
        out = "include/krpc/services/%s.hpp" % _snake_case(service)
    clientgen(
        name = name,
        service = service,
        defs = defs,
        deps = deps,
        out = out,
        language = "cpp",
    )

# buildifier: disable=function-docstring
def clientgen_java(name, service, defs, out = None, deps = []):
    if out == None:
        out = "services/%s.java" % service
    clientgen(
        name = name,
        service = service,
        defs = defs,
        deps = deps,
        out = out,
        language = "java",
    )

# buildifier: disable=function-docstring
def clientgen_cnano(name, service, defs, out = None, deps = []):
    if out == None:
        out = "include/krpc_cnano/services/%s.h" % _snake_case(service)
    clientgen(
        name = name,
        service = service,
        defs = defs,
        deps = deps,
        out = out,
        language = "cnano",
    )

# buildifier: disable=function-docstring
def clientgen_python(name, service, defs, out = None, deps = []):
    if out == None:
        out = "krpc/services/%s.py" % service.lower()
    clientgen(
        name = name,
        service = service,
        defs = defs,
        deps = deps,
        out = out,
        language = "python",
    )

_CLIENTGEN = {
    "cnano": clientgen_cnano,
    "cpp": clientgen_cpp,
    "csharp": clientgen_csharp,
    "java": clientgen_java,
    "python": clientgen_python,
}

def _target(service):
    return ":services-%s" % service.lower()

def clientgen_services(language, outs = {}):
    """Declares a services-<name> target per service in SERVICES, generating its code.

    Args:
        language: the language to generate.
        outs: the output path of a service, by name, where it is not the language's default.

    Returns:
        The targets of the services a release ships, which leaves out the test service.
    """
    for service in SERVICES:
        _CLIENTGEN[language](
            name = _target(service.name)[1:],
            service = service.name,
            defs = service.defs,
            out = outs.get(service.name),
            deps = [_target(dep) for dep in service.deps],
        )
    return [_target(service.name) for service in SERVICES if not service.test]

def clientgen_python_init(name, template, out):
    """Expands the template of the Python services module, which declares every service.

    Args:
        name: the target name.
        template: the template, with placeholders for the generated lines.
        out: the output path.
    """
    imports = ["from krpc.services.%s import %s" % (s.name.lower(), s.name) for s in SERVICES]
    imports = sorted(imports, key = lambda x: x.split(" ")[1])
    expand_template(
        name = name,
        template = template,
        out = out,
        substitutions = {
            "{TYPE_CHECKING_IMPORTS}": "\n    ".join(imports),
            "{IMPORTS}": "\n        ".join(imports),
            "{UNAVAILABLE}": "\n        ".join(["%s = _unavailable" % s.name for s in SERVICES]),
            "{ATTRIBUTES}": "\n        ".join([
                "self.%s: %s = %s(self)" % (_snake_case(s.name), s.name, s.name)
                for s in SERVICES
            ]),
            "{SERVICES}": "\n            ".join([
                '"%s": self.%s,' % (s.name, _snake_case(s.name))
                for s in SERVICES
            ]),
        },
    )
