using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Exocortex.DSP;
using System.Threading;

namespace Shazam
{
    class Program
    {
        static void Main(string[] args)
        {
            ////FFT1.main(null);
            ////return;

            //Mp3ToWavConverter converter = new Mp3ToWavConverter();
            ////converter.Convert(@"D:\Sound\Shazam\Shazam\moumoon-sunshine_girl.mp3", @"D:\Sound\Shazam\Shazam\moumoon-sunshine_girl.wav");

            //byte[] audio = converter.ReadBytes(@"D:\Sound\Shazam\Shazam\moumoon-sunshine_girl.mp3");

            ////for (int i = 0; i < 200; i++ )
            ////{
            ////    Debug.WriteLine(bytes[i].ToString());
            ////}

            //PointsFinder pointFinder = new PointsFinder();
            //Complex[][] results = pointFinder.FFT(audio);
            //int[][] lines = pointFinder.GetKeyPoints(results);

            //DataBase dataBase = new DataBase();
            //dataBase.AddNewSong(@"D:\Sound\Shazam\Shazam\moumoon-sunshine_girl.mp3");


            //MicRecorder recoder = new MicRecorder();
            //recoder.RecStart(20);
            //byte[] audio = recoder.GetAudioData();

            //dataBase.IndexSong(audio);
            //int debug = 2;

            if (args.Length < 2)
            {
                Console.WriteLine("[usage] build|test datafolder");
                return;
            }
            else
            {
                string firstArg = args[0].ToLower();
                if (firstArg.CompareTo("build") == 0)
                {
                    BuildDataBase(args[1]);
                }
                else if (firstArg.CompareTo("test") == 0)
                {
                    Test(args[1]);
                }
                else if (firstArg.CompareTo("simple") == 0)
                {
                    SimpleTest(args[1]);
                }
                else if (firstArg.CompareTo("stat") == 0)
                {
                    CaculateMiddleValue(args[1]);
                }
                else if (firstArg.CompareTo("randc") == 0)
                {
                    RecordAndCaculate();
                }
                else if (firstArg.CompareTo("auto") == 0)
                {
                    AutoTest(args[1], bool.Parse(args[2]));
                }
            }
        }

        private static Thread recorderThread = null;
        public static void Test(string dataFolder)
        {
            DataBase dataBase = new DataBase();
            if(!dataFolder.EndsWith("\\"))
                dataFolder += "\\";
            string fileName = "database.txt";
            Console.WriteLine("Loading database from file...");
            dataBase.Load(dataFolder + fileName);
            Console.WriteLine("Loading database done.");
            
            int seconds = 15;
            int minSec = 5;
            int interval = 2;

            while (true)
            {
                Console.WriteLine("Press any key to identify a new song, press ESC to exit.\n");
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                if(keyInfo.Key == ConsoleKey.Escape)
                    break;

                Console.WriteLine("Start recording audio from mic ...", seconds);
                MicRecorder recorder = new MicRecorder();
                recorder.Seconds = seconds;
                recorder.RequestStop = false;
                recorderThread = new Thread(new ThreadStart(recorder.RecStart));
                recorderThread.Start();
                DateTime startTime = DateTime.Now;
                Thread.Sleep(minSec * 1000);
                for (int t = minSec; t < seconds; t += interval)
                {
                    byte[] audio = recorder.GetAudioData();
                    Console.WriteLine("Length of audio data is {0}.", audio.Length);
                    List<int> ids = new List<int>();
                    for (int i = 0; i < 10; i++)
                    {
                        int offSet = i * 400;
                        int length = audio.Length - offSet;
                        byte[] tmp = new byte[length];
                        Array.Copy(audio, offSet, tmp, 0, length);
                        //Console.WriteLine(">>>>>>>>>>>>>>Offset is {0}<<<<<<<<<<<<<<<", offSet);
                        //Console.WriteLine("Start indexing the song in database...");
                        Dictionary<int, List<int>> results = dataBase.IndexSong(tmp);
                        //Console.WriteLine("Indexing done. Printing the result..");
                        //dataBase.Print(results);
                        //dataBase.GetBestHitByHitCount(results, false);
                        int id = dataBase.GetBestHitBySpanMatch(results, true);
                        ids.Add(id);
                        if (i == 0)
                        {
                            Console.WriteLine("The default result is: {0}", dataBase.GetNameByID(id));
                        }
                    }
                    KeyValuePair<int, int> topHit = Utility.GetTopHit(ids);
                    foreach (int id in ids)
                        Console.Write("{0},", id);
                    Console.WriteLine();
                    Console.WriteLine("The best result is: {0}, <{1},{2}>", 
                        dataBase.GetNameByID(topHit.Key), topHit.Key, topHit.Value);
                    if (topHit.Value >= 5)
                    {
                        Console.WriteLine("It takes {0} seconds to identify this song.", DateTime.Now.Subtract(startTime).Seconds);
                        break;
                    }                    
                    Thread.Sleep(interval * 1000);
                }
                recorder.RequestStop = true;
                recorderThread.Join();
            }
        }

