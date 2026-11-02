using System;
using System.Collections.Generic;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

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

        // Keyed by module type, as the field is declared by whichever mod provides the module
        static readonly Dictionary<Type, Field<PartModule, string>> fields =
            new Dictionary<Type, Field<PartModule, string>> ();

        /// <summary>
        /// The name tag part module of the part, or null if it has none.
        /// </summary>
        public static PartModule Module (Part part)
        {
            return part.Module (ModuleName) ?? part.Module (LegacyModuleName);
        }

        public static string Get (PartModule module)
        {
            return Tag (module).Get (module);
        }

        public static void Set (PartModule module, string value)
        {
            Tag (module).Set (module, value);
        }

        static Field<PartModule, string> Tag (PartModule module)
        {
            return fields.GetOrAdd (module.GetType (), type => Accessor.Field<PartModule, string> (type, FieldName));
        }
    }
}
