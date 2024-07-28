using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleCache.Test
{
    public class MockService
    {
        public SampleObjectWithTimestamp GetCityById(int id)
        {
            switch (id)
            {
                case 1: return new SampleObjectWithTimestamp("New York");
                case 2: return new SampleObjectWithTimestamp("Jerusalem");
                case 3: return new SampleObjectWithTimestamp("London");
            }
            throw new KeyNotFoundException();
        }

        public SampleObjectWithTimestamp GetCityByImage(LargeInputData largeInputData)
        {
            byte[] newYorkImage = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            byte[] jerusalemImage = new byte[] { 0x0A, 0x0B, 0x0C, 0x0D };
            byte[] londonImage = new byte[] { 0x10, 0x20, 0x30, 0x40 };

            var cityImage = largeInputData.CityImage;
            if (cityImage.SequenceEqual(newYorkImage))
            {
                return new SampleObjectWithTimestamp("New York");
            }
            else if (cityImage.SequenceEqual(jerusalemImage))
            {
                return new SampleObjectWithTimestamp("Jerusalem");
            }
            else if (cityImage.SequenceEqual(londonImage))
            {
                return new SampleObjectWithTimestamp("London");
            }

            throw new KeyNotFoundException();
        }


        public SampleObjectWithTimestamp GetUserName()
        {
            return new SampleObjectWithTimestamp("Some User Name");
        }
    }

    public class SampleObjectWithTimestamp
    {
        public SampleObjectWithTimestamp(string value)
        {
            Value = value;
            Timestamp = DateTime.Now;
        }

        public string Value { get; private set; }

        public DateTime Timestamp { get; private set; }
    }

}
