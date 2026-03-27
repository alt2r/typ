using System;

namespace third_year_project.Services
{
    //mockable interface for the sound player
    public interface ISoundPlayer
    {
        event Action<double> SoundPlayed;
        bool TryAcquire(object requester);
        void Initialize(object requester);
        void Release(object requester);
        void ScheduleNote(object requester, long sample, Note note);
        long MsToSample(double ms);
        long GetCurrentSample();
        void Stop(object requester);

        void PlayLiveNote(Note note);
        double SampleToMs(long samples);
        void ClearScheduledNotes(object requester);
    }
}