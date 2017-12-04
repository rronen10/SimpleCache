﻿using System;
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
