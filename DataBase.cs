using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Exocortex.DSP;
using System.Diagnostics;
using System.Drawing;

namespace Shazam
{
    public class DataBase
    {
        private List<string> songNames = new List<string>();
        private Dictionary<long, List<DataPoint>> hashMap = new Dictionary<long, List<DataPoint>>();        
        IHashMaker hashMaker;

        public DataBase(IHashMaker hasher)
        {
            hashMaker = hasher;
        }

        private long[] GetAudioHash(byte[] audio)
        {
            Complex[][] results = FFT(audio, 0, audio.Length, hashMaker.ChunkSize);
            return hashMaker.GetHash(results);
        }

        private long[] GetAudioHash(byte[] audio, int start, int length)
        {
            Complex[][] results = FFT(audio, start, length, hashMaker.ChunkSize);
            return hashMaker.GetHash(results);
        }

        //public static Complex[][] FFT(byte[] audio, int chunkSize)
        //{
        //    return FFT(audio, 0, audio.Length, chunkSize);
        //}

        public static Complex[][] FFT(byte[] audio, int start, int length, int chunkSize)
        {
            int totalSize = length;
            int amountPossible = totalSize / chunkSize;

            //When turning into frequency domain we'll need complex numbers:
            Complex[][] results = new Complex[amountPossible][];
            //For all the chunks:
            for (int i = 0; i < amountPossible; i++)
            {
                Complex[] complex = new Complex[chunkSize];
                for (int j = 0; j < chunkSize; j++)
                {
                    //Put the time domain data into a complex number with imaginary part as 0:
                    complex[j] = new Complex(audio[start++], 0);
                }
                
                //Perform FFT analysis on the chunk:
                Fourier.FFT(complex, complex.Length, FourierDirection.Forward);
                results[i] = complex;

            }
            return results;
        }
        public string GetNameByID(int id)
        {
            if (id < 0 || id >= songNames.Count)
                throw new ArgumentOutOfRangeException("id");

            return songNames[id];
        }
        public int GetIDByName(string name)
        {
            int i = 0;
            foreach (string fileName in songNames)
            {
                if (fileName.CompareTo(name) == 0)
                    return i;
                i++;
            }

            return -1;
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
        private void AddHash(int songID, long[] hashes)
        {
            for (int i = 0, size = hashes.Length; i < size; i++)
            {
                long hash = hashes[i];
                List<DataPoint> pointList = null;
                if (hashMap.ContainsKey(hash))
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
                while (en.MoveNext())
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
            Console.WriteLine("Loading data base from file: {0} ...", fileName);
            using (StreamReader sr = new StreamReader(fileName))
            {
                songNames.Clear();
                int nameCount = int.Parse(sr.ReadLine());
                for (int i = 0; i < nameCount; i++)
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
            Console.WriteLine("Done!");
        }
        public void BuildDataBase(string dataFolder)
        {
            if (!dataFolder.EndsWith("\\"))
                dataFolder += "\\";
            if (!Directory.Exists(dataFolder))
                return;

            String tmpFileName = Path.GetTempFileName();

            FileInfo[] files = Utility.GetFiles(dataFolder, "*.mp3");
            Console.WriteLine("There are total {0} songs. Star indexing...", files.Length);
            int count = 0;
            foreach (FileInfo file in files)
            {
                count++;
                Console.WriteLine("Indexing {0}: {1}..", count, file.Name);
                AddNewSong(file.FullName);

                if (count % 100 == 0)
                {
                    Console.WriteLine("Saving index to tmp file:" + tmpFileName);
                    Save(tmpFileName);
                }
            }
            Console.WriteLine("Indexing done. Saving to the file.");
            File.Delete(tmpFileName);
        }

        public Dictionary<int, List<int>> IndexSong(byte[] audio, int start, int length)
        {
            Dictionary<int, List<int>> results = new Dictionary<int, List<int>>();
            long[] hashes = GetAudioHash(audio, start, length);
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
                if (countSpan.Count >= 2 && Math.Abs(countSpan[0].Value - countSpan[1].Value) < 10)
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
                    //if (Math.Abs(span - value) <= 1)
                    if (span == value)
                    {
                        count++;
                        //value = span;
                    }
                    else
                    {
                        countSpan.Add(new KeyValuePair<int, int>(count, value));
                        count = 1;
                        value = span;
                    }
                }
            }
            countSpan.Add(new KeyValuePair<int, int>(count, value));

            countSpan.Sort(
                delegate(KeyValuePair<int, int> firstPair,
                KeyValuePair<int, int> secondPair)
                {
                    return secondPair.Key.CompareTo(firstPair.Key);
                }
            );
                                    
            return countSpan;
        }
        public int GetBestHit(byte[] audio, int shiftCount)
        {
            return GetBestHit(audio, 0, audio.Length, shiftCount);
        }

        private void StatisticOnResult(IEnumerable<KeyValuePair<int, List<int>>> results)
        {
            List<int> count = new List<int>();
            foreach (KeyValuePair<int, List<int>> value in results)
            {
                count.Add(value.Value.Count);
            }

            count.Sort();
            Dictionary<int, int> keyCount = Utility.Count(count);

            foreach (KeyValuePair<int, int> pair in keyCount)
            {
                Console.WriteLine("{0} - {1}", pair.Key, pair.Value);
            }
        }

        public double MaxScore = 0;
        public int GetBestHit(byte[] audio, int start, int length, int shiftCount)
        {
            int max = hashMaker.ChunkSize;
            int step = max / shiftCount;

            int maxScore = -1;
            int bestId = -1;
            for (int i = 0; i < max; i += step)
            {
                int startIndex = start + i;
                int newLength = length - i;

                Dictionary<int, List<int>> results = IndexSong(audio, startIndex, newLength);
                IEnumerable<KeyValuePair<int, List<int>>> query = results.Where(result => result.Value.Count > 5);
                //StatisticOnResult(query);
                Dictionary<int, List<int>> filteredResults = new Dictionary<int, List<int>>();
                foreach (KeyValuePair<int, List<int>> keyValue in query)
                {
                    filteredResults.Add(keyValue.Key, keyValue.Value);
                }
                if (filteredResults.Count <= 0)
                {
                    continue;
                }

                int score = 0;
                int id = GetBestHitBySpanMatch(filteredResults, ref score, false);
                Console.WriteLine("[ID]{0} [Score]{1} [Name]{2}", id, score, GetNameByID(id));
                if (score > maxScore)
                {
                    maxScore = score;
                    bestId = id;
                }
            }
            MaxScore = maxScore;
            return bestId;
        }

        private static int SECONDS = 10;
        private static int DATA_LENGTH = SECONDS * Mp3ToWavConverter.RATE;
        public void QuestSigleFile(string file)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(file);

            int chunkSize = hashMaker.ChunkSize * 2;

            Console.WriteLine("Total {0}", (audio.Length - DATA_LENGTH) / chunkSize);
            int startIndex = 0;
            int iCount = 1;
            byte[] audioSegment = new byte[DATA_LENGTH];
        
            while (audio.Length >= startIndex + DATA_LENGTH)
            {
                Console.Write("{0}\tStart index: {1}", iCount++, startIndex);
                //Array.Copy(audio, startIndex, audioSegment, 0, DATA_LENGTH);
                int id = GetBestHit(audio, startIndex, DATA_LENGTH, 1);

                string name = GetNameByID(id);
                bool bSuccess = (fileName.CompareTo(name) == 0);
                Console.WriteLine(bSuccess ? "\t++" : "\t--");

                if (!bSuccess)
                {
                    int debug = 2;
                }
                startIndex += chunkSize;
            }
        }

