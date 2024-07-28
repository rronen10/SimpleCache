using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace SimpleCache
{
    /// <summary>
    ///  Represents a collection of keys and values(Dictionary) with caching.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TResultOutput">The type of the values in the dictionary</typeparam>
    public class CacheDictionaryWithInput<TKey, TRequestInput, TResultOutput> : Dictionary<TKey, TResultOutput>
        where TRequestInput: IKeyAbstruction<TKey>
    {
        private readonly ObjectCache _cache;
        private readonly CacheItemPolicy _policy;
        private Func<TRequestInput, TResultOutput> _getValueFunc;
        private readonly int _chashTimeoutSeconds;

        /// <summary>
        /// Constractor for CacheDictionary.
        /// </summary>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheDictionaryWithInput(Func<TRequestInput, TResultOutput> getValueFunc)
            : this(60, getValueFunc)
        {
        }

        /// <summary>
        /// Constractor for CacheDictionary.
        /// </summary>
        /// <param name="chashTimeoutSeconds">Expiration time(seconds) for cache</param>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheDictionaryWithInput(int chashTimeoutSeconds, Func<TRequestInput, TResultOutput> getValueFunc)
        {
            _chashTimeoutSeconds = chashTimeoutSeconds;
            _cache = new MemoryCache(Guid.NewGuid().ToString());
            _policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(chashTimeoutSeconds) };
            _getValueFunc = getValueFunc;
        }

        

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
        private new void Add(TKey key, TResultOutput value)
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
        public new TResultOutput this[TRequestInput requestInput]
        {
            get
            {
                //Handle multi threads
                lock (_cache)
                {
                    //Try to get the value from the cech
                    var cacheValue = _cache[requestInput.Key.ToString()];

                    //if the value is not in the cech
                    if (cacheValue == null)
                    {
                        //get the new value 
                        cacheValue = _getValueFunc.Invoke(requestInput);

                        //set the new value to the cech
                        Add(requestInput.Key, (TResultOutput)cacheValue);


                        _policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(_chashTimeoutSeconds);
                    }
                    return (TResultOutput)cacheValue;
                }
            }
            set
            {
                _cache.Set(requestInput.Key.ToString(),
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
    }
}
