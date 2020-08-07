using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using Serilog;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;

namespace FFTConsole.Services.Interfaces
{
    public class SerialService : ICommunicationService
    {
        private SerialPort serial;
        bool stopListening;
        private readonly ILogger logger;
        private ResponseStream responseStream;
        private Thread listenThread;

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
                if (this.listenThread == null)
                {
                    this.listenThread = new Thread(this.Listen);
                    this.listenThread.Start();
                    this.stopListening = false;
                }
            }
        }

        public void Disconnect()
        {
            if (this.serial.IsOpen)
            {
                this.stopListening = true;
                this.listenThread.Join();
                this.serial.Close();
                this.listenThread = null;
            }
        }

        public void Send(Command command)
        {
            Connect();

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

            if (command.dataLen > 0)
            {
                Array.Copy(command.data, 0, message, 2, command.dataLen);
                for (int i = 0; i < command.dataLen; i++)
                {
                    checksum ^= command.data[i];
                }
            }

            message[message.Length - 2] = checksum;
            message[message.Length - 1] = 255;

             this.serial.Write(message, 0, message.Length);
        }

        public IDisposable ResponseSubscribe(Action<Response> onResponse)
        {
            ResponseStreamObserver observer = new ResponseStreamObserver(onResponse, this.logger);
            return this.responseStream.Subscribe(observer);
        }

        private void Listen()
        {
            Connect();
            byte[] inBuf = new byte[256];
            int currSize = 0;

            while(this.stopListening == false)
            {
                if (this.serial.BytesToRead == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                inBuf[currSize] = (byte)this.serial.ReadByte();

                if (inBuf[currSize] == 255)
                {
                    if (currSize < 3)
                    {
                        currSize = 0;
                        continue;
                    }

                    Response response = new Response();

                    // Response format (bytes) Size = DataLen + 5
                    // [0]      : CommandType
                    // [1]      : ReturnStatus
                    // [2]      : DataLen
                    // [3]->[X] : Data (where X is DataLen + 3)
                    // [X + 1]  : Checksum
                    // [X + 2]  : '\r' end of message
                    response.commandType = (ECommandType)inBuf[0];
                    response.returnStatus = (EReturnStatus)inBuf[1];
                    response.dataLen = inBuf[2];

                    byte checksum = 0;
                    checksum ^= inBuf[0];
                    checksum ^= inBuf[1];
                    checksum ^= inBuf[2];

                    if (response.dataLen > 0)
                    {
                        response.data = new byte[response.dataLen];
                        Array.Copy(inBuf, response.data, response.dataLen);
                        for (int i = 0; i < response.dataLen; i++)
                        {
                            checksum ^= response.data[i];
                        }
                    }

                    // Don't bother copying the data if the checksum is invalid.
                    if (checksum == inBuf[response.dataLen + 3 + 1])
                    {
                        if (response.dataLen > 0)
                        {
                            Array.Copy(inBuf, 3, response.data, 0, response.dataLen);
                        }
                    }
                    
                    //Console.WriteLine($"Received - {response.ToString()}\n");
                    this.responseStream.AddResponse(response);

                    currSize = 0;
                }
                else
                {
                    currSize++;
                    if (currSize == inBuf.Length)
                    {
                        currSize = 0;
                    }
                }
            }
        }
    }
}
