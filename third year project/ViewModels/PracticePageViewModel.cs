using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CSCore.DirectSound;
using CSCore.DSP;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using third_year_project.Services;
using third_year_project.Views;

namespace third_year_project.ViewModels
{
    public class PracticePageViewModel : ViewModelBase
    {
        readonly ISoundPlayer soundPlayer;
        readonly IAppDispatcher dispatcher;

        Key leftKey = SettingsService.Instance.LeftInputKey != Key.None ? SettingsService.Instance.LeftInputKey : Key.A; //default keys if settings havent been set yet
        Key rightKey = SettingsService.Instance.RightInputKey != Key.None ? SettingsService.Instance.RightInputKey : Key.L;
        const double LOOKAHEADSECONDS = 0.1;
        long startSample = 0;
        bool leftOn = true, rightOn = true;

        Note leftPatternNote = Note.C4;
        Note rightPatternNote = Note.F4;
        public Note LeftPatternNote => leftPatternNote; //shorthand getter methods, something i discovered way too late into development lol
        public Note RightPatternNote => rightPatternNote;

        InputDevice MidiInputDevice;
        public event Action<Note> midiAction;


        int[] leftPattern;
        int[] rightPattern;
        private DispatcherTimer? leftTimer;
        private DispatcherTimer? rightTimer;

        private int _bpmSliderValue = 120;
        public int BpmSliderValue
        {
            get => _bpmSliderValue;
            set => this.RaiseAndSetIfChanged(ref _bpmSliderValue, value);
        }

        private bool midiTextVisible = false;
        public bool MidiTextVisible
        {
            get => midiTextVisible;
            set => this.RaiseAndSetIfChanged(ref midiTextVisible, value);
        }

        public ReactiveCommand<Unit, Unit> HomeClick { get; }
        public ReactiveCommand<Unit, Unit> LearnClick { get; }
        public ReactiveCommand<Unit, Unit> SwitchClick { get; }
        public ReactiveCommand<Unit, Unit> ToggleLeftClick { get; }
        public ReactiveCommand<Unit, Unit> ToggleRightClick { get; }

        private bool _leftMuted;
        public bool LeftMuted
        {
            get => _leftMuted;
            set => this.RaiseAndSetIfChanged(ref _leftMuted, value);
        }

        public string LeftIconSource =>
            new string(LeftMuted ? "Pattern not playing" : "Pattern playing");

        private bool _rightMuted;
        public bool RightMuted
        {
            get => _rightMuted;
            set => this.RaiseAndSetIfChanged(ref _rightMuted, value);
        }

        public string RightIconSource =>
            new string(RightMuted ? "Pattern not playing" : "Pattern playing");

        public string LeftKeyText => $"Left Key Binding: {leftKey}";

        public string RightKeyText => $"Right Key Binding: {rightKey}";

        public string PressTheLeftKeyText => $"Press the {leftKey} key!";

        public string PressTheRightKeyText => $"Press the {rightKey} key!";

        //main constructor
        public PracticePageViewModel(MainWindowViewModel mainWindowVM, List<int[][]> rhythm)
            : this(mainWindowVM, rhythm, new SoundPlayerAdapter(), new AvaloniaDispatcher(), performAcquire: true)
        { }

        //manual constructor for testing
        public PracticePageViewModel(MainWindowViewModel mainWindowVM, List<int[][]> rhythm, ISoundPlayer soundPlayer, IAppDispatcher dispatcher, bool performAcquire = false)
        {
            this.soundPlayer = soundPlayer ?? throw new ArgumentNullException(nameof(soundPlayer));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            leftPattern = rhythm[0][rhythm[0].Length - 2];
            rightPattern = rhythm[1][rhythm[1].Length - 2];

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            LearnClick = ReactiveCommand.Create(() =>
            {
                StopBeepingCycles();
                soundPlayer.ClearScheduledNotes(this);
                soundPlayer.Release(this);

                mainWindowVM.CurrentPage = new LearnPageViewModel(mainWindowVM, rhythm);
            }, outputScheduler: AvaloniaScheduler.Instance);

            SwitchClick = ReactiveCommand.Create(() => SwapSides(), outputScheduler: AvaloniaScheduler.Instance);
            ToggleLeftClick = ReactiveCommand.Create(() => ToggleLeft(), outputScheduler: AvaloniaScheduler.Instance);
            ToggleRightClick = ReactiveCommand.Create(() => ToggleRight(), outputScheduler: AvaloniaScheduler.Instance);

            if (performAcquire)
            {
                while (!this.soundPlayer.TryAcquire(this))
                {
                    Thread.Sleep(200);
                }
                this.soundPlayer.Initialize(this);
            }

            this.WhenAnyValue(x => x.BpmSliderValue)
                .Skip(1)
                .Subscribe(_ =>
                {
                    StopBeepingCycles();
                    StartBeepingCycles();
                });

            this.WhenAnyValue(x => x.LeftMuted)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(LeftIconSource)));

