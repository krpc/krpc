using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A field of a part module. Obtained by calling <see cref="Module.FieldList"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class PartField : Equatable<PartField>
    {
        readonly BaseField baseField;

        internal PartField (Module module, BaseField partField)
        {
            Module = module;
            baseField = partField;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (PartField other)
        {
            return !ReferenceEquals (other, null) && Module == other.Module && ReferenceEquals (baseField, other.baseField);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Module.GetHashCode () ^ baseField.GetHashCode ();
        }

        /// <summary>
        /// The part module that contains this field.
        /// </summary>
        [KRPCProperty]
        public Module Module { get; private set; }

        /// <summary>
        /// The identifier of the field. This is stable and does not change between game versions,
        /// unlike <see cref="GuiName"/>.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return baseField.name; }
        }

        /// <summary>
        /// The name of the field, as displayed in the right-click menu of the part. This may be
        /// empty for fields that are not visible in the menu.
        /// </summary>
        [KRPCProperty]
        public string GuiName {
            get { return baseField.guiName; }
        }

        /// <summary>
        /// Whether the field is visible in the right-click menu of the part, in the current scene
        /// (flight or editor).
        /// </summary>
        [KRPCProperty]
        public bool Visible {
            get { return HighLogic.LoadedSceneIsEditor ? baseField.guiActiveEditor : baseField.guiActive; }
        }

        /// <summary>
        /// The type of the field.
        /// </summary>
        [KRPCProperty]
        public FieldType Type {
            get { return TypeOf (baseField.FieldInfo.FieldType); }
        }

        static FieldType TypeOf (Type type)
        {
            if (type == typeof (bool))
                return FieldType.Boolean;
            if (type == typeof (float))
                return FieldType.Float;
            if (type == typeof (double))
                return FieldType.Double;
            if (type == typeof (string))
                return FieldType.String;
            if (type.IsEnum ||
                type == typeof (int) || type == typeof (short) || type == typeof (long) ||
                type == typeof (byte) || type == typeof (sbyte) ||
                type == typeof (uint) || type == typeof (ushort) || type == typeof (ulong))
                return FieldType.Integer;
            return FieldType.Unknown;
        }

        /// <summary>
        /// BaseField.SetValue fails silently if the value being set is of the wrong type.
        /// (It only outputs a message to the debug log, it does not throw on error.)
        /// This method first checks that the type of the field is assignable from the type
        /// of the value being set, and throws an exception if not.
        /// </summary>
        void Assign (object value)
        {
            var type = baseField.FieldInfo.FieldType;
            if (!type.IsAssignableFrom (value.GetType ()))
                throw new ArgumentException (
                    "Cannot set field with type " + type + " to a value of type " + value.GetType ());
            baseField.SetValue (value, Module.InternalModule);
        }

        void CheckType (FieldType expected)
        {
            if (Type != expected)
                throw new InvalidOperationException (
                    "Field " + baseField.name + " has type " + Type + ", not " + expected);
        }

        /// <summary>
        /// The value of the field, as a string. This works for fields of any type, and returns the
        /// same string that is shown in the right-click menu of the part.
        /// </summary>
        /// <remarks>
        /// Setting the value using this property is only permitted for string fields. Use the typed
        /// properties (<see cref="BoolValue"/>, <see cref="IntValue"/>, <see cref="FloatValue"/>,
        /// <see cref="DoubleValue"/>) to set fields of other types.
        /// </remarks>
        [KRPCProperty]
        public string Value {
            get { return baseField.GetValue (Module.InternalModule).ToString (); }
            set { Assign (value); }
        }

        /// <summary>
        /// The value of a boolean field.
        /// </summary>
        /// <remarks>
        /// The getter throws an exception if the field is not a boolean field
        /// (see <see cref="PartField.Type"/>).
        /// </remarks>
        [KRPCProperty]
        public bool BoolValue {
            get {
                CheckType (FieldType.Boolean);
                return Convert.ToBoolean (baseField.GetValue (Module.InternalModule));
            }
            set { Assign (value); }
        }

        /// <summary>
        /// The value of an integer field.
        /// </summary>
        /// <remarks>
        /// The getter throws an exception if the field is not an integer field
        /// (see <see cref="PartField.Type"/>).
        /// </remarks>
        [KRPCProperty]
        public int IntValue {
            get {
                CheckType (FieldType.Integer);
                return Convert.ToInt32 (baseField.GetValue (Module.InternalModule));
            }
            set { Assign (value); }
        }

        /// <summary>
        /// The value of a single precision floating point field.
        /// </summary>
        /// <remarks>
        /// The getter throws an exception if the field is not a single precision floating point
        /// field (see <see cref="PartField.Type"/>).
        /// </remarks>
        [KRPCProperty]
        public float FloatValue {
            get {
                CheckType (FieldType.Float);
                return Convert.ToSingle (baseField.GetValue (Module.InternalModule));
            }
            set { Assign (value); }
        }

        /// <summary>
        /// The value of a double precision floating point field.
        /// </summary>
        /// <remarks>
        /// The getter throws an exception if the field is not a double precision floating point
        /// field (see <see cref="PartField.Type"/>).
        /// </remarks>
        [KRPCProperty]
        public double DoubleValue {
            get {
                CheckType (FieldType.Double);
                return Convert.ToDouble (baseField.GetValue (Module.InternalModule));
            }
            set { Assign (value); }
        }

        /// <summary>
        /// Set the value of the field to its original value.
        /// </summary>
        /// <remarks>
        /// The original value is the value the field had when the part was loaded.
        /// Works for any field, including those not visible in the right-click menu.
        /// </remarks>
        [KRPCMethod]
        public void Reset ()
        {
            Module.RestoreOriginalFieldValue (baseField);
        }
    }
}
