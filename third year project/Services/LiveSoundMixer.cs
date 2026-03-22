using CSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace third_year_project.Services
{
    //    //so this is some old code from a piano i built while researching frameworks for this project, reusing it is the fastest way to
    //    //get user input to make sound in the practice page

    public class LiveSoundMixer : IWaveSource
    {
        private readonly int SAMPLERATE;
        private readonly WaveFormat WAVEFORMAT;
        private readonly List<SineVoice> activeTones;

        private readonly object _lock;
        private long _positionBytes;

        public LiveSoundMixer(int sampleRate)
        {
            SAMPLERATE = sampleRate;
            WAVEFORMAT = new WaveFormat(sampleRate, 16, 1);
            activeTones = new List<SineVoice>();
            _lock = new object();
            _positionBytes = 0;
        }

        public WaveFormat WaveFormat => WAVEFORMAT;
        public long Length
        {
            get { return long.MaxValue; }
        }

        public long Position
        {
            get { return _positionBytes; }
            set { _positionBytes = value; }
        }

        public bool CanSeek
        {
            get { return false; }
        }

        public void AddTone(double frequency, float amplitude, long duration)
        {
            lock (_lock)
            {
                activeTones.Add(new SineVoice(frequency, amplitude, SAMPLERATE, duration));
            }

        }

        public void StopTone(Note noteValue)
        {
            lock (_lock)
            {
                for (int i = 0; i < activeTones.Count; i++) //this is a shit way of doing it
                {
                    if (activeTones[i].note == noteValue)
                    {
                        activeTones[i].remainingSamples = 0;
                    }
                }
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int bytesToWrite = count - (count % 2);
            int samplesToWrite = bytesToWrite / 2;
            if (samplesToWrite == 0) return 0;

            int writtenBytes = 0;

            for (int s = 0; s < samplesToWrite; s++)
            {
                float mixedSample = 0.0f;
                int activeVoices = 0;

                lock (_lock)
                {
                    for (int t = activeTones.Count - 1; t >= 0; t--)
                    {
                        SineVoice tone = activeTones[t];
                        if (tone.remainingSamples <= 0)
                        {
                            activeTones.RemoveAt(t);
                            continue;
                        }

                        double value = Math.Sin(tone.phase) * tone.amplitude;
                        mixedSample += (float)value;
                        activeVoices++;

                        tone.phase += tone.phaseIncrement;
                        if (tone.phase > Math.PI * 2.0) tone.phase -= Math.PI * 2.0;

                        if (tone.remainingSamples != long.MaxValue)
                        {
                            tone.remainingSamples--;
                        }
                    }
                }

                if (activeVoices > 1) mixedSample = mixedSample / activeVoices;

                if (mixedSample > 1.0f) mixedSample = 1.0f;
                if (mixedSample < -1.0f) mixedSample = -1.0f;

                short intSample = (short)(mixedSample * short.MaxValue);
                int byteIndex = offset + s * 2;
                buffer[byteIndex] = (byte)(intSample & 0xFF);
                buffer[byteIndex + 1] = (byte)((intSample >> 8) & 0xFF);

                writtenBytes += 2;
                _positionBytes += 2;
            }

            return writtenBytes;
        }


        public void Dispose()
        {
            lock (_lock)
            {
                activeTones.Clear();
            }
        }
    }
}