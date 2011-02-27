using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Exocortex.DSP;
using System.Diagnostics;

namespace Shazam
{
    class DataBase
    {
        private List<string> songNames = new List<string>();
        private Dictionary<long, List<DataPoint>> hashMap = new Dictionary<long, List<DataPoint>>();
        Mp3ToWavConverter converter = new Mp3ToWavConverter();
        PointsFinder pointFinder = new PointsFinder();

        public string GetNameByID(int id)
        {
            if (id < 0 || id >= songNames.Count)
                throw new ArgumentOutOfRangeException("id");

            return songNames[id];
        }
                
        private long[] GetAudioHash(byte[] audio)
        {
            Complex[][] results = pointFinder.FFT(audio);
            int[][] lines = pointFinder.GetKeyPoints(results);
            long[] hashes = pointFinder.GetHash(lines);
            return hashes;
        }

        public void AddNewSong(string filePath)
        {
            try
            {
                int id = songNames.Count;
                byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(filePath);
                long[] hashes = GetAudioHash(audio);

                AddHash(id, hashes);
                songNames.Add(Path.GetFileNameWithoutExtension(filePath));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("{0} is not added to the database.", filePath);
            }
        }
        public Dictionary<int, List<int>> IndexSong(byte[] audio)
        {
            Dictionary<int, List<int>> results = new Dictionary<int, List<int>>();
            long[] hashes = GetAudioHash(audio);
            for (int i = 0; i < hashes.Length; i++)
            {
                if (hashMap.ContainsKey(hashes[i]))
                {
                    List<DataPoint> pointList = hashMap[hashes[i]];
                    foreach (DataPoint dataPoint in pointList)
                    { 
                        List<int> hits = null;
                        if (results.ContainsKey(dataPoint.SongID))
                            hits = results[dataPoint.SongID];
                        else
                        {
                            hits = new List<int>();
                            results.Add(dataPoint.SongID, hits);
                        }

                        hits.Add(dataPoint.Time - i);
                    }
                }
            }

            return results;
        }

        public void GetBestHitByHitCount(Dictionary<int, List<int>> results, bool bPrintCandidates)
        {
            IOrderedEnumerable<KeyValuePair<int, List<int>>> oe = results.OrderBy(x => (x.Value.Count) * -1); 
            
            if (bPrintCandidates)
            {
                int count = 1;
                foreach (KeyValuePair<int, List<int>> keyValuePair in oe)
                {
                    Console.WriteLine("{0}: [{1}]\t{2}", count,
                        songNames[keyValuePair.Key],
                        keyValuePair.Value.Count);                    
                    if (count == 5)
                        break;
                    count++;
                }
            }
                        
            IEnumerator<KeyValuePair<int, List<int>>> en = oe.GetEnumerator();
            en.MoveNext();
            KeyValuePair<int, List<int>> firstOne = en.Current;
            Console.WriteLine("---------------------------");
            Console.WriteLine("Best Hit by HitCount: {0}", songNames[firstOne.Key]);
            Console.WriteLine("---------------------------");
        }
        public int GetBestHitBySpanMatch(Dictionary<int, List<int>> results, bool bPrintCandidates)
        { 
            int score = 0;
            return GetBestHitBySpanMatch(results, ref score, bPrintCandidates);
        }

        public int GetBestHitBySpanMatch(Dictionary<int, List<int>> results, ref int score, bool bPrintCandidates)
        {
            List<KeyValuePair<int, int>> bestHit = GetBestNHitBySpanMatch(results, 1, bPrintCandidates);
            int bestID = -1;

            if (bestHit.Count > 0)
            {
                bestID = bestHit[0].Value;
                score = bestHit[0].Key;
            }

            return bestID;
        }

        public List<KeyValuePair<int, int>> GetBestNHitBySpanMatch(Dictionary<int, List<int>> results, int n, bool bPrintCandidates)
        {
            List<KeyValuePair<int, int>> countID = new List<KeyValuePair<int, int>>();
            foreach (KeyValuePair<int, List<int>> keyValuePair in results)
            {
                keyValuePair.Value.Sort();
                List<KeyValuePair<int, int>> countSpan = GetBestTwoSpan(keyValuePair.Value);
                if (countSpan.Count >= 2 && Math.Abs(countSpan[0].Key - countSpan[1].Key) < 10)
                {
                    countID.Add(new KeyValuePair<int, int>(countSpan[0].Key + countSpan[1].Key, keyValuePair.Key));
                }
                else if(countSpan.Count > 0)
                {
                    countID.Add(new KeyValuePair<int, int>(countSpan[0].Key, keyValuePair.Key));
                }
            }

            countID.Sort(
                delegate(KeyValuePair<int, int> firstPair,
                KeyValuePair<int, int> secondPair)
                {
                    return secondPair.Key.CompareTo(firstPair.Key);
                }
            );

            List<KeyValuePair<int, int>> bestHit = new List<KeyValuePair<int, int>>();

            for (int i = 0; i < n && i < countID.Count; i++)
            {
                bestHit.Add(countID[i]);
            }

            if (bPrintCandidates)
            {
                for (int i = 0; i < countID.Count && i < n; i++)
                {
                    int id = countID[i].Value;
                    Console.WriteLine("{0}: [{1}]", i + 1, songNames[id]);
                    List<KeyValuePair<int, int>> countSpan = GetBestTwoSpan(results[id]);
                    for (int j = 0; j < countSpan.Count && j < 2; j++)
                    {
                        KeyValuePair<int, int> cSpan = countSpan[j];
                        Console.WriteLine("\t<Span-Count:{0}-{1}>", cSpan.Value, cSpan.Key);
                    }
                }
            }

            return bestHit;
        }

