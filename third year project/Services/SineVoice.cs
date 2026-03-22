using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace third_year_project.Services
{
    public sealed class SineVoice
    {
        public double phase;
        public double phaseIncrement;
        public float amplitude;
        public long remainingSamples;
        public Note? note;

        public SineVoice(double _frequency, float _amplitude, int _sampleRate, long _durationSamples)
        {
            phase = 0;
            phaseIncrement = 2.0 * Math.PI * _frequency / _sampleRate;
            amplitude = _amplitude;
            remainingSamples = _durationSamples;
        }

        public SineVoice(double _frequency, float _amplitude, int _sampleRate, long _durationSamples, Note _note)
        {
            phase = 0;
            phaseIncrement = 2.0 * Math.PI * _frequency / _sampleRate;
            amplitude = _amplitude;
            remainingSamples = _durationSamples;
            note = _note;
        }

    }
}
