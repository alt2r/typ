using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using third_year_project.Services;
using third_year_project.ViewModels;

namespace third_year_project.Views;

public partial class PracticePage : UserControl
{
    int bpm = 120; //a global setter for this would be good but were going to put it on a slider later so might as well sort that out when we get there
    int inTimeRange = 35; //how many ms either side of the beat is considered in time

    DispatcherTimer? _beatTimer;

    public PracticePage()
    {
        InitializeComponent();

        DetachedFromVisualTree += OnDetached;
        bpmSlider.PropertyChanged += BpmSlider_PropertyChanged;
        this.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        this.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
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
        if (DataContext is PracticePageViewModel vm)
            vm.OnViewClosed();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();


        //IF THERE IS TIME MAKE THIS LESS BAD
        //this polling solution isnt great but was the obvious way to avoid doing heavy lifting on the audio thread
        //which would have been bad bc it delays the sound from palying exactly on the samples 
        _beatTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(5) // fast enough for beats
        };

        _beatTimer.Tick += BeatTimer_Tick;
        _beatTimer.Start();
    }

    private void BeatTimer_Tick(object? sender, EventArgs e)
    {
        if (DataContext is not PracticePageViewModel vm)
            return;

        while (SoundPlayer.instance.GetSoundMixerNotificationQueue().TryDequeue(out var frequency))
        {
            Console.WriteLine(frequency);
            //sending about 90ms late????
            AddLeftBlock(Brushes.Green);
        }
    }

    //hacky i know but the blocks were coming early 

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is PracticePageViewModel vm)
        {
            if (e.Key == vm.GetLeftKey())
            {
                AddLeftBlock(Brushes.Red);
                double distanceFromBeat = vm.OnKeyDown(e.Key);

                double timeBetweenBeats = 60000 / (float)bpm; //measuered in ms
                if (distanceFromBeat < inTimeRange && distanceFromBeat > -inTimeRange)
                {
                    LeftTopText.Text = $"Good";
                    LeftTopText.Foreground = Avalonia.Media.Brushes.Green;
                    LeftBottomText.Text = $"";
                }
                else if (distanceFromBeat > inTimeRange)
                {
                    LeftTopText.Text = $"Early";
                    LeftTopText.Foreground = Avalonia.Media.Brushes.Red;
                    LeftBottomText.Text = $"{distanceFromBeat}ms";
                }
                else
                {
                    LeftTopText.Text = $"Late";
                    LeftTopText.Foreground = Avalonia.Media.Brushes.Red;
                    LeftBottomText.Text = $"{Math.Abs(distanceFromBeat)}ms";
                }
            }
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is PracticePageViewModel vm)
        {
            vm.OnKeyUp(e.Key);
        }
    }


    private async Task AddLeftBlock(IImmutableSolidColorBrush colour)
    {
        var rect = new Border
        {
            Background = colour,
            Width = 10,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var transform = new TranslateTransform();
        rect.RenderTransform = transform;

        leftDisplay.Children.Add(rect);

        await Move(rect, transform, leftDisplay.Width);

        leftDisplay.Children.Remove(rect);
    }

    private async Task Move(Control rect, TranslateTransform transform, double targetX)
    {
        const double speed = 5;
        const int delayMs = 16;

        while (transform.X < targetX)
        {
            transform.X += speed;
            await Task.Delay(delayMs);
        }
    }
}