using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MusicIdentifier;

namespace TestPlayer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PlayerForm());
            //PlayMp3();
        }

        static void PlayMp3()
        {
            //FileInfo[] fileInfos = Utility.GetFiles(@"d:\music", "*.mp3");
            MyMedia player = new MyMedia();
            player.FileName = @"D:\Music\孙燕姿《经典全纪录 主打精华版》[224kbps VBR]\07.害怕.mp3";
            player.Play();
            Console.ReadLine();
        }
    }
}
