using System;
using KRPC.Service.Attributes;

namespace KRPC.UI
{
    /// <summary>
    /// What an input field lets the user type into it.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "UI")]
    public enum InputContentType
    {
        /// <summary>
        /// Any text.
        /// </summary>
        Standard,
        /// <summary>
        /// A whole number: digits, with a leading minus sign.
        /// </summary>
        Integer,
        /// <summary>
        /// A number: digits, with a decimal separator and a leading minus sign.
        /// </summary>
        Decimal,
        /// <summary>
        /// Letters and digits.
        /// </summary>
        Alphanumeric,
        /// <summary>
        /// Any text, drawn as asterisks so that it cannot be read off the screen.
        /// </summary>
        Password
    }
}
