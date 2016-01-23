using System.Collections.Generic;

namespace KRPC.Utils
{
    interface IScheduler<T> : IEnumerable<T>
    {
        bool Empty { get; }

        T Next ();

        void Add (T client);

        void Remove (T client);
    }
}
