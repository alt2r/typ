using CSCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace third_year_project.Services
{

    public sealed class SoundMixer : IWaveSource
    {
        private readonly int sampleRate;
        private readonly WaveFormat format;

        private readonly List<SineVoice> voices = new();
        private readonly object voiceLock = new();

        private readonly ConcurrentQueue<ScheduledAudioEvent> _events = new();

        private long _samplePosition;
        private readonly DateTime startTime = DateTime.UtcNow;
        public DateTime StartTime => startTime;

        //ConcurrentQueue<double> _noteNotifications = new();
        //public ConcurrentQueue<double> NoteNotifications => _noteNotifications;

        public event Action<double> soundPlayed;

        public SoundMixer(int _sampleRate = 44100)
        {
            sampleRate = _sampleRate;
            format = new WaveFormat(sampleRate, 16, 1);
        }

        public long SamplePosition => Interlocked.Read(ref _samplePosition);

        public WaveFormat WaveFormat => format;
        public bool CanSeek => false;
        public long Length => long.MaxValue;

        public long Position
        {
            get => SamplePosition * 2;
            set { }
        }
        public void Schedule(long sampleTime, Action action)
        {
            _events.Enqueue(new ScheduledAudioEvent
            {
                SampleTime = sampleTime,
                Trigger = action
            });
        }

        public void ScheduleInMs(double ms, Action action)
        {
            long samples = MsToSamples(ms);
            Schedule(SamplePosition + samples, action);
        }

        public void ScheduleSine(long sampleTime, double frequency, float amplitude, double durationMs)
        {
            Schedule(sampleTime, () =>
            {
                long durationSamples = MsToSamples(durationMs);
                //lock (voiceLock) //old locking system - not needed anymore?
                //{
                voices.Add(new SineVoice(
                    frequency,
                    amplitude,
                    sampleRate,
                    durationSamples));
                //}
                //_noteNotifications.Enqueue(frequency);
                soundPlayed?.Invoke(frequency);
            });
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            int samples = count / 2;
            int written = 0;

            for (int i = 0; i < samples; i++)
            {
                long currentSample = _samplePosition;

                //send scheduled events exactly on this sample
                while (_events.TryPeek(out var ev) &&
                       ev.SampleTime <= currentSample)
                {
                    _events.TryDequeue(out ev);
                    ev.Trigger();
                    Console.WriteLine($"triggering on {currentSample}");

                    //OnAudioEventTriggered?.Invoke(currentSample);
                }

                float mixed = 0f;
                int active = 0;

                lock (voiceLock)
                {
                    for (int v = voices.Count - 1; v >= 0; v--)
                    {
                        var voice = voices[v];

                        if (voice.remainingSamples <= 0)
                        {
                            voices.RemoveAt(v);
                            continue;
                        }

                        mixed += (float)Math.Sin(voice.phase) * voice.amplitude;
                        active++;

                        voice.phase += voice.phaseIncrement;
                        if (voice.phase > Math.PI * 2)
                            voice.phase -= Math.PI * 2;

                        voice.remainingSamples--;
                    }
                }

                if (active > 1)
                    mixed /= active;

                mixed = Math.Clamp(mixed, -1f, 1f);

                short sample = (short)(mixed * short.MaxValue);
                buffer[offset + written++] = (byte)(sample & 0xff);
                buffer[offset + written++] = (byte)(sample >> 8);

                Interlocked.Increment(ref _samplePosition);
            }

            return written;
        }

        public void Dispose()
        {
            lock (voiceLock)
            {
                voices.Clear();
            }
        }
        public long MsToSamples(double ms)
        {
            return (long)(ms * sampleRate / 1000.0);
        }

        public double SamplesToMs(long samples)
        {
            return samples * 1000.0 / sampleRate;
        }
    }

}
