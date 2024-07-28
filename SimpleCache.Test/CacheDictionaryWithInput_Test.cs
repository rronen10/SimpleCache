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
            var listOfCities = new CacheDictionaryWithInput<string, LargeInputData, SampleObjectWithTimestamp>(2,
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

    }

    public class LargeInputData : IKeyAbstruction<string>
    {
        public byte[] CityImage { get; set; }

        public string Key => CityImage.First().ToString();
    }
}
