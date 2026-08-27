using System;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace KRPC.Test.Service
{
    /// <summary>
    /// Members added to classes belonging to other services.
    /// </summary>
    public static class TestServiceExtensions
    {
        /// <summary>
        /// Extension method documentation string.
        /// </summary>
        [KRPCMethod]
        public static string ExtensionMethod (this TestService.TestClass obj, int x)
        {
            return obj.Value + x;
        }

        [KRPCMethod (Nullable = true)]
        public static TestService.TestClass ExtensionNullableMethod (
            this TestService.TestClass obj, [KRPCNullable] TestService.TestClass other)
        {
            return other;
        }

        [KRPCMethod (GameScene = GameScene.EditorVAB)]
        public static string ExtensionMethodAvailableInSpecifiedGameScene (this TestService.TestClass obj)
        {
            return obj.Value;
        }

        [Obsolete ("Use ExtensionMethod instead.")]
        [KRPCMethod]
        public static string DeprecatedExtensionMethod (this TestService.TestClass obj)
        {
            return obj.Value;
        }

        [KRPCMethod]
        public static TestClass3 ExtensionMethodReturningClassFromOtherService (this TestService.TestClass obj)
        {
            return new TestClass3 ();
        }

        /// <summary>
        /// Extension property documentation string.
        /// </summary>
        [KRPCProperty]
        public static string GetExtensionProperty (this TestService.TestClass obj)
        {
            return obj.Value;
        }

        [KRPCProperty]
        public static int GetExtensionReadWriteProperty (this TestService.TestClass obj)
        {
            return obj.IntProperty;
        }

        [KRPCProperty]
        public static void SetExtensionReadWriteProperty (this TestService.TestClass obj, int value)
        {
            obj.IntProperty = value;
        }

        [KRPCProperty (Nullable = true)]
        public static TestService.TestClass GetExtensionNullableProperty (this TestService.TestClass obj)
        {
            return obj.ObjectProperty;
        }

        [KRPCProperty]
        public static void SetExtensionNullableProperty (
            this TestService.TestClass obj, [KRPCNullable] TestService.TestClass value)
        {
            obj.ObjectProperty = value;
        }

        [KRPCMethod]
        public static string ExtensionMethodOnOtherServicesClass (this TestClass3 obj)
        {
            return obj == null ? string.Empty : "jeb";
        }
    }
}
