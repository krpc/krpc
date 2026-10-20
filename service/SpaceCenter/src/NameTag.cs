using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Access to a part's name tag. The name tag part module is provided by another mod:
    /// <c>ModuleNameTag</c> from KSPCommunityPartModules 0.5 or later, or <c>KOSNameTag</c>
    /// from kOS. Both store the tag in a field called <c>nameTag</c>.
    /// </summary>
    static class NameTag
    {
        public const string ModuleName = "ModuleNameTag";
        public const string LegacyModuleName = "KOSNameTag";
        public const string FieldName = "nameTag";

        /// <summary>
        /// The name tag part module of the part, or null if it has none.
        /// </summary>
        public static PartModule Module (Part part)
        {
            return part.Module (ModuleName) ?? part.Module (LegacyModuleName);
        }

        public static string Get (PartModule module)
        {
            return (string)module.GetType ().GetField (FieldName).GetValue (module);
        }

        public static void Set (PartModule module, string value)
        {
            module.GetType ().GetField (FieldName).SetValue (module, value);
        }
    }
}
