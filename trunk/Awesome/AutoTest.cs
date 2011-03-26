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
        private static int SECONDS = 10;
        private static int DATA_LENGTH = SECONDS * Mp3ToWavConverter.RATE;
        private static int TEST_COUNT = 5;
        private static Random random = new Random();
        private static int GetRandomStartIndex(int length)
        {
            int value = random.Next(length - DATA_LENGTH);
            return value;
        }

        public static void Test(string dataFolder, string dataBaseFile, string indexFile)
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.Load(dataBaseFile);

            FileInfo[] fileInfoArray = Utility.GetFiles(dataFolder, "*.mp3");

            int bSuccessCount = 0;
            int total = fileInfoArray.Length;

            using (StreamWriter sw = new StreamWriter(indexFile))
            {
                foreach (FileInfo fileInfo in fileInfoArray)
                {
                    string fileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 4);
                    Console.Write(fileName);
                    sw.Write(fileName);

                    byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(fileInfo.FullName);
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
                        sw.Write("\t{0}", startIndex);

                        int id = dataBase.GetBestHit(audioSegment, 16);

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

            Console.WriteLine("accuray rate: {0}", (double)bSuccessCount / total / 5);
        }
    }
}
