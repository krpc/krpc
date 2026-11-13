using System;
using System.Collections.Generic;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class MemberBinderTest
    {
        Action<string, Logger.Severity> writer;
        List<string> warnings;

        [SetUp]
        public void SetUp ()
        {
            writer = Logger.Writer;
            warnings = new List<string> ();
            Logger.Writer = (message, severity) => {
                if (severity == Logger.Severity.Warning)
                    warnings.Add (message);
            };
        }

        [TearDown]
        public void TearDown ()
        {
            Logger.Writer = writer;
        }

        [Test]
        public void FoundMembers ()
        {
            var bind = new MemberBinder ("Mod", typeof (Base));
            var obj = new Base ();
            Assert.AreEqual (42, bind.Getter<Base, int> ("number") (obj));
            Assert.AreEqual ("base", bind.StaticGetter<string> ("label") ());
            bind.Setter<Base, int> ("number") (obj, 7);
            Assert.AreEqual (7, obj.Number);
            var field = bind.Field<Base, double> ("Ratio");
            field.Set (obj, 2);
            Assert.AreEqual (2, field.Get (obj));
            Assert.AreEqual ("base", bind.Method<Func<Base, string>> ("Describe") (obj));
            CollectionAssert.IsEmpty (warnings);
        }

        [Test]
        public void MissingMembers ()
        {
            var bind = new MemberBinder ("Mod", typeof (Base));
            var get = bind.Getter<Base, int> ("missing");
            var error = Assert.Throws<ModMemberMissingException> (() => get (new Base ()));
            Assert.AreEqual ("Mod: Base.missing is not available in the installed version", error.Message);
            Assert.IsTrue (Accessor.IsMissing (get));
            Assert.IsTrue (Accessor.IsMissing (bind.StaticGetter<string> ("missing")));
            Assert.IsTrue (Accessor.IsMissing (bind.Setter<Base, int> ("missing")));
            Assert.IsTrue (Accessor.IsMissing (bind.Method<Func<Base, string>> ("Missing")));
            CollectionAssert.AreEqual (new [] {
                "Mod: Base.missing is not available in the installed version",
                "Mod: Base.missing is not available in the installed version",
                "Mod: Base.missing is not available in the installed version",
                "Mod: Base.Missing is not available in the installed version"
            }, warnings.ConvertAll (x => x.Substring (x.IndexOf ("Mod:", StringComparison.Ordinal))));
        }

        [Test]
        public void MissingField ()
        {
            var field = new MemberBinder ("Mod", typeof (Base)).Field<Base, int> ("missing");
            Assert.Throws<ModMemberMissingException> (() => field.Get (new Base ()));
            Assert.Throws<ModMemberMissingException> (() => field.Set (new Base (), 1));
            Assert.AreEqual (1, warnings.Count);
        }

        [Test]
        public void ReadOnlyField ()
        {
            var field = new MemberBinder ("Mod", typeof (Base)).Field<Base, int> ("limit");
            Assert.AreEqual (1, warnings.Count);
            Assert.AreEqual (3, field.Get (new Base ()));
            Assert.Throws<ModMemberMissingException> (() => field.Set (new Base (), 1));
        }

        [Test]
        public void RequiredReadOnlyField ()
        {
            var bind = new MemberBinder ("Mod", typeof (Base)).Require;
            var error = Assert.Throws<ModMemberMissingException> (() => bind.Field<Base, int> ("limit"));
            Assert.AreEqual ("Base.limit", error.Member);
        }

        [Test]
        public void NullType ()
        {
            var bind = new MemberBinder ("Mod", (Type)null);
            var get = bind.Getter<object, int> ("number");
            var error = Assert.Throws<ModMemberMissingException> (() => get (new Base ()));
            Assert.AreEqual ("Mod: number is not available in the installed version", error.Message);
            Assert.IsTrue (Accessor.IsMissing (bind.Method<Func<object, string>> ("Describe")));
            Assert.IsNull (bind.Type);
            CollectionAssert.IsEmpty (warnings);
        }

        [Test]
        public void MissingType ()
        {
            var bind = new MemberBinder ("Mod", "Gone");
            var get = bind.Getter<object, int> ("number");
            var error = Assert.Throws<ModMemberMissingException> (() => get (new Base ()));
            Assert.AreEqual ("Mod: Gone.number is not available in the installed version", error.Message);
            var create = bind.Constructor<Func<object>> ();
            error = Assert.Throws<ModMemberMissingException> (() => create ());
            Assert.AreEqual ("Mod: Gone constructor is not available in the installed version", error.Message);
            Assert.IsNull (bind.Type);
            CollectionAssert.IsEmpty (warnings);
        }

        [Test]
        public void Constructor ()
        {
            var bind = new MemberBinder ("Mod", typeof (Base));
            Assert.AreEqual (typeof (Base), bind.Type);
            Assert.IsInstanceOf<Base> (bind.Constructor<Func<object>> () ());
            Assert.IsTrue (Accessor.IsMissing (bind.Constructor<Func<string, string, object>> ()));
            Assert.AreEqual (1, warnings.Count);
        }

        [Test]
        public void MissingMemberException ()
        {
            var get = new MemberBinder ("Mod", typeof (Base)).Getter<Base, int> ("missing");
            var error = Assert.Throws<ModMemberMissingException> (() => get (new Base ()));
            Assert.AreEqual ("Mod", error.Mod);
            Assert.AreEqual ("Base.missing", error.Member);
        }

        [Test]
        public void Require ()
        {
            var bind = new MemberBinder ("Mod", typeof (Base)).Require;
            Assert.AreEqual (42, bind.Getter<Base, int> ("number") (new Base ()));
            var error = Assert.Throws<ModMemberMissingException> (() => bind.Getter<Base, int> ("missing"));
            Assert.AreEqual ("Base.missing", error.Member);
            CollectionAssert.IsEmpty (warnings);
        }

        [Test]
        public void For ()
        {
            var bind = new MemberBinder ("Mod", (Type)null).For (typeof (Base));
            Assert.AreEqual (42, bind.Getter<Base, int> ("number") (new Base ()));
        }
    }
}
