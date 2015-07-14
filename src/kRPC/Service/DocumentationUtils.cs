using System;
using System.Xml.Linq;
using KRPC.Utils;
using KRPC.Service.Attributes;
using System.Linq;

namespace KRPC.Service
{
    static class DocumentationUtils
    {
        static string ResolveCref (string cref)
        {
            if (cref.Length < 3 || cref [1] != ':')
                return cref;
            var code = cref [0];
            var typeString = cref.Substring (2);
            try {
                if (code == 'T') {
                    var type = Reflection.GetType (typeString);
                    if (type == null)
                        return cref;
                    if (Reflection.HasAttribute<KRPCServiceAttribute> (type)) {
                        TypeUtils.ValidateKRPCService (type);
                        return TypeUtils.GetServiceName (type);
                    } else if (Reflection.HasAttribute<KRPCClassAttribute> (type)) {
                        TypeUtils.ValidateKRPCClass (type);
                        return TypeUtils.GetClassServiceName (type) + "." + type.Name;
                    } else if (Reflection.HasAttribute<KRPCEnumAttribute> (type)) {
                        TypeUtils.ValidateKRPCEnum (type);
                        return TypeUtils.GetEnumServiceName (type) + "." + type.Name;
                    }
                } else if (code == 'M') {
                    typeString = typeString.Split ('(') [0];
                    var parts = typeString.Split ('.').ToList ();
                    var methodName = parts.Last ();
                    parts.RemoveAt (parts.Count - 1);
                    var typeName = String.Join (".", parts.ToArray ());
                    var type = Reflection.GetType (typeName);
                    if (type == null)
                        return cref;
                    var method = type.GetMethod (methodName);
                    if (Reflection.HasAttribute<KRPCProcedureAttribute> (method)) {
                        TypeUtils.ValidateKRPCProcedure (method);
                        return type.Name + "." + methodName;
                    } else if (Reflection.HasAttribute<KRPCMethodAttribute> (method)) {
                        TypeUtils.ValidateKRPCMethod (method);
                        return TypeUtils.GetClassServiceName (type) + "." + type.Name + "." + methodName;
                    }
                } else if (code == 'P') {
                    var parts = typeString.Split ('.').ToList ();
                    var propertyName = parts.Last ();
                    parts.RemoveAt (parts.Count - 1);
                    var typeName = String.Join (".", parts.ToArray ());
                    var type = Reflection.GetType (typeName);
                    if (type == null)
                        return cref;
                    var property = type.GetProperty (propertyName);
                    if (Reflection.HasAttribute<KRPCServiceAttribute> (type) &&
                        Reflection.HasAttribute<KRPCPropertyAttribute> (property)) {
                        TypeUtils.ValidateKRPCProperty (property);
                        return type.Name + "." + propertyName;
                    } else if (Reflection.HasAttribute<KRPCClassAttribute> (type) &&
                               Reflection.HasAttribute<KRPCPropertyAttribute> (property)) {
                        TypeUtils.ValidateKRPCClassProperty (property);
                        return TypeUtils.GetClassServiceName (type) + "." + type.Name + "." + propertyName;
                    }
                } else if (code == 'F') {
                    var parts = typeString.Split ('.').ToList ();
                    var fieldName = parts.Last ();
                    parts.RemoveAt (parts.Count - 1);
                    var typeName = String.Join (".", parts.ToArray ());
                    var type = Reflection.GetType (typeName);
                    if (type == null)
                        return cref;
                    if (type.GetField (fieldName) == null)
                        return cref;
                    if (Reflection.HasAttribute<KRPCEnumAttribute> (type)) {
                        TypeUtils.ValidateKRPCEnum (type);
                        return TypeUtils.GetEnumServiceName (type) + "." + type.Name + "." + fieldName;
                    }
                }
            } catch (ServiceException) {
            }
            return cref;
        }

        internal static string ResolveCrefs (string documentation)
        {
            if (documentation == "")
                return "";
            var xml = XDocument.Parse (documentation);
            foreach (var node in xml.Descendants()) {
                foreach (var attr in node.Attributes()) {
                    if (attr.Name == "cref") {
                        attr.SetValue (ResolveCref (attr.Value));
                    }
                }
            }
            return xml.ToString ().Replace ("\r\n", "\n");
        }
    }
}
