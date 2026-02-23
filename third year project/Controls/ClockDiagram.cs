using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CSCore.Codecs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using third_year_project.Services;


namespace third_year_project.Controls
{
    internal class ClockDiagram : Control, SwitchableControl
    {
        DrawingContext context;
        bool clockDrawn = false;
        Point center;
        double bufferZone = 1; //percentage of the control area to use for drawing the clock
        double width, height;
        List<Double> radiusPlural = new List<double>();
        List<float> thetas = new List<float>();
        List<int> beatCounts = new List<int>();
        static Point offset = new Point(0, 0);
        int[][] structure;
        //SoundPlayer soundPlayer = SoundPlayer.instance;
        List<double[]> beatPositionsOnCircles = new List<double[]>();

        int bpm = 120;

        string[] notes;


        bool updateBounds = true;

        double accentPointSize = 0.15;

        public static readonly StyledProperty<string> ClockStructureProperty =
          AvaloniaProperty.Register<TreeDiagram, string>(nameof(ClockStructure));
        public string ClockStructure
        {
            get => GetValue(ClockStructureProperty);
            set => SetValue(ClockStructureProperty, value);
        }
        public ClockDiagram(int[][] _structure, string[] _notes)
        {
            notes = _notes;
            structure = _structure;
            beatPositionsOnCircles.Clear();
            for (int i = 0; i < structure.Length; i++)
            {
                thetas.Add(-0.5f * MathF.PI);
                beatCounts.Add(structure[i].Sum());
                beatPositionsOnCircles.Add(new double[structure[i].Length]);
                //soundPlayer.Initialize();
                //soundPlayer.scheduleNote(soundPlayer.getCurrentSample(), notes[i]); //was having an issue where the initial notes didnt play 
            }

            LayoutUpdated += (_, _) =>
            {
                updateBounds = true;
                clockDrawn = false;
                StopSpinningCycle();
                InvalidateVisual();
            };

        }

        public ClockDiagram()
        {
            structure = IntArrayParser.ParseTo2DIntArray(ClockStructure);
            beatPositionsOnCircles.Clear();
            for (int i = 0; i < structure.Length; i++)
            {
                thetas.Add(-0.5f * MathF.PI);
                beatCounts.Add(structure[i].Sum());
                beatPositionsOnCircles.Add(new double[structure[i].Length + 1]);

                //soundPlayer.scheduleNote(soundPlayer.getCurrentSample(), notes[i]); //was having an issue where the initial notes didnt play 
            }

            LayoutUpdated += (_, _) =>
            {
                updateBounds = true;
                clockDrawn = false;
                StopSpinningCycle();
                InvalidateVisual();
            };
        }

        public void OnControlSwitched()
        {
            for (int i = 0; i < thetas.Count; i++)
            {
                thetas[i] = -0.5f * MathF.PI;
                //soundPlayer.scheduleNote(soundPlayer.getCurrentSample(), notes[i]); //was having an issue where the initial notes didnt play 
            }
        }

