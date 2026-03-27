using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace third_year_project.ViewModels
{
    public class LevelSelectViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> HomeClick { get; }

        public ReactiveCommand<string, Unit> LevelClick { get; }

        public LevelSelectViewModel(MainWindowViewModel mainWindowVM, bool learnPage, IScheduler? outputScheduler = null)
        {
            var scheduler = outputScheduler ?? AvaloniaScheduler.Instance;

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: scheduler);

            LevelClick = ReactiveCommand.Create<string>(level =>
            {
                var rhythm = BuildRhythm(level);
                if (learnPage)
                    mainWindowVM.CurrentPage = new LearnPageViewModel(mainWindowVM, rhythm);
                else
                    mainWindowVM.CurrentPage = new PracticePageViewModel(mainWindowVM, rhythm);
            }, outputScheduler: scheduler);
        }

        //this bit is its own function so that we can test it :)
        //(so that the implementation isnt tied to the UI)
        public static List<int[][]> BuildRhythm(string level)
        {
            List<int[][]> rhythm = new List<int[][]>();
            switch (level)
            {
                case "1": //bohemian rhapsody
                    rhythm = new List<int[][]>([[[6], [3, 3], Enumerable.Repeat(1, 6).ToArray()], [[6], [2, 2, 2], Enumerable.Repeat(1, 6).ToArray()]]);
                    break;
                case "2": //critical acclaim 
                    rhythm = new List<int[][]>([[[12], [3, 3, 3, 3], Enumerable.Repeat(1, 12).ToArray()], [[12], [4, 4, 4], Enumerable.Repeat(1, 12).ToArray()]]);
                    break;
                case "3": //saving hope
                    rhythm = new List<int[][]>([[[20], [4, 4, 4, 4, 4], Enumerable.Repeat(1, 20).ToArray()], [[20], [5, 5, 5, 5], [2, 3, 2, 3, 2, 3, 2, 3],Enumerable.Repeat(1, 20).ToArray()]]);
                    break;
                case "4":
                    rhythm = new List<int[][]>([[[14], [8, 6], [3, 2, 3, 3, 3], Enumerable.Repeat(1, 14).ToArray()], [[16], [4, 4, 4, 4], Enumerable.Repeat(1, 16).ToArray()]]);
                    break;
                case "5": //monomyth
                    rhythm = new List<int[][]>([[[36], [12, 12, 12], [5, 7, 7, 5, 5, 7], [2, 3, 2, 2, 3, 2, 2, 3, 2, 3, 2, 3, 2, 2, 3], Enumerable.Repeat(1, 36).ToArray()], [[32],[16, 16],[4, 4, 4, 4, 4, 4, 4, 4], Enumerable.Repeat(1, 32).ToArray()]]);
                    break;
                default:
                    //default to something. although this function only gets called with set params so we will never get here
                    rhythm = new List<int[][]>([[[16], [4, 4, 4, 4], Enumerable.Repeat(1, 16).ToArray()]]);
                    break;
            }
            return rhythm;
        }
    }
}
