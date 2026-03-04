using Avalonia.Input;
using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace third_year_project.Services
{
    public sealed class SoundPlayer //exclusive ownership 
    {

        private static readonly SoundPlayer _instance = new SoundPlayer();
        public static SoundPlayer Instance => _instance;

        private readonly object _lock = new();
        private object? _owner;

        private SoundPlayer() { }

        // Try to become owner
        public bool TryAcquire(object requester)
        {
            lock (_lock)
            {
                if (_owner == null)
                {
                    _owner = requester;
                    return true;
                }
                Console.WriteLine("failed sound player acquisition (shouldnt happen!!!) ");
                return false;
            }
        }

        // Transfer ownership
        public bool Transfer(object currentOwner, object newOwner)
        {
            lock (_lock)
            {
                if (!ReferenceEquals(_owner, currentOwner))
                    return false;

                _owner = newOwner;
                return true;
            }
        }

        // Release ownership
        public void Release(object requester)
        {
            lock (_lock)
            {
                if (ReferenceEquals(_owner, requester))
                    _owner = null;
            }
        }

        public bool IsOwner(object requester)
        {
            lock (_lock)
            {
                return ReferenceEquals(_owner, requester);
            }
        }


        private ISoundOut soundOut;
        //private IWaveSource _waveSource;
        //private string soundsFolder = Path.Combine(AppContext.BaseDirectory, "Sounds");
        private double bufferTime = 0.1; // seconds

        //private int kickPlayingInt = 0, snarePlaying;
        public SoundMixer soundMixer;
        private float noteAmplitude = 0.8f;
        private int noteDurationMs = 200;

        //this needs to be called on every page that wants to use this
        public void Initialize(object requester)
        {
            if (!IsOwner(requester))
            {
                return;
            }
            soundMixer = new SoundMixer(44100);
            soundOut = new WasapiOut();
            soundOut.Initialize(soundMixer);
            soundOut.Play();
        }
        //general idea is that only one page will be using the soundplayer at a time.
        //could look into locks for robustness 


        public void scheduleNote(object requester, long sample, Note note)
        {
            if (!IsOwner(requester))
            {
                Console.WriteLine("An object tried to access soundplayer without being the owner");
                return;
            }

            double frequency = NoteToFrequency(note);
            soundMixer.ScheduleSine(sample, frequency, noteAmplitude, noteDurationMs);
        }

        //tbh im all good if non owners wanna call these functions
        public long msToSample(double ms)
        {
            return soundMixer.MsToSamples(ms);
        }
        public double sampleToMs(long samples)
        {
            return soundMixer.SamplesToMs(samples);
        }
        public long getCurrentSample()
        {
            return soundMixer.SamplePosition;
        }
        public DateTime getStartTime()
        {
            return soundMixer.StartTime;
        }

        public static double NoteToFrequency(Note note) //can be static since it doesnt access instance data
        {
            int octave = 0;
            while ((int)note >= 12)
            {
                note -= 12;
                octave++;
            }
            double frequency = 0;
            //Console.WriteLine(note);
            switch (note)
            {
                case Note.C0:
                    frequency = 16.35;
                    break;
                case Note.CSharp0:
                    frequency = 17.32;
                    break;
                case Note.D0:
                    frequency = 18.35;
                    break;
                case Note.DSharp0:
                    frequency = 19.45;
                    break;
                case Note.E0:
                    frequency = 20.6;
                    break;
                case Note.F0:
                    frequency = 21.83;
                    break;
                case Note.FSharp0:
                    frequency = 23.12;
                    break;
                case Note.G0:
                    frequency = 24.5;
                    break;
                case Note.GSharp0:
                    frequency = 25.96;
                    break;
                case Note.A0:
                    frequency = 27.5;
                    break;
                case Note.ASharp0:
                    frequency = 29.14;
                    break;
                case Note.B0:
                    frequency = 30.87;
                    break;

                default:
                    Console.WriteLine("error invalid note entry!");
                    break;
            }
            for (int i = 0; i < octave; i++)
            {
                frequency *= 2;
            }
            return frequency;
        }

        public static Note FrequencyToNote(double frequency)
        {
            int octave = 0;
            Note note = Note.C0;
            while(frequency > 31) //B0, the highest note in octave 0, has a frequency of 30.87
            {
                Console.WriteLine(frequency);
                octave++;
                frequency = frequency / 2;
            }

            switch (frequency)
            {
                case 16.35:
                    note = Note.C0;
                    break;
                case 17.32:
                    note = Note.CSharp0;
                    break;
                case 18.35:
                    note = Note.D0;
                    break;
                case 19.45:
                    note = Note.DSharp0;
                    break;
                case 20.6:
                    note = Note.E0;
                    break;
                case 21.83:
                    note = Note.F0;
                    break;
                case 23.12:
                    note = Note.FSharp0;
                    break;
                case 24.5:
                    note = Note.G0;
                    break;
                case 25.96:
                    note = Note.GSharp0;
                    break;
                case 27.5:
                    note = Note.A0;
                    break;
                case 29.14:
                    note = Note.ASharp0;
                    break;
                case 30.87:
                    note = Note.B0;
                    break;

                default:
                    Console.WriteLine("error invalid note entry!");
                    break;
            }
            while(octave > 0)
            {
                Console.WriteLine(octave);
                octave -= 1;
                note += 12;
            }
            Console.WriteLine(note);
            return note;
        }

        public ConcurrentQueue<double> GetSoundMixerNotificationQueue(object requester)
        {
            if (!IsOwner(requester))
            {
                Console.WriteLine("An object tried to access soundplayer without being the owner");
                return null;
            }
            return soundMixer.NoteNotifications;
        }
        //public void PlayKick()
        //{
        //    //Console.WriteLine("kick playing is now " + kickPlayingInt);
        //    if (Interlocked.CompareExchange(ref kickPlayingInt, 1, 0) == 1)
        //        return;

        //    Task.Delay(50).ContinueWith(_ =>
        //    {
        //        //Console.WriteLine("setting kick playing to false");
        //        Interlocked.Exchange(ref kickPlayingInt, 0);
        //    });
        //    //Console.WriteLine("setting kick playing to treu");
        //    // Decode MP3 → PCM
        //    _waveSource = CodecFactory.Instance.GetCodec(Path.Combine(soundsFolder, "TMKD04 - dying_kick_01.mp3"))
        //        .ToSampleSource()
        //        .ToWaveSource();

        //    // Output through WASAPI
        //    _soundOut = new WasapiOut();
        //    _soundOut.Initialize(_waveSource);

        //    //kickPlaying = true;

        //    _soundOut.Play();

        //}

        //public void PlaySnare()
        //{
        //    // Decode MP3 → PCM
        //    _waveSource = CodecFactory.Instance.GetCodec(Path.Combine(soundsFolder, "TMKD_SnareYMCAN_dyn_L7_05.mp3"))
        //        .ToSampleSource()
        //        .ToWaveSource();
        //    // Output through WASAPI
        //    _soundOut = new WasapiOut();
        //    _soundOut.Initialize(_waveSource);
        //    _soundOut.Play();
        //}

        public void Stop(object requester)
        {
            if (!IsOwner(requester))
            {
                Console.WriteLine("An object tried to access soundplayer without being the owner");
                return;
            }
            Console.WriteLine("stopping");
            soundOut?.Stop();
            soundOut?.Dispose();
            soundOut = null;
            if (soundMixer != null)
            {
                soundMixer.Dispose();
            }
        }
    }
    public sealed class SineVoice
    {
        public double Phase;
        public double PhaseIncrement;
        public float Amplitude;
        public long RemainingSamples;

        public SineVoice(double frequency, float amplitude, int sampleRate, long durationSamples)
        {
            Phase = 0;
            PhaseIncrement = 2.0 * Math.PI * frequency / sampleRate;
            Amplitude = amplitude;
            RemainingSamples = durationSamples;
        }
    }

    public sealed class ScheduledAudioEvent
    {
        public long SampleTime;
        public Action Trigger;
    }

    public sealed class SoundMixer : IWaveSource
    {
        private readonly int _sampleRate;
        private readonly WaveFormat _format;

        private readonly List<SineVoice> _voices = new();
        private readonly object _voiceLock = new();

        private readonly ConcurrentQueue<ScheduledAudioEvent> _events = new();

        private long _samplePosition;
        private readonly DateTime startTime = DateTime.UtcNow;
        public DateTime StartTime => startTime;

        ConcurrentQueue<double> _noteNotifications = new();
        public ConcurrentQueue<double> NoteNotifications => _noteNotifications;


        public SoundMixer(int sampleRate = 44100)
        {
            _sampleRate = sampleRate;
            _format = new WaveFormat(sampleRate, 16, 1);
        }

        public long SamplePosition => Interlocked.Read(ref _samplePosition);

        public WaveFormat WaveFormat => _format;
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
                lock (_voiceLock)
                {
                    _voices.Add(new SineVoice(
                        frequency,
                        amplitude,
                        _sampleRate,
                        durationSamples));
                }
                _noteNotifications.Enqueue(frequency);
            });
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            int samples = count / 2;
            int written = 0;

            for (int i = 0; i < samples; i++)
            {
                long currentSample = _samplePosition;

                // Fire scheduled events exactly on this sample
                while (_events.TryPeek(out var ev) &&
                       ev.SampleTime <= currentSample)
                {
                    _events.TryDequeue(out ev);
                    ev.Trigger();

                    //OnAudioEventTriggered?.Invoke(currentSample);
                }

                float mixed = 0f;
                int active = 0;

                lock (_voiceLock)
                {
                    for (int v = _voices.Count - 1; v >= 0; v--)
                    {
                        var voice = _voices[v];

                        if (voice.RemainingSamples <= 0)
                        {
                            _voices.RemoveAt(v);
                            continue;
                        }

                        mixed += (float)Math.Sin(voice.Phase) * voice.Amplitude;
                        active++;

                        voice.Phase += voice.PhaseIncrement;
                        if (voice.Phase > Math.PI * 2)
                            voice.Phase -= Math.PI * 2;

                        voice.RemainingSamples--;
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
            lock (_voiceLock)
            {
                _voices.Clear();
            }
        }
        public long MsToSamples(double ms)
            => (long)(ms * _sampleRate / 1000.0);

        public double SamplesToMs(long samples)
            => samples * 1000.0 / _sampleRate;
    }

}
