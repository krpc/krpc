using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class BrokenProceduresTest
    {
        [Test]
        public void NoneWhenEveryMemberResolves ()
        {
            // The test services follow into their own code, generic and iterator methods
            // included, and each member they use is present
            CollectionAssert.IsEmpty (BrokenProcedures.Find ());
        }

        [Test]
        public void MissingMember ()
        {
            // The fixture is built against version 1 of the library, and loaded with version 2,
            // which removes a method it calls
            var directory = Path.Combine (Environment.GetEnvironmentVariable ("TEST_SRCDIR"), "_main", "core");
            var library = Path.Combine (directory, "BrokenProceduresLib.v2", "net10.0", "BrokenProceduresLib.dll");
            var context = new AssemblyLoadContext ("BrokenProcedures", true);
            context.Resolving += (loadContext, name) => name.Name == "BrokenProceduresLib" ? loadContext.LoadFromAssemblyPath (library) : null;
            var fixture = context.LoadFromAssemblyPath (Path.Combine (directory, "BrokenProceduresFixture", "net10.0", "BrokenProceduresFixture.dll"));
            var type = fixture.GetType ("BrokenProceduresFixture.Procedures");
            var procedures = new [] { "Direct", "Indirect", "InAnIterator", "Kept" }
                .Select (x => new KeyValuePair<string, MethodBase> (x, type.GetMethod (x)));
            var broken = BrokenProcedures.Find (procedures, new [] { fixture });
            Assert.AreEqual (new [] { "Direct", "InAnIterator", "Indirect" }, broken.Select (x => x.Key).ToArray ());
            foreach (var procedure in broken)
                StringAssert.Contains ("BrokenProceduresLib.Lib.Removed", procedure.Value);
            StringAssert.Contains ("(in BrokenProceduresFixture.Procedures.Helper)", broken [2].Value);
        }
    }
}
