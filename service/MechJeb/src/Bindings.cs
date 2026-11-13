using System;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    // Binds to the members of MechJeb by name. MechJeb has no public API, so we bind to the
    // members of its 2.15.4.0 release. A missing member throws an exception when used, so the
    // rest of the service still works.
    sealed class Bindings : ModAPI
    {
        static ModBinder mod;
        static Field<object, double> editableDouble;
        static Field<object, int> editableInt;

        public Bindings () : base (ServiceInfo.Mods.MechJeb.Name, ServiceInfo.Mods.MechJeb.Assembly)
        {
        }

        /// <summary>
        /// Bind the members shared by the rest of the bindings, then the rest.
        /// </summary>
        protected override void Bind (ModBinder binder)
        {
            mod = binder;
            editableDouble = Type ("MuMech.EditableDoubleMult").Field<object, double> ("Val");
            editableInt = Type ("MuMech.EditableInt").Field<object, int> ("Val");
            MechJeb.Load ();
        }

        /// <summary>
        /// A binder for a type of MechJeb.
        /// </summary>
        public static MemberBinder Type (string name)
        {
            return mod.Type (name);
        }

        /// <summary>
        /// The value of an EditableDouble, or a type derived from it, held in a field of the type.
        /// We read the field on each access, as MechJeb replaces some of these objects.
        /// </summary>
        public static Field<object, double> EditableDouble (MemberBinder type, string name)
        {
            return Editable (type, name, editableDouble);
        }

        /// <summary>
        /// The value of an EditableInt held in a field of the type.
        /// </summary>
        public static Field<object, int> EditableInt (MemberBinder type, string name)
        {
            return Editable (type, name, editableInt);
        }

        static Field<object, T> Editable<T> (MemberBinder type, string name, Field<object, T> value)
        {
            var field = type.Getter<object, object> (name);
            return new Field<object, T> (
                instance => value.Get (field (instance)),
                (instance, x) => value.Set (field (instance), x));
        }
    }
}
