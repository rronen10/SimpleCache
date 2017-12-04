using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace SimpleCache
{
    /// <summary>
    /// Object this cache behaviors.
    /// </summary>
    /// <typeparam name="T">The object type</typeparam>
    public class CacheObject<T>
    {
        #region Ctor
        /// <summary>
        /// Constractor for CacheObject.
        /// </summary>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheObject(Func<T> getValueFunc)
            : this(Properties.Settings.Default.DefaultCacheTimeoutSeconds, getValueFunc)
        {
        }

        /// <summary>
        /// Constractor for CacheObject.
        /// </summary>
        /// <param name="chashTimeoutSeconds">Expiration time(seconds) for cache</param>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheObject(int chashTimeoutSeconds, Func<T> getValueFunc)
        {
            _chashTimeoutSeconds = chashTimeoutSeconds;
            _cache = new MemoryCache(Guid.NewGuid().ToString());
            _policy = new CacheItemPolicy();
            _policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(_chashTimeoutSeconds);
            _getValueFunc = getValueFunc;
        }
        #endregion

        #region Members
        private Func<T> _getValueFunc;
        private CacheItemPolicy _policy;
        private ObjectCache _cache;
        private static object lockingObject = new object();
        private int _chashTimeoutSeconds;
        #endregion

        #region Properties
        /// <summary>
        /// The object value
        /// </summary>
        public T Value
        {
            get
            {
                lock (_cache)
                {
                    return GetValue();
                }
            }
            set
            {
                _cache[string.Empty] = value;
            }
        }
        #endregion

        #region Private Methods
        private T GetValue()
        {
            //Try to get the value from the cech
            var cacheValue = _cache[string.Empty];

            //if the value is not in the cech
            if (cacheValue == null)
            {
                //get the new value 
                cacheValue = _getValueFunc.Invoke();

                //set the new value to the cech
                _cache.Set(string.Empty,
                    cacheValue,
                    _policy);
                //refresh the cech timeout
                _policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(_chashTimeoutSeconds);
            }
            return (T)cacheValue;
        }
        #endregion
    }
}
