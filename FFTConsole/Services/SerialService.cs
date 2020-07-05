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
            this.serial = new SerialPort("COM");
            this.serial.BaudRate = 9600;
        }

        public void Connect()
        {
            if (!this.serial.IsOpen) this.serial.Open();
        }

        public void Disconnect()
        {
            if (this.serial.IsOpen) this.serial.Close();
        }

        public void Send(Command command)
        {
            this.Connect();
            this.serial.Write(JsonConvert.SerializeObject(command));
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
