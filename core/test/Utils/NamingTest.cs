using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class NamingTest
    {
        // The test vectors of the name case conversion specification, from the runfiles
        static IEnumerable<TestCaseData> Vectors ()
        {
            var path = Path.Combine (
                Environment.GetEnvironmentVariable ("TEST_SRCDIR"), "_main", "client", "conformance", "names.json");
            using (var document = JsonDocument.Parse (File.ReadAllText (path))) {
                foreach (var vector in document.RootElement.EnumerateArray ()) {
                    var name = vector.GetProperty ("name").GetString ();
                    yield return new TestCaseData (
                        name,
                        vector.GetProperty ("snake").GetString (),
                        vector.GetProperty ("upper_snake").GetString ()).SetName (name);
                }
            }
        }

        [TestCaseSource (nameof (Vectors))]
        public void Convert (string name, string snake, string upperSnake)
        {
            Assert.AreEqual (snake, Naming.SnakeCase (name));
            Assert.AreEqual (upperSnake, Naming.UpperSnakeCase (name));
        }
    }
}
