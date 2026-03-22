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

        SoundPlayer soundPlayer = SoundPlayer.Instance;
        double lookAheadMs = 200; //basically we want to schedule sounds a bit in advance to avoid latency issues
        //also this wants to change based on the bpm apparently. 120bpm quite likes around 200 so gonna call it an even * 2
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

        bool running = false;

        //private List<int[][]> rhythmToDisplay = new List<int[][]>();

        public void setRhythmToDisplay(List<int[][]> rhythm)
        {
            //rhythmToDisplay = rhythm;
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
            while(!soundPlayer.TryAcquire(this)) //this surely wont cause an infinite loop right? :clueless:
            { Task.Delay(500); }

            soundPlayer.Initialize(this);
            setRhythmToDisplay(rhythm);

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            PracticeClick = ReactiveCommand.Create(() =>
            {
                soundPlayer.Release(this);
                mainWindowVM.CurrentPage = new PracticePageViewModel(mainWindowVM, rhythm);
            }, outputScheduler: AvaloniaScheduler.Instance);

            _currentDiagram = clock; // set initial control

            SwitchCommand = ReactiveCommand.Create(execute: () =>
            {
                CurrentDiagram = CurrentDiagram == tree ? clock : tree; //switcharoo

                //carry over the bpm
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
            .Subscribe(val =>
            {
                // This runs whenever the slider moves and on load apparently
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
                    ClockDiagram cd = (ClockDiagram)clockBorder.Child;
                    cd.SetBpm(val);
                }
                lookAheadMs = val * 2;
                running = false;
            });

            soundPlayer.soundPlayed += (frequency) => //frequency isnt used here tho
            {
                if (!running)
                {
                    Dispatcher.UIThread.Post(() =>
                    {

                        running = true;
                        if (CurrentDiagram == clock)
                        {
                            Border clockBorder = (Border)CurrentDiagram;
                            var cd = (ClockDiagram)clockBorder.Child;
                            cd?.OnControlSwitched(); //resets hand position
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

        }

        public int[] GetLeftPattern()
        {
            return leftPattern;
        }

        public int[] GetRightPattern()
        {
            return rightPattern;
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
            running = false;
            StartBeepingCycles();
        }

        public void StartBeepingCycles()
        {
            soundPlayer.Initialize(this);
            double beatMs = (60000.0 / _bpmSliderValue) / 2; //it should be over 4 but for some reason that was making it twice as fast dont even ask why
            //double rightBeatMs = ((60000.0 * rightPattern.Sum() / leftPattern.Sum()) / _bpmSliderValue) / 2;

            int leftSixteenthNoteCounter = 0; //counts sixteenth notes in between the beats
            int leftPatternIndex = 0; //what note of the looping pattern we are currently on
            int leftBeatNumber = 0;
            TimeSpan interval = TimeSpan.FromMilliseconds(beatMs * 0.9); //may need a -100 here as a buffer?
                                                                         //yes the longer it runs the further ahead of real time the schedule queue will get but i dont think its that much of a bad thing atp
            startSample = soundPlayer.GetCurrentSample() + soundPlayer.MsToSample(lookAheadMs);
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
                    long beatSample = startSample + soundPlayer.MsToSample(leftBeatNumber * beatMs); //-100 because keyboard input delay? idk this might be wrong still
                    soundPlayer.ScheduleNote(this, beatSample, Note.C4);
                }
                leftSixteenthNoteCounter++;
                leftBeatNumber++;
            });

            int rightSixteenthNoteCounter = 0; //counts sixteenth notes in between the beats
            int rightPatternIndex = 0; //what note of the looping pattern we are currently on
            int rightBeatNumber = 0;

            startSample = soundPlayer.GetCurrentSample() + soundPlayer.MsToSample(lookAheadMs);
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
                    soundPlayer.ScheduleNote(this, beatSample, Note.F4);
                }
                rightSixteenthNoteCounter++;
                rightBeatNumber++;
            });

        }


        public void StopBeepingCycles()
        {
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
            StopBeepingCycles();
            soundPlayer.Release(this);
        }
    }
}
