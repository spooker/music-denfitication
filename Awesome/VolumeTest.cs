using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicIdentifier;
using System.IO;

namespace Test
{
    class VolumeTest
    {
        private static int SECONDS = 10;
        private static int DATA_LENGTH = SECONDS * Mp3ToWavConverter.RATE;
        private static int TEST_COUNT = 10;
        private static Random random = new Random();
        private static int GetRandomStartIndex(int length)
        {
            int value = random.Next(length - DATA_LENGTH);
            return value;
        }
        private static string trackName = "10.风筝";

        private static int GetBestHit(byte[] audio, DataBase dataBase, int shiftCount)
        {
            byte[] tmp = null;
            int max = 4096;
            int step = max / shiftCount;

            int maxScore = -1;
            int bestId = -1;
            for (int i = 0; i < max; i += step)
            {
                tmp = new byte[audio.Length - i];
                Array.Copy(audio, i, tmp, 0, tmp.Length);

                Dictionary<int, List<int>> results = dataBase.IndexSong(tmp, 0, tmp.Length);
                int score = 0;
                int id = dataBase.GetBestHitBySpanMatch(results, ref score, false);
                if (score > maxScore)
                {
                    maxScore = score;
                    bestId = id;
                }
            }
            Console.WriteLine("Max score : {0}", maxScore);
            return bestId;
        }

        public static void Test(string dataFolder, string dataBaseFile)
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.Load(dataBaseFile);

            FileInfo[] fileInfoArray = Utility.GetFiles(dataFolder, "*.wav");
            if(fileInfoArray.Length <= 0)
            {
                Console.WriteLine("No wav file found.");
                return;
            }
            
            int[] startIndices = new int[TEST_COUNT];
            int length = Mp3ToWavConverter.ReadBytesFromWav(fileInfoArray[0].FullName).Length;
            //Console.Write("\t");
            for(int i = 0; i < TEST_COUNT; i++)
            {
                startIndices[i] = GetRandomStartIndex(length);
                Console.Write("\t{0}", startIndices[i]);
            }
            Console.WriteLine();

            foreach (FileInfo fileInfo in fileInfoArray)
            {
                Console.Write(fileInfo.Name);
                TestSingleFile(fileInfo.FullName, startIndices, dataBase);
                Console.WriteLine();
            }
        }

        private static void TestSingleFile(string fileName, int[] startIndices, DataBase dataBase)
        {
            byte[] audio = Mp3ToWavConverter.ReadBytesFromWav(fileName);
            if (audio.Length < DATA_LENGTH)
            {
                Console.WriteLine("File too small, not enough data for test.");
                return;
            }

            byte[] audioSegment = new byte[DATA_LENGTH];
            foreach(int startIndex in startIndices)
            {
                Array.Copy(audio, startIndex, audioSegment, 0, DATA_LENGTH);
                int id = GetBestHit(audioSegment, dataBase, 16);
                string name = dataBase.GetNameByID(id);
                bool bSuccess = (name.CompareTo(trackName) == 0);
                Console.Write(bSuccess ? "\t++" : "\t--");
            }            
        }


        public static void SimpleTest(string dataBaseFile)
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.Load(dataBaseFile);

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
                Console.WriteLine("Length of audio data is {0}.", audio.Length);
                
                int id = GetBestHit(audio, dataBase, 16);
                Console.WriteLine("[Normal] The name of this song is : {0}", dataBase.GetNameByID(id));
                
                int peakPercent = 95;
                double normPercent = 1.0;
                sbyte minPeak;
                sbyte maxPeak;
                sbyte peak;
                double ratio = 1.0;

                Normalize.getPeaks8(audio, out minPeak, out maxPeak);
                Console.WriteLine("Normal Peak : {0} {1}", minPeak, maxPeak);

                minPeak *= -1;
                peak = (minPeak > maxPeak) ? minPeak : maxPeak;
                                    
                ratio = 127.0 / peak * normPercent;
                Console.WriteLine("Ratio : {0}", ratio);
                byte[] newData = Normalize.Amplify(audio, ratio);

                id = GetBestHit(newData, dataBase, 16);
                Console.WriteLine("[Normal Peak] The name of this song is : {0}", dataBase.GetNameByID(id));


                if (!Normalize.getSmartPeaks8(audio, peakPercent, out minPeak, out maxPeak))
                    Console.WriteLine("Error in Normalize.getSmartPeaks8");
                else
                {
                    Console.WriteLine("Smart Peak : {0} {1}", minPeak, maxPeak);
                    minPeak *= -1;
                    peak = (minPeak > maxPeak) ? minPeak : maxPeak;
                                        
                    ratio = 127.0 / peak * normPercent;
                    Console.WriteLine("Ratio : {0}", ratio);
                    newData = Normalize.Amplify(audio, ratio);

                    Normalize.getPeaks8(newData, out minPeak, out maxPeak);
                    Console.WriteLine("Normal Peak After Amplify : {0} {1}", minPeak, maxPeak);

                    id = GetBestHit(newData, dataBase, 16);
                    Console.WriteLine("[Smart Peak] The name of this song is : {0}", dataBase.GetNameByID(id));
                }

                
            }
        }        
    }
}
