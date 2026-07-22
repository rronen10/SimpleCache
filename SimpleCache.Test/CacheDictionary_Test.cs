using NUnit.Framework;
using System.Threading;

namespace SimpleCache.Test
{
    [TestFixture]
    public class CacheDictionary_Test
    {
        public CacheDictionary_Test()
        {
            _service = new MockService();
        }
        private readonly MockService _service;

        [Test]
        public void CacheDictionaryTest()
        {
            using var listOfCities = new CacheDictionary<int, SampleObjectWithTimestamp>(2, cityId => _service.GetCityById(cityId));

            var city1 = _service.GetCityById(1);
            var city2 = _service.GetCityById(2);
            var city3 = _service.GetCityById(3);

            Thread.Sleep(1000);
            
            Assert.AreEqual(listOfCities[1].Value, city1.Value);
            Assert.AreEqual(listOfCities[2].Value, city2.Value);
            Assert.AreEqual(listOfCities[3].Value, city3.Value);

            Assert.AreNotEqual(listOfCities[1].Timestamp, city1.Timestamp);
            Assert.AreNotEqual(listOfCities[2].Timestamp, city2.Timestamp);
            Assert.AreNotEqual(listOfCities[3].Timestamp, city3.Timestamp);

            var retryGetCity1 = listOfCities[1];
            var retryGetCity2 = listOfCities[2];
            var retryGetCity3 = listOfCities[3];

            Assert.AreEqual(listOfCities[1].Timestamp, retryGetCity1.Timestamp);
            Assert.AreEqual(listOfCities[2].Timestamp, retryGetCity2.Timestamp);
            Assert.AreEqual(listOfCities[3].Timestamp, retryGetCity3.Timestamp);

            //wait for cache is expired
            

            retryGetCity1 = listOfCities[1];
            retryGetCity2 = listOfCities[2];
            retryGetCity3 = listOfCities[3];

            Thread.Sleep(2 * 1000);

            Assert.AreNotEqual(listOfCities[1].Timestamp, retryGetCity1.Timestamp);
            Assert.AreNotEqual(listOfCities[2].Timestamp, retryGetCity2.Timestamp);
            Assert.AreNotEqual(listOfCities[3].Timestamp, retryGetCity3.Timestamp);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithNonPositiveTimeout_Throws(int cacheTimeoutSeconds)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new CacheDictionary<int, string>(cacheTimeoutSeconds, _ => string.Empty));
        }

        [Test]
        public void Constructor_WithNullGetValueFunc_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CacheDictionary<int, string>(null!));
        }

        [Test]
        public void CacheFacingDictionaryApis_UseMemoryCacheEntries()
        {
            using var cache = new CacheDictionary<int, string>(60, _ => "factory value");

            cache[1] = "one";

            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(cache.ContainsKey(1));
            Assert.AreEqual("one", cache[1]);
            Assert.IsTrue(cache.Remove(1));
            Assert.AreEqual(0, cache.Count);
            Assert.IsFalse(cache.ContainsKey(1));
            Assert.IsFalse(cache.Remove(1));
        }

        [Test]
        public void NestedCacheDictionary_CachesInnerDictionary()
        {
            using var innerCache = new CacheDictionary<int, SampleObjectWithTimestamp>(
                60,
                cityId => _service.GetCityById(cityId));
            var outerFactoryCallCount = 0;
            using var outerCache = new CacheDictionary<int, CacheDictionary<int, SampleObjectWithTimestamp>>(
                60,
                _ =>
                {
                    outerFactoryCallCount++;
                    return innerCache;
                });

            var cachedInner = outerCache[10];
            var city = cachedInner[2];

            Assert.AreSame(innerCache, cachedInner);
            Assert.AreSame(cachedInner, outerCache[10]);
            Assert.AreEqual("Jerusalem", city.Value);
            Assert.AreEqual(1, outerFactoryCallCount);
            Assert.AreEqual(1, outerCache.Count);
            Assert.AreEqual(1, innerCache.Count);
            Assert.That(
                outerCache,
                Is.AssignableTo<System.Collections.Generic.Dictionary<
                    int,
                    CacheDictionary<int, SampleObjectWithTimestamp>>>());
        }

        [Test]
        public void Insertions_CreatePoliciesAtInsertionTime()
        {
            var getValueCallCount = 0;
            using var cache = new CacheDictionary<int, string>(1, _ =>
            {
                getValueCallCount++;
                return "generated value";
            });

            Thread.Sleep(1100);

            var generatedValue = cache[1];

            Assert.AreSame(generatedValue, cache[1]);
            Assert.AreEqual(1, getValueCallCount);

            cache[2] = "assigned value";

            Assert.AreEqual("assigned value", cache[2]);
            Assert.AreEqual(1, getValueCallCount);
        }

        [Test]
        public void CachedValues_AreNotDisposedByDefault()
        {
            var cachedValue = new TrackingDisposable();
            using var cache = new CacheDictionary<int, TrackingDisposable>(60, _ => cachedValue);

            cache[1] = cachedValue;
            cache.Remove(1);

            Assert.AreEqual(0, cachedValue.DisposeCallCount);
        }

        [Test]
        public void CachedValues_AreDisposedOnRemovalAndCacheDisposal_WhenEnabled()
        {
            var removedValue = new TrackingDisposable();
            var remainingValue = new TrackingDisposable();

            using (var cache = new CacheDictionary<int, TrackingDisposable>(
                60,
                _ => removedValue,
                disposeCachedValuesOnRemoval: true))
            {
                Assert.AreSame(removedValue, cache[1]);
                cache[2] = remainingValue;

                Assert.IsTrue(cache.Remove(1));
                Assert.AreEqual(1, removedValue.DisposeCallCount);
                Assert.AreEqual(0, remainingValue.DisposeCallCount);
            }

            Assert.AreEqual(1, remainingValue.DisposeCallCount);
        }



    }
}
