using System.Runtime.CompilerServices;

// For the Visual Studio solution and the dotnet build. The Bazel build injects the
// attribute through rules_dotnet, so this file sits outside the src/ directory that
// Bazel globs.
[assembly: InternalsVisibleTo("KRPC.Core.Test")]
[assembly: InternalsVisibleTo("TestingTools")]
// The Benchmark service runs a call through Services, the server's own dispatch path
[assembly: InternalsVisibleTo("KRPC.Benchmark")]
