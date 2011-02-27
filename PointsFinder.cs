using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Exocortex.DSP;

namespace Shazam
{
    class Harvester
    { 
        public static int CHUNK_SIZE = 4096;
        public static int LOWER_LIMIT = 40;
        public static int UPPER_LIMIT = 300;
        public static int[] RANGE = new int[] {80, 120, 180, 300};
    }
    class PointsFinder
    {

        public Complex[][] FFT(byte[] audio)
        { 
            int totalSize = audio.Length;
            int amountPossible = totalSize/Harvester.CHUNK_SIZE;

            //When turning into frequency domain we'll need complex numbers:
            Complex[][] results = new Complex[amountPossible][];
            //For all the chunks:
            for(int i = 0; i < amountPossible; i++ ) 
            {
                Complex[] complex = new Complex[Harvester.CHUNK_SIZE];
                for (int start = i * Harvester.CHUNK_SIZE, j = 0; 
                     j < Harvester.CHUNK_SIZE; 
                     j++) 
                {
                    //Put the time domain data into a complex number with imaginary part as 0:
                    complex[j] = new Complex(audio[start + j], 0);
                }
                //Complex[] tmpRs = FFT1.fft(complex);

                //Perform FFT analysis on the chunk:
                Fourier.FFT(complex, complex.Length, FourierDirection.Forward);                
                results[i] = complex;

            }
            return results;
        }

        
        //Find out in which range
        private int getIndex(int freq) {
            int i = 0;
            while(Harvester.RANGE[i] < freq) 
                i++;
            return i;
        }

        public int[][] GetKeyPoints(Complex[][] results)
        {
            int[][] lines = new int[results.Length][];

            int count = 0;
            foreach (Complex[] result in results)
            {
                lines[count++] = GetKeyPoints(result);
            }

            using (StreamWriter sw = new StreamWriter(@"D:\Sound\Shazam\Shazam\moumoon-sunshine_girl.log"))
            {
                //Write the points to a file:
                foreach (int[] line in lines)
                {
                    for (int i = 0; i < line.Length; i++)
                    {
                        sw.Write(string.Format("{0}\t", line[i]));
                    }
                    sw.Write("\n");
                }                
                sw.Flush();
            }
            // ... snip ...            

            return lines;
        }

        public int[] GetKeyPoints(Complex[] result)
        {
            int[] recordPoints = new int[] { 0, 0, 0, 0 };
            double[] highscores = new double[] { 0.0, 0.0, 0.0, 0.0 };

            //For every line of data:
            for (int i = Harvester.LOWER_LIMIT; i < Harvester.UPPER_LIMIT; i++)
            {
                //Get the magnitude:
                double mag = Math.Log(result[i].GetModulus() + 1);
                //Find out which range we are in:
                int index = getIndex(i);
                //Save the highest magnitude and corresponding frequency:
                if (mag > highscores[index]) {
                    highscores[index] = mag;
                    recordPoints[index] = i;
                }
            }
            return recordPoints;
        }

        //Using a little bit of error-correction, damping
        private static int FUZ_FACTOR = 2;
         
        private long Hash(int[] points) {
            return (points[3] - (points[3] % FUZ_FACTOR)) * (long)100000000 
                + (points[2] - (points[2] % FUZ_FACTOR)) * (long)100000
                + (points[1] - (points[1] % FUZ_FACTOR)) * (long)100
                + (points[0] - (points[0] % FUZ_FACTOR));
        }

        public long[] GetHash(int[][] lines)
        { 
            long[] hashes = new long[lines.Length];
            for (int i = 0; i < lines.Length; i++ )
            { 
                hashes[i] = Hash(lines[i]);
            }
            return hashes;
        }
    }
}
