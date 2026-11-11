using System;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    enum Color
    {
        Red,
        Green
    }

    enum Shade
    {
        Light,
        Dark
    }

    class Base
    {
#pragma warning disable 0649, 0169, 0414
        int number = 42;
        static string label = "base";
        static int count = 1;
#pragma warning restore 0649, 0169, 0414

        protected Shade shade = Shade.Dark;
        readonly int limit = 3;

        double Ratio { get; set; } = 0.5;

        public int Fixed { get { return 7; } }

        public Base ()
        {
        }

        Base (Shade shade)
        {
            this.shade = shade;
        }

        public Shade Shade { get { return shade; } }

        static bool Ready { get { return true; } }

        protected virtual string Describe ()
        {
            return "base";
        }

        string Scale (float factor)
        {
            return "float " + factor;
        }

        string Scale (int factor)
        {
            return "int " + factor;
        }

        string Pick (int a)
        {
            return "a" + a;
        }

        string Pick (string b)
        {
            return "b" + b;
        }

        string Paint (Shade value)
        {
            return value.ToString ();
        }

        string Maybe (float? value)
        {
            return value.HasValue ? value.Value.ToString (System.Globalization.CultureInfo.InvariantCulture) : "none";
        }

        static void Split (int value, out int half, out int rest)
        {
            half = value / 2;
            rest = value - half;
        }

        public int Number { get { return number; } }

        public int Limit { get { return limit; } }
    }

    sealed class Derived : Base
    {
        protected override string Describe ()
        {
            return "derived";
        }

        public new string Fixed { get { return "derived"; } }
    }

    abstract class Abstract
    {
    }

    delegate void SplitDelegate (int value, out int half, out int rest);

    delegate void SplitLongDelegate (int value, out long half, out long rest);

    [TestFixture]
    public class AccessorTest
    {
        [Test]
        public void GetterReadsPrivateField ()
        {
            var get = Accessor.Getter<Base, int> ("number");
            Assert.AreEqual (42, get (new Base ()));
        }

        [Test]
        public void GetterReadsPrivateProperty ()
        {
            var get = Accessor.Getter<Base, double> ("Ratio");
            Assert.AreEqual (0.5, get (new Base ()));
        }

        [Test]
        public void GetterThroughBaseType ()
        {
            var get = Accessor.Getter<object, int> (typeof (Base), "number");
            Assert.AreEqual (42, get (new Derived ()));
        }

        [Test]
        public void GetterPrivateBaseMember ()
        {
            Assert.AreEqual (42, Accessor.Getter<object, int> (typeof (Derived), "number") (new Derived ()));
            Assert.AreEqual (0.5, Accessor.Getter<Derived, double> ("Ratio") (new Derived ()));
            Assert.AreEqual ("base", Accessor.StaticGetter<string> (typeof (Derived), "label") ());
        }

        [Test]
        public void GetterHiddenProperty ()
        {
            Assert.AreEqual ("derived", Accessor.Getter<Derived, string> ("Fixed") (new Derived ()));
        }

        [Test]
        public void GetterConvertsValue ()
        {
            Assert.AreEqual (42L, Accessor.Getter<Base, long> ("number") (new Base ()));
            Assert.AreEqual (Color.Green, Accessor.Getter<Base, Color> ("shade") (new Base ()));
            Assert.AreEqual (42, Accessor.Getter<Base, object> ("number") (new Base ()));
        }

        [Test]
        public void GetterMissingMember ()
        {
            Assert.IsNull (Accessor.Getter<Base, int> ("missing"));
        }

        [Test]
        public void GetterNoConversion ()
        {
            Assert.IsNull (Accessor.Getter<Base, Base> ("number"));
        }

        [Test]
        public void StaticGetter ()
        {
            Assert.AreEqual ("base", Accessor.StaticGetter<string> (typeof (Base), "label") ());
            Assert.IsTrue (Accessor.StaticGetter<bool> (typeof (Base), "Ready") ());
            Assert.IsNull (Accessor.StaticGetter<int> (typeof (Base), "number"));
        }

        [Test]
        public void StaticSetter ()
        {
            var get = Accessor.StaticGetter<int> (typeof (Base), "count");
            Accessor.StaticSetter<int> (typeof (Base), "count") (5);
            Assert.AreEqual (5, get ());
            Accessor.StaticSetter<int> (typeof (Base), "count") (1);
            Assert.IsNull (Accessor.StaticSetter<bool> (typeof (Base), "Ready"));
            Assert.IsNull (Accessor.StaticSetter<int> (typeof (Base), "missing"));
        }

        [Test]
        public void SetterWritesPrivateField ()
        {
            var obj = new Base ();
            Accessor.Setter<Base, int> ("number") (obj, 7);
            Assert.AreEqual (7, obj.Number);
        }

        [Test]
        public void SetterPrivateBaseField ()
        {
            var obj = new Derived ();
            Accessor.Setter<Derived, int> ("number") (obj, 7);
            Assert.AreEqual (7, obj.Number);
        }

        [Test]
        public void SetterReadOnlyField ()
        {
            Assert.IsNull (Accessor.Setter<Base, int> ("limit"));
        }

        [Test]
        public void SetterConvertsValue ()
        {
            var obj = new Base ();
            Accessor.Setter<Base, Color> (typeof (Base), "shade") (obj, Color.Red);
            Assert.AreEqual (Shade.Light, Accessor.Getter<Base, Shade> ("shade") (obj));
        }

        [Test]
        public void SetterReadOnlyProperty ()
        {
            Assert.IsNull (Accessor.Setter<Base, int> (typeof (Base), "Fixed"));
        }

        [Test]
        public void Field ()
        {
            var obj = new Base ();
            var field = Accessor.Field<Base, double> ("Ratio");
            field.Set (obj, 2);
            Assert.AreEqual (2, field.Get (obj));
        }

        [Test]
        public void FieldReadOnlyProperty ()
        {
            var field = Accessor.Field<Base, int> ("Fixed");
            Assert.AreEqual (7, field.Get (new Base ()));
            Assert.IsNull (field.Set);
        }

        [Test]
        public void FieldReadOnlyField ()
        {
            var field = Accessor.Field<Base, int> ("limit");
            Assert.AreEqual (3, field.Get (new Base ()));
            Assert.IsNull (field.Set);
        }

        [Test]
        public void FieldMissingMember ()
        {
            Assert.IsNull (Accessor.Field<Base, int> ("missing"));
        }

        [Test]
        public void MethodDispatchesVirtually ()
        {
            var describe = Accessor.Method<Func<Base, string>> (typeof (Base), "Describe");
            Assert.AreEqual ("base", describe (new Base ()));
            Assert.AreEqual ("derived", describe (new Derived ()));
        }

        [Test]
        public void MethodOnObjectInstance ()
        {
            var describe = Accessor.Method<Func<object, string>> (typeof (Base), "Describe");
            Assert.AreEqual ("derived", describe (new Derived ()));
        }

        [Test]
        public void MethodPicksExactOverload ()
        {
            Assert.AreEqual ("float 2", Accessor.Method<Func<Base, float, string>> (typeof (Base), "Scale") (new Base (), 2));
            Assert.AreEqual ("int 2", Accessor.Method<Func<Base, int, string>> (typeof (Base), "Scale") (new Base (), 2));
        }

        [Test]
        public void MethodConvertsParameters ()
        {
            var paint = Accessor.Method<Func<object, Color, string>> (typeof (Base), "Paint");
            Assert.AreEqual ("Dark", paint (new Base (), Color.Green));
            var maybe = Accessor.Method<Func<Base, float, string>> (typeof (Base), "Maybe");
            Assert.AreEqual ("1.5", maybe (new Base (), 1.5f));
        }

        [Test]
        public void MethodAmbiguousConversion ()
        {
            Assert.IsNull (Accessor.Method<Func<Base, long, string>> (typeof (Base), "Pick"));
        }

        [Test]
        public void MethodStaticWithOutParameters ()
        {
            var split = Accessor.Method<SplitDelegate> (typeof (Base), "Split");
            int half, rest;
            split (5, out half, out rest);
            Assert.AreEqual (2, half);
            Assert.AreEqual (3, rest);
        }

        [Test]
        public void MethodByRefMismatch ()
        {
            Assert.IsNull (Accessor.Method<SplitLongDelegate> (typeof (Base), "Split"));
        }

        [Test]
        public void MethodNoInstanceConversion ()
        {
            Assert.IsNull (Accessor.Method<Func<int, string>> (typeof (Base), "Describe"));
        }

        [Test]
        public void MethodMissing ()
        {
            Assert.IsNull (Accessor.Method<Action<Base>> (typeof (Base), "Missing"));
            Assert.IsNull (Accessor.Method<Action<Base, int, int>> (typeof (Base), "Describe"));
        }

        [Test]
        public void ConstructorCallsPrivateConstructor ()
        {
            var create = Accessor.Constructor<Func<Shade, Base>> (typeof (Base));
            Assert.AreEqual (Shade.Light, create (Shade.Light).Shade);
        }

        [Test]
        public void ConstructorConvertsParameters ()
        {
            var create = Accessor.Constructor<Func<int, object>> (typeof (Base));
            Assert.AreEqual (Shade.Light, ((Base)create ((int)Shade.Light)).Shade);
        }

        [Test]
        public void ConstructorMissing ()
        {
            Assert.IsNull (Accessor.Constructor<Func<int, int, object>> (typeof (Base)));
            Assert.IsNull (Accessor.Constructor<Func<string, int>> (typeof (Base)));
            Assert.IsNull (Accessor.Constructor<Func<object>> (typeof (Abstract)));
        }
    }
}
