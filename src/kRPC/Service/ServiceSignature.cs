using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Utils;

namespace KRPC.Service
{
    class ServiceSignature
    {
        public string Name { get; private set; }

        public Dictionary<string,ProcedureSignature> Procedures { get; private set; }

        public ServiceSignature (Type type)
        {
            Name = type.Name;
            var procedures = Reflection.GetMethodsWith<KRPCProcedure> (type);
            var properties = Reflection.GetPropertiesWith<KRPCProperty> (type).SelectMany (x => x.GetAccessors ());
            var procedureTypes = procedures.ToList ();
            procedureTypes.AddRange (properties);
            try {
                Procedures = procedureTypes
                    .Select (x => {
                    if (x.Name.StartsWith ("get_") || x.Name.StartsWith ("set_")) {
                        string attribute = (x.Name.StartsWith ("get_") ? "Get" : "Set");
                        attribute = "Property." + attribute + "(" + x.Name.Split ('_') [1] + ")";
                        return new ProcedureSignature (type.Name, x, attribute);
                    } else {
                        return new ProcedureSignature (type.Name, x);
                    }
                })
                    .ToDictionary (x => x.Name);
            } catch (ArgumentException) {
                // Handle procedure name clashes
                // TODO: move into Utils
                var duplicates = procedureTypes
                    .Select (x => x.Name)
                    .GroupBy (x => x)
                    .Where (group => group.Count () > 1)
                    .Select (group => group.Key)
                    .ToArray ();
                throw new ServiceException (
                    "Service " + Name + " contains duplicate Procedures, " +
                    "and overloading is not permitted. " +
                    "Duplicates are " + String.Join (", ", duplicates));
            }
            if (Procedures.Count == 0)
                throw new ServiceException ("Service " + Name + " does not contain any Procedures");
        }
    }
}
