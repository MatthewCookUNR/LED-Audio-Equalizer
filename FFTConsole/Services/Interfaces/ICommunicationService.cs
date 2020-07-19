using Accord;
using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace FFTConsole.Services.Interfaces
{
    public interface ICommunicationService
    {
        void Connect();
        void Disconnect();
        void Send(Command command);
        IDisposable ResponseSubscribe(Action<Response> onResponse);
    }

    public class Command
    {
        public ECommandType commandType;
        public byte[] data;
        public int dataLen;

        public Command()
        {
            this.data = null;
            this.dataLen = 0;
        }
    }

    public class Response
    {
        public ECommandType commandType;
        public EReturnStatus returnStatus;
        public byte[] data;
        public int dataLen;

        public Response()
        {
            this.data = new byte[64];
            this.dataLen = 0;
        }
    }

    public enum ECommandType
    {
        Ping = 0x00,
        EqualizerUpdate = 0x10
    }

    public enum EReturnStatus
    {
        ok = 0x00,
        error = 0x01
    }

    
    public class ResponseStream : IObservable<Response>
    {
        private List<IObserver<Response>> observers;
        private List<Response> buffer;

        public ResponseStream()
        {
            this.observers = new List<IObserver<Response>>();
            this.buffer = new List<Response>();
        }

        public IDisposable Subscribe(IObserver<Response> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);

                // Provide observer with existing data.
                foreach (var packet in this.buffer) observer.OnNext(packet);
            }
            return new ResponseStreamDisposable(observer, this.observers);
        }

        public void AddResponse(Response Response)
        {
            foreach(var observer in this.observers) observer.OnNext(Response);
        }
        public int SubscriberCount()
        {
            return this.observers.Count;
        }
    }

    public class ResponseStreamObserver : IObserver<Response>
    {
        private readonly ILogger logger;
        private readonly Action<Response> onNextAction;

        public ResponseStreamObserver(Action<Response> onNextAction, ILogger logger)
        {
            this.logger = logger;
            this.onNextAction = onNextAction;
        }

        public void OnCompleted() {}

        public void OnError(Exception exception)
        {
            this.logger.Error("ResponseStream Exception: " + exception.ToString());
            throw new Exception("ResponseStreamObserver exception: " + exception.ToString());
        }

        public void OnNext(Response packet)
        {
            this.onNextAction(packet);
        }
    }

    public class ResponseStreamDisposable : IDisposable
    {
        private List<IObserver<Response>> observers;
        private IObserver<Response> observer;

        internal ResponseStreamDisposable(IObserver<Response> observer, List<IObserver<Response>> observers)
        {
            this.observers = observers;
            this.observer = observer;
        }

        public void Dispose()
        {
            if (observers.Contains(this.observer)) observers.Remove(this.observer);
        }
    }

}
