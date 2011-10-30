using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace MusicIdentifier
{
    // status
    public enum State { Playing, Paused, Stopped };


    public class APIClass
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName(
            string lpszLongPath,
            string shortFile,
            int cchBuffer
        );

        [DllImport("winmm.dll", EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        public static extern int mciSendString(
            string lpstrCommand,
            string lpstrReturnString,
            int uReturnLength,
            int hwndCallback
        );
    }

    public class MyMedia
    {
        private struct CurrentInformation
        {
            public int CurPos;
            public State CurState;
            public int Valume;
            public int CurSpeed;
            public int TimeLength;
        }

        public MyMedia()
        {
            CurInf.CurPos = 0;
            CurInf.CurState = State.Stopped;
            CurInf.Valume = 0;
            CurInf.CurSpeed = 1000;
            CurInf.TimeLength = 0;

        }

        private string Name = string.Empty;
        private string TempName = string.Empty;
        private CurrentInformation CurInf = new CurrentInformation();
        
        public string FileName
        {
            set
            {
                Name = value;
                CurInf.TimeLength = 0;

                TempName = TempName.PadLeft(260, ' ');
                int iCode = APIClass.GetShortPathName(Name, TempName, TempName.Length);
                if (iCode != 0)
                {
                    Console.WriteLine("Error in FileName!");
                }
                Console.WriteLine(TempName);
                TempName = Trim(TempName);
                InitFile();
            }
            get { return Name; }
        }

        private void InitFile()
        {
            string DeviceID = GetDeviceID(TempName);
            int iCode = APIClass.mciSendString("close all", null, 0, 0);
            if (iCode != 0)
            {
                Console.WriteLine("Error in Play!");
            }
            if (DeviceID != "RealPlay")
            {
                string MciCommand = String.Format("open {0} type {1} alias media", TempName, DeviceID);
                iCode = APIClass.mciSendString(MciCommand, null, 0, 0);
                if (iCode != 0)
                {
                    Console.WriteLine("Error in Play!");
                }
                CurInf.CurState = State.Stopped;
            }
        }

        private string Trim(string name)
        {
            name = name.Trim();
            if (name.Length > 1)//if name contains '\0'
            {
                name = name.Substring(0, name.Length - 1);//trim the '\0'
            }
            return name;
        }

        // get file extention
        private string GetDeviceID(string name)
        {
            string result = string.Empty;
            name = name.ToUpper().Trim();
            if (name.Length < 3)
            {
                return name;
            }
            switch (name.Substring(name.Length - 3))
            {
                case "MID":
                    result = "Sequencer";
                    break;
                case "RMI":
                    result = "Sequencer";
                    break;
                case "IDI":
                    result = "Sequencer";
                    break;
                case "WAV":
                    result = "Waveaudio";
                    break;
                case "ASX":
                    result = "MPEGVideo2";
                    break;
                case "IVF":
                    result = "MPEGVideo2";
                    break;
                case "LSF":
                    result = "MPEGVideo2";
                    break;
                case "LSX":
                    result = "MPEGVideo2";
                    break;
                case "P2V":
                    result = "MPEGVideo2";
                    break;
                case "WAX":
                    result = "MPEGVideo2";
                    break;
                case "WVX":
                    result = "MPEGVideo2";
                    break;
                case ".WM":
                    result = "MPEGVideo2";
                    break;
                case "WMX":
                    result = "MPEGVideo2";
                    break;
                case "WMP":
                    result = "MPEGVideo2";
                    break;
                case ".RM":
                    result = "RealPlay";
                    break;
                case "RAM":
                    result = "RealPlay";
                    break;
                case ".RA":
                    result = "RealPlay";
                    break;
                case "MVB":
                    result = "RealPlay";
                    break;
                default:
                    result = "MPEGVideo";
                    break;
            }
            return result;
        }

        public bool IsReady()
        {
            string Ready = new string(' ', 10);
            APIClass.mciSendString("status media ready", Ready, Ready.Length, 0);
            Ready = Ready.Trim();
            if (Ready.Contains("true"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Play()
        {
            if (CurInf.CurState != State.Playing)
            {
                CurInf.CurState = State.Playing;
                int iCode = APIClass.mciSendString("play media", null, 0, 0);
                if (iCode != 0)
                {
                    Console.WriteLine("Error in Play!");

                    CurInf.CurState = State.Stopped;
                }
            }
        }
        
        public void Stop()
        {
            //if (CurInf.CurState != State.Stopped)
            {
                int iCode = APIClass.mciSendString("stop media", null, 0, 0);
                if (iCode != 0)
                {
                    Console.WriteLine("Error in Stop!");
                }
                CurInf.CurState = State.Stopped;
            }
        }

        public void Puase()
        {
            if (CurInf.CurState == State.Playing)
            {
                APIClass.mciSendString("pause media", null, 0, 0);
                CurInf.CurState = State.Paused;
            }
        }
        
        public void SetAudioOnOff(bool IsOff)
        {
            string SetOnOff = string.Empty;
            if (IsOff)
                SetOnOff = "off";
            else
                SetOnOff = "on";
            string MciCommand = String.Format("setaudio media {0}", SetOnOff);
            APIClass.mciSendString(MciCommand, null, 0, 0);
        }
        
        public void GoStartPosition()
        {
            APIClass.mciSendString("seek media to start", null, 0, 0);
            CurInf.CurState = State.Stopped;
            CurInf.CurPos = 0;
        }
        // 1000 is normal, 2000 is two times faster, 500 is half
        public int CurrentSpeed
        {
            get
            {
                return CurInf.CurSpeed;
            }
            set
            {
                CurInf.CurSpeed = value;
                string MciCommand = String.Format("set media speed {0}", value);
                APIClass.mciSendString(MciCommand, null, 0, 0);
            }
        }
        
        public int TotalSeconds
        {
            get
            {
                int iCode = APIClass.mciSendString("set media time format milliseconds", null, 0, 0);//设置时间格式单位为毫秒
                if (iCode != 0)
                {
                    Console.WriteLine("Error in TotalSeconds!");
                }
                CurInf.TimeLength = GetDuration();
                return CurInf.TimeLength;
            }
        }

        private int GetDuration()
        {
            string durLength = string.Empty;
            durLength = durLength.PadLeft(20, ' ');//19 is big enough to hold the length
            int iCode = APIClass.mciSendString("status media length", durLength, durLength.Length, 0);
            if (iCode != 0)
            {
                Console.WriteLine("Error in GetDuration!");
            }
            durLength = durLength.Trim();
            if (durLength.Length > 1)//end with '\0'
            {
                durLength = durLength.Substring(0, durLength.Length - 1);// trim '\0'
                return (int)(long.Parse(durLength) / 1000);
            }
            return 0;
        }

        public State CurrentState
        {
            get
            {
                //if (CurInf.CurPos == TotalSeconds)
                if (CurInf.CurPos == CurInf.TimeLength)
                {
                    CurInf.CurState = State.Stopped;
                }
                return CurInf.CurState;
            }
        }
        
        public int CurrentPosition
        {
            get
            {
                string TempPos = string.Empty;
                TempPos = TempPos.PadLeft(20, ' ');
                APIClass.mciSendString("status media position", TempPos, TempPos.Length, 0);
                TempPos = TempPos.Trim();
                if (TempPos.Length > 1)
                {
                    CurInf.CurPos = (int)(long.Parse(TempPos) / 1000);
                }
                else
                {
                    CurInf.CurPos = 0;
                }
                return CurInf.CurPos;
            }
            set
            {
                string step = String.Format("seek media to {0}", value);
                APIClass.mciSendString(step, null, 0, 0);
                CurInf.CurState = State.Stopped;
                CurInf.CurPos = value;
                Play();
            }
        }

        public int CurrentValume
        {
            get
            {
                return CurInf.Valume;
            }
            set
            {
                if (value >= 0)
                {
                    CurInf.Valume = value;
                    string MciCommand = String.Format("setaudio media volume to {0}", CurInf.Valume);
                    APIClass.mciSendString(MciCommand, null, 0, 0);
                }
            }
        }
    }

    public class Mp3Player
    {
        public Mp3Player()
        { }

        private string Name = "";
        private string durLength = "";
        private string TemStr = "";
        int ilong;
    
        public struct structMCI
        {
            public bool bMut;
            public int iDur;
            public int iPos;
            public int iVol;
            public int iBal;
            public string iName;
            public State state;
        }

        public structMCI mc = new structMCI();

        public string FileName
        {
            get
            {
                return mc.iName;
            }
            set
            {
                //ASCIIEncoding asc = new ASCIIEncoding(); 
                try
                {
                    TemStr = "";
                    TemStr = TemStr.PadLeft(127, Convert.ToChar(" "));
                    Name = Name.PadLeft(260, Convert.ToChar(" "));
                    mc.iName = value;
                    ilong = APIClass.GetShortPathName(mc.iName, Name, Name.Length);
                    Name = GetCurrPath(Name);                    
                    //Name = mc.iName;
                    Name = "open " + Convert.ToChar(34) + Name + Convert.ToChar(34) + " alias media";
                    ilong = APIClass.mciSendString("close all", TemStr, TemStr.Length, 0);
                    Console.WriteLine("Close All : {0}", ilong);
                    ilong = APIClass.mciSendString(Name, TemStr, TemStr.Length, 0);
                    Console.WriteLine("Open : {0}", ilong);
                    ilong = APIClass.mciSendString("set media time format milliseconds", TemStr, TemStr.Length, 0);
                    Console.WriteLine("Set Time Format : {0}", ilong);
                    mc.state = State.Stopped;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        
        public void Play()
        {
            TemStr = "";
            TemStr = TemStr.PadLeft(127, Convert.ToChar(" "));
            ilong = APIClass.mciSendString("play media", TemStr, TemStr.Length, 0);
            Console.WriteLine("Play : {0}", ilong);
            mc.state = State.Playing;
        }
        
        public void Stop()
        {
            TemStr = "";
            TemStr = TemStr.PadLeft(128, Convert.ToChar(" "));
            ilong = APIClass.mciSendString("close media", TemStr, 128, 0);
            ilong = APIClass.mciSendString("close all", TemStr, 128, 0);
            mc.state = State.Stopped;
        }

        public void Puase()
        {
            TemStr = "";
            TemStr = TemStr.PadLeft(128, Convert.ToChar(" "));
            ilong = APIClass.mciSendString("pause media", TemStr, TemStr.Length, 0);
            mc.state = State.Paused;
        }
        private string GetCurrPath(string name)
        {
            if (name.Length < 1) return "";
            name = name.Trim();
            name = name.Substring(0, name.Length - 1);
            return name;
        }
        
        public int Duration
        {
            get
            {
                durLength = "";
                durLength = durLength.PadLeft(128, Convert.ToChar(" "));
                APIClass.mciSendString("status media length", durLength, durLength.Length, 0);
                durLength = durLength.Trim();
                if (durLength == "") return 0;
                double lengthInSecond;
                if(!double.TryParse(durLength, out lengthInSecond))
                    lengthInSecond = -1;
                else
                    lengthInSecond /= 1000f;

                return (int)lengthInSecond;
            }
        }
                
        public int CurrentPosition
        {
            get
            {
                durLength = "";
                durLength = durLength.PadLeft(128, Convert.ToChar(" "));
                APIClass.mciSendString("status media position", durLength, durLength.Length, 0);
                mc.iPos = (int)(Convert.ToDouble(durLength) / 1000f);
                return mc.iPos;
            }
        }
    }    
}
