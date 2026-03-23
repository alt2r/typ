using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CSCore.DirectSound;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using third_year_project.Services;
using third_year_project.Views;

namespace third_year_project.ViewModels
{
    public class PracticePageViewModel : ViewModelBase
    {
        SoundPlayer soundPlayer = SoundPlayer.Instance;
        Key leftKey = Key.A;
        Key rightKey = Key.L;
        const double LOOKAHEADSECONDS = 0.1; //basically we want to schedule sounds a bit in advance to avoid latency issues
        long startSample = 0;
        bool leftOn = true, rightOn = true;

        Note leftPatternNote = Note.C4;
        Note rightPatternNote = Note.F4;
        public Note LeftPatternNote => leftPatternNote; //shorthand getter methods, something i discovered way too late into development lol
        public Note RightPatternNote => rightPatternNote;


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

        public ReactiveCommand<Unit, Unit> HomeClick { get; }
        public ReactiveCommand<Unit, Unit> LearnClick { get; }
        public ReactiveCommand<Unit, Unit> SwitchClick { get; }
        public ReactiveCommand<Unit, Unit> ToggleLeftClick { get; }
        public ReactiveCommand<Unit, Unit> ToggleRightClick { get; }

        public PracticePageViewModel(MainWindowViewModel mainWindowVM, List<int[][]> rhythm)
        {
            leftPattern = rhythm[0][rhythm[0].Length - 2];
            rightPattern = rhythm[1][rhythm[1].Length - 2];

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            LearnClick = ReactiveCommand.Create(() =>
            {
                soundPlayer.Release(this);
                mainWindowVM.CurrentPage = new LearnPageViewModel(mainWindowVM, rhythm);
            }, outputScheduler: AvaloniaScheduler.Instance);

            SwitchClick = ReactiveCommand.Create(() =>
            {
                SwapSides();
            }, outputScheduler: AvaloniaScheduler.Instance);

            ToggleLeftClick = ReactiveCommand.Create(() =>
            {
                ToggleLeft();
            }, outputScheduler: AvaloniaScheduler.Instance);

            ToggleRightClick = ReactiveCommand.Create(() =>
            {
                ToggleRight();
            }, outputScheduler: AvaloniaScheduler.Instance);



            while (!soundPlayer.TryAcquire(this))
            { Task.Delay(500); }
            soundPlayer.Initialize(this);

            this.WhenAnyValue(x => x.BpmSliderValue)
            .Subscribe(val =>
            {
                // This runs whenever the slider moves
                StopBeepingCycles();
                StartBeepingCycles();

            });

            this.WhenAnyValue(x => x.LeftMuted)
                .Subscribe(_ => { 
                    this.RaisePropertyChanged(nameof(LeftIconSource));
                });

            this.WhenAnyValue(x => x.RightMuted)
                .Subscribe(_ => {
                    this.RaisePropertyChanged(nameof(RightIconSource));
                });
        }


        private bool _leftMuted;
        public bool LeftMuted
        {
            get => _leftMuted;
            set => this.RaiseAndSetIfChanged(ref _leftMuted, value);
        }

        public string LeftIconSource => //yes this was meant to be a speaker icon. yes i spent 2 hours trying to get it to work
            new string(LeftMuted        //yes i gave up, deadlines too soon and there are core features that are currently broken
                ? "Pattern not playing" 
                : "Pattern playing");

        private bool _rightMuted;
        public bool RightMuted
        {
            get => _rightMuted;
            set => this.RaiseAndSetIfChanged(ref _rightMuted, value);
        }

        public string RightIconSource =>
            new string(RightMuted
                ? "Pattern not playing"
                : "Pattern playing");
        public void SwapSides()
        {
            //Console.WriteLine("in swapsides");
            StopBeepingCycles() ;
            var temp = leftPattern;
            leftPattern = rightPattern;
            rightPattern = temp;
            StartBeepingCycles();
        }

