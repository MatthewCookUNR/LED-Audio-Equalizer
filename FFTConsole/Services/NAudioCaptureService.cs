using FFTConsole.Services.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using Serilog;
using NAudio.CoreAudioApi;
using System.Threading.Tasks;
using System.Threading;

namespace FFTConsole.Services
{
    public class NAudioCaptureService : IAudioCaptureService
    {
        private AudioStream audioStream;
        private ILogger logger;
        private int sampleRate;
        private int maxPacketLen;
        private bool recording;
        private readonly object recordingLock = new object();
        private WaveInEvent waveIn;
        private int cachedVolume;
        private DateTime nextVolumeUpdate;
        private BufferedWaveProvider streamBuff;
        private Thread forwardThread;

        public NAudioCaptureService(int sampleRate, int maxPacketLen, ILogger logger)
        {
            this.logger = logger;

            this.audioStream = new AudioStream();
            this.sampleRate = sampleRate; // sample rate of the sound card
            this.maxPacketLen = maxPacketLen; // must be a multiple of 2
            this.recording = false;
            this.nextVolumeUpdate = new DateTime();
            this.forwardThread = null;
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
                if (this.recording == false)
                {
                    return;
                }

                try
                {
                    this.waveIn.StopRecording();
                    this.recording = false;
                    this.forwardThread.Join();
                    this.forwardThread = null;
                }
                catch (Exception e)
                {
                    this.recording = true;
                    this.logger.Error("Unable to stop recording: " + e.ToString());
                    throw new Exception("Unable to stop recording: " + e.ToString());
                }
            }
        }

        public int GetVolume()
        {
            if (this.nextVolumeUpdate < DateTime.Now)
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                this.cachedVolume = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100.00);
                this.nextVolumeUpdate = DateTime.Now.AddSeconds(30);
            }

            return this.cachedVolume;
        }

        private void StartAudioCapture()
        {
            // Check if we're already listening.
            lock(this.recordingLock)
            {
                if (this.recording == true)
                {
                    return;
                }
            }

            // Attempt to find the Windows Stereo Mix.
            int deviceNum = -1;
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                WaveInCapabilities wc = WaveInEvent.GetCapabilities(i);
                if (wc.ProductName.Contains("Stereo Mix"))
                {
                    deviceNum = i;
                    break;
                }
            }

            if (deviceNum == -1)
            {
                throw new Exception("Unable to find Windows Stereo Mix");
            }
            
            // Start audio recording.
            lock (this.recordingLock)
            {
                // Quick check if some other thread already started listening.
                if (this.recording == true)
                {
                    return;
                }

                WaveInEvent waveIn = new WaveInEvent();
                waveIn.DeviceNumber = deviceNum;
                waveIn.WaveFormat = new WaveFormat(this.sampleRate, 1);
                // Set the max buffer milliseconds in the event no data is being streamed we'll keep the pipeline streaming.
                waveIn.BufferMilliseconds = this.maxPacketLen/(this.sampleRate/1000);
                waveIn.DataAvailable += new EventHandler<WaveInEventArgs>((object sender, WaveInEventArgs e) =>
                {
                    this.streamBuff.AddSamples(e.Buffer, 0, e.BytesRecorded);
                });

                // Allocate the stream buffer and discard on overflow to keep it responsive.
                BufferedWaveProvider streamBuff = new BufferedWaveProvider(waveIn.WaveFormat);
                streamBuff.BufferLength = this.maxPacketLen*2;
                streamBuff.DiscardOnBufferOverflow = true;
                
                try
                {
                    waveIn.StartRecording();
                    this.waveIn = waveIn;
                    this.streamBuff = streamBuff;
                    this.recording = true;

                    // Check if a forwardthread was previously running.
                    if (this.forwardThread == null)
                    {
                        this.forwardThread = new Thread(this.ForwardBuffToListeners);
                        this.forwardThread.Start();
                    }
                }
                catch (Exception e)
                {
                    this.recording = false;
                    this.logger.Error("StartAudioCapture returned error: " + e.ToString());
                    throw new Exception("Unable to start recording audio: " + e.ToString());
                }
            }
        }

        private void ForwardBuffToListeners()
        {
            AudioPacket packet = new AudioPacket()
            {
                sampleByteSize = 2,
                sampleRate = this.sampleRate,
                data = new byte[this.maxPacketLen]
            };

            while (this.recording == true)
            {
                while (this.streamBuff.BufferedBytes >= this.maxPacketLen)
                {
                    this.streamBuff.Read(packet.data, 0, this.maxPacketLen);
                    this.audioStream.AddPacket(packet);
                }

                Thread.Sleep(this.waveIn.BufferMilliseconds / 2);
            }
        }
    }
}
