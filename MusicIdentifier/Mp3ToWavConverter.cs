using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Yeti.MMedia;
using Yeti.WMFSdk;
using WaveLib;

namespace MusicIdentifier
{
    public class Mp3ToWavConverter
    {
        public static int RATE = 44100;
        public static void Convert(string mp3File, string wavFile)
        {
            using (WmaStream str = new WmaStream(mp3File, new WaveFormat(RATE, 8, 1))) 
            {
                byte[] buffer = new byte[str.SampleSize * 2];
                AudioWriter writer = new WaveWriter(new FileStream(wavFile, FileMode.Create), str.Format);
                try
                {
                    int read;
                    while ((read = str.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, read);
                    }
                }
                finally
                {
                    writer.Close();
                }
            } //str.Close() is automatically called by Dispose.            
        }

        public static void WriteBytesToWav(string wavFile, byte[] data)
        {
            WriteBytesToWav(wavFile, data, 0, data.Length);
        }

        public static void WriteBytesToWav(string wavFile, byte[] data, int index, int count)
        {
            AudioWriter writer = new WaveWriter(new FileStream(wavFile, FileMode.Create), new WaveFormat(RATE, 8, 1));
            try
            {
                writer.Write(data, index, count);
            }
            finally
            {
                writer.Close();
            }
        }

        public static byte[] ReadBytes(string file)
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext.CompareTo(".mp3") == 0)
                return ReadBytesFromMp3(file);
            else if (ext.CompareTo(".wav") == 0)
                return ReadBytesFromWav(file);
            else
                throw new Exception(file);
        }

        public static byte[] ReadBytesFromMp3(string mp3File)
        {
            List<byte> bytes = new List<byte>();
            try
            {
                using (WmaStream str = new WmaStream(mp3File, new WaveFormat(RATE, 8, 1)))
                {
                    bytes.Capacity = (int)str.Length * 2;
                    byte[] buffer = new byte[str.SampleSize * 2];

                    int read;
                    while ((read = str.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            bytes.Add(buffer[i]);
                        }
                    }
                } //str.Close() is automatically called by Dispose.            
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {                
            }

            return bytes.ToArray();
        }

        public static byte[] ReadBytesFromWav(string wavFile)
        {
            List<byte> bytes = new List<byte>();
            using (WaveStream str = new WaveStream(wavFile))
            {
                bytes.Capacity = (int)str.Length * 2;
                byte[] buffer = new byte[4000];

                int read;
                while ((read = str.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        bytes.Add(buffer[i]);
                    }
                }
            } //str.Close() is automatically called by Dispose.            

            return bytes.ToArray();
        }

    }
}
