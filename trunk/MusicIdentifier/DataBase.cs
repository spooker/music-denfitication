using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using Exocortex.DSP;
using fftwlib;
using System.Runtime.InteropServices;

namespace MusicIdentifier
{
    public class DataBase
    {
        private static int BINARY_HEADER = -1;

        private List<string> songNames = new List<string>();
        private Dictionary<long, List<DataPoint>> hashMap = new Dictionary<long, List<DataPoint>>();
        private HashSet<string> songNameSet = new HashSet<string>();
        
        IHashMaker hashMaker;

        public bool CheckDuplicate { set; get; }        

        public DataBase(IHashMaker hasher)
        {
            hashMaker = hasher;
            CheckDuplicate = false;
        }

        public bool Quiet
        {
            set;
            get;
        }

        #region For Unit Test
        public List<string> SongNames
        {
            get { return songNames; }
        }

        public Dictionary<long, List<DataPoint>> HashMap
        {
            get { return hashMap; }
        }

        public string GetSongNamesString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string name in songNames)
                sb.Append(name + "|");
            return sb.ToString();
        }

        public string GetHashMapString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<long, List<DataPoint>> keyValue in hashMap)
            {
                sb.Append(keyValue.Key);
                sb.Append("=>");
                foreach(DataPoint dataPoint in keyValue.Value)
                {
                    sb.Append(dataPoint.ToString());
                    sb.Append(":");
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }
        #endregion
        private long[] GetAudioHash(byte[] audio)
        {
            Complex[][] results = FFT(audio, 0, audio.Length, hashMaker.ChunkSize);
            return hashMaker.GetHash(results);
        }

        private long[] GetAudioHash(byte[] audio, int start, int length)
        {
            Complex[][] results = null;
            if(UseFFTW)
                results = FFT_test(audio, start, length, hashMaker.ChunkSize);
            else
                results = FFT(audio, start, length, hashMaker.ChunkSize);
            return hashMaker.GetHash(results);
        }

        //public static Complex[][] FFT(byte[] audio, int chunkSize)
        //{
        //    return FFT(audio, 0, audio.Length, chunkSize);
        //}

        private static List<Complex[]> cache = new List<Complex[]>();
        public static Complex[][] FFT(byte[] audio, int start, int length, int chunkSize)
        {        
            int totalSize = length;            
            int amountPossible = totalSize / chunkSize;

            //When turning into frequency domain we'll need complex numbers:
            Complex[][] results = new Complex[amountPossible][];

            //For all the chunks:
            for (int i = 0; i < amountPossible; i++)
            {
                Complex[] complex = null;
                if (i >= cache.Count)
                {
                    complex = new Complex[chunkSize];
                    cache.Add(complex);
                }
                else
                    complex = cache[i];

                for (int j = 0; j < chunkSize; j++)
                {
                    //Put the time domain data into a complex number with imaginary part as 0:
                    if(complex[j] == null)
                        complex[j] = new Complex(audio[start++], 0);
                    else
                    {
                        complex[j].Re = audio[start++];
                        complex[j].Im = 0;
                    }
                }
                
                //Perform FFT analysis on the chunk:
                Fourier.FFT(complex, complex.Length, FourierDirection.Forward);
                results[i] = complex;
            }

            return results;
        }

        public bool UseFFTW { set; get; }
        public static Complex[][] FFT_test(byte[] audio, int start, int length, int chunkSize)
        {
            int totalSize = length;
            int amountPossible = totalSize / chunkSize;

            //When turning into frequency domain we'll need complex numbers:
            Complex[][] results = new Complex[amountPossible][];
            double[] tmp = new double[chunkSize * 2];

            //For all the chunks:
            double totalFFTTime = 0;
            for (int i = 0; i < amountPossible; i++)
            {                
                Complex[] complex = null;
                if (i >= cache.Count)
                {
                    complex = new Complex[chunkSize];
                    cache.Add(complex);
                }
                else
                    complex = cache[i];

                for (int j = 0; j < chunkSize; j++)
                {
                    //Put the time domain data into a complex number with imaginary part as 0:
                    if (complex[j] == null)
                        complex[j] = new Complex();
                }

                for (int j = 0; j < chunkSize; j++)
                {
                    tmp[j * 2] = audio[start++];
                    tmp[j * 2 + 1] = 0;
                }

                int len = chunkSize * 2 * sizeof(double);
                IntPtr inData = fftw.malloc(len);
                IntPtr outData = fftw.malloc(len);
                Marshal.Copy(tmp, 0, inData, tmp.Length);
                IntPtr fplan = fftw.dft_1d(chunkSize, inData, outData, fftw_direction.Forward, fftw_flags.Estimate);
                fftw.execute(fplan);
                fftw.destroy_plan(fplan);
                Marshal.Copy(outData, tmp, 0, tmp.Length);
                fftw.free(inData);
                fftw.free(outData);

                for (int j = 0; j < chunkSize; j++)
                {
                    complex[j].Re = tmp[j * 2];
                    complex[j].Im = tmp[j * 2 + 1];
                }

                //Perform FFT analysis on the chunk:
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
                string name = Path.GetFileNameWithoutExtension(filePath);
                songNames.Add(name);
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

                pointList.Add(new DataPoint((short)i, (short)songID));
            }
        }

        public void SaveInBinary(string fileName)
        {
            Console.WriteLine("Saving to binary file: {0}", fileName);
            try
            {
                TimeInterval ti = new TimeInterval();
                using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                {
                    BinaryWriter bw = new BinaryWriter(fs);

                    bw.Write(BINARY_HEADER);
                    bw.Write(songNames.Count);
                    foreach (string name in songNames)
                    {
                        bw.Write(name);
                    }

                    bw.Write(hashMap.Count);
                    Dictionary<long, List<DataPoint>>.Enumerator en = hashMap.GetEnumerator();
                    while (en.MoveNext())
                    {
                        KeyValuePair<long, List<DataPoint>> current = en.Current;
                        bw.Write(current.Key);
                        bw.Write(current.Value.Count);
                        foreach (DataPoint dataPoint in current.Value)
                        {
                            bw.Write(dataPoint.Time);
                            bw.Write(dataPoint.SongID);
                        }
                    }
                    bw.Flush();
                }
                Console.WriteLine("Done. {0}s", ti.GetDurationInSecond());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Save(string fileName)
        {
            Console.WriteLine("Saving to text file: {0}", fileName);
            try
            {
                TimeInterval ti = new TimeInterval();
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
                Console.WriteLine("Done. {0}s", ti.GetDurationInSecond());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void LoadFromBinary(string fileName)
        {
            Console.WriteLine("Loading data base from binary file: {0} ...", fileName);            
            try
            {
                TimeInterval loadTimeInterval = new TimeInterval();
                using (FileStream fr = new FileStream(fileName, FileMode.Open))
                {
                    BinaryReader br = new BinaryReader(fr);
                    songNames.Clear();

                    int header = br.ReadInt32();
                    if (header != BINARY_HEADER)
                    {
                        throw new InvalidDataException("Invalid binary index file!");
                    }

                    int nameCount = br.ReadInt32();
                    for (int i = 0; i < nameCount; i++)
                    {
                        string name = br.ReadString();
                        songNames.Add(name);
                        if (CheckDuplicate)
                            songNameSet.Add(name);
                    }

                    hashMap.Clear();
                    int hashCount = br.ReadInt32();
                    for (int i = 0; i < hashCount; i++)
                    {
                        long key = br.ReadInt64();
                        int count = br.ReadInt32();

                        List<DataPoint> pointList = new List<DataPoint>(count);
                        for (int j = 0; j < count; j++)
                        {
                            short time = br.ReadInt16();
                            short songID = br.ReadInt16();
                            DataPoint dataPoint = new DataPoint(time, songID);
                            pointList.Add(dataPoint);
                        }

                        hashMap.Add(key, pointList);
                    }
                }
                Console.WriteLine("Done! {0}s", loadTimeInterval.GetDurationInSecond());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public void Load(string fileName)
        {
            Console.WriteLine("Loading data base from file: {0} ...", fileName);            
            try
            {
                TimeInterval loadTimeInterval = new TimeInterval();
                using (StreamReader sr = new StreamReader(fileName))
                {
                    songNames.Clear();
                    int nameCount = int.Parse(sr.ReadLine());
                    for (int i = 0; i < nameCount; i++)
                    {
                        string name = sr.ReadLine();
                        songNames.Add(name);
                        if (CheckDuplicate)
                            songNameSet.Add(name);
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
                            short time = short.Parse(values[0]);
                            short songID = short.Parse(values[1]);
                            DataPoint dataPoint = new DataPoint(time, songID);
                            pointList.Add(dataPoint);
                        }

                        hashMap.Add(hash, pointList);
                    }
                }
                Console.WriteLine("Done! {0}s", loadTimeInterval.GetDurationInSecond());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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
            int unSkippedCount = 0;
            foreach (FileInfo file in files)
            {
                count++;

                string name = Path.GetFileNameWithoutExtension(file.Name);
                if (CheckDuplicate && songNameSet.Contains(name))
                {
                    Console.WriteLine("Skipping {0}: {1}..", count, file.Name);
                    continue;
                }
            
                unSkippedCount++;
                Console.WriteLine("Indexing {0}: {1}..", count, file.Name);
                AddNewSong(file.FullName);

                if (unSkippedCount % 100 == 0)
                {
                    Console.WriteLine("Saving index to tmp file:" + tmpFileName);
                    Save(tmpFileName);
                }

                if (CheckDuplicate)
                {
                    songNameSet.Add(name);
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
            List<KeyValuePair<int, int>> bestHit = SortHitBySpanMatch(results);
            int bestID = -1;

            if (bestHit.Count > 0)
            {
                bestID = bestHit[0].Value;
                score = bestHit[0].Key;
            }

            return bestID;
        }

        public bool UseFilter { set; get; }
        public bool UseSort { set; get; }
        public List<KeyValuePair<int, int>> SortHitBySpanMatch(Dictionary<int, List<int>> results)
        {
            List<KeyValuePair<int, int>> countID = new List<KeyValuePair<int, int>>();
            IEnumerable<KeyValuePair<int, List<int>>> filteredResults;
            int filteredCount = 0;
            foreach (KeyValuePair<int, List<int>> keyValuePair in results)
            {
                if (UseFilter && keyValuePair.Value.Count < 10)
                    continue;
                if(UseSort)
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
                filteredCount++;
            }
            if (UseFilter)
                Console.WriteLine("Count {0}\t Filtered Count {1}", results.Count, filteredCount);

            countID.Sort(
                delegate(KeyValuePair<int, int> firstPair,
                KeyValuePair<int, int> secondPair)
                {
                    return secondPair.Key.CompareTo(firstPair.Key);
                }
            );

            return countID;
            //List<KeyValuePair<int, int>> bestHit = new List<KeyValuePair<int, int>>();

            //for (int i = 0; i < n && i < countID.Count; i++)
            //{
            //    bestHit.Add(countID[i]);
            //}

            //if (bPrintCandidates)
            //{
            //    for (int i = 0; i < countID.Count && i < n; i++)
            //    {
            //        int id = countID[i].Value;
            //        Console.WriteLine("{0}: [{1}]", i + 1, songNames[id]);
            //        List<KeyValuePair<int, int>> countSpan = GetBestTwoSpan(results[id]);
            //        for (int j = 0; j < countSpan.Count && j < 2; j++)
            //        {
            //            KeyValuePair<int, int> cSpan = countSpan[j];
            //            Console.WriteLine("\t<Span-Count:{0}-{1}>", cSpan.Value, cSpan.Key);
            //        }
            //    }
            //}

            //return bestHit;
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

        private void Swap(ref int x, ref int y)
        {
            int tmp = x;
            x = y;
            y = tmp;
        }

        private List<KeyValuePair<int, int>> GetBestTwoSpan(List<int> timeSpans)
        {
            List<KeyValuePair<int, int>> countSpan = new List<KeyValuePair<int, int>>();
            int firstKey, firstValue;
            int secondKey, secondValue;
            firstKey = secondKey = 0;
            firstValue = 100;
            secondValue = -100;

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
                        if (count > firstKey)
                        {
                            Swap(ref firstKey, ref secondKey);
                            Swap(ref firstValue, ref secondValue);

                            firstKey = count;
                            firstValue = value;
                        }
                        else if(count > secondKey)
                        {
                            secondKey = count;
                            secondValue = value;
                        }

                        //countSpan.Add(new KeyValuePair<int, int>(count, value));
                        count = 1;
                        value = span;
                    }
                }
            }
            //countSpan.Add(new KeyValuePair<int, int>(count, value));
            if (count > firstKey)
            {
                Swap(ref firstKey, ref secondKey);
                Swap(ref firstValue, ref secondValue);

                firstKey = count;
                firstValue = value;
            }
            else if (count > secondKey)
            {
                secondKey = count;
                secondValue = value;
            }

            //countSpan.Sort(
            //    delegate(KeyValuePair<int, int> firstPair,
            //    KeyValuePair<int, int> secondPair)
            //    {
            //        return secondPair.Key.CompareTo(firstPair.Key);
            //    }
            //);
            countSpan.Add(new KeyValuePair<int, int>(firstKey, firstValue));
            countSpan.Add(new KeyValuePair<int, int>(secondKey, secondValue));
                                    
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

            HitCounter counterOne = new HitCounter(HitCounter.CounterStyle.One);
            HitCounter counterMany = new HitCounter(HitCounter.CounterStyle.Many);

            int maxScore = -1;
            int bestId = -1;
            for (int i = 0; i < max; i += step)
            {
                int startIndex = start + i;
                int newLength = length - i;

                Dictionary<int, List<int>> results = IndexSong(audio, startIndex, newLength);
                //IEnumerable<KeyValuePair<int, List<int>>> query = results.Where(result => result.Value.Count > 5);
                ////StatisticOnResult(query);
                //Dictionary<int, List<int>> filteredResults = new Dictionary<int, List<int>>();
                //foreach (KeyValuePair<int, List<int>> keyValue in query)
                //{
                //    filteredResults.Add(keyValue.Key, keyValue.Value);
                //}
                //if (filteredResults.Count <= 0)
                //{
                //    continue;
                //}

                int score = 0;
                List<KeyValuePair<int, int>> bestHit = SortHitBySpanMatch(results);

                int id = -1;
                if (bestHit.Count > 0)
                {
                    id = bestHit[0].Value;
                    score = bestHit[0].Key;
                }

                //int id = GetBestHitBySpanMatch(filteredResults, ref score, false);
                //Console.WriteLine("[ID]{0} [Score]{1} [Name]{2}", id, score, GetNameByID(id));

                counterOne.Update(id, 1);
                counterMany.Update(bestHit);
                if (score > maxScore)
                {
                    maxScore = score;
                    bestId = id;
                }
            }
            MaxScore = maxScore;

            int scoreOne = 0;
            int scoreMany = 0;
            int idOne = counterOne.GetBestID(ref scoreOne);
            int idMany = counterMany.GetBestID(ref scoreMany);
            if (!Quiet)
            {
                Console.WriteLine("[Counter One]\t[ID] {0}\t[Score] {1}\t[Name]{2}", idOne, scoreOne, GetNameByID(idOne));
                Console.WriteLine("[Counter Many]\t[ID] {0}\t[Score] {1}\t[Name]{2}", idMany, scoreMany, GetNameByID(idMany));
                Console.WriteLine("[Max Scoure]\t[ID] {0}\t[Score] {1}\t[Name]{2}", bestId, maxScore, GetNameByID(bestId));
            }

            if (maxScore < 5 && idMany != bestId)
            {
                if(!Quiet)
                    Console.WriteLine("Score is too small!");
                bestId = -1;
            }

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

        public void QuestRandomSigleFile(string file, int count, int step, int testCount, string correctName)
        {
            byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(file);

            for (int j = 0; j < testCount; j++)
            {
                int start = GetRandomStartIndex(audio.Length);
                Console.WriteLine("--------------------");
                for (int i = 0; i < count; i++, start += step)
                {
                    if (start + DATA_LENGTH > audio.Length)
                        break;

                    int id = GetBestHit(audio, start, DATA_LENGTH, 1);
                    string name = GetNameByID(id);
                    bool bSuccess = (correctName.CompareTo(name) == 0);

                    Console.WriteLine("{0}\t{1}", MaxScore, bSuccess ? "+" : "-");
                }
            }
        }

        public void QuestSigleFile(string file, int count, int step, int offset, string correctName)
        {
            byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(file);

            for(int i = 0, start = offset; i < count; i++, start += step)
            {
                if (start + DATA_LENGTH > audio.Length)
                    break;

                int id = GetBestHit(audio, start, DATA_LENGTH, 1);
                string name = GetNameByID(id);
                bool bSuccess = (correctName.CompareTo(name) == 0);

                Console.Write("{0}:{1} {2}\t", start, MaxScore, bSuccess ? "+":"-");
            }
        }

        private static Random random = new Random();
        private static int GetRandomStartIndex(int length)
        {
            int value = random.Next(length - DATA_LENGTH);
            return value;
        }

        public void RandomQuerySigleFile(string file, int count, string correctName)
        {
            //string fileName = Path.GetFileNameWithoutExtension(file);
            string fileDir = Path.GetDirectoryName(file);
            byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(file);

            Console.WriteLine("File:" + correctName);
            Console.WriteLine("ID:" + GetIDByName(correctName));
            Console.WriteLine("Test Count:{0}", count);

            for (int i = 0; i < count; i++)
            {
                int start = GetRandomStartIndex(audio.Length);
                //start = 9049082;
                int id = GetBestHit(audio, start, DATA_LENGTH, 16);
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

                if (name.CompareTo(correctName) != 0)
                {
                    Mp3ToWavConverter.WriteBytesToWav(string.Format(@"{0}\BadCase-{1}.wav", fileDir, start), audio, start, DATA_LENGTH); 
                }
            }
        }

        public static void SimpleTest(string indexFile, IHashMaker hashMaker, bool bQuiet)
        {
            bool bTextFile = IsTextFile(indexFile);
            DataBase dataBase = new DataBase(hashMaker);
            if (bTextFile)
                dataBase.Load(indexFile);
            else
                dataBase.LoadFromBinary(indexFile);
            dataBase.Quiet = bQuiet;
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
                dataBase.UseFFTW = false;
                Indexing(dataBase, audio);
                dataBase.UseFFTW = true;
                Indexing(dataBase, audio);
                dataBase.UseSort = true;
                Indexing(dataBase, audio);

                //dataBase.UseFilter = true;
                //timeInterval.Reset();
                //id = dataBase.GetBestHit(audio, 16);
                //intervalInSecond = timeInterval.GetDurationInSecond();

                //Console.WriteLine("--------------------");
                //Console.Write("Final Match ({0}s):\t", intervalInSecond);
                //if (id < 0)
                //    Console.WriteLine("No match!");
                //else
                //    Console.WriteLine(dataBase.GetNameByID(id));
                //Console.WriteLine("--------------------");
            }
        }

        private static void Indexing(DataBase dataBase, byte[] audio)
        {
            Console.WriteLine("Matching the song...");
            dataBase.UseFilter = false;
            TimeInterval timeInterval = new TimeInterval();
            int id = dataBase.GetBestHit(audio, 16);
            double intervalInSecond = timeInterval.GetDurationInSecond();

            Console.WriteLine("--------------------");
            Console.Write("Final Match ({0}s):\t", intervalInSecond);
            if (id < 0)
                Console.WriteLine("No match!");
            else
                Console.WriteLine(dataBase.GetNameByID(id));
            Console.WriteLine("--------------------");
        }

        public void Combine(DataBase other)
        { 
            if(other == null)
                return;

            short count = (short)songNames.Count;
            songNames.AddRange(other.songNames);

            foreach (KeyValuePair<long, List<DataPoint>> keyValue in other.hashMap)
            {
                List<DataPoint> values = keyValue.Value;
                for (int i = 0; i < values.Count; i++)
                {
                    values[i].SongID += count;
                }
                if (hashMap.ContainsKey(keyValue.Key))
                {
                    hashMap[keyValue.Key].AddRange(values);
                }
                else
                {
                    hashMap.Add(keyValue.Key, values);
                }
            }
        }

        public static DataBase Combine(DataBase first, DataBase second)
        {
            first.Combine(second);
            return first;
        }

        public static bool IsTextFile(string fileName)
        {
            try
            {
                using(FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    using(BinaryReader br = new BinaryReader(fs))
                    {
                        int header = br.ReadInt32();
                        if (header != BINARY_HEADER)
                            return true;
                        else
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }
        
        public static void ConvertIndexFileFromTextToBinary(string textFile, string binaryFile, IHashMaker hashMaker)
        {
            DataBase dataBase = new DataBase(hashMaker);
            dataBase.Load(textFile);
            dataBase.SaveInBinary(binaryFile);
        }
    }
}
