using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MusicIdentifier;
using System.IO;
using System.Threading;

namespace TestPlayer
{
    public partial class PlayerForm : Form
    {
        public PlayerForm()
        {
            InitializeComponent();
        
        }
        System.Windows.Forms.Timer mytime;
        MyMedia myclass = new MyMedia();
        bool IsOff = false;
        bool IsStop = false;
        MicRecorder recorder = null;
        FileInfo[] files = null;
        int fileIndex = -1;
        bool Recording = false;
  
        private void Form1_Load(object sender, EventArgs e)
        {
            this.trackBar1.LargeChange = 10;
            this.hScrollBar1.Value = 400;
            myclass.CurrentValume =this.hScrollBar1.Value;
            mytime = new System.Windows.Forms.Timer();
            mytime.Interval = 1000;
            mytime.Tick += new EventHandler(mytime_Tick);
          
        }
        private void mytime_Tick(object sender,EventArgs e)
        {
            IsStop = false;
            if (myclass.CurrentState==State.Playing)
            {
                this.trackBar1.Value = myclass.CurrentPosition;
                this.label2.Text = this.trackBar1.Value.ToString();
            }
            else if (myclass.CurrentState==State.Paused)
            {
                mytime.Stop();
            }
            if (myclass.CurrentState==State.Stopped)
            {
                Stop();

                if (Recording == true)
                {
                    if (fileIndex != -1)
                        StopRecording();

                    fileIndex++;
                    if (fileIndex < files.Length)
                    {
                        StartPlayAndRecord(files[fileIndex].FullName);
                    }
                    else
                    {
                        Recording = false;
                    }
                }
            }            
        }

        private void UpdateStatus(string message)
        {
            if (this.InvokeRequired || StatusLabel.InvokeRequired)
            { 
                this.Invoke(new Action<string>(UpdateStatus));
            }
            else
            {
                StatusLabel.Text = message;   
            }
        }

        private void StopRecording()
        {
            recorder.RequestStop = true;
            byte[] data = recorder.GetAudioData();

            string outputFile = myclass.FileName.ToLower().Replace(".mp3", ".wav");
            SaveRecord(outputFile, data);
        }

        private void SaveRecord(string fileName, byte[] data)
        {
            Mp3ToWavConverter.WriteBytesToWav(fileName, data);
        }

        private void Open(string fileName)
        {
            myclass = new MyMedia();
            myclass.FileName = fileName;
            this.trackBar1.Maximum = myclass.TotalSeconds;
        }

        private void Play()
        {
            if (myclass.CurrentState != State.Playing)
            {
                mytime.Start();
                myclass.Play();
            }
        }

        private void Stop()
        {
            mytime.Stop();
            myclass.Stop();
            myclass.GoStartPosition();
            this.trackBar1.Value = 0;
            this.label2.Text = this.trackBar1.Value.ToString();
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog mydlg = new OpenFileDialog();
            mydlg.ShowDialog();
            Open(mydlg.FileName);
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            Play();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            myclass.Puase();            
        }
        private void button4_Click(object sender, EventArgs e)
        {
            this.Text += "time:"+myclass.TotalSeconds.ToString();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            IsOff = !IsOff;
          myclass.SetAudioOnOff(IsOff);          
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            myclass.CurrentValume = this.hScrollBar1.Value;
            this.label1.Text= myclass.CurrentValume.ToString();
        }
        private void button7_Click(object sender, EventArgs e)
        {
            this.Text = myclass.CurrentState.ToString();
        }
        private void StopButton_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (IsStop==false)
            {
                mytime.Stop();
                IsStop = true;
            }
            
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {

            myclass.CurrentPosition = this.trackBar1.Value*1000;
            this.label2.Text = this.trackBar1.Value.ToString();
            mytime.Start();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {

        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            myclass.CurrentSpeed = (int)this.numericUpDown1.Value;

        }

        //private 
        private void button8_Click(object sender, EventArgs e)
        {
            string filePath = @"D:\Music\李建.-.[音乐傲骨].专辑.(MP3)";
            files = Utility.GetFiles(filePath, "*.mp3");
            Recording = true;
            mytime.Start();
        }

        private void StartPlayAndRecord(string file)
        {
            UpdateStatus(file);
            Open(file);
            Play();

            recorder = new MicRecorder();
            recorder.Seconds = int.MaxValue;
            recorder.RequestStop = false;

            Thread newThread = new Thread(new ThreadStart(StartRecord));
            newThread.Start();
        }

        private void StartRecord()
        {
            recorder.RecStart();
        }

        private void button9_Click(object sender, EventArgs e)
        {
   
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void trackBar1_MouseHover(object sender, EventArgs e)
        {
         
        } 

    }
}