        private void PrintCountSpan(List<KeyValuePair<int, int>> countSpan)
        {
            if(countSpan == null)
                return;

            for (int j = 0; j < countSpan.Count && j < 2; j++)
            {
                KeyValuePair<int, int> cSpan = countSpan[j];
                Console.Write("\t<{0}-{1}>", cSpan.Value, cSpan.Key);
            }
            Console.WriteLine();
        }

        public void Print(Dictionary<int, List<int>> results)
        {
            Console.WriteLine(">>>>>");

            IOrderedEnumerable<KeyValuePair<int, List<int>>> oe = results.OrderBy(x => x.Value.Count * -1);
            int count = 1;
            foreach (KeyValuePair<int, List<int>> keyValuePair in oe)
            {
                Console.WriteLine("{0}: [{1}]\t{2}", count, 
                    songNames[keyValuePair.Key],
                    keyValuePair.Value.Count);
                keyValuePair.Value.Sort();
                foreach (int timeSpan in keyValuePair.Value)
                    Console.Write("{0},", timeSpan);
                Console.WriteLine();
                List<KeyValuePair<int, int>> countSpan = GetBestTwoSpan(keyValuePair.Value);
                for (int i = 0; i < countSpan.Count && i < 2; i++)
                {
                    KeyValuePair<int, int> cSpan = countSpan[i];
                    Console.WriteLine("<Span-Count:{0}-{1}>", cSpan.Value, cSpan.Key);
                }
                if (count == 5)
                    break;
                count++;
            }
        }


        private List<KeyValuePair<int, int>> GetBestTwoSpan(List<int> timeSpans)
        {
            List<KeyValuePair<int, int>> countSpan = new List<KeyValuePair<int, int>>();
            int count = 0;
            int value = int.MaxValue;
            foreach (int span in timeSpans)
            {
                if (value == int.MaxValue)
                {
                    value = span;
                    count = 1;
                }
                else
                {
                    if (Math.Abs(span - value) <= 1)
                    {
                        count++;
                        value = span;
                    }
                    else
                    {
                        countSpan.Add(new KeyValuePair<int, int>(count, value));
                        count = 1;
                        value = span;
                    }
                }
            }
            countSpan.Sort(
                delegate(KeyValuePair<int, int> firstPair,
                KeyValuePair<int, int> secondPair)
                {
                    return secondPair.Key.CompareTo(firstPair.Key);
                }
            );
                                    
            return countSpan;
        }

        private void AddHash(int songID, long[] hashes)
        { 
            for(int i = 0, size = hashes.Length; i < size; i++)
            {
                long hash = hashes[i];
                List<DataPoint> pointList = null;
                if(hashMap.ContainsKey(hash))
                    pointList = hashMap[hash];
                else
                {
                    pointList = new List<DataPoint>();
                    hashMap.Add(hash, pointList);
                }

                pointList.Add(new DataPoint(i, songID));
            }
        }

        public void Save(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName, false))
            {
                sw.WriteLine(songNames.Count);
                foreach (string name in songNames)
                {
                    sw.WriteLine(name);
                }

                sw.WriteLine(hashMap.Count);
                Dictionary<long, List<DataPoint>>.Enumerator en = hashMap.GetEnumerator();
                while(en.MoveNext())
                {
                    KeyValuePair<long, List<DataPoint>> current = en.Current;
                    sw.Write(current.Key);
                    foreach (DataPoint dataPoint in current.Value)
                    {
                        sw.Write(string.Format("\t{0},{1}", dataPoint.Time, dataPoint.SongID));
                    }
                    sw.WriteLine();
                }
                sw.Flush();
            }
        }

        public void Load(string fileName)
        {
            using (StreamReader sr = new StreamReader(fileName))
            {
                songNames.Clear();
                int nameCount = int.Parse(sr.ReadLine());
                for (int i = 0; i < nameCount; i++ )
                {
                    songNames.Add(sr.ReadLine());
                }

                hashMap.Clear();
                int hashCount = int.Parse(sr.ReadLine());
                for (int i = 0; i < hashCount; i++)
                { 
                    string record = sr.ReadLine();
                    string[] items = record.Split('\t');

                    long hash = long.Parse(items[0]);
                    List<DataPoint> pointList = new List<DataPoint>();
                    for (int j = 1; j < items.Length; j++)
                    {
                        string[] values = items[j].Split(',');
                        int time = int.Parse(values[0]);
                        int songID = int.Parse(values[1]);
                        DataPoint dataPoint = new DataPoint(time, songID);
                        pointList.Add(dataPoint);
                    }

                    hashMap.Add(hash, pointList);
                }
            }
        }
    }
}
