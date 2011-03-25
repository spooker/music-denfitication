using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shazam;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //BuildDataBase();
            //RunAutoTest();
            //Mp3ToWav(args);
            //RunVolumeTest();
            //VolumeTest.SimpleTest(@"D:\Music\Avatar\DataBase.txt");
            //QueryTest.Test(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\Avatar\01-james_horner-you_dont_dream_in_cryo._..mp3");
            //ShortFileCount();
            DataBase.SimpleTest(@"D:\Music\Avatar\DataBase.txt");
            //QueryTest.TestRandom(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\Avatar\01-record.mp3");
            //QueryTest.TestRandom(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\Avatar\1.iphone.mp3");
            //QueryTest.TestRandom(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\信.-.[趁我].专辑.(MP3)\04. 独领风骚.mp3");
        }

        static void ShortFileCount()
        {
            FileInfo[] fileInfoArray = Utility.GetFiles(@"D:\Music", "*.mp3");

            int total = fileInfoArray.Length;
            Console.WriteLine(total);

            foreach (FileInfo fileInfo in fileInfoArray)
            {
                byte[] audio = Mp3ToWavConverter.ReadBytesFromMp3(fileInfo.FullName);

                if (audio.Length < 441000)
                {
                    Console.WriteLine(fileInfo.FullName);
                    total--;
                }
            }
            Console.WriteLine(total);
            Console.Beep();
        }

        static void BuildDataBase()
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.BuildDataBase(@"D:\Music\");
            dataBase.Save(@"D:\Music\Avatar\DataBase.txt");
            Utility.ShutDown();
        }

        static void RunAutoTest()
        {
            AutoTest.Test(@"D:\Music\", @"D:\Music\Avatar\DataBase.txt", @"D:\Music\Avatar\FileIndex_3_24.txt");
            Utility.ShutDown();
        }

        static void RunVolumeTest()
        {
            VolumeTest.Test(@"D:\Sound\Trunk\bin\Release\", @"D:\Music\Avatar\DataBase.txt");
        }

        static void Mp3ToWav(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: Mp3ToWav input output\n");
                return;
            }

            Mp3ToWavConverter.Convert(args[0], args[1]);
        }
    }
}
