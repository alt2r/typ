using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using third_year_project.Services;

namespace third_year_project.Tests.Services
{
    public class SoundPlayerTest
    {
        [Fact]
        public void CanAcquireOwnership()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            soundPlayer.TryAcquire(this);
            Assert.True(soundPlayer.IsOwner(this));
        }

        [Fact]
        public void CanReleaseOwnership()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            soundPlayer.TryAcquire(this);
            soundPlayer.Release(this);
            Assert.False(soundPlayer.IsOwner(this));
        }
        [Fact]
        public void CannotAcquireIfAlreadyOwned()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            soundPlayer.TryAcquire(this);
            object anotherOwner = new object();
            Assert.False(soundPlayer.TryAcquire(anotherOwner));
        }
        [Fact]
        public void CannotReleaseIfNotOwner()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            object anotherOwner = new object();
            soundPlayer.Release(anotherOwner);
            Assert.False(soundPlayer.IsOwner(anotherOwner));
        }

        [Fact]
        public void CanReacquireAfterRelease()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            soundPlayer.TryAcquire(this);
            soundPlayer.Release(this);
            Assert.False(soundPlayer.IsOwner(this));
            Assert.True(soundPlayer.TryAcquire(this));
            Assert.True(soundPlayer.IsOwner(this));
        }

        [Fact]
        public void MultipleReleasesDoNotCauseIssues()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            soundPlayer.TryAcquire(this);
            soundPlayer.Release(this);
            soundPlayer.Release(this); // Should not cause any issues
            Assert.False(soundPlayer.IsOwner(this));
        }

        [Fact]
        public void CanTransferOwnership()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            soundPlayer.TryAcquire(this);
            object newOwner = new object();
            soundPlayer.Transfer(this, newOwner);
            Assert.True(soundPlayer.IsOwner(newOwner));
        }
        [Fact]
        public void CannotTransferIfNotOwner()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            object anotherOwner = new object();
            soundPlayer.Transfer(anotherOwner, this);
            Assert.False(soundPlayer.IsOwner(this));
        }
        [Fact]
        public void CannotAcquireIfNullOwner()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            Assert.Throws<ArgumentNullException>(() => soundPlayer.TryAcquire(null));
        }
        [Fact]
        public void MsToSample0Returns0()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            double samples = soundPlayer.MsToSample(0);
            Assert.Equal(0, samples);
        }
        [Fact]
        public void MsToSample1000Returns44100() //obv if we ever implement changing sample rates we will change this
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            long samples = soundPlayer.MsToSample(1000);
            Assert.Equal(44100, samples);
        }
        [Fact]
        public void MsToSampleNegativeThrows()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            Assert.Throws<ArgumentOutOfRangeException>(() => soundPlayer.MsToSample(-1));
        }
        [Fact]
        public void SampleToMs0Returns0()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            double ms = soundPlayer.SampleToMs(0);
            Assert.Equal(0, ms);
        }
        [Fact]
        public void SampleToMs44100Returns1000() //obv if we ever implement changing sample rates we will change this
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            double ms = soundPlayer.SampleToMs(44100);
            Assert.Equal(1000, ms);
        }
        [Fact]
        public void SampleToMsNegativeThrows()
        {
            SoundPlayer soundPlayer = new SoundPlayer();
            Assert.Throws<ArgumentOutOfRangeException>(() => soundPlayer.SampleToMs(-1));
        }
        [Fact]
        public void NoteToFrequencyA4Returns440()
        {
            double frequency = SoundPlayer.NoteToFrequency(Note.A4);
            Assert.Equal(440, frequency);
        }
        [Fact]
        public void NoteToFrequencyC4Returns261Point63()
        {
            double frequency = SoundPlayer.NoteToFrequency(Note.C4);
            Assert.Equal(261.6, frequency);
        }
        [Fact]
        public void NoteToFrequencyNegativeThrows()
        {
            Assert.Throws<InvalidDataException>(() => SoundPlayer.NoteToFrequency((Note)(-1)));
        }
        [Fact]
        public void NoteToFrequencyAboveRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SoundPlayer.NoteToFrequency((Note)6767));
        }
        [Fact]
        public void FrequencyToNote440ReturnsA4()
        {
            Note note = SoundPlayer.FrequencyToNote(440);
            Assert.Equal(Note.A4, note);
        }
        [Fact]
        public void FrequencyToNote261Point63ReturnsC4()
        {
            Note note = SoundPlayer.FrequencyToNote(261.63);
            Assert.Equal(Note.C4, note);
        }
        [Fact]
        public void FrequencyToNoteAboveRangeThrows()
        {
            Assert.Throws<InvalidDataException>(() => SoundPlayer.FrequencyToNote(6767));
        }
        [Fact]
        public void FrequencyToNoteNegativeThrows()
        {
            Assert.Throws<InvalidDataException>(() => SoundPlayer.FrequencyToNote(-1));
        }
    }
}
