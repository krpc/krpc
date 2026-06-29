using System;

namespace KRPC.Utils
{
    /// <summary>
    /// Memoizes the object resolved from a stable identifier, so that repeated
    /// accesses can skip a potentially expensive lookup while the object is still
    /// alive.
    ///
    /// The object is held through a <see cref="WeakReference{T}"/>, so caching it
    /// never keeps a destroyed game object alive (which would leak it in the
    /// object store, see issue #771). On each access the cached object is returned
    /// only while it is still reachable and still reported alive by
    /// <c>isAlive</c>; otherwise the resolver is invoked again and the result
    /// re-cached.
    /// </summary>
    /// <remarks>
    /// Not thread safe: instances are expected to be accessed on the game's main
    /// thread, like the rest of the service objects.
    /// </remarks>
    public sealed class CachedReference<T> where T : class
    {
        readonly Func<T> resolve;
        readonly Func<T, bool> isAlive;
        WeakReference<T> cache;

        /// <summary>
        /// Create a cached reference with an empty cache; the first access resolves
        /// the object.
        /// </summary>
        public CachedReference (Func<T> resolve, Func<T, bool> isAlive)
            : this (resolve, isAlive, null)
        {
        }

        /// <summary>
        /// Create a cached reference seeded with the object it currently resolves to
        /// (typically the live game object the owning proxy was constructed from), so
        /// even the first access uses it directly, without an initial lookup.
        /// </summary>
        /// <param name="resolve">
        /// Resolves the object from its identifier. May return <c>null</c> when the
        /// object no longer exists.
        /// </param>
        /// <param name="isAlive">
        /// Returns whether a resolved object is still alive. For a
        /// <c>UnityEngine.Object</c> this must use Unity's overloaded equality
        /// (<c>obj != null</c>), which reports a destroyed object as null even
        /// before it has been garbage collected; <see cref="object.ReferenceEquals"/>
        /// would miss that.
        /// </param>
        /// <param name="initial">
        /// The object to seed the cache with, or <c>null</c> to start empty. A
        /// non-alive seed is ignored.
        /// </param>
        public CachedReference (Func<T> resolve, Func<T, bool> isAlive, T initial)
        {
            if (resolve == null)
                throw new ArgumentNullException (nameof (resolve));
            if (isAlive == null)
                throw new ArgumentNullException (nameof (isAlive));
            this.resolve = resolve;
            this.isAlive = isAlive;
            if (initial != null && isAlive (initial))
                cache = new WeakReference<T> (initial);
        }

        /// <summary>
        /// The resolved object: the cached value while it is still alive, otherwise
        /// the result of re-resolving (which is also re-cached). Returns whatever the
        /// resolver returns when it has to re-resolve, which may be <c>null</c> if the
        /// object no longer exists; the caller decides how to handle that.
        /// </summary>
        public T Get ()
        {
            T cached;
            if (cache != null && cache.TryGetTarget (out cached) && isAlive (cached))
                return cached;

            var resolved = resolve ();
            // Only cache a live object. A null (gone) or destroyed-but-not-yet-collected
            // object is returned but never cached, so the next access re-resolves.
            if (resolved != null && isAlive (resolved)) {
                if (cache == null)
                    cache = new WeakReference<T> (resolved);
                else
                    cache.SetTarget (resolved);
            }
            return resolved;
        }
    }
}
