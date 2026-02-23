using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

namespace third_year_project.ViewModels
{
    public partial class HomePageViewModel : ReactiveObject
    {
        public ReactiveCommand<Unit, Unit> LearnClick { get; } //goes to level select page before reaching levels
        public ReactiveCommand<Unit, Unit> SandboxClick { get; }
        public ReactiveCommand<Unit, Unit> PracticeClick { get; }
        public ReactiveCommand<Unit, Unit> InfoClick { get; }
        public ReactiveCommand<Unit, Unit> SettingsClick { get; }

        public HomePageViewModel(MainWindowViewModel mainWindowVM)
        {
            // Ensure notifications and command results are scheduled on the Avalonia UI scheduler
            LearnClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new LevelSelectViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            SandboxClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new SandboxPageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            PracticeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new PracticePageViewModel(mainWindowVM, [4, 4, 4], [3, 3, 3, 3]);
            }, outputScheduler: AvaloniaScheduler.Instance);

            InfoClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new InfoPageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            SettingsClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new SettingsPageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);
        }
    }
}
