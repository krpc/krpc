using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service
{
    static class DocumentationUtils
    {
        [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
        public static string ResolveCrefs (string documentation)
        {
            if (documentation.Length == 0)
                return String.Empty;
            var xml = XDocument.Parse (documentation);
            foreach (var node in xml.Descendants())
                foreach (var attr in node.Attributes())
                    if (attr.Name == "cref")
                        attr.SetValue (ResolveCref (attr.Value));
            return xml.ToString ().Replace ("\r\n", "\n");
        }

        static string ResolveCref (string cref)
        {
            if (cref.Length <= 3 || cref [1] != ':')
                throw new DocumentationException ("Invalid cref '" + cref + "'");
            var code = cref [0];
            var reference = cref.Substring (2);
            if (code == 'T')
                return ResolveTypeCref (reference);
            else if (code == 'M')
                return ResolveMethodCref (reference);
            else if (code == 'P')
                return ResolvePropertyCref (reference);
            else if (code == 'F')
                return ResolveFieldCref (reference);
            throw new DocumentationException ("Invalid code '" + code + "' in cref '" + cref + "'");
        }

        static string ResolveTypeCref (string reference)
        {
            var type = GetType (reference);
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
            reference = reference.Split ('(') [0];
            var type = GetType (GetTypeName (reference));
            var method = GetMethod (type, GetMemberName (reference));
            var name = type.Name + "." + method.Name;
            if (Reflection.HasAttribute<KRPCProcedureAttribute> (method)) {
                TypeUtils.ValidateKRPCProcedure (method);
                return "M:" + name;
            } else if (Reflection.HasAttribute<KRPCMethodAttribute> (method)) {
                TypeUtils.ValidateKRPCMethod (method);
                return "M:" + TypeUtils.GetClassServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC procedure or method");
        }

        static string ResolvePropertyCref (string reference)
        {
            var type = GetType (GetTypeName (reference));
            var property = GetProperty (type, GetMemberName (reference));
            var name = type.Name + "." + property.Name;
            if (Reflection.HasAttribute<KRPCServiceAttribute> (type) &&
                Reflection.HasAttribute<KRPCPropertyAttribute> (property)) {
                TypeUtils.ValidateKRPCProperty (property);
                return "M:" + name;
            } else if (Reflection.HasAttribute<KRPCClassAttribute> (type) &&
                       Reflection.HasAttribute<KRPCPropertyAttribute> (property)) {
                TypeUtils.ValidateKRPCClassProperty (property);
                return "M:" + TypeUtils.GetClassServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC property");
        }

        static string ResolveFieldCref (string reference)
        {
            var type = GetType (GetTypeName (reference));
            var field = GetField (type, GetMemberName (reference));
            var name = type.Name + "." + field.Name;
            if (Reflection.HasAttribute<KRPCEnumAttribute> (type)) {
                TypeUtils.ValidateKRPCEnum (type);
                return "M:" + TypeUtils.GetEnumServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC enumeration value");
        }

        static string GetTypeName (string reference)
        {
            var parts = reference.Split ('.').ToList ();
            parts.RemoveAt (parts.Count - 1);
            return String.Join (".", parts.ToArray ());
        }

        static string GetMemberName (string reference)
        {
            var parts = reference.Split ('.').ToList ();
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
