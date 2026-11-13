" clientgen tool "

load("@bazel_skylib//rules:expand_template.bzl", "expand_template")
load("@bazel_skylib//rules:write_file.bzl", "write_file")
load(":services.bzl", "CONFORMANCE_SERVICES", "SERVICES")

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
    if ctx.attr.client_module:
        includes.append("--client-module=%s" % ctx.attr.client_module)

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
    if language == "js":
        # A JavaScript module imports one beside it by the path of its compiled output
        include = "./%s.js" % include.rpartition("/")[2].removesuffix(".ts")
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
        # The module the generated code imports the client from, where not the client's package
        "client_module": attr.string(),
        "_clientgen": attr.label(
            default = Label("//tools/krpctools:clientgen"),
            executable = True,
            cfg = "exec",
        ),
    },
)

_UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
_LOWER = "abcdefghijklmnopqrstuvwxyz"
_DIGIT = "0123456789"
_COMPOUND_WORDS = ["UInt", "ULong", "SInt", "FoV"]

def _char(name, i):
    return name[i] if i < len(name) else ""

def _skip(name, i, chars):
    """The index past the run of characters from chars at i."""
    for j in range(i, len(name)):
        if name[j] not in chars:
            return j
    return len(name)

def _acronym(name, i):
    """The index past the acronym at i, or None if there is none."""
    stop = _skip(name, i, _UPPER)
    if stop > i and (_char(name, stop) == "" or _char(name, stop) not in _LOWER):
        return stop
    if stop > i + 1:
        return stop - 1
    return None

def _word_digits(name, i):
    """The index past the digits at i, with their lower case letters, that the word before takes."""
    stop = _skip(name, i, _DIGIT)
    if stop > i and _acronym(name, stop) == None:
        return _skip(name, stop, _LOWER)
    return i

def _is_lower(name, i):
    return _char(name, i) != "" and _char(name, i) in _LOWER

def _word_end(name, i):
    """The index past the word at i, by the rules of krpc.utils.words."""
    for word in _COMPOUND_WORDS:
        if name[i:i + len(word)] == word and not _is_lower(name, i + len(word)):
            return _word_digits(name, i + len(word))
    capitals = _skip(name, i, _UPPER)
    if capitals > i + 1 and _char(name, capitals) == "s" and not _is_lower(name, capitals + 1):
        return _word_digits(name, capitals + 1)
    stop = _acronym(name, i)
    if stop != None:
        return _word_digits(name, stop)
    start = i + 1 if name[i] in _UPPER else i
    stop = _skip(name, start, _LOWER)
    if stop > start:
        return _word_digits(name, stop)
    stop = _skip(name, i, _DIGIT)
    if stop > i:
        return _acronym(name, stop) or _skip(name, stop, _LOWER)
    return i + 1

def _words(name):
    """The words of a name with no underscore, as krpc.utils.words gives them."""
    words = []
    i = 0
    for j in range(len(name)):
        if j == i:
            i = _word_end(name, j)
            words.append(name[j:i])
    return words

def snake_case(name):
    """Converts a name to snake case, as krpc.utils.snake_case does."""
    return "__".join(["_".join(_words(part)).lower() for part in name.split("_")])

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
        out = "include/krpc/services/%s.hpp" % snake_case(service)
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
        out = "include/krpc_cnano/services/%s.h" % snake_case(service)
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

# buildifier: disable=function-docstring
def clientgen_js(name, service, defs, out = None, deps = []):
    if out == None:
        out = "src/services/%s.ts" % service
    clientgen(
        name = name,
        service = service,
        defs = defs,
        deps = deps,
        out = out,
        language = "js",
        # A module inside the package imports the client by its path, as the package's own
        # name resolves to a module that imports this one
        client_module = "../declarations.js",
    )

def _lower_camel_case(name):
    """Converts a name to lower camel case, as the JavaScript client names a service."""
    capitals = 0
    for i in range(len(name)):
        if not name[i].isupper():
            break
        capitals = i + 1
    if capitals > 1 and capitals < len(name) and name[capitals].islower():
        capitals -= 1
    return name[:capitals].lower() + name[capitals:]

def clientgen_js_index(name, out):
    """Writes the module of the JavaScript client that names the services a release ships.

    Args:
        name: the name of the target.
        out: the output path of the module.
    """
    services = [service.name for service in SERVICES if not service.test]
    lines = [
        "// The services of a kRPC release, generated from the services clientgen generates.",
        "",
    ]
    lines += ['import type * as %s from "./%s.js";' % (x, x) for x in services]
    lines.append("")
    lines += ['export * as %s from "./%s.js";' % (x, x) for x in services]
    lines += [
        "",
        "/** The services of a connection, each a property in lower camel case. */",
        "export interface Services {",
    ]
    lines += ["  readonly %s: %s.Service;" % (_lower_camel_case(x), x) for x in services]
    lines += ["}", ""]
    write_file(
        name = name,
        out = out,
        content = lines,
    )

_CLIENTGEN = {
    "cnano": clientgen_cnano,
    "cpp": clientgen_cpp,
    "csharp": clientgen_csharp,
    "java": clientgen_java,
    "js": clientgen_js,
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

# The services a conformance testee calls through the generated stubs
_DISPATCH_SERVICES = ["KRPC", "TestService", "ConformanceService"]

# buildifier: disable=unnamed-macro
def clientgen_dispatch(language, out, services_out = None):
    """Declares a conformance-dispatch-<name> target per service a conformance testee calls.

    Each generates the service's dispatch. The services-<name> targets of SERVICES must be
    declared too. Those of CONFORMANCE_SERVICES are declared here.

    Args:
        language: the language to generate.
        out: the output path of a service's dispatch, formatted with the service's name.
        services_out: the output path of a conformance service's code, formatted the same way,
            where it is not the language's default.

    Returns:
        The targets, including the code of the conformance services.
    """
    targets = []
    for service in CONFORMANCE_SERVICES:
        _CLIENTGEN[language](
            name = _target(service.name)[1:],
            service = service.name,
            defs = service.defs,
            out = services_out.format(
                service = service.name,
                snake_service = snake_case(service.name),
            ) if services_out else None,
            deps = [_target(dep) for dep in service.deps],
        )
        targets.append(_target(service.name))
    for service in SERVICES + CONFORMANCE_SERVICES:
        if service.name not in _DISPATCH_SERVICES:
            continue
        target = "conformance-dispatch-%s" % service.name.lower()
        clientgen(
            name = target,
            service = service.name,
            defs = service.defs,
            out = out.format(service = service.name, snake_service = snake_case(service.name)),
            deps = [_target(dep) for dep in service.deps],
            language = language + "-dispatch",
            testonly = True,
        )
        targets.append(":" + target)
    return targets

def clientgen_python_bundle(name, game, template, out):
    """Expands the template of a game's Python package.

    The package combines the client type of each of the core's and the game's services.

    Args:
        name: the target name.
        game: the game, as the services name it.
        template: the template, with placeholders for the generated lines.
        out: the output path.
    """

    # The core's modules are in krpc.services, and the game's in krpc.<game>
    services = [
        (s, "services" if s.game == None else game)
        for s in SERVICES
        if (s.game == None and not s.test) or s.game == game
    ]
    expand_template(
        name = name,
        template = template,
        out = out,
        substitutions = {
            "{IMPORTS}": "\n".join(sorted([
                "from krpc.%s.%s import %sConnection" % (package, s.name.lower(), s.name)
                for s, package in services
            ])),
            "{BASES}": "\n    ".join(["%sConnection," % s.name for s, _ in services]),
        },
    )
