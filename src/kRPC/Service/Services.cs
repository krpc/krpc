using System;
using System.IO;
using System.Reflection;
using Google.ProtocolBuffers;
using KRPC.Schema.RPC;

namespace KRPC.Service
{
    class Services
    {
        public static Response.Builder HandleRequest (Assembly assembly, string ns, Request request)
        {
            var serviceType = GetServiceType (assembly, ns + "." + request.Service);
            MethodInfo handler = GetServiceMethod(serviceType, request.Method);

            object[] parameters = { request.Request_ };
            if (handler.GetParameters().Length == 0)
                parameters = null;

            // Invoke the handler
            object result = handler.Invoke (null, parameters);

            // Process the result
            if (result != null) {
                byte[] resultBytes;
                using (MemoryStream stream = new MemoryStream ()) {
                    ((IMessage)result).WriteTo (stream);
                    resultBytes = stream.ToArray ();
                }
                return Response.CreateBuilder ()
                    .SetResponse_ (ByteString.CopyFrom (resultBytes))
                    .SetError (false);
            }
            return Response.CreateBuilder()
                .SetError(false);
        }

        private static Type GetServiceType(Assembly assembly, string name) {
            Type serviceType = assembly.GetType (name);
            if (serviceType == null)
                throw new NoSuchServiceException (name);
            return serviceType;
        }

        private static MethodInfo GetServiceMethod(Type service, string name) {
            MethodInfo method = service.GetMethod (name, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new NoSuchServiceMethodException (service, name);
            if (method.GetCustomAttributes(typeof(Service.KRPCMethod), false).Length == 0)
                throw new NoSuchServiceMethodException (service, name);
            return method;
        }
    }
}

