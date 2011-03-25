using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;
using System.IO;
using System.Drawing;

namespace Shazam
{
    public class Utility
    {
        public static KeyValuePair<int, int> GetTopHit(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return new KeyValuePair<int,int>(-1, -1);

            ids.Sort();
            int id = ids[0];
            int count = 1;
            KeyValuePair<int, int> topHit = new KeyValuePair<int, int>(0, 0);
            for (int i = 1; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    count++;
                }
                else
                {
                    if (count > topHit.Value)
                    {
                        topHit = new KeyValuePair<int, int>(id, count);
                    }
                    id = ids[i];
                    count = 1;
                }
            }

            if (count > topHit.Value)
            {
                topHit = new KeyValuePair<int, int>(id, count);
            }

            return topHit;
        }
        public static double GetAverage(byte[] data, ref double min, ref double max)
        {
            long total = 0;
            foreach (byte value in data)
            {
                if (value < min)
                    min = value;
                if (value > max)
                    max = value;
                total += value;
            }

            return ((double)total) / data.Length;
        }
        public static String FormatComplex(Complex complex)
        {
            return String.Format("({0},{1})", complex.Re, complex.Im);
        }
        public static String FormatComplexArray(Complex[] complexArray)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0}", complexArray.Length);
            foreach (Complex complex in complexArray)
            {
                sb.AppendFormat(",{0},{1}", complex.Re, complex.Im);
            }
            return sb.ToString();
        }
        public static String FormatComplexMatrix(Complex[][] complexMatrix)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}", complexMatrix.Length);
            foreach (Complex[] complexArray in complexMatrix)
            {
                sb.AppendFormat(",{0}", complexArray.Length);
                foreach(Complex complex in complexArray)
                    sb.AppendFormat(",{0},{1}", complex.Re, complex.Im);
            }
            return sb.ToString();
        }
        
        public static FileInfo[] GetFiles(String path, String searchPattern)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            return dirInfo.GetFiles(searchPattern, SearchOption.AllDirectories);            
        }

        public void Spectrum(Complex[][] results)
        {
            Bitmap image = new Bitmap(results.Length, results[0].Length / 10);
            Graphics g = Graphics.FromImage(image);
            SolidBrush brush = new SolidBrush(Color.White);
            g.FillRectangle(brush, new Rectangle(0, 0, image.Width, image.Height));

            int blockSizeX = 1;
            int blockSizeY = 1;
            bool logModeEnabled = false;

            int size = results[0].Length;
            for (int i = 0; i < results.Length; i++)
            {
                int freq = 1;
                double maxMag = -1;
                int count = 0;
                for (int line = 0; line < size; line++)
                {
                    // To get the magnitude of the sound at a given frequency slice 
                    // get the abs() from the complex number. 
                    // In this case I use Math.log to get a more managable number (used for color) 
                    double magnitude = Math.Log(results[i][line].GetModulus() + 1);
                    if (maxMag < magnitude)
                        maxMag = magnitude;

                    if (++count != 10)
                        continue;
                    else
                    {
                        int blue = (int)maxMag * 20;
                        blue = blue > 255 ? 255 : blue;
                        // The more blue in the color the more intensity for a given frequency point: 
                        brush.Color = Color.FromArgb(0, (int)maxMag * 10, blue);
                        // Fill: 
                        g.FillRectangle(brush, i * blockSizeX, line / 10 * blockSizeY, blockSizeX, blockSizeY);

                        maxMag = -1;
                        count = 0;
                    }
                    //// I used a improviced logarithmic scale and normal scale: 
                    //if (logModeEnabled && (Math.Log10(line) * Math.Log10(line)) > 1)
                    //{
                    //    freq += (int)(Math.Log10(line) * Math.Log10(line));
                    //}
                    //else
                    //{
                    //    freq++;
                    //}
                }
            }
            image.Save(@"d:\spect.bmp");
        }

        public void Dump(string fileName, int[][] lines)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (int[] line in lines)
                {
                    string str = string.Empty;
                    foreach (int value in line)
                        str += string.Format("\t{0}", value);
                    sw.WriteLine(str);
                }
            }
        }

        public static void ShutDown()
        {
            //Declare and instantiate a new process component.
            System.Diagnostics.Process process;
            process = new System.Diagnostics.Process();

            //Do not receive an event when the process exits.
            process.EnableRaisingEvents = false;
            process.StartInfo.FileName = @"c:\shutdown.bat";
            process.Start();
        }

        // not used
        public double[][] GetTopMagnitude(Complex[][] results, int n)
        {
            double[][] magnitude = new double[results.Length][];

            int count = 0;
            foreach (Complex[] result in results)
            {
                magnitude[count++] = GetTopMagnitude(result, n);
            }

            return magnitude;
        }
        public double[] GetTopMagnitude(Complex[] result, int n)
        {
            if (n > result.Length)
                n = result.Length;

            List<double> magnitude = new List<double>(n);
            foreach (Complex value in result)
            {
                //double mag = Math.Log(value.GetModulus() + 1);
                double mag = value.GetModulus();
                if (magnitude.Count < n)
                    magnitude.Add(mag);
                else
                {
                    int minIndex = 0;
                    for (int i = 1; i < magnitude.Count; i++)
                    {
                        if (magnitude[minIndex] > magnitude[i])
                            minIndex = i;
                    }
                    magnitude[minIndex] = mag;
                }
            }

            magnitude.Sort();
            return magnitude.ToArray();
        }

        public static Dictionary<int, int> Count(List<int> values)
        {
            if (values == null)
                return null;
            if (values.Count == 0)
                return new Dictionary<int, int>();

            values.Sort();

            Dictionary<int, int> result = new Dictionary<int, int>();
            int i = 0;
            int key = values[i++];
            int count = 1;
            while (i < values.Count)
            {
                if (values[i] == key)
                    count++;
                else 
                {
                    result.Add(key, count);
                    key = values[i];
                    count = 1;
                }
                i++;
            }

            result.Add(key, count);

            return result;
        }
    }
}
