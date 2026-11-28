using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using KRPC.Utils;

namespace KRPC.Tools
{
    // Command line handling shared by the tools, compiled into each of them
    static class ToolCommandLine
    {
        // The informational version, the version people read
        sealed class VersionAction : SynchronousCommandLineAction
        {
            public override int Invoke (ParseResult parseResult)
            {
                Console.WriteLine (Assembly.GetEntryAssembly ().InformationalVersion ());
                return 0;
            }
        }

        public static int Invoke (RootCommand root, string [] args)
        {
            var versionOption = root.Options.OfType<VersionOption> ().Single ();
            versionOption.Aliases.Add ("-v");
            versionOption.Action = new VersionAction ();
            // Arguments starting with @ are values, as no tool reads a response file
            return root.Parse (args, new ParserConfiguration { ResponseFileTokenReplacer = null }).Invoke ();
        }
    }
}
