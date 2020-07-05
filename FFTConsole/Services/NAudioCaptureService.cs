using FFTConsole.Services.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using Serilog;


namespace FFTConsole.Services
{
    public class NAudioCaptureService : IAudioCaptureService
    {
        private AudioStream audioStream;
        private ILogger logger;
        public int sampleRate;
        public int maxPacketLen;
        private bool recording;
        private readonly object recordingLock = new object();
        private WaveInEvent waveIn;

        public NAudioCaptureService(int sampleRate, int maxPacketLen, ILogger logger)
        {
            this.logger = logger;

            this.audioStream = new AudioStream();
            this.sampleRate = sampleRate; // sample rate of the sound card
            this.maxPacketLen = maxPacketLen; // must be a multiple of 2
            this.recording = false;
        }

        public IDisposable Start(Action<AudioPacket> onPacketReceived)
        {
            AudioStreamObserver observer = new AudioStreamObserver(onPacketReceived , this.logger);
            IDisposable disposable = audioStream.Subscribe(observer);
            this.StartAudioCapture();
            return disposable;
        }

        public void Stop()
        {
            lock(this.recordingLock)
            {
                if (this.recording == false) return;

                try
                {
                    this.waveIn.StopRecording();
                    this.recording = false;
                }
                catch (Exception e)
                {
                    this.recording = true;
                    this.logger.Error("Unable to stop recording: " + e.ToString());
                    throw new Exception("Unable to stop recording: " + e.ToString());
                }
            }
        }

        public void StartAudioCapture()
        {
            // Check if we're already listening.
            lock(this.recordingLock)
            {
                if (this.recording == true) return;
            }

            // Attempt to find the Windows Stereo Mix.
            int deviceNum = -1;
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                WaveInCapabilities wc = WaveInEvent.GetCapabilities(i);
                if (wc.ProductName.Equals("Stereo Mix (Realtek High Defini") == true)
                {
                    deviceNum = i;
                    break;
                }
            }

            if (deviceNum == -1) throw new Exception("Unable to find Windows Stereo Mix");
            
            // Start audio recording.
            lock (this.recordingLock)
            {
                // Quick check if some other thread already started listening.
                if (this.recording == true) return;

                this.waveIn = new WaveInEvent();
                this.waveIn.DeviceNumber = deviceNum;
                this.waveIn.WaveFormat = new NAudio.Wave.WaveFormat(this.sampleRate, 1);
                
                // Set the max buffer milliseconds in the event no data is being streamed we'll keep the pipeline streaming.
                this.waveIn.BufferMilliseconds = (int)((double)this.maxPacketLen / (double)this.sampleRate * 1000.0);
                this.waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(this.AudioDataAvailable);
                
                try
                {
                    this.waveIn.StartRecording();
                    this.recording = true;
                }
                catch (Exception e)
                {
                    this.recording = false;
                    this.logger.Error("StartAudioCapture returned error: " + e.ToString());
                    throw new Exception("Unable to start recording audio: " + e.ToString());
                }
            }
        }

        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            this.audioStream.AddPacket(new AudioPacket()
            {
                data = e.Buffer,
                sampleRate = this.sampleRate,
                sampleByteSize = 2
            });
        }
    }
}
