using System;

namespace KRPC.Service
{
	public class NoSuchServiceException : Exception
	{
		public NoSuchServiceException (string name):
			base(name)
		{
		}
	}
}

