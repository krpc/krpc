using System;
using System.Linq;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ServiceProblemsTest
    {
        static readonly string [] none = new string [0];
        static readonly ProblemCategory [] noCategories = new ProblemCategory [0];

        [Test]
        public void None ()
        {
            var categories = ServiceProblems.GetCategories (null, none, none, none, noCategories);
            CollectionAssert.IsEmpty (categories);
            Assert.IsNull (ServiceProblems.GetDigest (categories));
        }

        [Test]
        public void Grouped ()
        {
            var categories = ServiceProblems.GetCategories (
                null,
                new [] {
                    "In service B: Procedure B.Foo has id 0",
                    "Services C, c share a name, compared case-insensitively",
                    "In service A, class Vessel: Extension member clashes",
                    "In service B: Members B.X, B.x share the name X, compared case-insensitively"
                },
                new [] { "Ignoring the extension members of Foo, as the class is not public" },
                new [] { "Wind: not installed", "Kerbal Alarm Clock: type KACWrapper is not available" },
                new [] { new ProblemCategory ("Broken by KSP 1.11.2", new [] { "SpaceCenter.Vessel.Name" }), new ProblemCategory ("Empty", none) });
            Assert.AreEqual (new [] {
                "Disabled members of A", "Disabled members of B", "Other errors", "Warnings", "Disabled mod support", "Broken by KSP 1.11.2"
            }, categories.Select (x => x.Title).ToArray ());
            Assert.AreEqual (new [] { "class Vessel: Extension member clashes" }, categories [0].Lines.ToArray ());
            Assert.AreEqual (new [] {
                "Members B.X, B.x share the name X, compared case-insensitively", "Procedure B.Foo has id 0"
            }, categories [1].Lines.ToArray ());
            Assert.AreEqual (new [] {
                "Kerbal Alarm Clock: type KACWrapper is not available", "Wind: not installed"
            }, categories [4].Lines.ToArray ());
        }

        [Test]
        public void Failure ()
        {
            var categories = ServiceProblems.GetCategories (new InvalidOperationException ("KRPC service could not be found"), none, none, none, noCategories);
            Assert.AreEqual (1, categories.Count);
            Assert.AreEqual ("kRPC is disabled, as its services failed to load", categories [0].Title);
            Assert.AreEqual (new [] { "KRPC service could not be found" }, categories [0].Lines.ToArray ());
        }

        [Test]
        public void DigestChangesWithTheProblems ()
        {
            var first = ServiceProblems.GetDigest (ServiceProblems.GetCategories (null, new [] { "In service A: X" }, none, none, noCategories));
            var same = ServiceProblems.GetDigest (ServiceProblems.GetCategories (null, new [] { "In service A: X" }, none, none, noCategories));
            var other = ServiceProblems.GetDigest (ServiceProblems.GetCategories (null, new [] { "In service A: Y" }, none, none, noCategories));
            Assert.AreEqual (64, first.Length);
            Assert.AreEqual (first, same);
            Assert.AreNotEqual (first, other);
        }
    }
}
