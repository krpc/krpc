" common service build defs "

load("//:config.bzl", "assembly_version", "author")
load("//tools/build:assembly_info.bzl", "csharp_assembly_info")
load("//tools/build:csharp.bzl", "csharp_library")
load("//tools/build:python.bzl", "py_pyright_test", "py_ruff_test")
load("//tools/build/ksp:build.bzl", "ksp_unity_libs")
load("//tools/ServiceDefinitions:build.bzl", "service_definitions")
load(":registry.bzl", "CORE_SERVICES", "SERVICES")

# Default dependencies for service assemblies
service_deps = [
    "//core:KRPC.Core.net472",
    "//server:KRPC",
    "//tools/build/ksp:Google.Protobuf",
] + ksp_unity_libs

def _service_info_impl(ctx):
    content = [
        "namespace %s" % ctx.attr.namespace,
        "{",
        "    static class ServiceInfo",
        "    {",
        "        public const uint Id = %d;" % ctx.attr.id,
        "    }",
        "}",
    ]
    ctx.actions.write(output = ctx.outputs.out, content = "\n".join(content) + "\n")

service_info = rule(
    doc = "Generates the ServiceInfo.cs of a service, holding its id from the registry.",
    implementation = _service_info_impl,
    attrs = {
        "id": attr.int(mandatory = True),
        "namespace": attr.string(mandatory = True),
    },
    outputs = {"out": "%{name}.cs"},
)

def _entry(field, value):
    for service in SERVICES:
        if getattr(service, field) == value:
            return service
    fail("No service with %s %s in SERVICES in //service:registry.bzl" % (field, value))

def registry_entry(package):
    """The SERVICES entry of the service in a directory under service/.

    Args:
      package: The directory's name.

    Returns:
      The entry.
    """
    return _entry("package", package)

def service_libraries(deps):
    """The assemblies of the services a service depends on.

    Args:
      deps: The services, by name. Those of the core are left out.

    Returns:
      The labels of the assemblies.
    """
    libraries = []
    for dep in deps:
        if dep not in CORE_SERVICES:
            package = _entry("name", dep).package
            libraries.append("//service/%s:KRPC.%s" % (package, package))
    return libraries

def service_targets(target = None):
    """A target of every service's package, such as its test suite.

    Args:
      target: The target's name, or None for the package's default target.

    Returns:
      The labels.
    """
    return [
        "//service/%s%s" % (service.package, ":" + target if target else "")
        for service in SERVICES
    ]

def service_zip_path_map():
    """The path map of the services' files in the mod archive.

    Returns:
      The archive path of each path prefix.
    """
    path_map = {}
    for service in SERVICES:
        package = "service/%s/" % service.package
        path_map[package] = "GameData/kRPC/"
        path_map[package + "KRPC.%s/net472/" % service.package] = "GameData/kRPC/"
        path_map[package + "CHANGELOG.md"] = "GameData/kRPC/CHANGELOG.%s.md" % service.package
        path_map[package + "LICENSE"] = "LICENSE.KRPC." + service.package
    return path_map

def ksp_service(
        name,
        copyright = author,
        license = False,
        data = [],
        internals_visible_to = [],
        lint_size = "small"):
    """Declares the targets of a KSP service, built from its package.

    The service's description and dependencies are its entry in SERVICES.

    Args:
      name: The name of the service's package, which is also the name of its KRPC.<name>
        assembly.
      copyright: The assembly copyright.
      license: Whether the package has a LICENSE file to ship with the service.
      data: Other files the mod archive ships, from the package or another repository.
      internals_visible_to: Assemblies that can see the service's internal members.
      lint_size: The size of the ruff test.
    """
    service = registry_entry(name)
    assembly = "KRPC." + name
    dep_libs = service_libraries(service.deps)

    native.exports_files(["CHANGELOG.md"])

    native.filegroup(
        name = name,
        srcs = ["CHANGELOG.md"] + (["LICENSE"] if license else []) + data + [
            ":" + assembly,
            ":ServiceDefinitions",
        ],
        visibility = ["//:__pkg__"],
    )

    csharp_assembly_info(
        name = "AssemblyInfo",
        cls_compliant = False,
        copyright = copyright,
        description = service.description,
        title = assembly,
        version = assembly_version,
        visibility = [
            "//:__pkg__",  # Make visible to //:csproj-deps so it can copy AssemblyInfo.cs to generated_deps
        ],
    )

    service_info(
        name = "ServiceInfo",
        id = service.id,
        namespace = assembly,
    )

    csharp_library(
        name = assembly,
        srcs = [":AssemblyInfo", ":ServiceInfo"] + native.glob(["src/**/*.cs"]),
        internals_visible_to = internals_visible_to,
        target_frameworks = ["net472"],
        treat_warnings_as_errors = True,
        visibility = ["//visibility:public"],
        warning_level = 4,
        deps = service_deps + dep_libs,
    )

    service_definitions(
        name = "ServiceDefinitions",
        out = assembly + ".json",
        assemblies = [":" + assembly] + dep_libs + ["//server:KRPC"],
        service = service.name,
        visibility = ["//visibility:public"],
    )

    native.test_suite(
        name = "test",
        tests = [":lint"],
    )

    native.test_suite(
        name = "lint",
        tests = [
            ":pyright",
            ":ruff",
        ],
    )

    py_ruff_test(
        name = "ruff",
        size = lint_size,
        srcs = native.glob(["test/*.py"]),
    )

    py_pyright_test(
        name = "pyright",
        size = "medium",
        srcs = native.glob(["test/*.py"]),
        # The tests do not check for None results
        disable = ["reportOptionalMemberAccess"],
        deps = [
            "//client/python:krpc_lib",
            "//tools/krpctools:krpctools_lib",
        ],
    )

    # Sources for //:csproj-test, which checks them against the .csproj file lists.
    native.filegroup(
        name = "csproj-srcs",
        srcs = native.glob([
            "**/*.cs",
            "**/*.csproj",
        ]),
        visibility = [
            "//:__pkg__",
            "//service:__pkg__",
        ],
    )
