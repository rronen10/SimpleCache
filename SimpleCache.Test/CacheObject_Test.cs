using NUnit.Framework;
using System.Threading;

namespace SimpleCache.Test
{
    [TestFixture]
    public class CacheObject_Test
    {
        public CacheObject_Test()
        {
            _service = new MockService();
        }
        private readonly MockService _service;

        [Test]
        public void CacheObjectTest()
        {
            using var userNameCached = new CacheObject<SampleObjectWithTimestamp>(3, () => _service.GetUserName());
            
            //Get the value for the firs time - call the service.
            var userName = _service.GetUserName();
            Thread.Sleep(1000);
            Assert.AreEqual(userName.Value, userNameCached.Value.Value);
            Assert.AreNotEqual(userName.Timestamp, userNameCached.Value.Timestamp);

            var time = userNameCached.Value.Timestamp;
            //Get the value for the second time - no call for the service - the value is finded in the cache
            Assert.AreEqual(userNameCached.Value.Timestamp, time);

            time = userNameCached.Value.Timestamp;
            //wait for cache is expired
            Thread.Sleep(3 * 1000);

            //The cache object is expired - call the service to refresh the value
            Assert.AreNotEqual(userNameCached.Value.Timestamp, time);

        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithNonPositiveTimeout_Throws(int cacheTimeoutSeconds)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new CacheObject<int>(cacheTimeoutSeconds, () => 1));
        }

        [Test]
        public void Constructor_WithNullGetValueFunc_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new CacheObject<int>(null!));
        }

        [Test]
        public void CachedValues_AreNotDisposedByDefault()
        {
            var cachedValue = new TrackingDisposable();
            var cache = new CacheObject<TrackingDisposable>(60, () => cachedValue);
            cache.Value = cachedValue;

            cache.Dispose();

            Assert.AreEqual(0, cachedValue.DisposeCallCount);
        }

        [Test]
        public void CachedValues_AreDisposedOnReplacementAndCacheDisposal_WhenEnabled()
        {
            var replacedValue = new TrackingDisposable();
            var currentValue = new TrackingDisposable();

            using (var cache = new CacheObject<TrackingDisposable>(
                60,
                () => new TrackingDisposable(),
                disposeCachedValuesOnRemoval: true))
            {
                cache.Value = replacedValue;
                cache.Value = currentValue;

                Assert.AreEqual(1, replacedValue.DisposeCallCount);
                Assert.AreEqual(0, currentValue.DisposeCallCount);
            }

            Assert.AreEqual(1, currentValue.DisposeCallCount);
        }


    }
}