            this.WhenAnyValue(x => x.RightMuted)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(RightIconSource)));

            if (performAcquire)
            {
                StartBeepingCycles();
            }

            //midi stuff
            ICollection<InputDevice> devices = InputDevice.GetAll();

            //on my setup this is taken as true whenever the audio interface is plugged in, even if it has no midi devices attached... might just have to roll with it 
            if (devices.Count > 0)
            {
                //so obviously if we had more time there would be a pick midi device menu, but for now we just take the first one
                //there are very few setups that allow for multiple midi device inputs anyway so this will be fine for now
                MidiInputDevice = InputDevice.GetByIndex(0);
                MidiInputDevice.EventReceived += new EventHandler<MidiEventReceivedEventArgs>(MidiEventReceived);
                MidiInputDevice.StartEventsListening();
                MidiTextVisible = true;
            }
            else
            {
                MidiTextVisible = false; //in case practice closes and reopens without midi plugged in
                //if we had more time id make this text appear when a device was plugged in but for now we assume all midi will be plugged in on page load
            }

        }

        public SoundPlayer GetSoundPlayer()
        {
            return SoundPlayer.Instance;
        }

        public void SwapSides()
        {
            StopBeepingCycles();
            var temp = leftPattern;
            leftPattern = rightPattern;
            rightPattern = temp;
            StartBeepingCycles();
        }

        public void ToggleLeft()
        {
            if (leftOn)
            {
                StopLeftCycle();
                leftOn = false;
            }
            else if(rightOn)
            {
                StopRightCycle();
                StartLeftCycle();
                StartRightCycle(); //if theres more time at the end have a more elegant way to realign the left beat with the right one
                leftOn = true;
            }
            else
            {
                StartLeftCycle();
                leftOn = true;
            }
                LeftMuted = !LeftMuted;
            this.RaisePropertyChanged(nameof(LeftIconSource));
        }

        public void ToggleRight()
        {
            if (rightOn)
            {
                StopRightCycle();
                rightOn = false;
            }
            else if(leftOn)
            {
                StopLeftCycle();
                StartLeftCycle();
                StartRightCycle();
                rightOn = true;
            }
            else
            {
                StartRightCycle();
                rightOn = true;
            }
            RightMuted = !RightMuted;
            this.RaisePropertyChanged(nameof(RightIconSource));
        }

        public void StartBeepingCycles()
        {
            soundPlayer.TryAcquire(this);
            soundPlayer.Initialize(this);

            startSample = soundPlayer.GetCurrentSample() + soundPlayer.MsToSample(LOOKAHEADSECONDS * 1000);

            StartLeftCycle();
            StartRightCycle();
        }

        public void StartLeftCycle()
        {
            if (leftTimer != null) return;

            double beatMs = (60000.0 / _bpmSliderValue) / 2;
            int leftSixteenthNoteCounter = 0;
            int leftPatternIndex = 0;
            int leftBeatNumber = 0;
            TimeSpan interval = TimeSpan.FromMilliseconds(beatMs * 0.9);

            leftTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e) =>
            {
                if (leftPattern[leftPatternIndex] == leftSixteenthNoteCounter)
                {
                    leftSixteenthNoteCounter = 0;
                    leftPatternIndex++;
                    if (leftPatternIndex == leftPattern.Count()) leftPatternIndex = 0;
                }
                if (leftSixteenthNoteCounter == 0)
                {
                    long beatSample = startSample + soundPlayer.MsToSample(leftBeatNumber * beatMs);
                    soundPlayer.ScheduleNote(this, beatSample, leftPatternNote);
                }
                leftSixteenthNoteCounter++;
                leftBeatNumber++;
            });
        }

        public void StartRightCycle()
        {
            if (rightTimer != null) return;

            double beatMs = (60000.0 / _bpmSliderValue) / 2;
            TimeSpan interval = TimeSpan.FromMilliseconds(beatMs * 0.9);
            int rightSixteenthNoteCounter = 0;
            int rightPatternIndex = 0;
            int rightBeatNumber = 0;

            rightTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e) =>
            {
                if (rightPattern[rightPatternIndex] == rightSixteenthNoteCounter)
                {
                    rightSixteenthNoteCounter = 0;
                    rightPatternIndex++;
                    if (rightPatternIndex == rightPattern.Count()) rightPatternIndex = 0;
                }
                if (rightSixteenthNoteCounter == 0)
                {
                    long beatSample = startSample + soundPlayer.MsToSample(rightBeatNumber * beatMs);
                    soundPlayer.ScheduleNote(this, beatSample, rightPatternNote);
                }
                rightSixteenthNoteCounter++;
                rightBeatNumber++;
            });
        }

        public void StopBeepingCycles()
        {
            soundPlayer.Stop(this);
            StopLeftCycle();
            StopRightCycle();
        }

        public void StopLeftCycle()
        {
            if (leftTimer != null)
            {
                leftTimer.Stop();
                leftTimer = null;
            }
        }

        public void StopRightCycle()
        {
            if (rightTimer != null)
            {
                rightTimer.Stop();
                rightTimer = null;
            }
        }

        public void OnViewClosed()
        {
            StopBeepingCycles();
            soundPlayer.ClearScheduledNotes(this);
            soundPlayer.Release(this);

            //release midi
            if (MidiInputDevice != null)
            {
                MidiInputDevice.EventReceived -= new EventHandler<MidiEventReceivedEventArgs>(MidiEventReceived);
                MidiInputDevice.Dispose();
                MidiInputDevice = null;
            }
        }

        public Key GetLeftKey() => leftKey;
        public Key GetRightKey() => rightKey;

        public double OnKeyDown(Key key)
        {
            if (key == leftKey)
            {
                if(!leftOn) //only make the noise if the app isnt playing this side
                    soundPlayer.PlayLiveNote(LeftPatternNote);

                long currentSample = soundPlayer.GetCurrentSample();
                currentSample -= startSample;
                double beatMs = (60000.0 / _bpmSliderValue) / 2;  //8th notes
                long beatSamples = soundPlayer.MsToSample(beatMs);
                int totalBeats = leftPattern.Sum();

                long samplePositionInBar = (totalBeats == 0) ? 0 : currentSample % (beatSamples * totalBeats); //how many samples through the bar are we 
                long timeDiff = long.MaxValue;

                long[] beatPlacements = new long[leftPattern.Length];
                for (int i = 0; i < leftPattern.Length; i++)
                {
                    beatPlacements[i] = leftPattern[0..i].Sum() * beatSamples;
                }
                beatPlacements = beatPlacements.Concat(new long[] { totalBeats * beatSamples }).ToArray(); //add the start of the next bar as a possible placement to compare against so that you can be early on the first beat and it counts as in time
                for (int i = 0; i < beatPlacements.Length; i++)
                {
                    if (Math.Abs(timeDiff) > Math.Abs(beatPlacements[i] - samplePositionInBar))
                    {
                        timeDiff = beatPlacements[i] - samplePositionInBar;
                    }
                }

                double msLate = soundPlayer.SampleToMs(timeDiff);
                return Math.Round(msLate);
            }
            else if (key == rightKey)
            {
                if (!rightOn) soundPlayer.PlayLiveNote(rightPatternNote);

                long currentSample = soundPlayer.GetCurrentSample();
                currentSample -= startSample;
                double beatMs = (60000.0 / _bpmSliderValue) / 2;
                long beatSamples = soundPlayer.MsToSample(beatMs);
                int totalBeats = rightPattern.Sum();

                long samplePositionInBar = (totalBeats == 0) ? 0 : currentSample % (beatSamples * totalBeats);
                long timeDiff = long.MaxValue;

                long[] beatPlacements = new long[rightPattern.Length];
                for (int i = 0; i < rightPattern.Length; i++)
                {
                    beatPlacements[i] = rightPattern[0..i].Sum() * beatSamples;
                }
                beatPlacements = beatPlacements.Concat(new long[] { totalBeats * beatSamples }).ToArray();
                for (int i = 0; i < beatPlacements.Length; i++)
                {
                    if (Math.Abs(timeDiff) > Math.Abs(beatPlacements[i] - samplePositionInBar))
                        timeDiff = beatPlacements[i] - samplePositionInBar;
                }

                double msLate = soundPlayer.SampleToMs(timeDiff);
                return Math.Round(msLate);
            }
            return 0;
        }

        private void MidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            MidiEvent midiEvent = e.Event;
            if (midiEvent is NoteOnEvent noteOn)
            {
                Note notePlayed = (Note)(int)noteOn.NoteNumber;
                if (notePlayed == leftPatternNote)
                {
                    if(!leftOn)
                        soundPlayer.PlayLiveNote(notePlayed);
                    midiAction?.Invoke(notePlayed);
                }
                else if(notePlayed == rightPatternNote)
                {
                    if (!rightOn)
                        soundPlayer.PlayLiveNote(notePlayed);
                    midiAction?.Invoke(notePlayed);
                }

            }
        }

        public void OnKeyUp(Key key) { }
    }
}
