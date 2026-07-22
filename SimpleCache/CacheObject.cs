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
    public class CacheObject<T> : IDisposable
    {
        #region Ctor
        /// <summary>
        /// Constractor for CacheObject.
        /// </summary>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheObject(Func<T> getValueFunc)
            : this(60, getValueFunc, false)
        {
        }

        /// <summary>
        /// Constractor for CacheObject.
        /// </summary>
        /// <param name="cacheTimeoutSeconds">Expiration time(seconds) for cache</param>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        public CacheObject(int cacheTimeoutSeconds, Func<T> getValueFunc)
            : this(cacheTimeoutSeconds, getValueFunc, false)
        {
        }

        /// <summary>
        /// Constractor for CacheObject.
        /// </summary>
        /// <param name="cacheTimeoutSeconds">Expiration time(seconds) for cache</param>
        /// <param name="getValueFunc">Function that returns the object (When the object is not in the cache, this method will be called)</param>
        /// <param name="disposeCachedValuesOnRemoval">Whether the cache owns and disposes cached IDisposable values when they are removed</param>
        public CacheObject(int cacheTimeoutSeconds, Func<T> getValueFunc, bool disposeCachedValuesOnRemoval)
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
        private readonly Func<T> _getValueFunc;
        private readonly ObjectCache _cache;
        private readonly object _syncRoot = new object();
        private readonly int _cacheTimeoutSeconds;
        private readonly bool _disposeCachedValuesOnRemoval;
        #endregion

        #region Properties
        /// <summary>
        /// The object value
        /// </summary>
        public T Value
        {
            get
            {
                lock (_syncRoot)
                {
                    return GetValue();
                }
            }
            set
            {
                var policy = CreatePolicy();
                _cache.Set(string.Empty,
                    value,
                    policy);
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
                var policy = CreatePolicy();
                _cache.Set(string.Empty,
                    cacheValue,
                    policy);
            }
            return (T)cacheValue;
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

        public void Dispose()
        {
            ((IDisposable)_cache).Dispose();
        }
        #endregion
    }
}
