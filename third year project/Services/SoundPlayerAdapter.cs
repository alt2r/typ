using Avalonia.Threading;
using System;

namespace third_year_project.Services
{
    //i made this adaptor after refactoring for ease of testing (isoundplayer) to make it so i wouldnt have to change much of the source code
    public sealed class SoundPlayerAdapter : ISoundPlayer
    {
        private readonly SoundPlayer _inner = SoundPlayer.Instance;
        public event Action<double> SoundPlayed
        {
            add => _inner.soundPlayed += value;
            remove => _inner.soundPlayed -= value;
        }
        public bool TryAcquire(object requester) => _inner.TryAcquire(requester);
        public void Initialize(object requester) => _inner.Initialize(requester);
        public void Release(object requester) => _inner.Release(requester);
        public void ScheduleNote(object requester, long sample, Note note) => _inner.ScheduleNote(requester, sample, note);
        public long MsToSample(double ms) => _inner.MsToSample(ms);
        public long GetCurrentSample() => _inner.GetCurrentSample();
        public void Stop(object requester) => _inner.Stop(requester);

        public void PlayLiveNote(Note note) => _inner.PlayLiveNote(note);
        public double SampleToMs(long samples) => _inner.SampleToMs(samples);
        public void ClearScheduledNotes(object requester) => _inner.ClearScheduledNotes(requester);
    }

    public sealed class AvaloniaDispatcher : IAppDispatcher
    {
        public void Post(Action callback) => Dispatcher.UIThread.Post(callback);
    }
}