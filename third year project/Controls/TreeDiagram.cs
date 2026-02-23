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
    public class TreeDiagram : Control, SwitchableControl
    {
        DrawingContext context;
        Node tree;
        private bool treeDrawn = false, resetFlashCycle = true;
        private int flashCycleStep;
        static Point offset = new Point(0, 0);
        //private int activeTrees = 0;
        int[][] structure;
        string noteToPlay;
        double bufferZone = 1; //having this as anything but 1 seems to offset everything
        int circleRadius = 12;

        private IBrush? highlightBrush;
        private IBrush? controlBackgroundBrush;

        int bpm = 120;

        //SoundPlayer soundPlayer = SoundPlayer.instance;

        public static readonly StyledProperty<string> TreeStructureProperty =
           AvaloniaProperty.Register<TreeDiagram, string>(nameof(TreeStructure));

        public string TreeStructure
        {
            get => GetValue(TreeStructureProperty);
            set => SetValue(TreeStructureProperty, value);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            highlightBrush = (IBrush)this.FindResource("BlueBrush");
            controlBackgroundBrush = (IBrush)this.FindResource("LightBlueBrush");
        }


        public TreeDiagram(int[][] _structure, string note)
        {
            structure = _structure;
            noteToPlay = note;
            //soundPlayer.Initialize();

            LayoutUpdated += (_, _) =>
            {
                offset = new Point(0, 0);
                treeDrawn = false;
                if (tree != null)
                {
                    flashCycleStep = tree.getCurrentFlashStep();
                }
                else
                { 
                    flashCycleStep = 1;
                }
                StopFlashingCycle();
                tree = null;
                InvalidateVisual();
            };
        }
        public TreeDiagram()
        {
            structure = IntArrayParser.ParseTo2DIntArray(TreeStructure);

            LayoutUpdated += (_, _) =>
            {
                offset = new Point(0, 0);
                treeDrawn = false;
                if (tree != null)
                {
                    flashCycleStep = tree.getCurrentFlashStep();
                }
                else
                {
                    flashCycleStep = 1;
                }
                StopFlashingCycle();
                tree = null;
                InvalidateVisual();
            };
        }

        public void OnControlSwitched()
        {
            //flashCycleStep = 1;
            //tree = null;
            //treeDrawn = false;
            resetFlashCycle = true;
            //StopFlashingCycle();
            //InvalidateVisual();

            //soundPlayer.scheduleNote(soundPlayer.getCurrentSample(), noteToPlay);// was having issues with the thing not playing
        }

        //these wonderful functions place the control in the center of the available space instead of giving it a width and height of 0
        protected override Size MeasureOverride(Size availableSize) //runs on resize and reload
        {
            double width = double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width * bufferZone;
            double height = double.IsInfinity(availableSize.Height) ? 100 : availableSize.Height * bufferZone;

            if (tree == null)
            {
                return new Size(width, height); //might have us return on a reload so this only runs on resize
            }
            offset = new Point(0, 0);
            treeDrawn = false;
            flashCycleStep = tree.getCurrentFlashStep();
            StopFlashingCycle();
            tree = null;
            InvalidateVisual();

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        public override void Render(DrawingContext _context)
        {
            context = _context;
            base.Render(context);

            if (offset == new Point(0, 0)) //this is a little hack to get the red lines to draw in the right place there was some issue with the black lines having local positions and the red ones being based on the global position (which i have no idea why that would be the case) but this seems to fix it
            {
                offset = (Point)this.TranslatePoint(new Point(0, 0), this.GetVisualRoot() as Visual);
                //offset = new Point(offset.X, 0); //vertical stuff is going weird this might fix it 
            }


            double width = this.Bounds.Width * bufferZone; //maybe this 0.9 should be a slider for people with weird size screens
            double height = this.Bounds.Height * bufferZone;

            if (!treeDrawn)
            {
                tree = buildTree();
                tree.SetControlBackgroundBrush(controlBackgroundBrush);
            }


            this.tree.draw(width, height, context, this, !treeDrawn, flashCycleStep);
            flashCycleStep = -1; //-1 will mean dont change it from what it was before

            if (!treeDrawn)
            {
                StartFlashingCycle(bpm);
            }

            if(resetFlashCycle)
            {
                setBranchColour(tree.GetBottomRowNodes()[0], new Pen(highlightBrush, 3));
                tree.setCurrentFlashStep(1);
                resetFlashCycle = false;
            }

            treeDrawn = true;

            //this.AttachedToVisualTree += OnAttachedToVisualTree;
            //this.DetachedFromVisualTree += OnDetachedFromVisualTree;

        }
        private Node BuildTreeRecursive(Node partTree, ref bool[][] nodesRead, ref int[][] definition)
        {
            int layerTotal = 0;
            //Console.WriteLine($"part tree root: { partTree.getValue()}");
            //foreach (int[] a in definition)
            //{
            //    foreach(int b in a)
            //    {
            //        Console.Write(b + " ");
            //    }
            //    Console.Write("|");
            //}
            //Console.WriteLine();

            if (definition.Length < 2)
            {
                return partTree;
            }

            for (int i = 0; layerTotal < definition[0][0]; i++)
            {
                try
                {
                    if (nodesRead[1][i]) //we mark as used so that other recursive calls know to skip it
                    {
                        continue;
                    }
                }
                catch (Exception e) { //this is possibly the laziest validation i could have tried but trust me it works 
                    throw new ArgumentException("invalid tree structure", nameof(structure));
                    return null;
                }
                layerTotal += definition[1][i]; //how far along the subtree we are 
                Node child = new Node(definition[1][i]);
                partTree.AddChild(child); //add this number as a new child to the main object

                //build sub arrays for the next layer down
                int[][] subDefinition = { [definition[1][i]] };
                bool[][] subNodesRead = { [nodesRead[1][i]] };
                subDefinition = subDefinition.Concat(definition.Skip(2)).ToArray();
                subNodesRead = subNodesRead.Concat(nodesRead.Skip(2)).ToArray();

                BuildTreeRecursive(child, ref subNodesRead, ref subDefinition);
                nodesRead[1][i] = true; //mark this position in the tree as read
            }
            return partTree;
        }

        private Node buildTree()
        {
            //int[][] definition = [ [14], [8, 6], [3, 5, 3, 3], [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1] ];

            bool[][] nodesRead = new bool[structure.Length][];

            for (int i = 0; i < structure.Length; i++)
            {
                nodesRead[i] = new bool[structure[i].Length];
            }
            foreach (int[] layer in structure) //verify that all rows add up to the top value
            {
                if (layer.Sum() != structure[0][0])
                {
                    throw new ArgumentException("invalid tree structure", nameof(structure));
                }
            }
            return BuildTreeRecursive(new Node(structure[0][0]), ref nodesRead, ref structure);

        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            StopFlashingCycle();
        }
        //protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        //{
        //    //when we resize the window we want to redraw
        //    base.OnPropertyChanged(change);
        //    if (tree == null)
        //    {
        //        return;
        //    }
        //    if (change.Property == BoundsProperty)
        //    {
        //        offset = new Point(0, 0);
        //        treeDrawn = false;
        //        //flashCycleStep = tree.getCurrentFlashStep();
        //        //Console.WriteLine($"flash cycle step set to {tree.getCurrentFlashStep()}");
        //        StopFlashingCycle();
        //        tree = null;
        //        InvalidateVisual();
        //    }
        //}

        private DispatcherTimer? flashTimer;

        public void StartFlashingCycle(double bpm)
        {
            bpm *= 2; //dont even ask, i dont know
            //but for some reason the bpm was moving things half the speed it should have done

            StopFlashingCycle(); // make sure we don't start twice
            List<Node> bottomRowNodes = tree.GetBottomRowNodes();
            int[] countsToPlaySound = structure[structure.Length - 2]; //the second last row is the one that determines when to play sounds

            var interval = TimeSpan.FromSeconds(60.0 / Math.Max(bpm, 0.001));

            flashTimer = new DispatcherTimer(interval, DispatcherPriority.Normal, (s, e) =>
            {
                //bottomRowNodes[tree.getCurrentFlashStep()].setColour(context, new Pen(Brushes.Red, 3));
                int step = tree.getCurrentFlashStep();
                setBranchColour(bottomRowNodes[step], new Pen(Brushes.Red, 3));
                this.InvalidateVisual();
                tree.setCurrentFlashStep((step + 1) % bottomRowNodes.Count);
                int count = 0;
                while (step > 0)
                {
                    step -= countsToPlaySound[count];
                    count++;
                }
                if(step == 0)
                {
                    //soundPlayer.scheduleNote(soundPlayer.getCurrentSample(), noteToPlay);
                }
            });
        }
        public void StopFlashingCycle()
        {
            if (flashTimer != null)
            {
                flashTimer.Stop();
                flashTimer = null;

            }
        }


        public void setBranchColour(Node node, Pen pen)
        {
            //only meant to be called on leaf nodes
            Node parent = node.GetParent();
            if (parent != node)
            {
                Point p = node.getThisPoint(); //+ offset;
                //double diff = this.TranslatePoint(p, this.GetVisualRoot() as Visual).Value.X - p.X;

                p = (Point)this.TranslatePoint(p, this.GetVisualRoot() as Visual) - offset; //nullable stuff doesnt matter right?
                Point parentPoint = (Point)this.TranslatePoint(parent.getThisPoint(), this.GetVisualRoot() as Visual) - offset;
                //Point parentPoint = parent.getThisPoint() + offset;

                setBranchColour(parent, pen);

                FormattedText formatted = new FormattedText(Convert.ToString(node.getValue()), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 20, pen.Brush);
                context.DrawEllipse(controlBackgroundBrush, pen, p, circleRadius, circleRadius);
                context.DrawText(formatted, new Point(p.X - 5, p.Y - 15));

                p = p + new Point(0, -circleRadius);
                parentPoint = parentPoint + new Point(0, circleRadius);
                context.DrawLine(pen, p, parentPoint);

                //if (node.GetChildCount() > 0)
                //{
                //    Console.WriteLine("bomba");
                //    if (node.GetChildAt(0).GetChildCount() == 0) //playy sound if we are moving across teh second layer down
                //    {
                //        Console.WriteLine("clart");
                //        soundPlayer.PlayKick();
                //    }
                //}
            }

        }

        public void SetBpm(int _bpm)
        {
            if (tree == null)
                return;
            bpm = _bpm;
            //offset = new Point(0, 0);
            treeDrawn = false;
            flashCycleStep = tree.getCurrentFlashStep();
            StopFlashingCycle();
            tree = null;
            InvalidateVisual();
        }

    }

    public class Node
    {
        private List<Node> children;
        private Node parent;
        private int value;
        Pen blackPen;
        private Point thisPoint = new Point();
        private double width, height;
        private int currentFlashStep = 0;
        private IBrush? controlBackgroundBrush;
        private int circleRadius = 12;
        public Node(int _value)
        {
            value = _value;
            children = new List<Node>();
            parent = this;
            blackPen = new Pen(Brushes.Black);
           
        }

        public void SetControlBackgroundBrush(IBrush? _controlBackgroundBrush)
        {
            controlBackgroundBrush = _controlBackgroundBrush;
        }

        public void AddChild(Node child)
        {
            children.Add(child);
            child.setParent(this);
        }

        public void AddChildren(params Node[] _children)
        {
            foreach (Node c in _children)
            {
                AddChild(c);
            }
        }

        public List<Node> GetChildren()
        {
            return children;
        }

        public Node GetChildAt(int index)
        {
            return children[index];
        }

        public int GetChildCount()
        {
            return children.Count();
        }

        public Node GetParent()
        {
            return parent;
        }

        public void setParent(Node _parent)
        {
            parent = _parent;
        }

        public int getValue()
        {
            return value;
        }

        public int GetTotalRowSize()
        {
            return TotalRowValUp(0);
        }

        private int TotalRowValUp(int level)
        {
            if (parent == this) //we are at the top level
            {
                return TotalRowValDown(level); //here level is how many levels we need to descend
            }
            else
            {
                return parent.TotalRowValUp(level + 1);
            }
        }

        private int TotalRowValDown(int level) //recursive call that fits into another funciton but can also be called from root node to get the size of the row (level) levels down
        {
            if (level == 0)
            {
                return 1;
            }
            else
            {
                int total = 0;
                foreach (Node c in children)
                {
                    total += c.TotalRowValDown(level - 1);
                }
                return total;
            }
        }

        public int getDepthOfSubtree()  //assumes all branches have the same depth, which they should do
        {
            if (children.Count == 0)
            {
                return 0;
            }
            else
            {
                return children[0].getDepthOfSubtree() + 1;
            }
        }
        public void draw(double _width, double _height, DrawingContext context, TreeDiagram container, bool firstRun, int flashCycleStep)
        {
            //to be called on the root node only
            width = _width;
            height = _height;
            if (flashCycleStep != -1)
                currentFlashStep = flashCycleStep;

            double startX = width / 2;
            int startY = 0;
            int depth = getDepthOfSubtree();
            int[] howfaralong = new int[depth + 1]; //this shit is becoming more overengineered and overcomplicated by the second 

            Point s = new Point(startX, startY);
            setThisPoint(s);

            int endY = (int)(((double)1 / depth) * height);
            for (int i = 0; i < children.Count; i++)
            {
                int endX = (int)(((i + (int)(children.Count / children[0].GetTotalRowSize())) / (children.Count + 1.0)) * width);
                //int endX = (int)((i + howfaralong[getDepthOfSubtree()] + 0.5) * (int)((width) / (children[0].GetTotalRowSize())));
                Point e = new Point(endX, endY);
                context.DrawLine(blackPen, s + new Point(0, circleRadius), e + new Point(0, -circleRadius));

                children[i].drawRecursive(context, width, height, depth, endX, endY, ref howfaralong);
                children[i].setThisPoint(e);
                howfaralong[getDepthOfSubtree()] += 1; //not convinced here tbh but seems to work, might want its own loop
            }
            context.DrawEllipse(controlBackgroundBrush, blackPen, new Point(startX, startY), 12, 12);
            FormattedText formatted = new FormattedText(Convert.ToString(value), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 18, Brushes.Black);
            context.DrawText(formatted, new Point(startX - 11, startY - 14));
        }

        //for a row with totalwidth, this node is subtreewidth wide, and is howfaralong in the row
        //so we want to draw the nodes between howfaralong and howfaralong + subtreewidth
        public void drawRecursive(DrawingContext context, double width, double height, int depth, int startX, int startY, ref int[] howfaralong)
        {
            //if howfaralong + subtreewidth > totalwidth then thats bad and we should throw an error
            if (children.Count >= 1)
            {
                int sectionsize = (int)((width) / (children[0].GetTotalRowSize()));

                int endY = (int)(((depth - getDepthOfSubtree() + 1.0) / depth) * height);
                for (int i = 0; i < children.Count; i++)
                {
                    int endX = (int)((i + howfaralong[getDepthOfSubtree()] + 0.5) * sectionsize);
                    Point s = new Point(startX, startY);
                    Point e = new Point(endX, endY);
                    context.DrawLine(blackPen, s + new Point(0, circleRadius), e + new Point(0, -circleRadius) );

                    children[i].drawRecursive(context, width, height, depth, endX, endY, ref howfaralong);
                    children[i].setThisPoint(e);

                }
                for (int i = 0; i < children.Count; i++)
                {
                    howfaralong[getDepthOfSubtree()] += 1;
                }
            }
            FormattedText formatted = new FormattedText(Convert.ToString(value), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 20, Brushes.Black);
            context.DrawEllipse(controlBackgroundBrush, blackPen, new Point(startX, startY), 12, 12);
            context.DrawText(formatted, new Point(startX - 5, startY - 15));
        }

        private void setThisPoint(Point point)
        {
            thisPoint = point;
        }

        public Point getThisPoint()
        {
            return thisPoint;
        }

        public void setColour(DrawingContext context, Pen pen)
        {
            //only meant to be called on leaf nodes
            if (parent != this)
            {
                Point p = getThisPoint();
                context.DrawLine(pen, p, parent.getThisPoint());
                parent.setColour(context, pen);

                FormattedText formatted = new FormattedText(Convert.ToString(value), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 20, pen.Brush);
                context.DrawEllipse(controlBackgroundBrush, pen, p, 12, 12);
                context.DrawText(formatted, new Point(p.X - 5, p.Y - 15));


            }

        }
        public List<Node> GetBottomRowNodes()
        {
            List<Node> bottomRowNodes = new List<Node>();
            GetBottomRowNodesRecursive(this, bottomRowNodes);
            return bottomRowNodes;
        }
        private void GetBottomRowNodesRecursive(Node node, List<Node> bottomRowNodes)
        {
            if (node.GetChildren().Count == 0)
            {
                bottomRowNodes.Add(node);
            }
            else
            {
                foreach (Node child in node.GetChildren())
                {
                    GetBottomRowNodesRecursive(child, bottomRowNodes);
                }
            }
        }

        public int getCurrentFlashStep()
        {
            return currentFlashStep;
        }

        public void setCurrentFlashStep(int flashStep)
        {
            currentFlashStep = flashStep;
        }
    }
}
