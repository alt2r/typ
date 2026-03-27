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
using System.Reflection.Metadata.Ecma335;
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

        public event Action<double> soundPlayed;

        private ISoundOut soundOut;
        private ISoundOut liveSoundOut; //getting one sound engine to paly scheduled stuff and live stuff is hard, and apprently scheduling for right now doesnt work
        private double bufferTime = 0.1; // seconds

        public SoundMixer soundMixer;
        public LiveSoundMixer liveSoundMixer;
        private double noteAmplitude = 0.8;
        private int noteDurationMs = 200;
        private const int SAMPLERATE = 44100;

        //attepmt to become owner
        public bool TryAcquire(object requester)
        {
            if(requester == null)
                throw new ArgumentNullException(nameof(requester));

            lock (_lock)
            {
                if (_owner == null)
                {
                    _owner = requester;
                    return true;
                }
                return false;
            }
        }

        //transfer ownership
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

        //give up ownership
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

        //this needs to be called on every page that wants to use this
        public void Initialize(object requester)
        {
            if (!IsOwner(requester))
            {
                return;
            }
            soundMixer = new SoundMixer(SAMPLERATE);
            soundOut = new WasapiOut();
            soundOut.Initialize(soundMixer);
            soundOut.Play();

            liveSoundMixer = new LiveSoundMixer(SAMPLERATE);
            liveSoundOut = new WasapiOut();
            liveSoundOut.Initialize(liveSoundMixer);
            liveSoundOut.Play();

            //this is basically a middle man action that forwards events from sound mixer to viewmodels
            soundMixer.soundPlayed += (frequency) => //when the sound mixer triggers its event
            {
                soundPlayed?.Invoke(frequency); //trigger the action event with the frequency
            };
        }
        //general idea is that only one page will be using the soundplayer at a time.

        public void PlayLiveNote(Note note)
        {
            liveSoundMixer.AddTone(NoteToFrequency(note), noteAmplitude, MsToSample(noteDurationMs));
        }

        public void ScheduleNote(object requester, long sample, Note note)
        {
            if (!IsOwner(requester))
            {
                return;
            }

            double frequency = NoteToFrequency(note);
            soundMixer.ScheduleSine(sample, frequency, noteAmplitude, noteDurationMs);
        }

        //tbh im all good if non owners wanna call these functions
        public long MsToSample(double ms)
        {
            if(ms < 0)
                throw new ArgumentOutOfRangeException(nameof(ms));

            return (long)(ms * SAMPLERATE / 1000.0);
        }
        public double SampleToMs(long samples)
        {
            return (double)samples * 1000.0 / SAMPLERATE;
        }
        public long GetCurrentSample()
        {
            return soundMixer.SamplePosition;
        }
        public DateTime getStartTime()
        {
            return soundMixer.StartTime;
        }

        public void ClearScheduledNotes(object requester)
        {
            if (!IsOwner(requester))
            {
                return;
            }
            soundMixer.ClearScheduledNotes();
        }

        public void SetAmplitude(object requester, double amplitude)
        {
            if (!IsOwner(requester))
            {
                return;
            }
            if (amplitude < 0 || amplitude > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amplitude));
            }
            noteAmplitude = amplitude;
        }

        public double GetAmplitude(object requester)
        { //its kind of fine if non owners call this
            return noteAmplitude;
        }

        public static double NoteToFrequency(Note note) //can be static since it doesnt access instance data
        {
            int octave = 0;
            while ((int)note >= 12)
            {
                note -= 12;
                octave++;
            }
            if(octave > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(note));
            }
            double frequency = 0;
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
                    throw new InvalidDataException("error invalid note entry!");
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
                octave++;
                frequency = frequency / 2;
            }
            frequency = Math.Round(frequency, 2); //round to 2 decimal places to avoid floating point precision issues messing with the switch statement
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
                    throw new InvalidDataException("error invalid note entry!");
                    break;
            }
            while(octave > 0)
            {
                octave -= 1;
                note += 12;
            }
            return note;
        }

        public void Stop(object requester)
        {
            if (!IsOwner(requester))
            {
                return;
            }
            soundOut?.Stop();
            soundOut?.Dispose();
            soundOut = null;

            liveSoundOut?.Stop();
            liveSoundOut?.Dispose();
            liveSoundOut = null;

            if (soundMixer != null)
            {
                soundMixer.Dispose();
            }
            if(liveSoundMixer != null)
            {
                liveSoundMixer.Dispose();
            }
        }
    }

}
