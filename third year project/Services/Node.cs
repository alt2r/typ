using Avalonia.Controls;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace third_year_project.Services
{
    public class Node //for sandbox tree building
    {
        Panel horizontalPanel { get; }
        Panel verticalPanel { get; }
        Node parent;
        List<Node> children;
        TextBox control;

        //constructor that creates the panels itself
        public Node(Node? _parent, TextBox textBox)
        {
            if (_parent == null)
            {
                parent = this;
            }
            else
            {
                parent = _parent;
            }
            verticalPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Vertical,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };
            horizontalPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            children = new List<Node>();
            control = textBox;
            verticalPanel.Children.Add(control);
            verticalPanel.Children.Add(horizontalPanel);
            if (parent != this)
            {
                parent.horizontalPanel.Children.Insert(0, verticalPanel);
            }
            control.Text = "1";

        }

        //constructor to manually add the panels
        public Node(Node _parent, Panel vPanel, Panel hPanel, TextBox textBox)
        {
            if (_parent == null)
            {
                parent = this;
            }
            else
            {
                parent = _parent;
            }
            verticalPanel = vPanel;
            horizontalPanel = hPanel;
            control = textBox;
            children = new List<Node>();
            control.Text = $"1";
        }

        public void AddChildNode(Node node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (children.Count < 4)
            {
                children.Add(node);
            }

            RefreshDisplayNumber();

            if (children.Count > 3)
            {
                //children.RemoveAt(children.Count - 1);

                //remove the add child button as 4 children is the maximum
                horizontalPanel.Children.RemoveAt(horizontalPanel.Children.Count - 1);
            }
        }

        private void RefreshDisplayNumber()
        {
            control.Text = $"{GetTotalChildrenCount()}";
            if (parent != this && parent != null)
            {
                parent.RefreshDisplayNumber();
            }
        }

        public void AddControlBelow(Control ctl)
        {
            if (ctl == null)
                throw new ArgumentNullException(nameof(ctl));

            if (verticalPanel.Children.Count > 2) //remove the button if we are adding children
            {
                verticalPanel.Children.RemoveAt(1);
            }
            verticalPanel.Children.Add(ctl);
        }

        public void AddControlBeside(Control ctl)
        {
            if (ctl == null)
                throw new ArgumentNullException(nameof(ctl));

            horizontalPanel.Children.Add(ctl);
        }

        public List<Node> GetChildren()
        {
            return children;
        }

        public Node GetParent()
        {
            return parent;
        }

        public void RemoveFromVertical()
        {
            verticalPanel.Children.RemoveAt(verticalPanel.Children.Count - 1);
        }

        public void RemoveFromHorizontal()
        {
            horizontalPanel.Children.RemoveAt(horizontalPanel.Children.Count - 1);
        }

        public int GetTotalChildrenCount()
        {
            if (children.Count == 0)
            {
                return 1;
            }
            else
            {
                int total = 0;
                foreach (Node x in children)
                {
                    total += x.GetTotalChildrenCount();
                }
                return total;
            }
        }

        public int GetDepth() //all branches must be the same depth so this is ok
        {
            if(children.Count == 0)
            {
                return 1;
            }
            return children[0].GetDepth() + 1;
        }
    }
}
