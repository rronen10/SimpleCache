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
            var listOfCities = new CacheDictionary<int, SampleObjectWithTimestamp>(2, cityId => _service.GetCityById(cityId));

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

    }
}
