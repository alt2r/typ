using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using third_year_project.Controls;
using third_year_project.Services;

namespace third_year_project.ViewModels
{
    public class LearnPageViewModel : ViewModelBase
    {
        private Control _currentDiagram;
        public Control CurrentDiagram
        {
            get => _currentDiagram;
            set => this.RaiseAndSetIfChanged(ref _currentDiagram, value);
        }

        readonly ISoundPlayer soundPlayer;
        readonly IAppDispatcher dispatcher;
        double lookAheadMs = 200;
        long startSample = 0;

        int[] leftPattern;
        int[] rightPattern;
        private Avalonia.Threading.DispatcherTimer? leftTimer;
        private Avalonia.Threading.DispatcherTimer? rightTimer;

        private int _bpmSliderValue = 120;
        public int BpmSliderValue
        {
            get => _bpmSliderValue;
            set => this.RaiseAndSetIfChanged(ref _bpmSliderValue, value);
        }
        public ReactiveCommand<Unit, Unit> SwitchCommand { get; }
        public ReactiveCommand<Unit, Unit> HomeClick { get; }
        public ReactiveCommand<Unit, Unit> PracticeClick { get; }

        bool running = false;

        //store handler so we can unsubscribe later
        private Action<double>? _soundPlayedHandler;

        public void setRhythmToDisplay(List<int[][]> rhythm)
        {
            Grid treegrid = (Grid)((Border)tree).Child;
            treegrid.Children.Clear();
            int[][] clockStruc = new int[rhythm.Count][];
            string[] notes = new string[] { "c4", "f4" };
            int counter = 0;
            foreach (int[][] treeStruc in rhythm)
            {
                treegrid.Children.Add(new TreeDiagram(treeStruc, notes[counter])
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    [Grid.ColumnProperty] = counter
                });
                clockStruc[counter] = treeStruc[treeStruc.Length - 2];
                counter++;
            }
            leftPattern = clockStruc[0];
            rightPattern = clockStruc[1];
            Border clockBorder = (Border)clock;
            clockBorder.Child = new ClockDiagram(clockStruc, notes);
        }

