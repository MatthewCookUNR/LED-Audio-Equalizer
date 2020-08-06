using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace FFTConsole.Services.Interfaces
{
    public interface IAudioCaptureService
    {
        IDisposable Start(Action<AudioPacket> onReceived);
        void Stop();
        int GetVolume();
    }

    public class AudioPacket
    {
        public byte[] data;
        public int sampleRate;
        public int sampleByteSize;
    }

    public class AudioStream : IObservable<AudioPacket>
    {
        private List<IObserver<AudioPacket>> observers;
        private List<AudioPacket> buffer;

        public AudioStream()
        {
            this.observers = new List<IObserver<AudioPacket>>();
            this.buffer = new List<AudioPacket>();
        }

        public IDisposable Subscribe(IObserver<AudioPacket> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);

                // Provide observer with existing data.
                foreach (AudioPacket packet in this.buffer)
                {
                    observer.OnNext(packet);
                }
            }
            return new AudioStreamDisposable(observer, this.observers);
        }

        public void AddPacket(AudioPacket audioPacket)
        {
            // Run this in parallel so that all subscribes can receive the payload ASAP.
            // If this were a traditional single threaded foreach, each subscriber must finish
            // their OnNext function before the next subscriber receives the Packet.
            Parallel.ForEach(this.observers, (observer) =>
            {
                observer.OnNext(audioPacket);
            });
        }
        public int SubscriberCount()
        {
            return this.observers.Count;
        }
    }

    public class AudioStreamObserver : IObserver<AudioPacket>
    {
        private readonly ILogger logger;
        private readonly Action<AudioPacket> onNextAction;

        public AudioStreamObserver(Action<AudioPacket> onNextAction, ILogger logger)
        {
            this.logger = logger;
            this.onNextAction = onNextAction;
        }

        public void OnCompleted() {}

        public void OnError(Exception exception)
        {
            this.logger.Error("AudioStream Exception: " + exception.ToString());
            throw new Exception("AudioStreamObserver exception: " + exception.ToString());
        }

        public void OnNext(AudioPacket packet)
        {
            this.onNextAction(packet);
        }
    }

    public class AudioStreamDisposable : IDisposable
    {
        private List<IObserver<AudioPacket>> observers;
        private IObserver<AudioPacket> observer;

        internal AudioStreamDisposable(IObserver<AudioPacket> observer, List<IObserver<AudioPacket>> observers)
        {
            this.observers = observers;
            this.observer = observer;
        }

        public void Dispose()
        {
            if (observers.Contains(this.observer))
            {
                observers.Remove(this.observer);
            }
        }
    }

}