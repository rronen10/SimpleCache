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
            var userNameCached = new CacheObject<SampleObjectWithTimestamp>(3, () => _service.GetUserName());
            
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

    }
}
