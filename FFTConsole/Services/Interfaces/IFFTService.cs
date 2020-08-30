using System;
using System.Collections.Generic;
using System.Text;

namespace LedEqualizer.Services.Interfaces
{
    public interface IFFTService
    {
        double[] Transform(double[] data);
    }
}
