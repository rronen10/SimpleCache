using NUnit.Framework;
using System.Linq;
using System.Threading;

namespace SimpleCache.Test
{
    [TestFixture]
    public class CacheDictionaryWithInput_Test
    {
        public CacheDictionaryWithInput_Test()
        {
            _service = new MockService();
        }
        private readonly MockService _service;

        [Test]
        public void CacheDictionaryWithInputTest()
        {
            using var listOfCities = new CacheDictionaryWithInput<string, LargeInputData, SampleObjectWithTimestamp>(2,
                input =>
                {
                    return _service.GetCityByImage(input);
                });

            var newYorkLargeInputData = new LargeInputData
            {
                CityImage = new byte[] { 0x01, 0x02, 0x03, 0x04 },
            };
            var jerusalemLargeInputData = new LargeInputData
            {
                CityImage = new byte[] { 0x0A, 0x0B, 0x0C, 0x0D },
            };
            var londonLargeInputData = new LargeInputData
            {
                CityImage = new byte[] { 0x10, 0x20, 0x30, 0x40 },
            };

            var city1 = _service.GetCityByImage(newYorkLargeInputData);
            var city2 = _service.GetCityByImage(jerusalemLargeInputData);
            var city3 = _service.GetCityByImage(londonLargeInputData);

            Thread.Sleep(1000);
            
            Assert.AreEqual(listOfCities[newYorkLargeInputData].Value, city1.Value);
            Assert.AreEqual(listOfCities[jerusalemLargeInputData].Value, city2.Value);
            Assert.AreEqual(listOfCities[londonLargeInputData].Value, city3.Value);

            Assert.AreNotEqual(listOfCities[newYorkLargeInputData].Timestamp, city1.Timestamp);
            Assert.AreNotEqual(listOfCities[jerusalemLargeInputData].Timestamp, city2.Timestamp);
            Assert.AreNotEqual(listOfCities[londonLargeInputData].Timestamp, city3.Timestamp);

            var retryGetCity1 = listOfCities[newYorkLargeInputData];
            var retryGetCity2 = listOfCities[jerusalemLargeInputData];
            var retryGetCity3 = listOfCities[londonLargeInputData];

            Assert.AreEqual(listOfCities[newYorkLargeInputData].Timestamp, retryGetCity1.Timestamp);
            Assert.AreEqual(listOfCities[jerusalemLargeInputData].Timestamp, retryGetCity2.Timestamp);
            Assert.AreEqual(listOfCities[londonLargeInputData].Timestamp, retryGetCity3.Timestamp);

            //wait for cache is expired
            Thread.Sleep(2000);

            retryGetCity1 = listOfCities[newYorkLargeInputData];
            retryGetCity2 = listOfCities[jerusalemLargeInputData];
            retryGetCity3 = listOfCities[londonLargeInputData];

            Thread.Sleep(2 * 1000);

            Assert.AreNotEqual(listOfCities[newYorkLargeInputData].Timestamp, retryGetCity1.Timestamp);
            Assert.AreNotEqual(listOfCities[jerusalemLargeInputData].Timestamp, retryGetCity2.Timestamp);
            Assert.AreNotEqual(listOfCities[londonLargeInputData].Timestamp, retryGetCity3.Timestamp);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithNonPositiveTimeout_Throws(int cacheTimeoutSeconds)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new CacheDictionaryWithInput<string, LargeInputData, SampleObjectWithTimestamp>(
                    cacheTimeoutSeconds,
                    _ => new SampleObjectWithTimestamp("value")));
        }

        [Test]
        public void Constructor_WithNullGetValueFunc_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new CacheDictionaryWithInput<string, LargeInputData, SampleObjectWithTimestamp>(null!));
        }

        [Test]
        public void CacheFacingDictionaryApis_UseMemoryCacheEntries()
        {
            var getValueCallCount = 0;
            using var cache = new CacheDictionaryWithInput<
                string,
                LargeInputData,
                SampleObjectWithTimestamp>(
                    60,
                    _ =>
                    {
                        getValueCallCount++;
                        return new SampleObjectWithTimestamp("factory value");
                    });
            var requestInput = new LargeInputData
            {
                CityImage = new byte[] { 0x01 }
            };
            var expected = new SampleObjectWithTimestamp("assigned value");

            cache[requestInput] = expected;

            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(cache.ContainsKey(requestInput.Key));
            Assert.AreSame(expected, cache[requestInput]);
            Assert.AreEqual(0, getValueCallCount);
            Assert.IsTrue(cache.Remove(requestInput.Key));
            Assert.AreEqual(0, cache.Count);
            Assert.IsFalse(cache.ContainsKey(requestInput.Key));
            Assert.IsFalse(cache.Remove(requestInput.Key));
            Assert.That(
                cache,
                Is.AssignableTo<System.Collections.Generic.Dictionary<
                    string,
                    SampleObjectWithTimestamp>>());
        }

        [Test]
        public void Insertions_CreatePoliciesAtInsertionTime()
        {
            var getValueCallCount = 0;
            using var cache = new CacheDictionaryWithInput<
                string,
                LargeInputData,
                SampleObjectWithTimestamp>(
                    1,
                    _ =>
                    {
                        getValueCallCount++;
                        return new SampleObjectWithTimestamp("generated value");
                    });
            var generatedInput = new LargeInputData
            {
                CityImage = new byte[] { 0x01 }
            };
            var assignedInput = new LargeInputData
            {
                CityImage = new byte[] { 0x02 }
            };

            Thread.Sleep(1100);

            var generatedValue = cache[generatedInput];

            Assert.AreSame(generatedValue, cache[generatedInput]);
            Assert.AreEqual(1, getValueCallCount);

            var assignedValue = new SampleObjectWithTimestamp("assigned value");
            cache[assignedInput] = assignedValue;

            Assert.AreSame(assignedValue, cache[assignedInput]);
            Assert.AreEqual(1, getValueCallCount);
        }

        [Test]
        public void CachedValues_AreNotDisposedByDefault()
        {
            var cachedValue = new TrackingDisposable();
            var requestInput = new LargeInputData
            {
                CityImage = new byte[] { 0x01 }
            };
            using var cache = new CacheDictionaryWithInput<string, LargeInputData, TrackingDisposable>(
                60,
                _ => cachedValue);

            cache[requestInput] = cachedValue;
            cache.Remove(requestInput.Key);

            Assert.AreEqual(0, cachedValue.DisposeCallCount);
        }

        [Test]
        public void CachedValues_AreDisposedOnRemovalAndCacheDisposal_WhenEnabled()
        {
            var removedValue = new TrackingDisposable();
            var remainingValue = new TrackingDisposable();
            var removedInput = new LargeInputData
            {
                CityImage = new byte[] { 0x01 }
            };
            var remainingInput = new LargeInputData
            {
                CityImage = new byte[] { 0x02 }
            };

            using (var cache = new CacheDictionaryWithInput<
                string,
                LargeInputData,
                TrackingDisposable>(
                    60,
                    _ => removedValue,
                    disposeCachedValuesOnRemoval: true))
            {
                Assert.AreSame(removedValue, cache[removedInput]);
                cache[remainingInput] = remainingValue;

                Assert.IsTrue(cache.Remove(removedInput.Key));
                Assert.AreEqual(1, removedValue.DisposeCallCount);
                Assert.AreEqual(0, remainingValue.DisposeCallCount);
            }

            Assert.AreEqual(1, remainingValue.DisposeCallCount);
        }



    }

    public class LargeInputData : IKeyAbstruction<string>
    {
        public byte[] CityImage { get; set; }

        public string Key => CityImage.First().ToString();
    }
}