        private readonly Control tree = new Border
        {
            Padding = new Thickness(25),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, *"),
                ColumnSpacing = 25,
            }
        };

        private readonly Control clock = new Border
        {
            Padding = new Thickness(25)
        };

        //constructor for actual use
        public LearnPageViewModel(MainWindowViewModel mainWindowVM, List<int[][]> rhythm)
            : this(mainWindowVM, rhythm, new SoundPlayerAdapter(), new AvaloniaDispatcher(), performAcquire: true)
        {
        }

        //constructor for testing 
        public LearnPageViewModel(MainWindowViewModel mainWindowVM, List<int[][]> rhythm, ISoundPlayer soundPlayer, IAppDispatcher dispatcher, bool performAcquire = false)
        {
            this.soundPlayer = soundPlayer ?? throw new ArgumentNullException(nameof(soundPlayer));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            if (performAcquire)
            {
                while (!this.soundPlayer.TryAcquire(this))
                {
                    Thread.Sleep(200); //wait until we acquire 
                }
                this.soundPlayer.Initialize(this);
            }

            setRhythmToDisplay(rhythm);

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            PracticeClick = ReactiveCommand.Create(() =>
            {
                this.soundPlayer.Release(this);
                mainWindowVM.CurrentPage = new PracticePageViewModel(mainWindowVM, rhythm);
            }, outputScheduler: AvaloniaScheduler.Instance);

            _currentDiagram = clock;

            SwitchCommand = ReactiveCommand.Create(execute: () =>
            {
                CurrentDiagram = CurrentDiagram == tree ? clock : tree;

                if (CurrentDiagram is Border border && border.Child is ClockDiagram cd)
                {
                    cd.SetBpm(_bpmSliderValue);
                }
                else if (CurrentDiagram is Border border2 && border2.Child is Grid treegrid)
                {
                    foreach (TreeDiagram td in treegrid.Children)
                    {
                        td.SetBpm(_bpmSliderValue);
                    }
                }
                resetDiagram();

            }, outputScheduler: AvaloniaScheduler.Instance);

            this.WhenAnyValue(x => x.BpmSliderValue)
                .Skip(1) //do not run immediately on subscription. oh my god this caused so many issues.
                .Subscribe(val =>
                {
                    StopBeepingCycles();
                    StartBeepingCycles();

                    if (CurrentDiagram == tree)
                    {
                        Grid treegrid = (Grid)((Border)tree).Child;
                        foreach (TreeDiagram t in treegrid.Children)
                        {
                            t.SetBpm(val);
                        }
                    }
                    else
                    {
                        Border clockBorder = (Border)CurrentDiagram;
                        if (clockBorder.Child is ClockDiagram cd)
                            cd.SetBpm(val);
                    }
                    lookAheadMs = val * 2;
                    running = false;
                });

            _soundPlayedHandler = (frequency) =>
            {
                if (!running)
                {
                    dispatcher.Post(() =>
                    {
                        running = true;
                        if (CurrentDiagram == clock)
                        {
                            Border clockBorder = (Border)CurrentDiagram;
                            var cd = (ClockDiagram)clockBorder.Child;
                            cd?.OnControlSwitched();
                            cd?.StartSpinningCycle();
                        }
                        else
                        {
                            Grid treegrid = (Grid)((Border)tree).Child;
                            foreach (TreeDiagram t in treegrid.Children)
                            {
                                t.ResetFlashCycle();
                            }
                        }
                    });
                }
            };
            soundPlayer.SoundPlayed += _soundPlayedHandler;

            //start doing stuff initially 
            if (performAcquire)
            {
                StartBeepingCycles();
            }
        }

        public int[] GetLeftPattern() => leftPattern;
        public int[] GetRightPattern() => rightPattern;

        private void resetDiagram()
        {
            StopBeepingCycles();
            if (CurrentDiagram is Border border && border.Child is SwitchableControl interactive)
            {
                interactive.OnControlSwitched();
            }
            else if (CurrentDiagram is Border border2 && border2.Child is Grid treegrid)
            {
                foreach (SwitchableControl sc in treegrid.Children)
                {
                    sc.OnControlSwitched();
                }
            }
            running = false;
            StartBeepingCycles();
        }

        public void StartBeepingCycles()
        {
            //don't start twice
            if (leftTimer != null || rightTimer != null)
                return;

            soundPlayer.Initialize(this);
            double beatMs = (60000.0 / _bpmSliderValue) / 2;

            int leftSixteenthNoteCounter = 0;
            int leftPatternIndex = 0;
            int leftBeatNumber = 0;
            TimeSpan interval = TimeSpan.FromMilliseconds(beatMs * 0.9);
            startSample = soundPlayer.GetCurrentSample() + soundPlayer.MsToSample(lookAheadMs);
            leftTimer = new Avalonia.Threading.DispatcherTimer(interval, Avalonia.Threading.DispatcherPriority.Normal, (s, e) =>
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
                    soundPlayer.ScheduleNote(this, beatSample, Note.C4);
                }
                leftSixteenthNoteCounter++;
                leftBeatNumber++;
            });

            int rightSixteenthNoteCounter = 0;
            int rightPatternIndex = 0;
            int rightBeatNumber = 0;

            startSample = soundPlayer.GetCurrentSample() + soundPlayer.MsToSample(lookAheadMs);
            rightTimer = new Avalonia.Threading.DispatcherTimer(interval, Avalonia.Threading.DispatcherPriority.Normal, (s, e) =>
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
                    long beatSample = startSample + soundPlayer.MsToSample(rightBeatNumber * beatMs);
                    soundPlayer.ScheduleNote(this, beatSample, Note.F4);
                }
                rightSixteenthNoteCounter++;
                rightBeatNumber++;
            });
        }

        public void StopBeepingCycles()
        {
            //only stop if we actually started timers (and thus initialized)
            if (leftTimer == null && rightTimer == null)
                return;

            soundPlayer.Stop(this);

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
            if (_soundPlayedHandler != null)
            {
                soundPlayer.SoundPlayed -= _soundPlayedHandler;
                _soundPlayedHandler = null;
            }

            StopBeepingCycles();
            soundPlayer.Release(this);
        }
    }
}
