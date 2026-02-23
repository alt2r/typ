using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Reactive;
using third_year_project.Controls;
using third_year_project.Services;

namespace third_year_project.ViewModels
{
    public partial class MainWindowViewModel : ReactiveObject
    {

        private ReactiveObject _currentPage;
        public ReactiveObject CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }


        public string Greeting { get; } = "Welcome to Avalonia!";

        public MainWindowViewModel()
        {
            CurrentPage = new HomePageViewModel(this);
            SoundPlayer soundPlayer = new SoundPlayer(); //really great to set this up somewhere on load

        }

    }
}
