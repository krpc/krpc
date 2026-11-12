" common service build defs "

load("//:config.bzl", "assembly_version", "author")
load("//tools/build:assembly_info.bzl", "csharp_assembly_info")
load("//tools/build:csharp.bzl", "csharp_library")
load("//tools/build:python.bzl", "py_pyright_test", "py_ruff_test")
load("//tools/build/ksp:build.bzl", "ksp_unity_libs")
load("//tools/ServiceDefinitions:build.bzl", "service_definitions")

# Default dependencies for service assemblies
service_deps = [
    "//core:KRPC.Core.net472",
    "//server:KRPC",
    "//tools/build/ksp:Google.Protobuf",
] + ksp_unity_libs

def ksp_service(
        name,
        description,
        deps = [],
        copyright = author,
        license = False,
        data = [],
        internals_visible_to = [],
        lint_size = "small"):
    """Declares the targets of a KSP service, built from its package.

    Args:
      name: The service's name, which is also the name of its package and of its KRPC.<name>
        assembly.
      description: The assembly description.
      deps: The services whose assemblies this service uses, by name.
      copyright: The assembly copyright.
      license: Whether the package has a LICENSE file to ship with the service.
      data: Other files the mod archive ships, from the package or another repository.
      internals_visible_to: Assemblies that can see the service's internal members.
      lint_size: The size of the ruff test.
    """
    assembly = "KRPC." + name
    dep_libs = ["//service/%s:KRPC.%s" % (dep, dep) for dep in deps]

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
        description = description,
        title = assembly,
        version = assembly_version,
        visibility = [
            "//:__pkg__",  # Make visible to //:csproj-deps so it can copy AssemblyInfo.cs to generated_deps
        ],
    )

    csharp_library(
        name = assembly,
        srcs = [":AssemblyInfo"] + native.glob(["src/**/*.cs"]),
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
        service = name,
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
        visibility = ["//:__pkg__"],
    )