        private static Mp3ToWavConverter converter = new Mp3ToWavConverter();
        private static DataBase Load(string dataFolder)
        {
            DataBase dataBase = new DataBase();
            if (!dataFolder.EndsWith("\\"))
                dataFolder += "\\";
            string fileName = "database.txt";
            Console.WriteLine("Loading database from file...");
            dataBase.Load(dataFolder + fileName);
            Console.WriteLine("Loading database done.");
            return dataBase;
        }



        private static Random random = new Random();
        private static int GetRandomStartIndex(int length)
        {
            int value = random.Next(length - 441000);
            return value;
        }

        public static void AutoTest(string dataFolder, bool loadIndex)
        {
            DataBase dataBase = Load(dataFolder);

            FileInfo[] fileInfoArray = GetAllMp3Files(dataFolder);

            int bSuccessCount = 0;
            int total = fileInfoArray.Length;

            if (!loadIndex)
            {
                using (StreamWriter sw = new StreamWriter(@"d:\music\fileIndex.txt"))
                {

                    foreach (FileInfo fileInfo in fileInfoArray)
                    {
                        string fileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 4);
                        Console.Write(fileName);
                        sw.Write(fileName);

                        byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(fileInfo.FullName);
                        if (audio.Length < 441000)
                        {
                            Console.WriteLine();
                            sw.WriteLine();
                            continue;
                        }
                        byte[] audioSegment = new byte[441000];

                        for (int i = 0; i < 5; i++)
                        {
                            int startIndex = GetRandomStartIndex(audio.Length);
                            Array.Copy(audio, startIndex, audioSegment, 0, 441000);
                            sw.Write("\t{0}", startIndex);

                            int id = GetBestHit(audioSegment, dataBase, 16);

                            string name = dataBase.GetNameByID(id);
                            bool bSuccess = (fileName.CompareTo(name) == 0);
                            Console.Write(bSuccess ? "\t++" : "\t--");

                            bSuccessCount += bSuccess ? 1 : 0;
                        }
                        Console.WriteLine();
                        sw.WriteLine();
                        sw.Flush();
                    }
                }

            }
            else
            {
                using (StreamReader sr = new StreamReader(@"d:\music\fileIndex.txt"))
                {

                    foreach (FileInfo fileInfo in fileInfoArray)
                    {
                        string fileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 4);                        
                        Console.Write(fileName);
                        string line = sr.ReadLine();
                        string[] indices = line.Split('\t');

                        byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(fileInfo.FullName);
                        if (audio.Length < 441000)
                        {
                            Console.WriteLine();
                            continue;
                        }
                        byte[] audioSegment = new byte[441000];

                        for (int i = 0; i < 5; i++)
                        {
                            int startIndex = int.Parse(indices[i + 1]);
                            Array.Copy(audio, startIndex, audioSegment, 0, 441000);

                            int id = GetBestHit(audioSegment, dataBase, 16);

                            string name = dataBase.GetNameByID(id);
                            bool bSuccess = (fileName.CompareTo(name) == 0);
                            Console.Write(bSuccess ? "\t++" : "\t--");

                            bSuccessCount += bSuccess ? 1 : 0;
                        }
                        Console.WriteLine();
                    }
                }
            }
            Console.WriteLine("accuray rate: {0}", (double)bSuccessCount/total/5);
        }

        private static int GetBestHit(byte[] audio, DataBase dataBase, int shiftCount)
        {
            byte[] tmp = null;
            int max = 4096;
            int step = max / shiftCount;

            //Dictionary<int, int> hitCount = new Dictionary<int, int>();
            //Dictionary<int, int> hitScore = new Dictionary<int, int>();

            int maxScore = -1;
            int bestId = -1;
            for (int i = 0; i < max; i += step)
            { 
                tmp = new byte[audio.Length - i];
                Array.Copy(audio, i, tmp, 0, tmp.Length);                

                Dictionary<int, List<int>> results = dataBase.IndexSong(tmp);
                int score = 0;
                int id = dataBase.GetBestHitBySpanMatch(results, ref score, false);
                if (score > maxScore)
                {
                    maxScore = score;
                    bestId = id;
                }

                //int n = 3;
                //List<KeyValuePair<int, int>> bestHit = dataBase.GetBestNHitBySpanMatch(results, n, true);
                //foreach (KeyValuePair<int, int> hit in bestHit)
                //{
                //    if (!hitCount.ContainsKey(hit.Value))
                //    {
                //        hitCount[hit.Value] = 1;
                //        hitScore[hit.Value] = hit.Key;
                //    }
                //    else
                //    {
                //        hitCount[hit.Value] += 1;
                //        hitScore[hit.Value] += hit.Key;
                //    }
                //}
            }

            //var countList = hitCount.OrderBy(d => d.Value * -1);
            //int count = 0;
            //int topCount = 4;
            //Console.WriteLine("Sorted by Count:");
            //foreach (var s in countList)
            //{
            //    Console.WriteLine("\t+++ {0} - {1}", dataBase.GetNameByID(s.Key), s.Value);
            //    if (++count > topCount)
            //        break;
            //}

            //var valueList = hitScore.OrderBy(d => d.Value * -1);
            //count = 0;
            //Console.WriteLine("Sorted by Score:");
            //foreach (var s in valueList)
            //{
            //    Console.WriteLine("\t+++ {0} - {1}", dataBase.GetNameByID(s.Key), s.Value);
            //    if (++count > topCount)
            //        break;
            //}



            //Console.WriteLine("The name of this song is : {0}", dataBase.GetNameByID(bestId));
            return bestId;
        }

        public static void SimpleTest(string dataFolder)
        {
            DataBase dataBase = Load(dataFolder);

            int seconds = 10;

            while (true)
            {
                Console.WriteLine("Press any key to identify a new song, press ESC to exit.\n");
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                if (keyInfo.Key == ConsoleKey.Escape)
                    break;

                byte[] audio = null;
                if (keyInfo.Key == ConsoleKey.A)
                {
                    Console.WriteLine("Load the wav file...");
                    audio = Mp3ToWavConverter.ReadBytesFromWav("tmp.wav");
                }
                else
                {
                    Console.WriteLine("Start recording audio from mic ...", seconds);
                    MicRecorder recorder = new MicRecorder();
                    recorder.Seconds = seconds;
                    recorder.RequestStop = false;
                    recorder.RecStart();

                    audio = recorder.GetAudioData();
                    Console.WriteLine("Length of audio data is {0}.", audio.Length);
                }

                //int id = GetBestHit(audio, dataBase, 4);
                //id = GetBestHit(audio, dataBase, 8);
                int id = GetBestHit(audio, dataBase, 16);
            }
        }

        public static void BuildDataBase(string dataFolder)
        {
            //string dataFolder = @"D:\Sound\Shazam\Shazam\TestData\";
            if(!dataFolder.EndsWith("\\"))
                dataFolder += "\\";
            if(!Directory.Exists(dataFolder))
                return;

            string dataBaseFileName = "database.txt";
            DataBase dataBase = new DataBase();

            DirectoryInfo dirInfo = new DirectoryInfo(dataFolder);
            FileInfo[] files = dirInfo.GetFiles("*.mp3", SearchOption.AllDirectories);
            Console.WriteLine("There are total {0} songs. Star indexing...", files.Length);
            int count = 0;
            foreach (FileInfo file in files)
            {
                count++;
                if (file.FullName.EndsWith(".mp3"))
                {
                    Console.WriteLine("Indexing {0}: {1}..", count, file.Name);
                    dataBase.AddNewSong(file.FullName);
                }

                if (count % 100 == 0)
                {
                    Console.WriteLine("Saving index to the file.");
                    dataBase.Save(dataFolder + dataBaseFileName);
                }
            }
            Console.WriteLine("Indexing done. Saving to the file.");
            dataBase.Save(dataFolder + dataBaseFileName);
        }

        private static FileInfo[] GetAllMp3Files(string dataFolder)
        {
            if (!dataFolder.EndsWith("\\"))
                dataFolder += "\\";
            if (!Directory.Exists(dataFolder))
                return null;

            DirectoryInfo dirInfo = new DirectoryInfo(dataFolder);
            FileInfo[] files = dirInfo.GetFiles("*.mp3", SearchOption.AllDirectories);

            return files;
        }

        public static void CaculateMiddleValue(string dataFolder)
        {
            FileInfo[] files = GetAllMp3Files(dataFolder);
            if (files == null)
                return;

            foreach (FileInfo file in files)
            {
                byte[] data = Mp3ToWavConverter.ReadBytesFromMp3(file.FullName);

                double min = double.MaxValue;
                double max = double.MinValue;
                double mid = double.NaN;
                mid = Utility.GetAverage(data, ref min, ref max);

                Console.WriteLine("<{0},{1}> {2}", min, max, mid);
            }
        }

        public static void RecordAndCaculate()
        {
            byte[] audio = null;
            int seconds = 10;
            Console.WriteLine("Start recording audio from mic ...", seconds);
            MicRecorder recorder = new MicRecorder();
            recorder.Seconds = seconds;
            recorder.RequestStop = false;
            recorder.RecStart();

            audio = recorder.GetAudioData();

            double min = double.MaxValue;
            double max = double.MinValue;
            double mid = double.NaN;
            mid = Utility.GetAverage(audio, ref min, ref max);
            Mp3ToWavConverter.WriteBytesToWav("Record.wav", audio);

            Console.WriteLine("<{0},{1}> {2}", min, max, mid);
        }
    }
}
