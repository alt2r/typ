using Avalonia.Input;
using Melanchall.DryWetMidi.Multimedia;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using third_year_project.Services;

namespace third_year_project.ViewModels
{
    internal class SettingsPageViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> HomeClick { get; }
        private SoundPlayer soundPlayer;

        private int volumeSliderValue; //default value
        public int VolumeSliderValue
        {
            get => volumeSliderValue;
            set => this.RaiseAndSetIfChanged(ref volumeSliderValue, value);
        }

        private string leftKeyValue = SettingsService.Instance.LeftInputKey.ToString();
        public string LeftKeyValue
        {
            get => leftKeyValue;
            set => this.RaiseAndSetIfChanged(ref leftKeyValue, value);
        }

        private string rightKeyValue = SettingsService.Instance.RightInputKey.ToString();
        public string RightKeyValue
        {
            get => rightKeyValue;
            set => this.RaiseAndSetIfChanged(ref rightKeyValue, value);
        }

        public SettingsPageViewModel(MainWindowViewModel mainWindowVM)
        {
            soundPlayer = SoundPlayer.Instance;
            soundPlayer.TryAcquire(this);
            volumeSliderValue = (int)(soundPlayer.GetAmplitude(this) * 100); //initialize slider to current volume
            HomeClick = ReactiveCommand.Create(() =>
            {
                soundPlayer.SetAmplitude(this, VolumeSliderValue / 100.0); //update volume immediately when leaving settings
                soundPlayer.Release(this);
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
                SettingsService.Instance.LeftInputKey = Enum.Parse<Key>(LeftKeyValue.ToUpper());
                SettingsService.Instance.RightInputKey = Enum.Parse<Key>(RightKeyValue.ToUpper());
            }, outputScheduler: AvaloniaScheduler.Instance);
        }
    }
}
