using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace SimpleCache
{
    /// <summary>
    ///  Represents a collection of keys and values(Dictionary) with caching.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    public class CacheDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDisposable
    {
        #region Ctor
        /// <summary>
        /// Constractor for CacheDictionary.
        /// </summary>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheDictionary(Func<TKey, TValue> getValueFunc)
            : this(60, getValueFunc, false)
        {
        }

        /// <summary>
        /// Constractor for CacheDictionary.
        /// </summary>
        /// <param name="cacheTimeoutSeconds">Expiration time(seconds) for cache</param>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheDictionary(int cacheTimeoutSeconds, Func<TKey, TValue> getValueFunc)
            : this(cacheTimeoutSeconds, getValueFunc, false)
        {
        }

        /// <summary>
        /// Constractor for CacheDictionary.
        /// </summary>
        /// <param name="cacheTimeoutSeconds">Expiration time(seconds) for cache</param>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        /// <param name="disposeCachedValuesOnRemoval">Whether the cache owns and disposes cached IDisposable values when they are removed</param>
        public CacheDictionary(int cacheTimeoutSeconds, Func<TKey, TValue> getValueFunc, bool disposeCachedValuesOnRemoval)
        {
            if (cacheTimeoutSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(cacheTimeoutSeconds));
            if (getValueFunc == null)
                throw new ArgumentNullException(nameof(getValueFunc));

            _cacheTimeoutSeconds = cacheTimeoutSeconds;
            _disposeCachedValuesOnRemoval = disposeCachedValuesOnRemoval;
            _cache = new MemoryCache(Guid.NewGuid().ToString());
            _getValueFunc = getValueFunc;
        }
        #endregion

        #region Members
        // MemoryCache is the real storage; inherited Dictionary members not reflect cached entries.
        private readonly ObjectCache _cache;
        private readonly object _syncRoot = new object();
        private readonly Func<TKey, TValue> _getValueFunc;
        private readonly int _cacheTimeoutSeconds;
        private readonly bool _disposeCachedValuesOnRemoval;
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the number of key/value pairs contained in the System.Runtime.Caching.ObjectCache
        /// </summary>
        public new int Count
        {
            get
            {
                return checked((int)_cache.GetCount());
            }
        }

        /// <summary>
        /// Add new item to the dictionary.
        /// </summary>
        /// <param name="key">The key in the dictionary</param>
        /// <param name="value">The value in the dictionary</param>
        private new void Add(TKey key, TValue value)
        {
            var policy = CreatePolicy();
            _cache.Set(key.ToString(),
                value,
                policy);
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
                lock (_syncRoot)
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
                    }
                    return (TValue)cacheValue;
                }
            }
            set
            {
                var policy = CreatePolicy();
                _cache.Set(key.ToString(),
                    value,
                    policy);
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

        public void Dispose()
        {
            ((IDisposable)_cache).Dispose();
        }

        private CacheItemPolicy CreatePolicy()
        {
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_cacheTimeoutSeconds)
            };

            if (_disposeCachedValuesOnRemoval)
                policy.RemovedCallback = DisposeRemovedValue;

            return policy;
        }

        private static void DisposeRemovedValue(CacheEntryRemovedArguments arguments)
        {
            if (arguments.CacheItem.Value is IDisposable disposable)
                disposable.Dispose();
        }
        #endregion
    }
}
