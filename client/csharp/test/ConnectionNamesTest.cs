using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class ConnectionNamesTest
    {
        // The members clientgen escapes a service accessor against, in
        // tools/krpctools/krpctools/lang/csharp.py
        static readonly string[] connectionMembers = {
            "ConnectLocal", "Dispose", "GetHashCode", "GetType", "ToString"
        };

        [Test]
        public void MembersCallableWithNoArguments ()
        {
            // A call of a service's extension method with no arguments calls such a member
            // instead
            var names = typeof(Connection)
                .GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where (method => method.GetParameters ().All (parameter => parameter.IsOptional))
                .Select (method => method.Name)
                .Distinct ()
                .OrderBy (name => name, System.StringComparer.Ordinal);
            CollectionAssert.AreEqual (connectionMembers, names);
        }
    }
}