        public void ToggleLeft()
        {
            //Console.WriteLine("in toggleleft");
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

        public SoundPlayer GetSoundPlayer() //view needs sound player reference lol
        {
            return soundPlayer;
        }


        public void StartBeepingCycles()
        {
            soundPlayer.TryAcquire(this);
            soundPlayer.Initialize(this);
            //so we are assuming that 1 is a 16th note at the bpm specified by bpm. meaning 4 is a a quarter. bpm is in minutes so 60000ms / bpm = ms per quarter note
            //this means we want to have beatMS = 60000 / bpm / 4 for a 16th note and then loop through each possible 16th note and add a note if its in the pattern
            //brain please start working again

            // bpm value doubled again here, maybe i am confusing 16th notes with 8th notes

            
            startSample = soundPlayer.GetCurrentSample() + soundPlayer.MsToSample(LOOKAHEADSECONDS * 1000);

            StartLeftCycle();
            StartRightCycle();

        }

        public void StartLeftCycle()
        {

            double beatMs = (60000.0 / _bpmSliderValue) / 2;
            int leftSixteenthNoteCounter = 0; //counts sixteenth notes in between the beats
            int leftPatternIndex = 0; //what note of the looping pattern we are currently on
            int leftBeatNumber = 0;
            TimeSpan interval = TimeSpan.FromMilliseconds(beatMs * 0.9); //may need a -100 here as a buffer?
                                                                         //yes the longer it runs the further ahead of real time the schedule queue will get but i dont think its that much of a bad thing atp

            leftTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e) =>
            {
                if (leftPattern[leftPatternIndex] == leftSixteenthNoteCounter)
                {
                    leftSixteenthNoteCounter = 0;
                    leftPatternIndex++;
                    if (leftPatternIndex == leftPattern.Count())
                    {
                        leftPatternIndex = 0;
                    }

                }
                if (leftSixteenthNoteCounter == 0)
                {
                    long beatSample = startSample + soundPlayer.MsToSample(leftBeatNumber * beatMs);
                    soundPlayer.ScheduleNote(this, beatSample, leftPatternNote);
                    //Console.WriteLine($"scheduling at {beatSample} and the current sample is {soundPlayer.getCurrentSample()}");
                }
                leftSixteenthNoteCounter++;
                leftBeatNumber++;
            });
        }

        public void StartRightCycle()
        {

            double beatMs = (60000.0 / _bpmSliderValue) / 2;
            TimeSpan interval = TimeSpan.FromMilliseconds(beatMs * 0.9);
            int rightSixteenthNoteCounter = 0; //counts sixteenth notes in between the beats
            int rightPatternIndex = 0; //what note of the looping pattern we are currently on
            int rightBeatNumber = 0;

            rightTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e) =>
            {
                if (rightPattern[rightPatternIndex] == rightSixteenthNoteCounter)
                {
                    rightSixteenthNoteCounter = 0;
                    rightPatternIndex++;
                    if (rightPatternIndex == rightPattern.Count())
                    {
                        rightPatternIndex = 0;
                    }

                }
                if (rightSixteenthNoteCounter == 0)
                {
                    long beatSample = startSample + soundPlayer.MsToSample(rightBeatNumber * beatMs); //-100 because keyboard input delay? idk this might be wrong still

                    soundPlayer.ScheduleNote(this, beatSample, rightPatternNote);
                }
                rightSixteenthNoteCounter++;
                rightBeatNumber++;
            });
        }


        public void StopBeepingCycles()
        {
            //Console.WriteLine("stopping in practice");
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
            soundPlayer.Release(this);
        }

        public Key GetLeftKey()
        {
            return leftKey;
        }

        public Key GetRightKey()
        {
            return rightKey;
        }

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

                long samplePositionInBar = currentSample % (beatSamples * totalBeats);
                long timeDiff = long.MaxValue;

                int counter = 0;
                int patternIndex = 0;
                long[] beatPlacements = new long[leftPattern.Length];
                for (int i = 0; i < leftPattern.Length; i++)
                {
                    beatPlacements[i] = leftPattern[0..i].Sum() * beatSamples;
                }
                for (int i = 0; i < beatPlacements.Length; i++)
                {
                    if (Math.Abs(timeDiff) > Math.Abs(beatPlacements[i] - samplePositionInBar))
                    {
                        timeDiff = beatPlacements[i] - samplePositionInBar;
                    }
                }

                double msLate = soundPlayer.SampleToMs(timeDiff);
                //msLate -= 100; //correcting for delays
                return Math.Round(msLate);
            }

            else if (key == rightKey)
            {
                if(!rightOn)
                    soundPlayer.PlayLiveNote(rightPatternNote);

                long currentSample = soundPlayer.GetCurrentSample();
                currentSample -= startSample;
                double beatMs = (60000.0 / _bpmSliderValue) / 2; 
                long beatSamples = soundPlayer.MsToSample(beatMs);
                int totalBeats = rightPattern.Sum();

                long samplePositionInBar = currentSample % (beatSamples * totalBeats);
                long timeDiff = long.MaxValue;

                int counter = 0;
                int patternIndex = 0;
                long[] beatPlacements = new long[rightPattern.Length];
                for (int i = 0; i < rightPattern.Length; i++)
                {
                    beatPlacements[i] = rightPattern[0..i].Sum() * beatSamples;
                }
                for (int i = 0; i < beatPlacements.Length; i++)
                {
                    if (Math.Abs(timeDiff) > Math.Abs(beatPlacements[i] - samplePositionInBar))
                    {
                        timeDiff = beatPlacements[i] - samplePositionInBar;
                    }
                }

                double msLate = soundPlayer.SampleToMs(timeDiff);
                //msLate -= 100; //correcting for delays
                return Math.Round(msLate);
            }
            return 0;
        }
        public void OnKeyUp(Key key)
        {

        }



    }
}
