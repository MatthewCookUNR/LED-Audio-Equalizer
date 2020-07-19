using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using Serilog;
using Newtonsoft.Json;


namespace FFTConsole.Services.Interfaces
{
    public class SerialService : ICommunicationService
    {
        private SerialPort serial;
        private readonly ILogger logger;
        private ResponseStream responseStream;

        public SerialService(ILogger logger) 
        { 
            this.logger = logger;
            
            this.responseStream = new ResponseStream();
            this.serial = new SerialPort("COM3");
            this.serial.BaudRate = 9600;
            this.Connect();
        }

        ~SerialService()
        {
            this.Disconnect();
        }

        public void Connect()
        {
            if (!this.serial.IsOpen)
            {
                this.serial.Open();
            }
        }

        public void Disconnect()
        {
            if (this.serial.IsOpen)
            {
                this.serial.Close();
            }
        }

        public void Send(Command command)
        {
            // Message format (bytes) Size = DataLen + 4
            // [0]      : CommandType
            // [1]      : DataLen
            // [2]->[X] : Data (where X is DataLen + 2)
            // [X + 1]  : Checksum
            // [X + 2]  : '\r' end of message
            byte[] message = new byte[1 + 1 + command.dataLen + 1 + 1];
            message[0] = (byte) command.commandType;
            message[1] = (byte) command.dataLen;
            
            byte checksum = 0;
            checksum ^= message[0];
            checksum ^= message[1];

            for (int i = 0; i < command.dataLen; i++)
            {
                checksum ^= command.data[i];
                message[2 + i] = command.data[i];
            }

            message[message.Length - 2] = checksum;
            message[message.Length - 1] = (byte)'\r';


            this.serial.Write(message, 0, message.Length);
            this.logger.Information("Just sent:" + JsonConvert.SerializeObject(command).ToString());
        }

        public IDisposable ResponseSubscribe(Action<Response> onResponse)
        {
            this.Listen();
            ResponseStreamObserver observer = new ResponseStreamObserver(onResponse, this.logger);
            return this.responseStream.Subscribe(observer);
        }

        private void Listen()
        {
            Connect();

            while(this.serial.IsOpen)
            {
                string line = this.serial.ReadLine();
                try
                {
                    Response response = JsonConvert.DeserializeObject<Response>(line);
                    Console.WriteLine("Received: " + line);
                    this.responseStream.AddResponse(response);
                }
                catch (Exception e)
                {
                    // Continue parsing despite a failed parse.
                    this.logger.Error("Unable to parse response, error: " + e.ToString() + ", content: " + line);
                }
            }
        }
    }
}
