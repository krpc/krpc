using System;
using System.Reflection;

namespace KRPC.MechJeb.ExtensionMethods {
	public static class ReflectionExtensions {
		public static T CreateInstance<T>(this Type type, object[] args) {
			try {
				Type[] types = Type.EmptyTypes;
				if(args != null) {
					types = new Type[args.Length];
					for(int i = 0; i < args.Length; i++)
						types[i] = args[i].GetType();
				}

				return (T)type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, types, null).Invoke(args);
			}
			catch(Exception ex) {
				Logger.Severe("Coudn't create an instance of " + type, ex);
				throw new MJServiceException(ex.ToString());
			}
		}

		public static FieldInfo GetCheckedField(this Type type, string name) {
			return type.GetField(name).CheckIfExists(type, name);
		}

		public static FieldInfo GetCheckedField(this Type type, string name, BindingFlags bindingAttr) {
			return type.GetField(name, bindingAttr).CheckIfExists(type, name);
		}

		public static PropertyInfo GetCheckedProperty(this Type type, string name) {
			return type.GetProperty(name).CheckIfExists(type, name);
		}

		public static MethodInfo GetCheckedMethod(this Type type, string name) {
			return type.GetMethod(name).CheckIfExists(type, name + "()");
		}

		public static MethodInfo GetCheckedMethod(this Type type, string name, Type[] types) {
			return type.GetMethod(name, types).CheckIfExists(type, name + "()");
		}

		public static MethodInfo GetCheckedMethod(this Type type, string name, BindingFlags bindingAttr) {
			return type.GetMethod(name, bindingAttr).CheckIfExists(type, name + "()");
		}

		private static T CheckIfExists<T>(this T obj, Type type, string name) {
			if(obj == null) {
				string error = type + "." + name + " not found";
				Logger.Severe(error);
				MechJeb.errors.Add(error);
			}
			else
				Logger.Debug(type + "." + name + " found");

			return obj;
		}

		public static object GetInstanceValue(this FieldInfo field, object instance) {
			return field != null && instance != null ? field.GetValue(instance) : null;
		}
	}
}
