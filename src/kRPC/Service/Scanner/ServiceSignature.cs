using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    class ServiceSignature
    {
        public string Name { get; private set; }

        public HashSet<string> Classes { get; private set; }

        public Dictionary<string,ProcedureSignature> Procedures { get; private set; }

        public ServiceSignature (Type type)
        {
            Name = type.Name;

            // Get all KRPC Classes
            Classes = new HashSet<string> ();
            var classes = Reflection.GetClassesWith<KRPCClass> (type);
            foreach (var cls in classes) {
                Classes.Add (cls.Name);
            }

            // Get all KRPC Procedures
            var procedures = new List<ProcedureSignature> ();
            procedures.AddRange (Reflection.GetMethodsWith<KRPCProcedure> (type)
                .Select (x => new ProcedureSignature (type.Name, x.Name, new ProcedureHandler (x))));

            // Get all property-defined KRPC Procedures
            procedures.AddRange (Reflection.GetPropertiesWith<KRPCProperty> (type)
                .SelectMany (x => {
                var accessors = new List<ProcedureSignature> ();
                if (x.GetGetMethod () != null) {
                    var method = x.GetGetMethod ();
                    var handler = new ProcedureHandler (method);
                    var attribute = "Property.Get(" + x.Name + ")";
                    accessors.Add (new ProcedureSignature (type.Name, method.Name, handler, attribute));
                }
                if (x.GetSetMethod () != null) {
                    var method = x.GetSetMethod ();
                    var handler = new ProcedureHandler (method);
                    var attribute = "Property.Set(" + x.Name + ")";
                    accessors.Add (new ProcedureSignature (type.Name, method.Name, handler, attribute));
                }
                return accessors;
            }));

            // Get all class-method-defined KRPC Procedures
            classes = Reflection.GetClassesWith<KRPCClass> (type);
            foreach (var cls in classes) {
                var classMethods = Reflection.GetMethodsWith<KRPCMethod> (cls);
                foreach (var method in classMethods) {
                    var handler = new ClassMethodHandler (method);
                    procedures.Add (new ProcedureSignature (type.Name, cls.Name + '_' + method.Name, handler,
                        "Class.Method(" + Name + "." + cls.Name + "," + method.Name + ")", "ParameterType(0).Class(" + Name + "." + cls.Name + ")"));
                }
            }

            // Get all class-property-defined KRPC Procedures
            classes = Reflection.GetClassesWith<KRPCClass> (type);
            foreach (var cls in classes) {
                procedures.AddRange (Reflection.GetPropertiesWith<KRPCProperty> (cls)
                    .SelectMany (x => {
                    var accessors = new List<ProcedureSignature> ();
                    if (x.CanRead) {
                        var method = x.GetGetMethod ();
                        var handler = new ClassMethodHandler (method);
                        var attribute = "Class.Property.Get(" + Name + "." + cls.Name + "," + x.Name + ")";
                        accessors.Add (new ProcedureSignature (type.Name, cls.Name + '_' + method.Name, handler, attribute));
                    }
                    if (x.CanWrite) {
                        var method = x.GetSetMethod ();
                        var handler = new ClassMethodHandler (method);
                        var attribute = "Class.Property.Set(" + Name + "." + cls.Name + "," + x.Name + ")";
                        accessors.Add (new ProcedureSignature (type.Name, cls.Name + '_' + method.Name, handler, attribute));
                    }
                    return accessors;
                }));
            }

            try {
                Procedures = procedures.ToDictionary (x => x.Name);
            } catch (ArgumentException) {
                // Handle procedure name clashes
                var duplicates = procedures
                                .Select (x => x.Name)
                                .Duplicates ()
                                .ToArray ();
                throw new ServiceException (
                    "Service " + Name + " contains duplicate Procedures, " +
                    "and overloading is not permitted. " +
                    "Duplicates are " + String.Join (", ", duplicates));
            }
        }
    }
}
