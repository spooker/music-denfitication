using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shazam
{
    class Utility
    {
        public static KeyValuePair<int, int> GetTopHit(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return new KeyValuePair<int,int>(-1, -1);

            ids.Sort();
            int id = ids[0];
            int count = 1;
            KeyValuePair<int, int> topHit = new KeyValuePair<int, int>(0, 0);
            for (int i = 1; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    count++;
                }
                else
                {
                    if (count > topHit.Value)
                    {
                        topHit = new KeyValuePair<int, int>(id, count);
                    }
                    id = ids[i];
                    count = 1;
                }
            }

            if (count > topHit.Value)
            {
                topHit = new KeyValuePair<int, int>(id, count);
            }

            return topHit;
        }
        public static double GetAverage(byte[] data, ref double min, ref double max)
        {
            long total = 0;
            foreach (byte value in data)
            {
                if (value < min)
                    min = value;
                if (value > max)
                    max = value;
                total += value;
            }

            return ((double)total) / data.Length;
        }
    }
}
