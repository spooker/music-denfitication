using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicIdentifier
{
    class HitCounter
    {
        public enum CounterStyle { One, Many };
        public Dictionary<int, int> counter = new Dictionary<int, int>();
        private CounterStyle style;
        
        public HitCounter(CounterStyle style)
        {
            this.style = style;
        }

        public void Update(Dictionary<int, List<int>> results)
        { 
            foreach(KeyValuePair<int, List<int>> result in results)
            {
                Update(result.Key, result.Value.Count);
            }
        }
        
        public void Update(int key, int count)
        {
            count = (style == CounterStyle.One) ? 1 : count;
            if (counter.ContainsKey(key))
            {
                counter[key] += count;
            }
            else
            {
                counter[key] = count;
            }
        }   

        public void Update(List<KeyValuePair<int, int>> results)
        {
            foreach (KeyValuePair<int, int> result in results)
            {
                Update(result.Value, result.Key);        
            }
        }

        public void Clear()
        {
            counter.Clear();
        }

        public int GetBestID(ref int score)
        {
            int maxID = -1;
            int maxCount = -1;
            foreach (KeyValuePair<int, int> song in counter)
            {
                if (song.Value > maxCount)
                {
                    maxID = song.Key;
                    maxCount = song.Value;
                }
            }
            score = maxCount;
            return maxID;
        }
    }
}
