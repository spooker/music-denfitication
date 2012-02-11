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
        static void Main(string[] args)
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
                    BuildDataBase(secondArg, new KeyPointHash(), thirdArg, shutDown);
                }
                else if (firstArg.CompareTo("test") == 0)
                {
                    string dataBaseFile = secondArg;
                    DataBase.SimpleTest(dataBaseFile, new KeyPointHash(), true);
                }
                else if (firstArg.CompareTo("convert") == 0)
                { 
                    string inputFile = args[1];
                    string outputFile = args[2];
                    DataBase.ConvertIndexFileFromTextToBinary(inputFile, outputFile, new KeyPointHash());
                }
                else if (firstArg.CompareTo("append") == 0)
                {
                    string indexFile = secondArg;
                    string dataFolder = args[2];

                    AppendData(indexFile, new KeyPointHash(), dataFolder);
                }
                else if (firstArg.CompareTo("combine") == 0)
                {
                    if (args.Length < 3)
                    {
                        PrintUsage();
                    }
                    else
                    {
                        string listFile = args[1];
                        string outputFile = args[2];
                        using (StreamReader sr = new StreamReader(listFile, Encoding.GetEncoding("GB2312")))
                        {
                            DataBase finalDataBase = null;

                            while (!sr.EndOfStream)
                            {
                                string indexFile = sr.ReadLine();
                                if (!File.Exists(indexFile))
                                {
                                    Console.WriteLine("File doesn't exist: {0}", indexFile);
                                    continue;
                                }

                                DataBase tmpDataBase = new DataBase(new KeyPointHash());
                                tmpDataBase.Load(indexFile);
                                if (finalDataBase == null)
                                    finalDataBase = tmpDataBase;
                                else
                                {
                                    Console.WriteLine("Combining...");
                                    finalDataBase.Combine(tmpDataBase);
                                }
                            }

                            finalDataBase.Save(outputFile);
                        }
                    }
                }
                else
                {
                    PrintUsage();
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

Usage : Build | Test | Combine | Append | Convert
    Build MusicFolder IndexFile
        MusicFolder:    search all mp3 files under MusicFolder and build index;
        IndexFile:      the index are saved to IndexFile;

    Test IndexFile
        IndexFile:      the index file created by Build command;

    Combine ListFile OutputFile
        ListFile: contains the path of all index files;
        OutputFile:     is the combined index file

    Append IndexFile MusicFolder
        IndexFile:      the index file created by Build command;
        MusicFolder:    search all mp3 files under MusicFolder and build index,
                        only the files which are not indexed will be add;
    Convert TextIndexFile BinaryIndexFile
        TextIndexFile   index file in text format
        BinaryIndexFile index file in binary format";

            WriteLog(usage);
        }

        static void WriteLog(string log)
        {
            Console.WriteLine(log);
        }

        static void AppendData(string indexFile, IHashMaker hashMaker, string dataFolder)
        {
            if (!Directory.Exists(dataFolder))
            {
                WriteLog(string.Format("'{0}' doesn't exist! ", dataFolder));
                return;
            }

            if (!File.Exists(indexFile))
            {
                WriteLog(string.Format("'{0}' file doesn't exist! ", indexFile));
                return;
            }

            DataBase dataBase = new DataBase(hashMaker);
            dataBase.CheckDuplicate = true;
            dataBase.Load(indexFile);
            dataBase.BuildDataBase(dataFolder);
            dataBase.Save(indexFile);    
        }

        static void BuildDataBase(string dataFolder, IHashMaker hashMaker, string dataBaseFile, bool shutDown)
        {
            if (!Directory.Exists(dataFolder))
            {
                WriteLog(string.Format("'{0}' doesn't exist! ", dataFolder));
                return;
            }

            DataBase dataBase = new DataBase(hashMaker);
            dataBase.BuildDataBase(dataFolder);
            dataBase.SaveInBinary(dataBaseFile);
            if (shutDown)
                Utility.ShutDown();
        }

        static void Main1(string[] args)
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
            //PlayMp3();
            //RunRecordAutoTest(false);
            //ConvertTextIndexToBinary();
            ImprovedTest(
                @"I:\all.index", @"I:\all.bin", @"I:\xxx.xxx"
                //@"I:\ZhangYuSheng.index", @"I:\ZhangYuSheng.bin", @"I:\ZhangYuSheng.text"
                );
        }

        static void ImprovedTest(string indexFile, string binaryFile, string newTextFile)
        {
            //DataBase.SimpleTest(binaryFile, new KeyPointHash(), true);
            ImprovedDataBase.SimpleTest(binaryFile, new KeyPointHash(), true);
            //DataBase dataBase = new DataBase(new KeyPointHash());
            //dataBase.LoadFromBinary(binaryFile);
            //dataBase.SaveInBinary(newBinaryFile);

            //ImprovedDataBase dataBase = new ImprovedDataBase(new KeyPointHash());
            //dataBase.Load(indexFile);
            //dataBase.SaveInBinary(binaryFile);
            //dataBase.LoadFromBinary(binaryFile);
            //dataBase.Save(newTextFile);
        }

        static void ConvertTextIndexToBinary()
        {
            StreamReader sr = new StreamReader(@"I:\indexFiles.txt", Encoding.GetEncoding("GB2312"));

            IHashMaker hashMaker = new KeyPointHash();

            while (!sr.EndOfStream)
            {
                string inputFile = sr.ReadLine();
                string outputFile = inputFile.Replace(".index", ".bin");

                DataBase.ConvertIndexFileFromTextToBinary(inputFile, outputFile, hashMaker);
            }
        }

        static void RunRecordAutoTest(bool shutDown)
        {

            //DataBase dataBase = new DataBase(new LongHash());
            //dataBase.Load(@"D:\Music\Avatar\DataBase.txt");
            DataBase dataBase = new DataBase(new KeyPointHash());
            dataBase.Load(@"D:\Music\DataBase.txt");
            dataBase.Quiet = true;

            AutoTest.TEST_COUNT = 10;
            AutoTest.SHIFT_COUNT = 64;
            //AutoTest.Test(@"D:\Music\李建.-.[音乐傲骨].专辑.(MP3)\", "*.wav", @"D:\Music\李建.-.[音乐傲骨].专辑.(MP3)\FileIndex_4_8_keyPoint.txt", dataBase);
            AutoTest.AnalyseFailure(@"D:\Music\李建.-.[音乐傲骨].专辑.(MP3)\", "*.wav", @"D:\Music\李建.-.[音乐傲骨].专辑.(MP3)\FileIndex_4_8_keyPoint.txt", dataBase);
            if (shutDown)
                Utility.ShutDown();
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

            AutoTest.Test(@"D:\Music\", ".mp3", @"D:\Music\Avatar\FileIndex_3_26_keyPoint.txt", dataBase);
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
