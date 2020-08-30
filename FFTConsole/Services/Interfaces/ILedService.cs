using System;
using System.Collections.Generic;
using System.Text;

namespace LedEqualizer.Services.Interfaces
{
    public interface ILedService
    {
        void Connect();
        void Disconnect();
        void EqualizerStart(int fftBuffLen, IAudioCaptureService audioCaptureService);
        void EqualizerStop();
        void Ping(int timeoutMsec);
    }
}
