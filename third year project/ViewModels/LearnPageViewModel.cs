using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using third_year_project.Controls;
using third_year_project.Services;
namespace third_year_project.ViewModels
{
    public class LearnPageViewModel : ReactiveObject
    {

        private Control _currentDiagram;
        public Control CurrentDiagram
        {
            get => _currentDiagram;
            set => this.RaiseAndSetIfChanged(ref _currentDiagram, value);
        }

        SoundPlayer soundPlayer = SoundPlayer.instance;
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
        public ReactiveCommand<Unit, Unit> SwitchCommand { get; }
        public ReactiveCommand<Unit, Unit> HomeClick { get; }
        public ReactiveCommand<Unit, Unit> PracticeClick { get;  }

        private List<int[][]> rhythmToDisplay = new List<int[][]>();

        public void setRhythmToDisplay(List<int[][]> rhythm)
        {
            rhythmToDisplay = rhythm;
            Grid treegrid = (Grid)((Border)tree).Child;
            treegrid.Children.Clear();
            int[][] clockStruc = new int[rhythm.Count][];
            string[] notes = new string[] { "c4", "f4" }; //controllers for this coming 
            int counter = 0;
            foreach (int[][] treeStruc in rhythm)
            {
                treegrid.Children.Add(new TreeDiagram(treeStruc, notes[counter])
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    [Grid.ColumnProperty] = counter
                });
                clockStruc[counter] = treeStruc[treeStruc.Length - 2];  //the lowest division of the beat that isnt the row of all 1's
                counter++;
            }
            leftPattern = clockStruc[0];
            rightPattern = clockStruc[1];
            //this locks us into having exactly 2 rhythms going which wasnt the original design but the direction of the
            //project means we will probs always be having 2 things at a time now anyway
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

        public LearnPageViewModel(MainWindowViewModel mainWindowVM, List<int[][]> rhythm)
        {
            soundPlayer.Initialize();
            setRhythmToDisplay(rhythm);

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            PracticeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new PracticePageViewModel(mainWindowVM, rhythm);
            }, outputScheduler: AvaloniaScheduler.Instance);

            _currentDiagram = clock; // set initial control

            SwitchCommand = ReactiveCommand.Create(execute: () =>
            {
                CurrentDiagram = CurrentDiagram == tree ? clock : tree; //switcharoo
                resetDiagram();
            }, outputScheduler: AvaloniaScheduler.Instance);


            this.WhenAnyValue(x => x.BpmSliderValue)
            .Subscribe(val =>
            {
                // This runs whenever the slider moves and on load apparently
                StopBeepingCycles();
                soundPlayer.Initialize();
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
                    ClockDiagram cd = (ClockDiagram)clockBorder.Child;
                    cd.SetBpm(val);
                }
            });

        }

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
            soundPlayer.Initialize();
            StartBeepingCycles();
        }

        public void StartBeepingCycles()
        {
            //aight here we go

            double beatMs = (60000.0 / _bpmSliderValue) / 2; //it should be over 4 but for some reason that was making it twice as fast dont even ask why
            //double rightBeatMs = ((60000.0 * rightPattern.Sum() / leftPattern.Sum()) / _bpmSliderValue) / 2;
            startSample = soundPlayer.getCurrentSample() + soundPlayer.msToSample(LOOKAHEADSECONDS * 1000);

            int leftSixteenthNoteCounter = 0; //counts sixteenth notes in between the beats
            int leftPatternIndex = 0; //what note of the looping pattern we are currently on
            int leftBeatNumber = 0;
            TimeSpan interval = TimeSpan.FromMilliseconds(beatMs * 0.9); //may need a -100 here as a buffer?
                                                                         //yes the longer it runs the further ahead of real time the schedule queue will get but i dont think its that much of a bad thing atp

            leftTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e) =>
            {
                Console.WriteLine("running in timer");
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
            Console.WriteLine("stopping in learn");
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
    }
}
