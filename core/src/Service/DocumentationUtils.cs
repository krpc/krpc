using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service
{
    static class DocumentationUtils
    {
        public static string ResolveCrefs (string documentation)
        {
            if (documentation.Length == 0)
                return string.Empty;
            return Transform (documentation, ResolveCref);
        }

        /// <summary>
        /// Resolve crefs in a deprecation reason (an [Obsolete] attribute message).
        /// The message is an XML text fragment. Unlike doc comments, the compiler does not
        /// resolve crefs in attribute strings, so unqualified references are permitted and
        /// resolved against the declaring type: a member name, a Type.Member pair, or a type
        /// name (in addition to the fully-qualified compiler form). Messages containing no
        /// markup are returned unchanged.
        /// </summary>
        public static string ResolveDeprecationReason (string reason, Type context)
        {
            if (reason.Length == 0 || reason.IndexOf ('<') < 0)
                return reason;
            string result;
            try {
                result = Transform ("<doc>" + reason + "</doc>", cref => ResolveAuthorCref (cref, context));
            } catch (XmlException exn) {
                throw new DocumentationException (
                    "Failed to parse deprecation reason '" + reason + "' as XML: " + exn.Message);
            }
            // Strip the <doc> wrapper element
            return result.Substring (5, result.Length - 11);
        }

        static string Transform (string documentation, Func<string, string> resolveCref)
        {
            var output = new StringBuilder ();
            using (XmlReader reader = XmlReader.Create (new StringReader (documentation))) {
                var ws = new XmlWriterSettings ();
                ws.OmitXmlDeclaration = true;
                ws.NewLineChars = "\n";
                using (XmlWriter writer = XmlWriter.Create (output, ws)) {
                    while (reader.Read ()) {
                        switch (reader.NodeType) {
                        case XmlNodeType.Element:
                            writer.WriteStartElement (reader.Name);
                            bool shortTag = reader.IsEmptyElement;
                            for (int i = 0; i < reader.AttributeCount; i++) {
                                reader.MoveToAttribute (i);
                                var name = reader.Name;
                                var value = reader.Value;
                                if (name == "cref")
                                    value = resolveCref (value);
                                writer.WriteStartAttribute (name);
                                writer.WriteValue (value);
                                writer.WriteEndAttribute ();
                            }
                            if (shortTag)
                                writer.WriteEndElement ();
                            break;
                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                            writer.WriteString (reader.Value);
                            break;
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                            writer.WriteProcessingInstruction (reader.Name, reader.Value);
                            break;
                        case XmlNodeType.Comment:
                            writer.WriteComment (reader.Value);
                            break;
                        case XmlNodeType.EndElement:
                            writer.WriteFullEndElement ();
                            break;
                        default:
                            throw new InvalidOperationException ("Unhandled");
                        }
                    }
                }
            }
            return output.ToString ();
        }

        static string ResolveCref (string cref)
        {
            if (cref.Length <= 3 || cref [1] != ':')
                throw new DocumentationException ("Invalid cref '" + cref + "'");
            var code = cref [0];
            var reference = cref.Substring (2);
            if (code == 'T')
                return ResolveTypeCref (reference);
            if (code == 'M')
                return ResolveMethodCref (reference);
            if (code == 'P')
                return ResolvePropertyCref (reference);
            if (code == 'F')
                return ResolveFieldCref (reference);
            throw new DocumentationException ("Invalid code '" + code + "' in cref '" + cref + "'");
        }

        static string ResolveAuthorCref (string cref, Type context)
        {
            // Fully-qualified compiler form, e.g. "M:Full.Name.Of.Type.Member"
            if (cref.Length > 2 && cref [1] == ':')
                return ResolveCref (cref);
            if (cref.Length == 0 || context == null)
                throw new DocumentationException ("Failed to resolve cref '" + cref + "'");
            var dot = cref.LastIndexOf ('.');
            string result = null;
            if (dot < 0) {
                // A member of the declaring type, or a type name
                result = TryResolveMember (context, cref) ?? TryResolveTypeCref (context, cref);
            } else {
                // Type.Member, or a (partially) qualified type name
                var type = FindType (context, cref.Substring (0, dot));
                if (type != null)
                    result = TryResolveMember (type, cref.Substring (dot + 1));
                if (result == null)
                    result = TryResolveTypeCref (context, cref);
            }
            if (result == null)
                throw new DocumentationException (
                    "Failed to resolve cref '" + cref + "' in deprecation reason on '" + context.Name + "'");
            return result;
        }

        static string TryResolveMember (Type type, string name)
        {
            var property = type.GetProperty (name);
            if (property != null && Reflection.HasAttribute<KRPCPropertyAttribute> (property))
                return FormatPropertyCref (type, property);
            MethodInfo method;
            try {
                method = type.GetMethod (name);
            } catch (AmbiguousMatchException) {
                method = type.GetMethods ().FirstOrDefault (
                    m => m.Name == name &&
                    (Reflection.HasAttribute<KRPCProcedureAttribute> (m) ||
                     Reflection.HasAttribute<KRPCMethodAttribute> (m)));
            }
            if (method != null &&
                (Reflection.HasAttribute<KRPCProcedureAttribute> (method) ||
                 Reflection.HasAttribute<KRPCMethodAttribute> (method)))
                return FormatMethodCref (type, method);
            var field = type.GetField (name);
            if (field != null && Reflection.HasAttribute<KRPCEnumAttribute> (type))
                return FormatFieldCref (type, field);
            return null;
        }

        static string TryResolveTypeCref (Type context, string name)
        {
            var type = FindType (context, name);
            return type == null ? null : FormatTypeCref (type);
        }

        /// <summary>
        /// Find a KRPC-attributed type by (possibly partially qualified) name. Matches the
        /// context type itself, then types in the context's assembly by simple name, then by
        /// dotted full-name suffix, then a fully-qualified name in any loaded assembly.
        /// Throws if the name is ambiguous within the context's assembly.
        /// </summary>
        static Type FindType (Type context, string name)
        {
            if (context.Name == name)
                return context;
            var candidates = context.Assembly.GetTypes ()
                .Where (t => Reflection.HasAttribute<KRPCServiceAttribute> (t) ||
                        Reflection.HasAttribute<KRPCClassAttribute> (t) ||
                        Reflection.HasAttribute<KRPCEnumAttribute> (t))
                .Where (t => t.Name == name ||
                        (t.FullName != null && t.FullName.Replace ('+', '.').EndsWith ("." + name, StringComparison.Ordinal)))
                .ToList ();
            if (candidates.Count > 1)
                throw new DocumentationException (
                    "Ambiguous cref '" + name + "': matches " +
                    string.Join (", ", candidates.Select (t => t.FullName).ToArray ()));
            if (candidates.Count == 1)
                return candidates [0];
            return Reflection.GetType (name);
        }

        static string ResolveTypeCref (string reference)
        {
            return FormatTypeCref (GetType (reference));
        }

        static string FormatTypeCref (Type type)
        {
            var name = type.Name;
            if (Reflection.HasAttribute<KRPCServiceAttribute> (type)) {
                TypeUtils.ValidateKRPCService (type);
                return "T:" + TypeUtils.GetServiceName (type);
            } else if (Reflection.HasAttribute<KRPCClassAttribute> (type)) {
                TypeUtils.ValidateKRPCClass (type);
                return "T:" + TypeUtils.GetClassServiceName (type) + "." + name;
            } else if (Reflection.HasAttribute<KRPCEnumAttribute> (type)) {
                TypeUtils.ValidateKRPCEnum (type);
                return "T:" + TypeUtils.GetEnumServiceName (type) + "." + name;
            }
            throw new DocumentationException ("Type '" + name + "' is not a kRPC service, class or enumeration");
        }

        static string ResolveMethodCref (string reference)
        {
            reference = reference.Split (new char[]{'('}) [0];
            var type = GetType (GetTypeName (reference));
            var method = GetMethod (type, GetMemberName (reference));
            return FormatMethodCref (type, method);
        }

        static string FormatMethodCref (Type type, MethodInfo method)
        {
            var name = type.Name + "." + method.Name;
            if (Reflection.HasAttribute<KRPCProcedureAttribute> (method)) {
                TypeUtils.ValidateKRPCProcedure (method);
                return "M:" + name;
            } else if (Reflection.HasAttribute<KRPCMethodAttribute> (method)) {
                TypeUtils.ValidateKRPCMethod (type, method);
                return "M:" + TypeUtils.GetClassServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC procedure or method");
        }

        static string ResolvePropertyCref (string reference)
        {
            var type = GetType (GetTypeName (reference));
            var property = GetProperty (type, GetMemberName (reference));
            return FormatPropertyCref (type, property);
        }

        static string FormatPropertyCref (Type type, PropertyInfo property)
        {
            var name = type.Name + "." + property.Name;
            if (Reflection.HasAttribute<KRPCServiceAttribute> (type) &&
                Reflection.HasAttribute<KRPCPropertyAttribute> (property)) {
                TypeUtils.ValidateKRPCProperty (property);
                return "M:" + name;
            } else if (Reflection.HasAttribute<KRPCClassAttribute> (type) &&
                       Reflection.HasAttribute<KRPCPropertyAttribute> (property)) {
                TypeUtils.ValidateKRPCClassProperty (type, property);
                return "M:" + TypeUtils.GetClassServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC property");
        }

        static string ResolveFieldCref (string reference)
        {
            var type = GetType (GetTypeName (reference));
            var field = GetField (type, GetMemberName (reference));
            return FormatFieldCref (type, field);
        }

        static string FormatFieldCref (Type type, FieldInfo field)
        {
            var name = type.Name + "." + field.Name;
            if (Reflection.HasAttribute<KRPCEnumAttribute> (type)) {
                TypeUtils.ValidateKRPCEnum (type);
                return "M:" + TypeUtils.GetEnumServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC enumeration value");
        }

        static string GetTypeName (string reference)
        {
            var parts = reference.Split (new char[]{'.'}).ToList ();
            parts.RemoveAt (parts.Count - 1);
            return string.Join (".", parts.ToArray ());
        }

        static string GetMemberName (string reference)
        {
            var parts = reference.Split (new char[]{'.'}).ToList ();
            return parts [parts.Count - 1];
        }

        static Type GetType (string name)
        {
            var type = Reflection.GetType (name);
            if (type == null)
                throw new DocumentationException ("Type '" + name + "' not found");
            return type;
        }

        static MethodInfo GetMethod (Type type, string name)
        {
            var method = type.GetMethod (name);
            if (method == null)
                throw new DocumentationException ("Method '" + type.Name + "." + name + "' not found");
            return method;
        }

        static PropertyInfo GetProperty (Type type, string name)
        {
            var property = type.GetProperty (name);
            if (property == null)
                throw new DocumentationException ("Property '" + type.Name + "." + name + "' not found");
            return property;
        }

        static FieldInfo GetField (Type type, string name)
        {
            var field = type.GetField (name);
            if (field == null)
                throw new DocumentationException ("Field '" + type.Name + "." + name + "' not found");
            return field;
        }
    }
}
