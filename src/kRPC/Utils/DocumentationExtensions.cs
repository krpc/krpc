using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;

namespace KRPC.Utils
{
    static class DocumentationExtensions
    {
        static IDictionary<string, XDocument> documentationFiles = new Dictionary<string, XDocument> ();

        internal static String GetDocumentation (this MemberInfo member)
        {
            var location = member.Module.Assembly.Location;
            var path = Path.GetDirectoryName (location) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (location) + ".xml";
            if (!documentationFiles.ContainsKey (path)) {
                if (!File.Exists (path)) {
                    Logger.WriteLine ("No documentation found for " + member.Module.Assembly.Location + " at " + path);
                    documentationFiles [path] = null;
                } else {
                    Logger.WriteLine ("Loading documentation for " + member.Module.Assembly.Location + " from " + path);
                    documentationFiles [path] = XDocument.Load (path);
                }
            }
            var xml = documentationFiles [path];
            var query = "string(/doc/members/member[@name='" + GetDocumentationName (member) + "']/summary)";
            return xml == null ? "" : xml.XPathEvaluate (query).ToString ().Trim ();
        }

        internal static string GetDocumentationName (MemberInfo member)
        {
            char prefix = '?';
            string name = member is Type ? ((Type)member).FullName : (member.DeclaringType.FullName + "." + member.Name);
            name = name.Replace ('+', '.');

            switch (member.MemberType) {

            case MemberTypes.NestedType:
            case MemberTypes.TypeInfo:
                prefix = 'T';
                break;

            case MemberTypes.Method:
                prefix = 'M';
                var parameters = String.Join (",", ((MethodBase)member).GetParameters ().Select (x => ParameterName (x)).ToArray ());
                if (!String.IsNullOrEmpty (parameters))
                    name += "(" + parameters + ")";
                break;

            case MemberTypes.Property:
                prefix = 'P';
                break;

            default:
                throw new ArgumentException ("Unknown member type");
            }
            return prefix + ":" + name;
        }

        static string TypeName (Type type)
        {
            var name = type.FullName;
            name = name.Replace('+', '.');
            if (!type.IsGenericType)
                return name;
            name = name.Remove (name.IndexOf ('`'));
            var typeArguments = "{" + String.Join (",", type.GetGenericArguments ().Select (x => TypeName (x)).ToArray ()) + "}";
            return name + typeArguments;
        }

        static string ParameterName (ParameterInfo parameter)
        {
            return TypeName (parameter.ParameterType);
        }
    }
}
