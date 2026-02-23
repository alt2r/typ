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

        public LevelSelectViewModel(MainWindowViewModel mainWindowVM)
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
                    case "1":
                        rhythm = new List<int[][]>([[[14], [8, 6], [3, 2, 3, 3, 3], Enumerable.Repeat(1, 14).ToArray()], [[16], [4, 4, 4, 4], Enumerable.Repeat(1, 16).ToArray()]]);
                        break;
                    case "2":
                        rhythm = new List<int[][]>([[[12], [3, 3, 3, 3], Enumerable.Repeat(1, 12).ToArray()], [[12], [4, 4, 4], Enumerable.Repeat(1, 12).ToArray()]]);
                        break;
                    case "3":
                        // Set up level 3 parameters if needed
                        break;
                    default:
                        Console.WriteLine("invalid level selected (somehow)");
                        rhythm = new List<int[][]>([[[16], [4, 4, 4, 4], Enumerable.Repeat(1, 16).ToArray()]]);
                        break;
                }
                //Console.WriteLine("Level selected, going to practice page");
                mainWindowVM.CurrentPage = new LearnPageViewModel(mainWindowVM, rhythm);
            }, outputScheduler: AvaloniaScheduler.Instance);
        }
    }
}
