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
    internal class PracticePageViewModel : ReactiveObject
    {
        SoundPlayer soundPlayer = SoundPlayer.instance;
        Key leftKey = Key.A;
        Key rightKey = Key.L;
        const double LOOKAHEADSECONDS = 0.1; //basically we want to schedule sounds a bit in advance to avoid latency issues
        long startSample = 0;

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

        //main constructor
        public PracticePageViewModel(MainWindowViewModel mainWindowVM, int[] _leftPattern, int[] _rightpattern)
        {
            leftPattern = _leftPattern;
            rightPattern = _rightpattern;

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            constructorSetUp(mainWindowVM);

        }

        //constructor from learn page
        public PracticePageViewModel(MainWindowViewModel mainWindowVM, List<int[][]> rhythm)
        {
            leftPattern = rhythm[0][rhythm[0].Length - 2];
            rightPattern = rhythm[1][rhythm[1].Length - 2];

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            constructorSetUp(mainWindowVM);


        }

        public void constructorSetUp(MainWindowViewModel mainWindowVM)
        {
            //StopBeepingCycles();
            //StartBeepingCycles();

            this.WhenAnyValue(x => x.BpmSliderValue)
            .Subscribe(val =>
            {
                // This runs whenever the slider moves
                StopBeepingCycles();
                StartBeepingCycles();

            });
        }

        public async void delayedSoundPlayerInit()
        {
            await Task.Delay(200); //was happening too soon apparently
            StartBeepingCycles();
        }


        public void StartBeepingCycles()
        {
            Console.WriteLine("starting in practice");
            soundPlayer.Initialize();
            foreach (var x in leftPattern)
            {
                Console.Write($"{x}, ");
            }
            Console.WriteLine();
            foreach (var x in rightPattern)
            {
                Console.Write($"{x}, ");
            }
            Console.WriteLine();

            //aight here we go
            int totalBeats = leftPattern.Sum();
            if(totalBeats != rightPattern.Sum())
            {
                Console.WriteLine("beat numbers dont match :( ");
                //return;
            }
            //so we are assuming that 1 is a 16th note at the bpm specified by bpm. meaning 4 is a a quarter. bpm is in minutes so 60000ms / bpm = ms per quarter note
            //this means we want to have beatMS = 60000 / bpm / 4 for a 16th note and then loop through each possible 16th note and add a note if its in the pattern
            //brain please start working again

            // bpm value doubled again here, maybe i am confusing 16th notes with 8th notes

            double beatMs = (60000.0 / _bpmSliderValue) / 2;
            startSample = soundPlayer.getCurrentSample() + soundPlayer.msToSample(LOOKAHEADSECONDS * 1000);

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
                    long beatSample = startSample + soundPlayer.msToSample(leftBeatNumber * beatMs); //-100 because keyboard input delay? idk this might be wrong still
                    soundPlayer.scheduleNote(beatSample, Note.C4);
                    Console.WriteLine($"scheduling at {beatSample} and the current sample is {soundPlayer.getCurrentSample()}");
                }
                leftSixteenthNoteCounter++;
                leftBeatNumber++;
            });

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
                    long beatSample = startSample + soundPlayer.msToSample(rightBeatNumber * beatMs); //-100 because keyboard input delay? idk this might be wrong still
                   
                    soundPlayer.scheduleNote(beatSample, Note.F4);
                }
                rightSixteenthNoteCounter++;
                rightBeatNumber++;
            });

        }


        public void StopBeepingCycles()
        {
            Console.WriteLine("stopping in practice");
            soundPlayer.Stop();
            if (leftTimer != null)
            {
                leftTimer.Stop();
                leftTimer = null;
            }
            if (rightTimer != null)
            {
                rightTimer.Stop();
                rightTimer = null;
            }
        }

        public void OnViewClosed()
        {
            StopBeepingCycles();
        }

        public Key GetLeftKey()
        {
            return leftKey;
        }

        public double OnKeyDown(Key key)
        {
            if (key == leftKey)
            {
                long currentSample = soundPlayer.getCurrentSample();

                currentSample -= startSample;
                double beatMs = (60000.0 / _bpmSliderValue) / 4;  //16th notes
                long beatSamples = soundPlayer.msToSample(beatMs);
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

                double msLate = soundPlayer.sampleToMs(timeDiff);
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
