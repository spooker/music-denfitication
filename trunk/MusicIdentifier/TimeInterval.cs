using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicIdentifier
{
    class TimeInterval
    {
        private DateTime startTime;
        private DateTime endTime;

        public TimeInterval()
        {
            startTime = DateTime.Now;    
        }

        public double GetDurationInSecond()
        {
            endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;
            return duration.TotalSeconds;
            //Console.WriteLine(duration.TotalSeconds);
        }
    }
}
