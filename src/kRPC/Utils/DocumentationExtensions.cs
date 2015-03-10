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
            var query = "string(/doc/members/member[@name='" + GetMemberElementName (member) + "']/summary)";
            return xml == null ? "" : xml.XPathEvaluate (query).ToString ().Trim ();
        }

        static String GetMemberElementName (MemberInfo member)
        {
            char prefixCode;
            string memberName = member is Type ? ((Type)member).FullName : (member.DeclaringType.FullName + "." + member.Name);

            switch (member.MemberType) {

            case MemberTypes.Constructor:
                memberName = memberName.Replace (".ctor", "#ctor");
                goto case MemberTypes.Method;

            case MemberTypes.Method:
                prefixCode = 'M';
                string paramTypesList = String.Join (",", ((MethodBase)member).GetParameters ().Select (x => x.ParameterType.FullName).ToArray ());
                if (!String.IsNullOrEmpty (paramTypesList))
                    memberName += "(" + paramTypesList + ")";
                break;

            case MemberTypes.Event:
                prefixCode = 'E';
                break;

            case MemberTypes.Field:
                prefixCode = 'F';
                break;

            case MemberTypes.NestedType:
                memberName = memberName.Replace ('+', '.');
                goto case MemberTypes.TypeInfo;

            case MemberTypes.TypeInfo:
                prefixCode = 'T';
                break;

            case MemberTypes.Property:
                prefixCode = 'P';
                break;

            default:
                throw new ArgumentException ("Unknown member type", "member");
            }
            return prefixCode + ":" + memberName;
        }
    }
}
