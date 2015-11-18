package(default_visibility = ["//visibility:public"])

filegroup(
    name = "protoc",
    srcs = select({
        "//third_party:windows_mingw": ["protoc-windows-x86_32.exe"],
        "//third_party:windows_msys64_mingw64": ["protoc-windows-x86_64.exe"],
        "//third_party:windows_clang": ["protoc-windows-x86_32.exe"],
        "//third_party:darwin": ["protoc-osx-x86_32.exe"],
        "//third_party:k8": ["protoc-linux-x86_64.exe"],
        "//third_party:piii": ["protoc-linux-x86_32.exe"],
        "//third_party:arm": ["protoc-linux-arm32.exe"],
        "//third_party:freebsd": ["protoc-linux-x86_32.exe"],
    }),
)

java_import(
    name = "protobuf",
    jars = ["protobuf/protobuf-java-3.0.0-alpha-3.jar"],
)

# For bootstrapping JavaBuilder
filegroup(
    name = "protobuf-jars",
    srcs = ["protobuf/protobuf-java-3.0.0-alpha-3.jar"],
)
