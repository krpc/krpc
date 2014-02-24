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
            var procedureTypes = Reflection.GetMethodsWith<KRPCProcedure> (type);
            try {
                Procedures = procedureTypes
                    .Select (x => new ProcedureSignature (type.Name, x))
                    .ToDictionary (x => x.Name);
            } catch (ArgumentException) {
                // Handle procedure name clashes
                var duplicates = procedureTypes
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
