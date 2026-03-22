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


        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LearnPageViewModel vm)
        {
            string left = "";
            string right = "";
            int[] leftpattern = vm.GetLeftPattern();
            int[] rightpattern = vm.GetRightPattern();
            foreach (int x in leftpattern)
            {
                left += Convert.ToString(x) + " ";
            }
            foreach (int x in rightpattern)
            {
                right += Convert.ToString(x) + " ";
            }
            LeftBottomDiagramDesc.Text = left;
            RightBottomDiagramDesc.Text = right;
            Console.WriteLine("text should be updaetd ");
        }
    }

    private void BpmSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
        {
            bpm = Convert.ToInt32(e.NewValue!);
            
        }
    }

    private void OnDetached(object? sender, EventArgs e)
    {
        if (DataContext is LearnPageViewModel vm)
            vm.OnViewClosed();
    }
}