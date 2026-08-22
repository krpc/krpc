using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    /// <summary>
    /// Extension members that are not valid. The class is internal, so the scanner skips
    /// it. Each member is validated on its own.
    /// </summary>
    internal static class InvalidExtensions
    {
        [KRPCMethod]
        public static string MethodWithoutThis (TestService.TestClass obj)
        {
            return obj.Value;
        }

        [KRPCMethod]
        public static string MethodExtendingAValueType (this string value)
        {
            return value;
        }

        [KRPCMethod]
        public static string MethodExtendingAStruct (this TestService.TestStruct value)
        {
            return value.StringField;
        }

        [KRPCMethod]
        public static string lowerCaseMethod (this TestService.TestClass obj)
        {
            return obj.Value;
        }

        /// <summary>
        /// Clashes with the TestClass method of the same name.
        /// </summary>
        [KRPCMethod]
        public static string FloatToString (this TestService.TestClass obj, float x)
        {
            return obj.Value + x;
        }

        [KRPCProperty]
        public static string PropertyWithoutAnAccessorPrefix (this TestService.TestClass obj)
        {
            return obj.Value;
        }

        [KRPCProperty]
        public static string GetPropertyWithTooManyParameters (this TestService.TestClass obj, int x)
        {
            return obj.Value + x;
        }

        [KRPCProperty]
        public static void GetPropertyWithNoReturnValue (this TestService.TestClass obj)
        {
        }

        [KRPCProperty]
        public static string SetPropertyWithAReturnValue (this TestService.TestClass obj, string value)
        {
            return obj.Value + value;
        }

        [KRPCProperty]
        public static void SetPropertyWithNoValue (this TestService.TestClass obj)
        {
        }

        /// <summary>
        /// Adds a setter to a property that TestClass declares read-only.
        /// </summary>
        [KRPCProperty]
        public static void SetClassPropertyAvailableInInheritedGameScene (
            this TestService.TestClass obj, string value)
        {
        }
    }
}
