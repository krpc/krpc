using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    sealed class ServiceSignature : ISerializable
    {
        /// <summary>
        /// The name of the service
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Documentation for the service
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// A mapping from procedure names to signatures for all RPCs in this service
        /// </summary>
        public Dictionary<string,ProcedureSignature> Procedures { get; private set; }

        /// <summary>
        /// The names of all classes defined in this service
        /// </summary>
        public Dictionary<string,ClassSignature> Classes { get; private set; }

        /// <summary>
        /// The names of all C# defined enums defined in this service, and their allowed values
        /// </summary>
        public Dictionary<string,EnumerationSignature> Enumerations { get; private set; }

        /// <summary>
        /// Which game scene(s) the service should be active during
        /// </summary>
        public GameScene GameScene { get; private set; }

        /// <summary>
        /// Create a service signature from a C# type annotated with the KRPCService attribute
        /// </summary>
        /// <param name="type">Type.</param>
        public ServiceSignature (Type type)
        {
            TypeUtils.ValidateKRPCService (type);
            Name = TypeUtils.GetServiceName (type);
            Documentation = DocumentationUtils.ResolveCrefs (type.GetDocumentation ());
            Classes = new Dictionary<string, ClassSignature> ();
            Enumerations = new Dictionary<string, EnumerationSignature> ();
            Procedures = new Dictionary<string, ProcedureSignature> ();
            GameScene = TypeUtils.GetServiceGameScene (type);
        }

        /// <summary>
        /// Create a service with the given name.
        /// </summary>
        public ServiceSignature (string name)
        {
            Name = name;
            Documentation = string.Empty;
            Classes = new Dictionary<string, ClassSignature> ();
            Enumerations = new Dictionary<string, EnumerationSignature> ();
            Procedures = new Dictionary<string, ProcedureSignature> ();
            GameScene = GameScene.All;
        }

        /// <summary>
        /// Add a procedure to the service
        /// </summary>
        void AddProcedure (ProcedureSignature signature)
        {
            if (Procedures.ContainsKey (signature.Name))
                throw new ServiceException ("Service " + Name + " contains duplicate procedures " + signature.Name);
            Procedures [signature.Name] = signature;
        }

        /// <summary>
        /// Add a procedure to the service for the given method annotated with the KRPCProcedure attribute.
        /// </summary>
        public void AddProcedure (MethodInfo method)
        {
            TypeUtils.ValidateKRPCProcedure (method);
            AddProcedure (new ProcedureSignature (Name, method.Name, method.GetDocumentation (), new ProcedureHandler (method), GameScene));
        }

        /// <summary>
        /// Add a property to the service for the given property annotated with the KRPCProperty attribute.
        /// </summary>
        public void AddProperty (PropertyInfo property)
        {
            TypeUtils.ValidateKRPCProperty (property);
            var name = property.Name;
            var getter = property.GetGetMethod ();
            var setter = property.GetSetMethod ();
            if (getter != null)
                AddPropertyMethod (property, getter, "Property.Get(" + name + ")");
            if (setter != null) {
                AddPropertyMethod (property, setter, "Property.Set(" + name + ")");
            }
        }

        void AddPropertyMethod (MemberInfo property, MethodInfo method, string attribute)
        {
            var handler = new ProcedureHandler (method);
            AddProcedure (new ProcedureSignature (Name, method.Name, property.GetDocumentation (), handler, GameScene, attribute));
        }

        /// <summary>
        /// Add a class to the service for the given class type annotated with the KRPCClass attribute.
        /// Returns the name of the class.
        /// </summary>
        public string AddClass (Type classType)
        {
            TypeUtils.ValidateKRPCClass (classType);
            var name = classType.Name;
            if (Classes.ContainsKey (name))
                throw new ServiceException ("Service " + Name + " contains duplicate classes " + name);
            Classes [name] = new ClassSignature (Name, name, classType.GetDocumentation ());
            return name;
        }

        /// <summary>
        /// Add an enum to the service for the given enum type annotated with the KRPCEnum attribute.
        /// Returns the name of the enumeration.
        /// </summary>
        public string AddEnum (Type enumType)
        {
            TypeUtils.ValidateKRPCEnum (enumType);
            var name = enumType.Name;
            if (Enumerations.ContainsKey (name))
                throw new ServiceException ("Service " + Name + " contains duplicate enumerations " + name);
            var values = new List<EnumerationValueSignature> ();
            foreach (FieldInfo field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static)) {
                values.Add (new EnumerationValueSignature (Name, name, field.Name, (int)field.GetRawConstantValue (), field.GetDocumentation ()));
            }
            Enumerations [name] = new EnumerationSignature (Name, name, values, enumType.GetDocumentation ());
            return name;
        }

        /// <summary>
        /// Add a class method to the given class in the given service for the given class type annotated with the KRPCClass attribute.
        /// </summary>
        public void AddClassMethod (string cls, MethodInfo method)
        {
            if (!Classes.ContainsKey (cls))
                throw new ArgumentException ("Class " + cls + " does not exist");
            var name = method.Name;
            if (!method.IsStatic) {
                var handler = new ClassMethodHandler (method);
                AddProcedure (new ProcedureSignature (Name, cls + '_' + name, method.GetDocumentation (), handler, GameScene,
                    "Class.Method(" + Name + "." + cls + "," + name + ")", "ParameterType(0).Class(" + Name + "." + cls + ")"));
            } else {
                var handler = new ClassStaticMethodHandler (method);
                AddProcedure (new ProcedureSignature (Name, cls + '_' + name, method.GetDocumentation (), handler, GameScene,
                    "Class.StaticMethod(" + Name + "." + cls + "," + name + ")"));
            }
        }

        /// <summary>
        /// Add a class property to the given class in the given service for the given property annotated with the KRPCProperty attribute.
        /// </summary>
        public void AddClassProperty (string cls, PropertyInfo property)
        {
            if (!Classes.ContainsKey (cls))
                throw new ArgumentException ("Class " + cls + " does not exist");
            var name = property.Name;
            var getter = property.GetGetMethod ();
            var setter = property.GetSetMethod ();
            if (getter != null)
                AddClassPropertyMethod (cls, property, getter, "Class.Property.Get(" + Name + "." + cls + "," + name + ")");
            if (setter != null)
                AddClassPropertyMethod (cls, property, setter, "Class.Property.Set(" + Name + "." + cls + "," + name + ")");
        }

        void AddClassPropertyMethod (string cls, MemberInfo property, MethodInfo method, string attribute)
        {
            var handler = new ClassMethodHandler (method);
            var parameter_attribute = "ParameterType(0).Class(" + Name + "." + cls + ")";
            AddProcedure (new ProcedureSignature (Name, cls + '_' + method.Name, property.GetDocumentation (), handler, GameScene, attribute, parameter_attribute));
        }

        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("documentation", Documentation);
            info.AddValue ("procedures", Procedures);
            info.AddValue ("classes", Classes);
            info.AddValue ("enumerations", Enumerations);
        }
    }
}
