using FFTConsole.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FFTConsole.Services
{
    public class AdafruitLedService : ILedService
    {
        private ICommunicationService communicationService;
        private IDisposable equalizerObserver;
        private IFFTService fftService;
        

        public AdafruitLedService(ICommunicationService communicationService, IFFTService fftService)
        {
            this.communicationService = communicationService;
            this.fftService = fftService;

            this.equalizerObserver = null;
        }

        public void EqualizerStart(IAudioCaptureService audioCaptureService)
        {
            // If we were already listening close the observer.
            if (this.equalizerObserver != null)
            {
                this.equalizerObserver.Dispose();
            }

            this.equalizerObserver = audioCaptureService.Start(this.EqualizerOnAudioPacket);
        }

        public void EqualizerStop()
        {
            if (this.equalizerObserver != null)
            {
                this.equalizerObserver.Dispose();
            }
        }

        private void EqualizerOnAudioPacket(AudioPacket packet)
        {
            if (packet.sampleByteSize != 2)
            {
                throw new Exception("This is Unsupported");
            }
            
            // Create a (32-bit) int array ready to fill with the 16-bit data
            int graphPointCount = packet.data.Length / packet.sampleByteSize;

            // Create double arrays to hold the data we will graph
            double[] pcm = new double[graphPointCount];
            double[] fft = new double[graphPointCount];
            double[] fftReal = new double[graphPointCount / 2];

            // Populate Xs and Ys with double data
            for (int i = 0; i < graphPointCount; i++)
            {
                // Read the int16 from the two bytes
                Int16 val = BitConverter.ToInt16(packet.data, i * 2);

                // Store the value in Ys as a percent (+/- 100% = 200%)
                pcm[i] = (double)(val) / Math.Pow(2, 16) * 200.0;
            }

            // Calculate the full FFT
            fft = this.fftService.Transform(pcm);

            // Determine horizontal axis units for graphs
            // Double pcmPointSpacingMs = RATE / 1000;
            double fftMaxFreq = packet.sampleRate / 2;
            double fftPointSpacingHz = fftMaxFreq / graphPointCount;

            // Just keep the real half (the other half imaginary)
            Array.Copy(fft, fftReal, fftReal.Length);

            // fftReal[i] == power. Frequency == i * 45hz

            // 16 x 32                          Samples in range        TOTAL
            // 1) 10 - 50 super low         |       1                   1
            // 2) 50 - 100 med low          |       1                   2
            // 3) 100 - 200 upper low       |       2                   4
            // 4) 200 - 500 low mid         |       6                   10
            // 5) 500 - 1000 med mid        |       11                  21
            // 6) 1000 - 2000 high mid      |       22                  43
            // 7) 2000 - 5000 low high      |       66                  109
            // 8) 5000 - 10000 med high     |       111
            // 9) 10000 - 20000 high high   |       222
            
            // X = 32                                                   FftReal[i]      Matrix X0->XN   SamplesAvail/LEDsInRange    FftAvg
            // 1) Bass      60 -> 250       |       4                   1->4                1->4                4/4                    1
            // 2) Low mid   250 -> 500      |       5                   5->9                5->9                5/5                    1
            // 3) Mid       500 -> 2K       |       25                  10->33              10->15              25/6                   4
            // 4) High mid  2K -> 4K        |       44.44               34->77              16->21              44/6                   7
            // 5) High      4K -> 6K        |       44.44               78->122             22->27              44/6                   7
            // 6) Brilliance? 6K -> 10K     |       88.88               123->211            28->32              89/5                   45
            
            // X = 16
            // 1) Bass      60 -> 250       |       4                   1->4                1->2                4/2                    1
            // 2) Low mid   250 -> 500      |       5                   5->9                3->4                5/2                    1
            // 3) Mid       500 -> 2K       |       25                  10->33              5->7                25/3                   4
            // 4) High mid  2K -> 4K        |       44.44               34->77              8->11               44/3                   7
            // 5) High      4K -> 6K        |       44.44               78->122             12->14              44/3                   7
            // 6) Brilliance? 6K -> 10K     |       88.88               123->211            15->16              89/2                   45



            // Serial write
            int[] calculatedLines = new int[32];
            double fftAvg;
            int totalFfts;
            double weight;

            for (int i = 0; i < 16; i++)
            {
                if (i < 5)
                {
                    totalFfts = 1;
                    weight = 0.20;
                }
                else if (i < 8)
                {
                    totalFfts = 4;
                    weight = 0.8;
                }
                else if (i < 14)
                {
                    totalFfts = 7;
                    weight = 1.6;
                }
                else
                {
                    totalFfts = 7;
                    weight = 1.6;
                }

                // We are grouping 2 X LEDs together
                totalFfts *= 2;
                fftAvg = 0.0;

                // Ignore the first array element because the input is too erratic to be used.
                for (int j = 1; j < (totalFfts + 1); j++)
                {
                    fftAvg += fftReal[(i * totalFfts) + j];
                }

                fftAvg = (fftAvg * weight)/totalFfts;
             
                calculatedLines[15 - i] = (int)(fftAvg * 100 * 16.0);
                if (calculatedLines[15 - i] > 16)
                {
                    calculatedLines[15 - i] = 16;
                }
            }


            string outBuf = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}," +
                                          "{8},{9},{10},{11},{12},{13},{14},{15},\r",
                                          calculatedLines[0], calculatedLines[1], calculatedLines[2], calculatedLines[3], calculatedLines[4], calculatedLines[5], calculatedLines[6], calculatedLines[7],
                                          calculatedLines[8], calculatedLines[9], calculatedLines[10], calculatedLines[11], calculatedLines[12], calculatedLines[13], calculatedLines[14], calculatedLines[15]);

            Command command = new Command()
            {
                commandType = ECommandType.EqualizerUpdate,
                data = outBuf
            };

            this.communicationService.Send(command);
        }

        public void Connect()
        {
            this.communicationService.Connect();
        }

        public void Disconnect()
        {
            this.communicationService.Disconnect();
        }

        public void Ping()
        {
            throw new NotImplementedException();
        }
    }
}
