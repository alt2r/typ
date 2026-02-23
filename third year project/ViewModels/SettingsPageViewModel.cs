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
    internal class SettingsPageViewModel : ReactiveObject
    {
        public ReactiveCommand<Unit, Unit> HomeClick { get; }

        public SettingsPageViewModel(MainWindowViewModel mainWindowVM)
        {
            HomeClick = ReactiveCommand.Create(() =>
            {
                //Console.WriteLine("Returning to home page");
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);
        }
    }
}