        public void QuestSigleFile(string file, int count, int step, int offset)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(file);

            int chunkSize = hashMaker.ChunkSize * 2;

            Console.WriteLine("Total {0}", (audio.Length - DATA_LENGTH) / chunkSize);

            for(int i = 0, start = offset; i < count; i++, start += step)
            {
                if (start + DATA_LENGTH > audio.Length)
                    break;

                int id = GetBestHit(audio, start, DATA_LENGTH, 1);
                string name = GetNameByID(id);
                bool bSuccess = (fileName.CompareTo(name) == 0);

                Console.Write("{0}:{1} {2}\t", start, MaxScore, bSuccess ? "+":"-");

                if (!bSuccess)
                {
                    //Console.Write("*");
                }
            }
        }

        private static Random random = new Random();
        private static int GetRandomStartIndex(int length)
        {
            int value = random.Next(length - DATA_LENGTH);
            return value;
        }

        public void RandomQuerySigleFile(string file, int count)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            fileName = "01-james_horner-you_dont_dream_in_cryo._.";
            string fileDir = Path.GetDirectoryName(file);
            byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(file);

            Console.WriteLine("File:" + fileName);
            Console.WriteLine("ID:" + GetIDByName(fileName));
            Console.WriteLine("Test Count:{0}", count);

            for (int i = 0; i < count; i++)
            {
                int start = GetRandomStartIndex(audio.Length);
                start = 10321975;
                int id = GetBestHit(audio, start, DATA_LENGTH, 1);
                string name = "***name id out of range***";
                try
                {
                    name = GetNameByID(id);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ID = {0}", id);
                    Console.WriteLine(e.Message);
                }
                //bool bSuccess = (fileName.CompareTo(name) == 0);
                //Console.Write("{0}:{1} {2}\t", start, MaxScore, bSuccess ? "+" : "-");
                Console.WriteLine("{0}\t{2}\t{3}:{1}", i, name, start, MaxScore);

                if (name.CompareTo(fileName) != 0)
                {
                    Mp3ToWavConverter.WriteBytesToWav(string.Format(@"{0}\BadCase-{1}.wav", fileDir, start), audio, start, DATA_LENGTH); 
                }
            }
        }

        public static void SimpleTest(string dataFolder)
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.Load(dataFolder);
            int seconds = 10;

            while (true)
            {
                Console.WriteLine("Press any key to identify a new song, press ESC to exit.\n");
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                if (keyInfo.Key == ConsoleKey.Escape)
                    break;

                byte[] audio = null;
                Console.WriteLine("Start recording audio from mic ...", seconds);
                MicRecorder recorder = new MicRecorder();
                recorder.Seconds = seconds;
                recorder.RequestStop = false;
                recorder.RecStart();

                audio = recorder.GetAudioData();
                //Console.WriteLine("Length of audio data is {0}.", audio.Length);
                
                int id = dataBase.GetBestHit(audio, 16);
                Console.WriteLine(dataBase.GetNameByID(id));
            }
        }
    }
}
