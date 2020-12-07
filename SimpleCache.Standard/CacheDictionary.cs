using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace SimpleCache.Standard
{
    /// <summary>
    ///  Represents a collection of keys and values(Dictionary) with caching.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    public class CacheDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        #region Ctor
        /// <summary>
        /// Constractor for CacheDictionary.
        /// </summary>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheDictionary(Func<TKey, TValue> getValueFunc)
            : this(60, getValueFunc)
        {
        }

        /// <summary>
        /// Constractor for CacheDictionary.
        /// </summary>
        /// <param name="chashTimeoutSeconds">Expiration time(seconds) for cache</param>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheDictionary(int chashTimeoutSeconds, Func<TKey, TValue> getValueFunc)
        {
            _chashTimeoutSeconds = chashTimeoutSeconds;
            _cache = new MemoryCache(Guid.NewGuid().ToString());
            _policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(chashTimeoutSeconds) };
            _getValueFunc = getValueFunc;
        }
        #endregion

        #region Members
        private readonly ObjectCache _cache;
        private readonly CacheItemPolicy _policy;
        private Func<TKey, TValue> _getValueFunc;
        private readonly int _chashTimeoutSeconds;
        private static object lockingObject = new object();
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the number of key/value pairs contained in the System.Runtime.Caching.ObjectCache
        /// </summary>
        public new int Count
        {
            get
            {
                return _cache.Count();
            }
        }

        /// <summary>
        /// Add new item to the dictionary.
        /// </summary>
        /// <param name="key">The key in the dictionary</param>
        /// <param name="value">The value in the dictionary</param>
        private new void Add(TKey key, TValue value)
        {
            _cache.Set(key.ToString(),
                value,
                _policy);
        }

        /// <summary>
        /// Remove item from the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new bool Remove(TKey key)
        {
            return (_cache.Remove(key.ToString()) != null);
        }

        /// <summary>
        /// Return the value by key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new TValue this[TKey key]
        {
            get
            {
                //Handle multi threads
                lock (_cache)
                {
                    //Try to get the value from the cech
                    var cacheValue = _cache[key.ToString()];

                    //if the value is not in the cech
                    if (cacheValue == null)
                    {
                        //get the new value 
                        cacheValue = _getValueFunc.Invoke(key);

                        //set the new value to the cech
                        Add(key, (TValue)cacheValue);


                        _policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(_chashTimeoutSeconds);
                    }
                    return (TValue)cacheValue;
                }
            }
            set
            {
                _cache.Set(key.ToString(),
                    value,
                    _policy);
            }
        }

        /// <summary>
        /// Check if the key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new bool ContainsKey(TKey key)
        {
            var cacheValue = _cache[key.ToString()];
            return cacheValue != null;
        }
        #endregion
    }
}
