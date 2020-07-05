using System;
using System.Collections.Generic;
using System.Text;

namespace FFTConsole.Services.Interfaces
{
    public interface IFFTService
    {
        double[] Transform(double[] data);
    }
}
