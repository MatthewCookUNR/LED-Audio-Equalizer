using FFTConsole.Services.Interfaces;
using FFTConsole.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Serilog;

namespace FFTConsole
{
    public class Main
    {
        ILedService ledService;
        ILogger logger;
        IAudioCaptureService audioCaptureService;
        bool running;
        bool connected;

        public Main(ILedService ledService, IAudioCaptureService audioCaptureService, ILogger logger)
        {
            this.ledService = ledService;
            this.audioCaptureService = audioCaptureService;
            this.logger = logger;
        }
        public void Run()
        {
            this.running = true;
            this.connected = false;

            while(this.running)
            {
                // Attempt to connect to the device.
                try 
                {
                    AttemptConnect();
                    connected = true;
                }
                catch (Exception e)
                {
                    connected = false;
                    this.logger.Error("Unable to connect to device: " + e.ToString());
                }

                // Start the equalizer.
                this.ledService.EqualizerStart(this.audioCaptureService);

                // Listen for user input.
                while (this.running)
                {
                    string input = Console.ReadLine();
                }
            }

            this.ledService.EqualizerStop();
            this.ledService.Disconnect();
            this.audioCaptureService.Stop();
        }

        private void AttemptConnect()
        {
            int retries = 5;
            
            while(retries > 0)
            {
                try
                {
                    this.ledService.Connect();
                    break;
                }
                catch (Exception e)
                {
                    this.logger.Warning($"Unable to connect to led device: {e.ToString()}");
                    retries--;
                    
                    // Prevent spamming;
                    Thread.Sleep(500);
                }
            }

            if (retries == 0)
            {
                throw new Exception("Unable to connect to device, max retries reached");
            }
        }
    }
}
