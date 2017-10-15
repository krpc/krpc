using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Utils
{
    /// <summary>
    /// Extension methods for dictionaries
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets the value for the given key from a dictionary, or a default value if the key does not exist.
        /// </summary>
        public static Value GetValueOrDefault<Key, Value> (this IDictionary<Key, Value> dictionary, Key key, Value defaultValue)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
            Value value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
}