        //these wonderful functions place the control in the center of the available space instead of the alternative of giving it a width and height of 0
        protected override Size MeasureOverride(Size availableSize)
        {

            //updateBounds = true;
            //clockDrawn = false;
            double w = double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width * bufferZone;
            double h = double.IsInfinity(availableSize.Height) ? 100 : availableSize.Height * bufferZone;
            //StopSpinningCycle();
            //InvalidateVisual();
            //Console.WriteLine("updating bounds :)");

            return new Size(w, h);

        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        public override void Render(DrawingContext _context)
        {
            if (updateBounds) //this is a little hack to get the red lines to draw in the right place there was some issue with the black lines having local positions and the red ones being based on the global position (which i have no idea why that would be the case) but this seems to fix it
            {
                offset = (Point)this.TranslatePoint(new Point(0, 0), this.GetVisualRoot() as Visual);
                updateBounds = false;

                width = (this.Bounds.Width * bufferZone); //maybe this 0.9 should be a slider for people with weird size screens
                height = (this.Bounds.Height * bufferZone);
                center = new Point(width / 2, height / 2);
            }
            double whAvg = (width + height) / 2;

            context = _context;

            IBrush fillColour = Brushes.Transparent;
            IPen radiusPen = new Pen(Brushes.Blue, 2);

            //int[][] structure = [[4, 3, 3, 3, 4, 4, 4], [4, 4, 4, 4]]; //it might decay

            int radiusIncr = 0;
            for (int i = 0; i < structure.Length; i++)
            {
                radiusPlural.Add(whAvg * (0.2 + (radiusIncr * 0.05)));
                radiusIncr++;
                context.DrawEllipse(fillColour, radiusPen, new Point(width / 2, height / 2), radiusPlural[i], radiusPlural[i]);

                int circleTotal = structure[i].Sum();
                int totalSoFar = 0;

                for (int j = 0; j < structure[i].Length; j++)
                {
                    float thetaOfLine = ((float)totalSoFar / circleTotal) * MathF.PI * 2;
                    thetaOfLine -= MathF.PI * 0.5f; //the circle will start at the rightmost point by default, this rotates it to start at the top
                    beatPositionsOnCircles[i][j] = thetaOfLine;

                    double radiusSin = (int)radiusPlural[i] * MathF.Sin(thetaOfLine); //minimise expensive operations in the nested for loop
                    double radiusCos = (int)radiusPlural[i] * MathF.Cos(thetaOfLine);
                    double innerX = center.X + (radiusCos * (1 - accentPointSize));
                    double innerY = center.Y + (radiusSin * (1 - accentPointSize));
                    double outerX = center.X + (radiusCos * (1 + accentPointSize));
                    double outerY = center.Y + (radiusSin * (1 + accentPointSize));
                    context.DrawLine(radiusPen, new Point(innerX, innerY), new Point(outerX, outerY));

                    //would be great to do this in the middle of each section would need to actually lock in and think 
                    FormattedText formatted = new FormattedText(Convert.ToString(structure[i][j]), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 18, Brushes.Black);
                    //double textX = center.X;
                    //double textY = center.Y;
                    //if (j == structure[i].Length - 1)
                    //{
                    //    textX += (int)radiusPlural[i] * (MathF.Sin(thetaOfLine) + MathF.Sin((float)beatPositionsOnCircles[i][0]) / 2);
                    //    textY += (int)radiusPlural[i] * (MathF.Cos(thetaOfLine) + MathF.Cos((float)beatPositionsOnCircles[i][0]) / 2);
                    //}
                    //else
                    //{
                    //    textX += (int)radiusPlural[i] * (MathF.Sin(thetaOfLine) + MathF.Sin((float)beatPositionsOnCircles[i][j + 1]) / 2);
                    //    textY += (int)radiusPlural[i] * (MathF.Cos(thetaOfLine) + MathF.Cos((float)beatPositionsOnCircles[i][j + 1]) / 2);
                    //}
                    context.DrawText(formatted, new Point(innerX, innerY));
                    totalSoFar += structure[i][j];
                }
            }


            if (!clockDrawn)
            {
                StartSpinningCycle(bpm);
                clockDrawn = true;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            StopSpinningCycle();
        }



        //do we still need this?
        //protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        //{
        //    //when we resize the window we want to redraw
        //    base.OnPropertyChanged(change);
        //    if (change.Property == BoundsProperty)
        //    {
        //        Console.WriteLine("on property changed");
        //        updateBounds = true;
        //        clockDrawn = false;
        //        StopSpinningCycle();
        //        InvalidateVisual();
        //    }
        //}
        private DispatcherTimer? spinTimer;
        public void StartSpinningCycle(int bpm)
        {

            StopSpinningCycle(); // make sure we don't start twice

            var interval = TimeSpan.FromSeconds(1.0 / 144.0); // aiming for 144fps since my screen is 144hz

            DateTime tick = DateTime.Now;

            double[] previousFrameThetas = Enumerable.Repeat(0.5 * MathF.PI, structure.Count()).ToArray();

            spinTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e) =>
            {
                TimeSpan elapsed = DateTime.Now - tick;
                tick = DateTime.Now;
                for (int i = 0; i < thetas.Count; i++)
                {

                    Point endpoint = new Point(center.X + (int)(radiusPlural[i] * MathF.Cos(thetas[i])), center.Y + (int)(radiusPlural[i] * MathF.Sin(thetas[i])));
                    endpoint = (Point)this.TranslatePoint(endpoint, this.GetVisualRoot() as Visual) - offset;
                    Point centerOffset = (Point)this.TranslatePoint(center, this.GetVisualRoot() as Visual) - offset;
                    context.DrawLine(new Pen(Brushes.DarkRed, 4), centerOffset, endpoint);

                    double movespeed = (bpm * MathF.PI)/ ((double)beatCounts[i] * 15.0);
                    thetas[i] += (float)(elapsed.TotalSeconds * movespeed); //each hand spins at a different speed

                    //if the hand has just passed a beat point then play a sound
                    for (int j = 0; j < structure[i].Length; j++)
                    {
                        foreach(double beat in beatPositionsOnCircles[i])
                        {
                            if (previousFrameThetas[i] <= beat && beat <= thetas[i])
                            {
                                //soundPlayer.scheduleNote(soundPlayer.getCurrentSample(), notes[i]);
                            }
                        }
                    }
                    previousFrameThetas[i] = thetas[i];
                    if (thetas[i] > MathF.PI * 1.5) //when we cross the 0 point
                    {
                        thetas[i] -= MathF.PI * 2;
                        //soundPlayer.scheduleNote(soundPlayer.getCurrentSample(), notes[i]);
                    }
                }
                this.InvalidateVisual();
            });
        }
        public void StopSpinningCycle()
        {
            if (spinTimer != null)
            {
                spinTimer.Stop();
                spinTimer = null;

            }
        }

        public void SetBpm(int _bpm)
        {
            bpm = _bpm;
            if (!clockDrawn)
            {
                clockDrawn = false;
                StopSpinningCycle();
                InvalidateVisual();
            }
        }
    }
    
}
