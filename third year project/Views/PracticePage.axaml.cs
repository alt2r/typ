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

        if (DataContext is PracticePageViewModel vm)
        {
            vm.GetSoundPlayer().soundPlayed += (frequency) =>
            {
                Dispatcher.UIThread.Post(() => //on the audio thread until now, for some reason
                {
                    if (vm.LeftPatternNote == SoundPlayer.FrequencyToNote(frequency))
                    {
                        AddBlock(Brushes.Green, true);
                    }
                    else if (vm.RightPatternNote == SoundPlayer.FrequencyToNote(frequency))
                    {
                        AddBlock(Brushes.Green, false);
                    }
                });
            };

            vm.midiAction += (note) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (vm.LeftPatternNote == note)
                    {
                        AddBlock(Brushes.Red, true);
                        double distanceFromBeat = vm.OnKeyDown(vm.GetLeftKey()); 
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
                    else if (vm.RightPatternNote == note)
                    {
                        AddBlock(Brushes.Red, false);
                        double distanceFromBeat = vm.OnKeyDown(vm.GetRightKey());

                        double timeBetweenBeats = 60000 / (float)bpm; //measuered in ms
                        if (distanceFromBeat < inTimeRange && distanceFromBeat > -inTimeRange)
                        {
                            RightTopText.Text = $"Good";
                            RightTopText.Foreground = Avalonia.Media.Brushes.Green;
                            RightBottomText.Text = $"";
                        }
                        else if (distanceFromBeat > inTimeRange)
                        {
                            RightTopText.Text = $"Early";
                            RightTopText.Foreground = Avalonia.Media.Brushes.Red;
                            RightBottomText.Text = $"{distanceFromBeat}ms";
                        }
                        else
                        {
                            RightTopText.Text = $"Late";
                            RightTopText.Foreground = Avalonia.Media.Brushes.Red;
                            RightBottomText.Text = $"{Math.Abs(distanceFromBeat)}ms";
                        }
                    }
                });
            };
        }

    }


    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is PracticePageViewModel vm)
        {
            if (e.Key == vm.GetLeftKey())
            {
                AddBlock(Brushes.Red, true);
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

            if (e.Key == vm.GetRightKey())
            {
                AddBlock(Brushes.Red, false);
                double distanceFromBeat = vm.OnKeyDown(e.Key);

                double timeBetweenBeats = 60000 / (float)bpm; //measuered in ms
                if (distanceFromBeat < inTimeRange && distanceFromBeat > -inTimeRange)
                {
                    RightTopText.Text = $"Good";
                    RightTopText.Foreground = Avalonia.Media.Brushes.Green;
                    RightBottomText.Text = $"";
                }
                else if (distanceFromBeat > inTimeRange)
                {
                    RightTopText.Text = $"Early";
                    RightTopText.Foreground = Avalonia.Media.Brushes.Red;
                    RightBottomText.Text = $"{distanceFromBeat}ms";
                }
                else
                {
                    RightTopText.Text = $"Late";
                    RightTopText.Foreground = Avalonia.Media.Brushes.Red;
                    RightBottomText.Text = $"{Math.Abs(distanceFromBeat)}ms";
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

    private async Task AddBlock(IImmutableSolidColorBrush colour, bool left)
    {
        int height = 110;
        int width = 10;
        int zIndex = 1;
        if (colour == Brushes.Green)
        {
            height = 150;
            width = 18;
            zIndex = 0; //put the green ones behiond the red
        }
        var rect = new Border
        {
            Background = colour,
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            ZIndex = zIndex
        };

        var transform = new TranslateTransform();
        rect.RenderTransform = transform;

        if (left)
        {
            leftDisplay.Children.Add(rect);
            await Move(rect, transform, leftDisplay.Width, leftDisplay);
        }
        else
        {
            rightDisplay.Children.Add(rect);
            await Move(rect, transform, leftDisplay.Width, rightDisplay);
        }

        

    }

    private async Task Move(Control rect, TranslateTransform transform, double targetX, Panel panel)
    {
        double speed = ((bpm / 24.0) + 5) / 2.0; //want bpm to have some effect but dont want it to get too fast/slow
        const int delayMs = 16; //60fps

        while (transform.X < targetX)
        {
            transform.X += speed;
            await Task.Delay(delayMs);
        }
        panel.Children.Remove(rect);
    }
}