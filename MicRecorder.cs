using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using System.Threading;
using System.Diagnostics;

namespace Shazam
{
    class MicRecorder
    {
        //[DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        //private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);

        //public void Record()
        //{
        //    mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
        //    mciSendString("record recsound", "", 0, 0);
        //}

        //public void Stop()
        //{
        //    mciSendString("save recsound c:\\record.wav", "", 0, 0);
        //    mciSendString("close recsound ", "", 0, 0);
        //    Computer c = new Computer();
        //    c.Audio.Stop();
        //}

        //public void Play()
        //{
        //    Computer computer = new Computer();
        //    computer.Audio.Play("c:\\record.wav", AudioPlayMode.Background);
        //}

        public MicRecorder()
        {
            // 初始化音频捕捉设备 
            InitCaptureDevice();
            // 设定录音格式 
            mWavFormat = CreateWaveFormat();

            Seconds = 10;
            RequestStop = false;
        }

        private WaveFormat mWavFormat;
        private WaveFormat CreateWaveFormat()
        {
            WaveFormat format = new WaveFormat();
            format.FormatTag = WaveFormatTag.Pcm;
            format.SamplesPerSecond = 44100;
            format.BitsPerSample = 8;
            format.Channels = 1;
            format.BlockAlign = (short)(format.Channels * (format.BitsPerSample / 8));
            format.AverageBytesPerSecond = format.BlockAlign * format.SamplesPerSecond;

            return format;
        }

        private List<byte> audio = null;
        private object lockObj = new object();
        public byte[] GetAudioData()
        {
            byte[] audioData = null;
            lock (lockObj)
            {
                audioData = audio.ToArray();
            }
            return audioData;
        }

        private Capture mCapDev = null;
        private bool InitCaptureDevice()
        {
            CaptureDevicesCollection devices = new CaptureDevicesCollection();
            Guid deviceGuid = Guid.Empty; 
            if (devices.Count > 0)
                deviceGuid = devices[0].DriverGuid;
            else
            {
                Debug.WriteLine("No audio device in the system.");
                return false;
            }

            try
            {
                mCapDev = new Capture(deviceGuid);
            }
            catch (DirectXException e)
            {
                Debug.WriteLine(e.ToString());
                return false;
            }
            return true;
        }

        private CaptureBuffer mRecBuffer = null;
        private Notify mNotify = null;
        private int cNotifyNum = 10;
        private int mBufferSize = 0;
        private int mNotifySize = 0;
        private int mNextCaptureOffset = 0;

        private void CreateCaptureBuffer()
        {
            CaptureBufferDescription bufferdescription = new CaptureBufferDescription();
            if (null != mNotify)
            {
                mNotify.Dispose();
                mNotify = null;
            }

            if (null != mRecBuffer)
            {
                mRecBuffer.Dispose();
                mRecBuffer = null;
            }

            // 设定通知的大小,默认为1s钟 
            mNotifySize = (1024 > mWavFormat.AverageBytesPerSecond / 8) ? 1024 : (mWavFormat.AverageBytesPerSecond / 8);
            mNotifySize -= mNotifySize % mWavFormat.BlockAlign;

            // 设定缓冲区大小 
            mBufferSize = mNotifySize * cNotifyNum;

            // 创建缓冲区描述  
            bufferdescription.BufferBytes = mBufferSize;
            bufferdescription.Format = mWavFormat;      

            // 创建缓冲区 
            mRecBuffer = new CaptureBuffer(bufferdescription, mCapDev);
            mNextCaptureOffset = 0;
        }

        private AutoResetEvent mNotificationEvent = null;
        private Thread mNotifyThread = null;
        private bool InitNotifications()
        {
            if (null == mRecBuffer)
            {
                Debug.WriteLine("Capture Buffer is null.");
                return false;
            }

            // 创建一个通知事件,当缓冲队列满了就激发该事件. 
            mNotificationEvent = new AutoResetEvent(false);
            // 创建一个线程管理缓冲区事件 
            if (null == mNotifyThread)
            {
                mNotifyThread = new Thread(new ThreadStart(WaitThread));
                mNotifyThread.Start();
            }
            // 设定通知的位置 
            BufferPositionNotify[] PositionNotify = new BufferPositionNotify[cNotifyNum];

            for (int i = 0; i < cNotifyNum; i++)
            {
                PositionNotify[i].Offset = (mNotifySize * i) + mNotifySize - 1;
                PositionNotify[i].EventNotifyHandle = mNotificationEvent.Handle;
            }
            mNotify = new Notify(mRecBuffer);
            mNotify.SetNotificationPositions(PositionNotify);

            return true;
        }

        private bool requestStop = false;
        private void WaitThread()
        {
            while (!requestStop)
            {
                // 等待缓冲区的通知消息 
                mNotificationEvent.WaitOne(Timeout.Infinite, true);
                // 录制数据 
                RecordCapturedData();
                //Console.WriteLine("[WaitThread] recording data...");
            }
            Console.WriteLine("[WaitThread] going to stop..");
        }

        private void RecordCapturedData()
        {
            byte[] CapturedData = null;
            int ReadPos;
            int CapturePos;
            int LockSize;
            mRecBuffer.GetCurrentPosition(out CapturePos, out ReadPos);
            LockSize = ReadPos - mNextCaptureOffset;

            if (LockSize < 0)
                LockSize += mBufferSize;
            // 对齐缓冲区边界,实际上由于开始设定完整,这个操作是多余的. 
            LockSize -= (LockSize % mNotifySize);
            if (0 == LockSize)
                return;

            // 读取缓冲区内的数据 
            CapturedData = (byte[])mRecBuffer.Read(mNextCaptureOffset, typeof(byte), LockFlag.None, LockSize);
            SaveData(CapturedData);
            // 移动录制数据的起始点,通知消息只负责指示产生消息的位置,并不记录上次录制的位置 
            mNextCaptureOffset += CapturedData.Length;
            mNextCaptureOffset %= mBufferSize; // Circular buffer
        }

        private void SaveData(byte[] capturedData)
        {
            lock (lockObj)
            {
                audio.AddRange(capturedData);
            }
        }

        public bool RequestStop { set; get; }
        public int Seconds { set; get; }
        public void RecStart()
        {
            audio = new List<byte>();
            // 创建一个录音缓冲区，并开始录音 
            CreateCaptureBuffer();
            // 建立通知消息,当缓冲区满的时候处理方法 
            requestStop = false;
            InitNotifications();
            mRecBuffer.Start(true);

            int second = 0;
            while (second < Seconds && !RequestStop)
            {
                Thread.Sleep(1000);
                second++;
            }
            RecStop();
        }

        public void RecStop()
        {
            // 关闭通知消息 
            if (null != mNotificationEvent)
            {
                Console.WriteLine("Request WaitThread to stop..");
                requestStop = true;
                mNotificationEvent.Set();
            }

            mNotifyThread.Join(500);
            if (mNotifyThread.ThreadState != System.Threading.ThreadState.Stopped)
            {
                Console.WriteLine("WaitThread doesn't stop. Abort it.");
                mNotifyThread.Abort();
            }
            // 停止录音 
            mRecBuffer.Stop();
            // 写入缓冲区最后的数据
            RecordCapturedData();
        }
    }
}
