using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using third_year_project.ViewModels;

namespace third_year_project.Views;

public partial class LearnPage : UserControl
{
    int bpm = 120;
    public LearnPage()
    {
        InitializeComponent();
        bpmSlider.PropertyChanged += BpmSlider_PropertyChanged;
        DetachedFromVisualTree += OnDetached;
    }

    private void BpmSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            bpm = Convert.ToInt32(e.NewValue!);
            //Console.WriteLine($"new bpm: {e.NewValue}");
        }
    }

    private void OnDetached(object? sender, EventArgs e)
    {
        if (DataContext is LearnPageViewModel vm)
            vm.OnViewClosed();
    }
}