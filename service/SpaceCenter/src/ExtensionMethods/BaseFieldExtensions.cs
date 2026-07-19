using System.Globalization;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class BaseFieldExtensions
    {
        /// <summary>
        /// Get the value of a part module field as a string.
        /// </summary>
        /// <remarks>
        /// The value is formatted using the invariant culture, so that a numeric field is
        /// rendered the same way whatever locale the game is running under. Clients parse these
        /// values, so a decimal comma would make them unreadable.
        /// </remarks>
        public static string GetValueString (this BaseField field, PartModule module)
        {
            if (field == null)
                return null;
            return System.Convert.ToString (field.GetValue (module), CultureInfo.InvariantCulture);
        }
    }
}
