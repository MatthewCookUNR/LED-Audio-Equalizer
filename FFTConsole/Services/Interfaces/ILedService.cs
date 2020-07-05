using System;
using System.Collections.Generic;
using System.Text;

namespace FFTConsole.Services.Interfaces
{
    public interface ILedService
    {
        void Connect();
        void Disconnect();
        void EqualizerStart(IAudioCaptureService audioCaptureService);
        void EqualizerStop();
        void Ping();
    }
}
