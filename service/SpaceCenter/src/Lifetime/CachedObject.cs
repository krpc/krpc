using System;
using KRPC.Service;
using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Remembers the game object that a service object last resolved from its identifier,
    /// so that reading it again does not have to search the game for it.
    /// </summary>
    /// <remarks>
    /// A service object holds one of these as a field, so a cached object costs a single
    /// weak reference and the code that reads it has no delegate to call through. The
    /// reference is weak, so caching never keeps a destroyed game object alive, and it
    /// is stamped with the generation of the game state the object was resolved in, so
    /// an object left over from a replaced game state is never handed back.
    /// </remarks>
    struct CachedObject<T> where T : UnityEngine.Object
    {
        WeakReference reference;
        uint generation;

        /// <summary>
        /// The game object that was last resolved, or null if it has to be resolved again.
        /// </summary>
        public T Get ()
        {
            if (reference == null || generation != GameState.Generation)
                return null;
            var cached = reference.Target as T;
            // Unity tears down a game object before the garbage collector gets to the
            // managed object wrapping it, and reports that wrapper as null. The check has
            // to be made against UnityEngine.Object, because == on a type parameter is
            // reference equality and would hand back a torn down object.
            UnityEngine.Object obj = cached;
            return obj == null ? null : cached;
        }

        /// <summary>
        /// Remember a game object that has just been resolved.
        /// </summary>
        public void Set (T obj)
        {
            if (reference == null)
                reference = new WeakReference (obj);
            else
                reference.Target = obj;
            generation = GameState.Generation;
        }
    }
}
