using System.Runtime.CompilerServices;

// For the Visual Studio solution / dotnet build. The Bazel build injects this
// via rules_dotnet's internals_visible_to attr instead, so this file lives
// outside src/ (which Bazel globs) to avoid a duplicate attribute.
[assembly: InternalsVisibleTo("KRPC.Core.Test")]
[assembly: InternalsVisibleTo("TestingTools")]
// The Benchmark service runs a call through Services, the server's own dispatch path
[assembly: InternalsVisibleTo("KRPC.Benchmark")]
