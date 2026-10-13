" C# rules that run the analyzers set in //tools/build/dotnet:analyzers.globalconfig "

load(
    "@rules_dotnet//dotnet:defs.bzl",
    _csharp_binary = "csharp_binary",
    _csharp_library = "csharp_library",
    _csharp_nunit_test = "csharp_nunit_test",
)

def _with_analyzers(kwargs):
    kwargs["deps"] = kwargs.get("deps", []) + ["//tools/build/dotnet:NetAnalyzers"]
    kwargs["analyzer_configs"] = ["//tools/build/dotnet:analyzers.globalconfig"]
    return kwargs

def csharp_library(**kwargs):
    _csharp_library(**_with_analyzers(kwargs))

def csharp_binary(**kwargs):
    _csharp_binary(**_with_analyzers(kwargs))

def csharp_nunit_test(**kwargs):
    _csharp_nunit_test(**_with_analyzers(kwargs))
