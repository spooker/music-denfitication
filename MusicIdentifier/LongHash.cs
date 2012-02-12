using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;

namespace MusicIdentifier
{
    public class LongHash : IHashMaker
    {
        private static int CHUNK_SIZE = 4096;
        private static int LOWER_LIMIT = 0;
        private static int UPPER_LIMIT = 2000;
        private static int[] RANGE = new int[] { 80, 120, 180, 300, 2000 };
        public static int STEP_SIZE = 2048;

        public LongHash()
        {
            ChunkSize = CHUNK_SIZE;
            StepSize = STEP_SIZE;
        }

        private int[] GetKeyPoints(Complex[] result)
        {
            int[] recordPoints = new int[] { 0, 0, 0, 0, 0};
            double[] highscores = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0 };

            //For every line of data:
            int index = 0;
            for (int i = LOWER_LIMIT; i < UPPER_LIMIT; i++)
            {
                //Get the magnitude:
                double mag = Math.Log(result[i].GetModulus() + 1);
                //Find out which range we are in:
                if (RANGE[index] < i)
                    index++;
                //Save the highest magnitude and corresponding frequency:
                if (mag > highscores[index])
                {
                    highscores[index] = mag;
                    recordPoints[index] = i;
                }
            }
            return recordPoints;
        }

        //Using a little bit of error-correction, damping
        private static long FUZ_FACTOR = 0xFFFFFFFFFE;
        private long Hash(int[] points)
        {
            return (points[4] & FUZ_FACTOR) * (long)100000000000
                + (points[3] & FUZ_FACTOR) * (long)100000000
                + (points[2] & FUZ_FACTOR) * (long)100000
                + (points[1] & FUZ_FACTOR) * (long)100
                + (points[0] & FUZ_FACTOR);
        }

        public long[] GetHash(Complex[][] data)
        {
            long[] hashes = new long[data.Length];
            int index = 0;
            foreach (Complex[] line in data)
            {
                hashes[index++] = Hash(GetKeyPoints(line));
            }

            return hashes;
        }
        public int ChunkSize { get; set; }

        public int StepSize { get; set; }
    }
    
}
