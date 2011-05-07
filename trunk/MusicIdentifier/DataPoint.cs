using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicIdentifier
{
    public class DataPoint
    {
        public short Time { set; get; }
        public short SongID { set; get; }

        public DataPoint(short time, short songID)
        {
            Time = time;
            SongID = songID;
        }

        public override string ToString()
        {
            return "{" + Time.ToString() + "," + SongID.ToString() + "}";
        }
    }
}
