using System;
using System.Linq;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    static class Scanner
    {
        public static IDictionary<string, ServiceSignature> GetServices ()
        {
            IDictionary<string, ServiceSignature> signatures;
            var serviceTypes = Reflection.GetTypesWith<KRPCService> ();
            try {
                signatures = serviceTypes
                    .Select (x => new ServiceSignature (x))
                    .ToDictionary (x => x.Name);
            } catch (ArgumentException) {
                // Handle service name clashes
                var duplicates = serviceTypes
                    .Select (x => x.Name)
                    .Duplicates ()
                    .ToArray ();
                throw new ServiceException (
                    "Multiple Services have the same name. " +
                    "Duplicates are " + String.Join (", ", duplicates));
            }
            // Check that the main KRPC service was found
            if (!signatures.ContainsKey ("KRPC"))
                throw new ServiceException ("KRPC service could not be found");
            return signatures;
        }
    }
}

