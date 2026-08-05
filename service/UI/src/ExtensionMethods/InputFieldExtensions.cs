using System;

namespace KRPC.UI.ExtensionMethods
{
    /// <summary>
    /// Extension methods for input field enumerations.
    /// </summary>
    public static class InputFieldExtensions
    {
        /// <summary>
        /// Convert a Unity engine input field content type to a kRPC one.
        /// </summary>
        public static InputContentType ToInputContentType (
            this UnityEngine.UI.InputField.ContentType type)
        {
            switch (type) {
            case UnityEngine.UI.InputField.ContentType.Standard:
                return InputContentType.Standard;
            case UnityEngine.UI.InputField.ContentType.IntegerNumber:
                return InputContentType.Integer;
            case UnityEngine.UI.InputField.ContentType.DecimalNumber:
                return InputContentType.Decimal;
            case UnityEngine.UI.InputField.ContentType.Alphanumeric:
                return InputContentType.Alphanumeric;
            case UnityEngine.UI.InputField.ContentType.Password:
                return InputContentType.Password;
            default:
                throw new ArgumentOutOfRangeException ("type");
            }
        }

        /// <summary>
        /// Convert a kRPC input field content type to a Unity engine one.
        /// </summary>
        public static UnityEngine.UI.InputField.ContentType FromInputContentType (
            this InputContentType type)
        {
            switch (type) {
            case InputContentType.Standard:
                return UnityEngine.UI.InputField.ContentType.Standard;
            case InputContentType.Integer:
                return UnityEngine.UI.InputField.ContentType.IntegerNumber;
            case InputContentType.Decimal:
                return UnityEngine.UI.InputField.ContentType.DecimalNumber;
            case InputContentType.Alphanumeric:
                return UnityEngine.UI.InputField.ContentType.Alphanumeric;
            case InputContentType.Password:
                return UnityEngine.UI.InputField.ContentType.Password;
            default:
                throw new ArgumentOutOfRangeException ("type");
            }
        }
    }
}
