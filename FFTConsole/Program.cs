using System;

using System.IO.Ports;
using System.Threading;
using Accord.Math;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using FFTConsole.Services;
using FFTConsole.Services.Interfaces;
using Serilog;
using Newtonsoft.Json;
namespace FFTConsole
{    class Program
    {
        static void Main(string[] args)
        {
            bool running;
            bool connected;
            int maxPacketLen = (int) Math.Pow(2, 12);
            ILogger logger = new LoggerConfiguration().WriteTo.File("log.txt", rollingInterval: RollingInterval.Month).CreateLogger();
            ICommunicationService communicationService = new SerialService(logger);
            IAudioCaptureService audioCaptureService = new NAudioCaptureService(96000, maxPacketLen, logger);
            IFFTService fftService = new FFTService();
            ILedService ledService = new AdafruitLedService(communicationService, fftService);

            running = true;
            // Attempt to connect to the device and start the equalizer.
            try
            {
                AttemptConnect(ledService, logger);
                ledService.EqualizerStart(maxPacketLen, audioCaptureService);
                connected = true;
            }
            catch (Exception e)
            {
                connected = false;
                logger.Error("Unable to connect to device: " + e.ToString());
            }

            
            // USEFUL DEBUG: Print everything to the Console.
            communicationService.ResponseSubscribe((response) =>
            {
                Console.WriteLine(JsonConvert.SerializeObject(response));
            });

            // Listen for user input.
            Console.WriteLine("Yo hit the spacebar to hit the lights bro, P to ping, Q/ESC to quit");
            while (running)
            {
                try
                {
                    Console.Write('>');
                    ConsoleKeyInfo input = Console.ReadKey();
                    switch (Char.ToUpper(input.KeyChar))
                    {
                        case ' ':
                            {
                                if (connected == true)
                                {
                                    ledService.Disconnect();
                                    connected = false;
                                }
                                else
                                {
                                    AttemptConnect(ledService, logger);
                                    ledService.EqualizerStart(maxPacketLen, audioCaptureService);
                                    connected = true;
                                }
                            }
                            break;
                        case 'P':
                            ledService.Ping(5000);
                            break;
                        case 'Q':
                        case (char)27: // ESC
                            running = false;
                            ledService.Disconnect();
                            break;
                        default:
                            break;
                    }

                    Console.WriteLine();
                }
                catch(Exception e)
                {
                    Console.WriteLine(">:( Okay now how did we get here?\n" + e.ToString());
                    continue; //momma didn't raise no quitter!
                }
            }
        }

        // DONT USE STATIC VOIDS - I did it here cuz Main doesn't have access to an instance of Program.
        // And honestly who cares what we're doin here in main anyways? the meat and potatoes are in the backend
        static void AttemptConnect(ILedService ledService, ILogger logger)
        {
            int retries = 5;

            while (retries > 0)
            {
                try
                {
                    ledService.Connect();
                    break;
                }
                catch (Exception e)
                {
                    logger.Warning($"Unable to connect to led device: {e.ToString()}");
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
