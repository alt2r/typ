using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace third_year_project.ViewModels
{
    internal class LevelSelectViewModel : ReactiveObject
    {
        public ReactiveCommand<Unit, Unit> HomeClick { get; }

        public ReactiveCommand<string, Unit> LevelClick { get; }

        public LevelSelectViewModel(MainWindowViewModel mainWindowVM, bool learnPage)
        {
            HomeClick = ReactiveCommand.Create(() =>
            {
                //Console.WriteLine("Returning to home page");
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            LevelClick = ReactiveCommand.Create<string>(level =>
            {
                List<int[][]> rhythm = new List<int[][]>();
                Console.WriteLine($"clicked level {level}");
                switch (level)
                {
                    case "1": //bohemian rhapsody
                        //rhythm = new List<int[][]>([[[14], [8, 6], [3, 2, 3, 3, 3], Enumerable.Repeat(1, 14).ToArray()], [[16], [4, 4, 4, 4], Enumerable.Repeat(1, 16).ToArray()]]);
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
                        Console.WriteLine("invalid level selected (somehow)");
                        rhythm = new List<int[][]>([[[16], [4, 4, 4, 4], Enumerable.Repeat(1, 16).ToArray()]]);
                        break;
                }
                //Console.WriteLine("Level selected, going to practice page");
                if(learnPage)
                    mainWindowVM.CurrentPage = new LearnPageViewModel(mainWindowVM, rhythm);
                else
                    mainWindowVM.CurrentPage = new PracticePageViewModel(mainWindowVM, rhythm);
            }, outputScheduler: AvaloniaScheduler.Instance);
        }
    }
}
