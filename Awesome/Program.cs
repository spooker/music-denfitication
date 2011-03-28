using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicIdentifier;
using System.IO;
using System.Threading;

namespace Test
{
    class Program
    {
        static void Main1(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }
            else
            {
                string firstArg = args[0].ToLower();
                string secondArg = args[1];
                if (firstArg.CompareTo("build") == 0)
                {
                    if (args.Length < 3)
                    {
                        WriteLog("Not enough parameters!");
                        PrintUsage();
                        return;
                    }
                    string thirdArg = args[2];
                    bool shutDown = false;
                    if (args.Length > 3)
                        shutDown = bool.Parse(args[3]);
                    BuildDataBase(secondArg, thirdArg, shutDown);
                }
                else if (firstArg.CompareTo("test") == 0)
                {
                    string dataBaseFile = secondArg;
                    DataBase.SimpleTest(dataBaseFile);
                }
            }
        }
        static void PrintUsage()
        {
            string usage =
@"This is a music identification application.
It has the same functionality as iphone app 'Shazam' and 'SoundHound'.
First, you should build index of the songs in mp3 format on your 
local machine. Then you can play any of them, this program can record 
10 seconds sound through the mic on your machin, then tells you what's
the name of that song. Enjoy it! contact:<sliveysun@gmail.com>

Usage : Shazam Build | Test 
    Build MusicFolder IndexFile
        MusicFolder:    search all mp3 files under MusicFolder and build index;
        IndexFile:      the index are saved to IndexFile;

    Test IndexFile
        IndexFile:      the index file created by Build command;";

            WriteLog(usage);
        }

        static void WriteLog(string log)
        {
            Console.WriteLine(log);
        }

        static void BuildDataBase(string dataFolder, string dataBaseFile, bool shutDown)
        {
            if (!File.Exists(dataFolder))
            {
                WriteLog(string.Format("'{0}' doesn't exist! ", dataFolder));
                return;
            }

            DataBase dataBase = new DataBase(new LongHash());
            dataBase.BuildDataBase(dataFolder);
            dataBase.Save(dataBaseFile);
            if (shutDown)
                Utility.ShutDown();
        }

        static void Main(string[] args)
        {
            //BuildDataBase();
            //RunAutoTest(false);
            //Mp3ToWav(args);
            //RunVolumeTest();
            //VolumeTest.SimpleTest(@"D:\Music\Avatar\DataBase.txt");
            //QueryTest.Test(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\Avatar\01-james_horner-you_dont_dream_in_cryo._..mp3");
            //ShortFileCount();
            //DataBase.SimpleTest(@"D:\Music\Avatar\DataBase.txt");
            //QueryTest.TestRandom(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\Avatar\01-record.mp3");
            //QueryTest.TestRandom(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\Avatar\1.iphone.mp3");
            //QueryTest.TestRandom(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\信.-.[趁我].专辑.(MP3)\04. 独领风骚.mp3");
            //QueryTest.Test(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\孙燕姿《经典全纪录 主打精华版》[224kbps VBR]\fc-懂事.mp3", "09.懂事");
            //QueryTest.TestRandom(@"D:\Music\Avatar\DataBase.txt", @"D:\Music\孙燕姿《经典全纪录 主打精华版》[224kbps VBR]\09.懂事.伴奏.mp3", "09.懂事");
            PlayMp3();
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

        static void BuildDataBase1()
        {
            DataBase dataBase = new DataBase(new LongHash());
            dataBase.BuildDataBase(@"D:\Music\");
            dataBase.Save(@"D:\Music\Avatar\DataBase.txt");
            Utility.ShutDown();
        }

        static void RunAutoTest(bool shutDown)
        {

            //DataBase dataBase = new DataBase(new LongHash());
            DataBase dataBase = new DataBase(new KeyPointHash());
            dataBase.Load(@"d:\Music\DataBase.txt");
            dataBase.Quiet = true;

            AutoTest.Test(@"D:\Music\", @"D:\Music\Avatar\FileIndex_3_26_keyPoint.txt", dataBase);
            if (shutDown)
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

        static void PlayMp3()
        {            
            //FileInfo[] fileInfos = Utility.GetFiles(@"d:\music", "*.mp3");
            MyMedia player = new MyMedia();
            player.FileName = @"D:\Music\孙燕姿《经典全纪录 主打精华版》[224kbps VBR]\07.害怕.mp3";
            player.Play();
            Console.ReadLine();
            //foreach (FileInfo fileInfo in fileInfos)
            //{
            //    Console.WriteLine(fileInfo.FullName);
            //    player.FileName = fileInfo.FullName;
            //    Console.WriteLine(player.Duration);
            //    player.Play();
            //    Console.WriteLine(player.Duration);
            //    Thread.Sleep(10000); // ten seconds
            //    player.Puase();
            //    Thread.Sleep(10000); // ten seconds
            //    player.Stop();
            //    Thread.Sleep(20000);
            //}
        }
    }
}
