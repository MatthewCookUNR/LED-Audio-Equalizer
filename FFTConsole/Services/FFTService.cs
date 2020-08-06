using FFTConsole.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FFTConsole.Services
{
    public class FFTService : IFFTService
    {
        public double[] Transform(double[] data)
        {
            double[] fft = new double[data.Length];
            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];

            for(int i = 0; i < data.Length; i++)
            {
                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
            };

            Accord.Math.Transforms.FourierTransform2.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);

            for (int i = 0; i < data.Length; i++)
            {
                fft[i] = fftComplex[i].Magnitude;
            }

            return fft;
        }
    }
}
