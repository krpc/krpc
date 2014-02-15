using System;

namespace KRPC.Utils
{
    interface IScheduler<T>
    {
        bool Empty { get; }
        T Next();
        void Add(T client);
        void Remove(T client);
    }
}
