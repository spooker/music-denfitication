using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicIdentifier
{
    public class Normalize
    {

        public static bool QUIET = false;
        public static bool getPeaks8(byte[] data, out sbyte minpeak, out sbyte maxpeak)
        {
            sbyte cur;
            sbyte minp = 0;
            sbyte maxp = 0;
            foreach (byte val in data)
            {
                cur = (sbyte)(val ^ 0x80);

                if (cur < minp)
                    minp = cur;
                if (cur > maxp)
                    maxp = cur;
            }


            minpeak = minp;
            maxpeak = maxp;

            return true;
        }

        public static bool getSmartPeaks8(byte[] data, int peakPercent, out sbyte minpeak, out sbyte maxpeak)
        {
            ulong[] stats = null; // memory for the sample statistics
            ulong numstat = 0;
            ulong ndone = 0;

            stats = new ulong[Byte.MaxValue +1];
            if (stats == null)
            {
                if (!QUIET)
                    Console.WriteLine("Cannot allocate buffer in memory.");
                minpeak = maxpeak = -1;
                return false;
            }

            sbyte cur;
            sbyte minp = 0;
            sbyte maxp = 0;
            foreach (byte val in data)
            {
                cur = (sbyte)(val ^ 0x80);

                stats[128 + cur]++;
                numstat++;
            }

            // let's find how many samples is <percent> of the max
            numstat = (ulong)(numstat * (1.0 - (peakPercent / 100.0)));
            // let's use this to accumulate values
            ndone = 0;
            int i;
            // let's count the min sample value that has the given percentile
            for (i = 0; (i < Byte.MaxValue) && (ndone <= numstat); i++)
                ndone += stats[i];
            minp = (sbyte)(i - 129);
            // let's count the max sample value that has the given percentile
            ndone = 0;
            for (i = Byte.MaxValue - 1; (i >= 0) && (ndone <= numstat); i--)
                ndone += stats[i];
            maxp = (sbyte)(i - 127);

            minpeak = minp;
            maxpeak = maxp;

            return true;
        }

        public static byte[] make_table8(double ratio)
        {
            byte[] table = new byte[Byte.MaxValue + 1];
        	byte i = 0;

            do 
            {
                double value = ((sbyte)i) * ratio;
	            if (value > 127.0)
		            table[i ^ 0x80] = (byte)0xFF;
	            else if (value < -127.0)
		            table[i ^ 0x80] = 0x00;
	            else
		            table[i ^ 0x80] = (byte)((sbyte)value ^ 0x80);
            } while (++i != 0);

            return table;
        }

        public static byte[] amplify(byte[] data, byte[] mapping)
        { 
            if(data == null)
                return null;

            byte[] result = new byte[data.Length];
	        for (int i = 0; i < data.Length; i++) 
            {
		        result[i] = mapping[data[i]];
	        }		  
            return result;
        }

        public static byte[] Amplify(byte[] data, double ratio)
        { 
            return amplify(data, make_table8(ratio));
        }

        public static void Test()
        { 
            byte[] map = make_table8(2.0);
            
            for (byte i = 0; i < Byte.MaxValue; i++)
            {
                sbyte value = (sbyte)(i ^ 0x80);
                sbyte mapped = (sbyte)(map[i] ^ 0x80);
                Console.WriteLine("{0} : {1}\t", value, mapped);
            }
        }
    }
}
