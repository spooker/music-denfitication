using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicIdentifier;
using System.IO;

namespace Test
{
    class AutoTest
    {
        public static int TEST_COUNT = 5;
        public static int SHIFT_COUNT = 1;

        private static int SECONDS = 10;
        private static int DATA_LENGTH = SECONDS * Mp3ToWavConverter.RATE;        
        private static Random random = new Random();
        private static int GetRandomStartIndex(int length)
        {
            int value = random.Next(length - DATA_LENGTH);
            return value;
        }

        public static void Test(string dataFolder, string searchPattern, string indexFile, DataBase dataBase)
        {
            FileInfo[] fileInfoArray = Utility.GetFiles(dataFolder, searchPattern);

            int bSuccessCount = 0;
            int total = fileInfoArray.Length;

            using (StreamWriter sw = new StreamWriter(indexFile))
            {
                foreach (FileInfo fileInfo in fileInfoArray)
                {
                    string fileName = fileInfo.Name;
                    string fileNameWithoutExtention = fileName.Substring(0, fileName.Length - 4);
                    Console.Write(fileName);
                    sw.Write(fileName);

                    byte[] audio = Mp3ToWavConverter.ReadBytes(fileInfo.FullName);
                    if (audio.Length < DATA_LENGTH)
                    {
                        Console.WriteLine();
                        sw.WriteLine();
                        total--;
                        continue;
                    }

                    byte[] audioSegment = new byte[DATA_LENGTH];
                    for (int i = 0; i < TEST_COUNT; i++)
                    {
                        int startIndex = GetRandomStartIndex(audio.Length);
                        Array.Copy(audio, startIndex, audioSegment, 0, DATA_LENGTH);

                        int id = dataBase.GetBestHit(audioSegment, SHIFT_COUNT);

                        string name = dataBase.GetNameByID(id);
                        bool bSuccess = (fileNameWithoutExtention.CompareTo(name) == 0);
                        Console.Write(bSuccess ? "\t++" : "\t--");
                        if (!bSuccess)
                        {
                            sw.Write("\t{0}", startIndex);
                        }

                        bSuccessCount += bSuccess ? 1 : 0;
                    }
                    Console.WriteLine();
                    sw.WriteLine();
                    sw.Flush();
                }
            }

            Console.WriteLine("accuray rate: {0}", (double)bSuccessCount / total / TEST_COUNT);
        }

        public static void AnalyseFailure(string dataFolder, string searchPattern, string indexFile, DataBase dataBase)
        {
            dataBase.Quiet = false;
            using (StreamReader sw = new StreamReader(indexFile))
            {
                while(!sw.EndOfStream)
                {
                    string line = sw.ReadLine();
                    string[] data = line.Split('\t');

                    if (data.Length < 1)
                        continue;

                    string fileName = data[0];
                    Console.WriteLine(fileName);
                    
                    byte[] audio = Mp3ToWavConverter.ReadBytes(Path.Combine(dataFolder, fileName));
                    if (audio.Length < DATA_LENGTH)
                    {
                        Console.WriteLine();
                        continue;
                    }

                    byte[] audioSegment = new byte[DATA_LENGTH];
                    for (int i = 1; i < data.Length; i++ )
                    {
                        int startIndex = int.Parse(data[i]);
                        Array.Copy(audio, startIndex, audioSegment, 0, DATA_LENGTH);

                        int id = dataBase.GetBestHit(audioSegment, SHIFT_COUNT);

                        string name = dataBase.GetNameByID(id);
                        Console.WriteLine("\t{0}\t{1}", startIndex, name);
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
