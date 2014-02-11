using System;

namespace KRPC.Service
{
	public class NoSuchServiceMethodException : Exception
	{
		public NoSuchServiceMethodException (Type service, string method):
			base(service.Name + "." + method)
		{
		}
	}
}

