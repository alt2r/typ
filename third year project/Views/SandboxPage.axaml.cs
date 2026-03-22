using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Xml.Serialization;
using third_year_project.Services;
using third_year_project.ViewModels;

namespace third_year_project.Views;

public partial class SandboxPage : UserControl
{
    SandboxPageViewModel vm;
    Node leftTree;
    Node rightTree;

    TextBox[,] textBoxReferencesLeft = new TextBox[5, 8];
    public SandboxPage()
    {
        InitializeComponent();
        this.DataContextChanged += (_, __) => //do this when the datacontext becomes available 
        {
            if (DataContext is SandboxPageViewModel _vm)
            {
                vm = _vm;
                vm.AddNodeInView.RegisterHandler(interaction =>
                {
                    interaction.SetOutput(Unit.Default);
                    AddNode(interaction.Input); //ascii conversion since int parse is being difficult
                });

                vm.AddRowInView.RegisterHandler(interaction =>
                {
                    interaction.SetOutput(Unit.Default);
                    AddRow(interaction.Input);
                });

                textBoxReferencesLeft[0, 0] = leftStartBox;

                vm.leftRootNode = new Node(vm.leftRootNode, leftRootStackPanel, leftRootHorizontal, leftStartBox);
                vm.rightRootNode = new Node(vm.rightRootNode, rightRootStackPanel, rightRootHorizontal, rightStartBox);

                leftTree = vm.leftRootNode;
                rightTree = vm.rightRootNode;
            }


            Button btnL = new Button
            {
                Content = "+",
                Opacity = 0.5,
                Margin = Avalonia.Thickness.Parse("5"),
                Command = vm.NewRow,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            btnL.CommandParameter = leftTree;
            leftRootStackPanel.Children.Add(btnL);

            Button btnR = new Button
            {
                Content = "+",
                Opacity = 0.5,
                Margin = Avalonia.Thickness.Parse("5"),
                Command = vm.NewRow,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            btnR.CommandParameter = rightTree;
            rightRootStackPanel.Children.Add(btnR);

        };
    }

    private Node GetNodeAtPosition(Node tree, string position)
    {
        if (position.Length == 0)
        { //this needs validation bro (from past harry)
            return tree;
        }
        return GetNodeAtPosition(tree.GetChildren()[Convert.ToInt32(position[0])], position[1..]);
    }
    private void AddNode(Node parent)
    {
        TextBox textBox = new TextBox
        {
            Height = 40,
            Width = 40,
            MinWidth = 0,
            MaxWidth =40,
            Watermark = "...",
            Margin = Avalonia.Thickness.Parse("3,10,3,10")
        };
        Node node = new Node(parent, textBox);
        Button plus = new Button
        {
            Content = "+",
            Opacity = 0.5,
            Margin = Avalonia.Thickness.Parse("5")
        };

        plus.Command = vm.NewRow;
        plus.CommandParameter = node;
        parent.AddChildNode(node);
        node.AddControlBelow(plus);
    }

    private void AddRow(Node nodeCalledOn)
    {

        Button plus = new Button
        {
            Content = "+",
            Opacity = 0.5,
            Margin = Avalonia.Thickness.Parse("2"),
            Command = vm.NewNode,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Padding = Avalonia.Thickness.Parse("4")

        };
        plus.CommandParameter = nodeCalledOn;
        AddNode(nodeCalledOn);
        AddNode(nodeCalledOn);
        nodeCalledOn.AddControlBeside(plus);
        nodeCalledOn.RemoveFromVertical(); //remove the button we just clicked

    }
}