using System;

using System.IO.Ports;
using System.Threading;
using Accord.Math;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using FFTConsole.Services;
using FFTConsole.Services.Interfaces;
using Serilog;

namespace FFTConsole
{    class Program
    {
        static void Main(string[] args)
        {
            int maxPacketLen = (int) Math.Pow(2, 8);
            ILogger logger = new LoggerConfiguration().WriteTo.File("log.txt", rollingInterval: RollingInterval.Month).CreateLogger();
            ICommunicationService communicationService = new SerialService(logger);
            IAudioCaptureService audioCaptureService = new NAudioCaptureService(44600, maxPacketLen, logger);
            IFFTService fftService = new FFTService();
            ILedService ledService = new AdafruitLedService(communicationService, fftService);

            Main body = new Main(ledService, audioCaptureService, logger);
            body.Run();
        }
    }     
      
}
