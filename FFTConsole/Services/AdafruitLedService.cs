using LedEqualizer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LedEqualizer.Services
{
    public class AdafruitLedService : ILedService
    {
        private const int PIXEL_COUNT_WIDTH = 16;
        private const int PIXEL_COUNT_HEIGHT = 16;

        private ICommunicationService communicationService;
        private IAudioCaptureService audioCaptureService;
        private IDisposable equalizerObserver;
        private IFFTService fftService;
        private byte[] equalizerBuff;
        private int equalizerBuffIndex;
        private int fftBuffLen;

        public AdafruitLedService(ICommunicationService communicationService, IFFTService fftService)
        {
            this.communicationService = communicationService;
            this.fftService = fftService;
        }

        public void EqualizerStart(int fftBuffLen, IAudioCaptureService audioCaptureService)
        {
            // If we were already listening close the observer.
            if (this.equalizerObserver != null)
            {
                this.equalizerObserver.Dispose();
            }

            this.fftBuffLen = fftBuffLen;
            this.equalizerBuff = new byte[this.fftBuffLen];
            this.equalizerBuffIndex = 0;
            this.equalizerObserver = audioCaptureService.Start(this.EqualizerOnAudioPacket);
            this.audioCaptureService = audioCaptureService;
        }

        public void EqualizerStop()
        {
            if (this.equalizerObserver != null)
            {
                this.equalizerObserver.Dispose();
                this.audioCaptureService.Stop();
                this.audioCaptureService = null;
            }
        }

        private void EqualizerOnAudioPacket(AudioPacket packet)
        {
            int overflow = (this.equalizerBuffIndex + packet.data.Length) - this.fftBuffLen;
            if (overflow >= 0)
            {
                Buffer.BlockCopy(packet.data, 0, this.equalizerBuff, this.equalizerBuffIndex, this.fftBuffLen - this.equalizerBuffIndex);
                // DONT FORGET to put the overflow into the start of our equalizer buff at the end.
            }
            else
            {
                Buffer.BlockCopy(packet.data, 0, this.equalizerBuff, this.equalizerBuffIndex, packet.data.Length);
                this.equalizerBuffIndex += packet.data.Length;
                return;
            }

            if (packet.sampleByteSize != 2)
            {
                throw new Exception("This is Unsupported");
            }

            // Create a (32-bit) double array ready to fill with the 16-bit data
            int graphPointCount = this.equalizerBuff.Length / packet.sampleByteSize;

            // Create double arrays to hold the data we will graph
            double[] pcm = new double[graphPointCount];
            double[] fft = new double[graphPointCount];

            // Populate Xs and Ys with double data
            for (int i = 0; i < graphPointCount; i++)
            {
                // Read the int16 from the two bytes
                Int16 val = BitConverter.ToInt16(this.equalizerBuff, i * 2);

                // Store the value in Ys as a percent (+/- 100% = 200%)
                pcm[i] = (double)(val) / Math.Pow(2, 16) * 200.0;
            }

            // Calculate the full FFT
            fft = this.fftService.Transform(pcm);

            // Determine horizontal axis units for graphs

            // fftReal[i] == power. Frequency == i * 45hz

            // Frequencies of hearing       % on Grid
            // 20 - 60 Hz Sub Base          5
            // 61 - 250 Hz Bass             15
            // 251 - 500 Hz Low Mid         25
            // 500 - 2500 Hz Mid            35
            // 2.5 - 4 KHz Upper Mid        20
            // 4 - 6 KHz Presence           15

            //  ReallFFTs available for the frequencies of music are smaller than we think.
            //  Because the buffer is of a much lower size than our audio sampling rate, our FFT does not     
            //  accurately represent the frequency down to the single Hz.
            //  Therefore we must determine that ourselves.
            //  And to make things worse half of our resulting FFT is imaginary.
            //
            //  EX realFFT buffer size of 1024 w/ fftMaxFreq 22400 
            //      this means that each realFFT[x] represents the frequency range of x * 22400/(1024/2) Hz == 22Hz
            // Using the chart and the frequency range per x of the realFFT we can determine how many x per individual frequency range our result set contains.


            // Serial write
            byte[] calculatedLines = new byte[16];
            int volumeLevel = this.audioCaptureService.GetVolume();

            // Just keep the real half (the other half imaginary)
            double fftMaxFreq = packet.sampleRate / 2;
            int entryFrequency = ((int)fftMaxFreq) / (fft.Length / 2);
            int[] entriesPerRange = new int[6];
            int[] samplesPerEntry = new int[6];
            int[] entryStartPoint = new int[6];
            int[] frequencyRange = new int[6];

            // 20 - 60 Hz Sub Base
            entriesPerRange[0] = 1;
            entryStartPoint[0] = 1;
            frequencyRange[0] = 60;
            samplesPerEntry[0] = frequencyRange[0] / (entriesPerRange[0] * entryFrequency);

            // 61 - 250 Hz Bass
            entriesPerRange[1] = 4;
            frequencyRange[1] = 200;

            // 251 - 500 Hz Low Mid
            entriesPerRange[2] = 5;
            frequencyRange[2] = 250;

            // 500 - 2000 Hz Mid
            entriesPerRange[3] = 3;
            frequencyRange[3] = 1500;

            // 2 - 4 KHz Upper Mid
            entriesPerRange[4] = 2;
            frequencyRange[4] = 2000;

            // 4 - 6 KHz Presence
            entriesPerRange[5] = 1;
            frequencyRange[5] = 1000;

            for (int i = 1; i < 6; i++)
            {
                entryStartPoint[i] = (samplesPerEntry[i - 1] * entriesPerRange[i - 1]) + entryStartPoint[i - 1];
                if (entryStartPoint[i] == entryStartPoint[i - 1])
                {
                    entryStartPoint[i] = entryStartPoint[i - 1]++;
                }
                samplesPerEntry[i] = frequencyRange[i] / (entriesPerRange[i] * entryFrequency);
            }

            double[] weight = new double[16];
            weight[0] = 0.32;
            weight[1] = 0.35;
            weight[2] = 0.45;
            weight[3] = 0.55;
            weight[4] = 0.65;
            weight[5] = 0.70;
            weight[6] = 0.75;
            weight[7] = 0.85;
            weight[8] = 0.95;
            weight[9] = 1.0;
            weight[10] = 1.2;
            weight[11] = 1.2;
            weight[12] = 2.0;
            weight[13] = 3.0;
            weight[14] = 4.0;
            weight[15] = 4.5;


            // Pre calculate the number of entries leading up to a range.
            int[] sumEntriesPerRange = new int[6];
            for (int i = 0; i < 6; i++)
            {
                sumEntriesPerRange[i] = 0;
                for (int j = 0; j < i; j++)
                {
                    sumEntriesPerRange[i] += entriesPerRange[j];
                }
            }

            Parallel.For(0, PIXEL_COUNT_WIDTH, (i) =>
            {
                int startPosition;
                int entries;
                int rangeNum;
                double fftAvg = 0.0;
                int posTotal = 0;

                for (rangeNum = 0; rangeNum < 6; rangeNum++)
                {
                    posTotal += entriesPerRange[rangeNum];
                    if (i < posTotal)
                    {
                        break;
                    }
                }

                startPosition = ((i - sumEntriesPerRange[rangeNum]) * samplesPerEntry[rangeNum] + entryStartPoint[rangeNum]);
                entries = samplesPerEntry[rangeNum];

                // Ignore the first array element because the input is too erratic to be used.
                for (int j = 0; j < entries; j++)
                {
                    fftAvg += fft[startPosition + j];
                }

                fftAvg = (fftAvg * weight[i]) / entries;

                calculatedLines[PIXEL_COUNT_WIDTH - i - 1] = (byte)((fftAvg) / (volumeLevel * 2));
                if (calculatedLines[PIXEL_COUNT_WIDTH - i - 1] > PIXEL_COUNT_HEIGHT)
                {
                    calculatedLines[PIXEL_COUNT_WIDTH - i - 1] = PIXEL_COUNT_HEIGHT;
                }
            });

            Command command = new Command()
            {
                commandType = ECommandType.EqualizerUpdate,
                data = calculatedLines,
                dataLen = calculatedLines.Length
            };

            this.communicationService.Send(command);

            // Don't forget to roll over the overflow from our packet into the next buffer.
            if (overflow > 0)
            {
                Buffer.BlockCopy(packet.data, packet.data.Length - overflow - 1, this.equalizerBuff, 0, overflow);
                this.equalizerBuffIndex = overflow;
            }
        }

        public void Connect()
        {
            this.communicationService.Connect();
        }

        public void Disconnect()
        {
            this.EqualizerStop();
            this.communicationService.Disconnect();
        }

        public void Ping(int timeoutMsec)
        {
            if (!this.communicationService.Ping(1000))
            {
                throw new TimeoutException("No response for Ping received");
            }
        }
    }
}
