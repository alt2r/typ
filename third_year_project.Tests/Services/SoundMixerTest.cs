using System;
using System.Linq;
using Xunit;
using third_year_project.Services;

namespace third_year_project.Tests.Services
{
    public class SoundMixerTests
    {
        [Fact]
        public void ScheduleActionIsTriggeredOnRead()
        {
            var mixer = new SoundMixer(_sampleRate: 1000); //small sample rate
            bool triggered = false;

            //schedule to run on the current sample position
            mixer.Schedule(mixer.SamplePosition, () => triggered = true);

            byte[] buffer = new byte[2];
            int read = mixer.Read(buffer, 0, buffer.Length);

            Assert.Equal(2, read); //one sample written
            Assert.True(triggered);
            Assert.Equal(1L, mixer.SamplePosition); //sample position advanced by 1
        }

        [Fact]
        public void ScheduleInMsSchedulesAtCorrectSample()
        {
            var mixer = new SoundMixer(_sampleRate: 1000);
            bool triggered = false;

            //schedule 10 ms in the future
            mixer.ScheduleInMs(10.0, () => triggered = true);

            long samplesToAdvance = mixer.MsToSamples(10.0);
            //read enough samples to pass the scheduled time + a margin
            byte[] buffer = new byte[(int)(samplesToAdvance + 2) * 2];
            int read = mixer.Read(buffer, 0, buffer.Length);

            Assert.True(triggered);
            Assert.Equal(samplesToAdvance + 2, mixer.SamplePosition);
        }

        [Fact]
        public void ScheduleSineSchedulesNote()
        {
            var mixer = new SoundMixer(_sampleRate: 1000);
            int eventCount = 0;
            double lastFreq = 0;
            mixer.soundPlayed += (f) => { eventCount++; lastFreq = f; };

            //schedule sine now with duration long enough to affect the buffer
            double freq = 440.0;
            double amplitude = 0.5;
            double durationMs = 100.0; // 100ms -> 100 samples at 1000Hz
            mixer.ScheduleSine(mixer.SamplePosition, freq, amplitude, durationMs);

            //read a chunk of samples larger than the duration to ensure voice has time to contribute
            int samplesToRead = 200;
            byte[] buffer = new byte[samplesToRead * 2];
            int read = mixer.Read(buffer, 0, buffer.Length);

            Assert.True(eventCount >= 1);
            Assert.Equal(freq, lastFreq);
            Assert.Equal(samplesToRead, mixer.SamplePosition);
        }

        [Fact]
        public void ClearScheduledNotesStopsThings()
        {
            var mixer = new SoundMixer(_sampleRate: 1000);
            bool triggered = false;

            mixer.Schedule(mixer.SamplePosition + 1, () => triggered = true);

            // clear scheduled notes before we advance samples
            mixer.ClearScheduledNotes();

            // advance two samples
            byte[] buffer = new byte[4];
            int read = mixer.Read(buffer, 0, buffer.Length);

            Assert.False(triggered);
            Assert.Equal(2L, mixer.SamplePosition);
        }
        [Fact]
        public void GetSetAmplitudeWorks()
        {
            var sp = SoundPlayer.Instance;
            var owner = new object();

            try
            {
                Assert.True(sp.TryAcquire(owner), "Test must acquire ownership to run reliably.");
                sp.SetAmplitude(owner, 0.35);
                Assert.Equal(0.35, sp.GetAmplitude(owner), 3);
            }
            finally
            {
                sp.Release(owner);
            }
        }
    }
}