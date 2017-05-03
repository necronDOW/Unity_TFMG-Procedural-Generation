// 
// Copyright (c) 2013 Jason Bell
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
// 

using System;

namespace LibNoise
{
    public class Perlin
        : GradientNoiseBasis, IModule
    {
        public float persistence { get; set; }
        public int seed { get; set; }
        private int mOctaveCount;
        public float lacunarity { get; set; }

        private const int MaxOctaves = 30;

        public Perlin()
        {
            lacunarity = 2.0f;
            OctaveCount = 6;
            persistence = 0.5f;
            seed = 0;
        }

        public float GetValue(float x, float y, float z)
        {
            float value = 0.0f;
            float signal = 0.0f;
            float curpersistence = 1.0f;
            long _seed;

            for(int currentOctave = 0; currentOctave < OctaveCount; currentOctave++)
            {
                _seed = (seed + currentOctave) & 0xffffffff;
                signal = GradientCoherentNoise(x, y, z, (int)_seed, NoiseQuality.Standard);

                value += signal * curpersistence;

                x *= lacunarity;
                y *= lacunarity;
                z *= lacunarity;
                curpersistence *= persistence;
            }

            return value;
        }

        public int OctaveCount
        {
            get { return mOctaveCount; }
            set
            {
                if (value < 1 || value > MaxOctaves)
                    throw new ArgumentException("Octave count must be greater than zero and less than " + MaxOctaves);

                mOctaveCount = value;
            }
        }
    }
}
