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
    INode? leftTree;
    INode? rightTree;

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
                    AddNode(interaction.Input);
                });

                vm.AddRowInView.RegisterHandler(interaction =>
                {
                    interaction.SetOutput(Unit.Default);
                    AddRow(interaction.Input);
                });

                textBoxReferencesLeft[0, 0] = leftStartBox;

                var leftRoot = new Node(null, leftRootStackPanel, leftRootHorizontal, leftStartBox);
                var rightRoot = new Node(null, rightRootStackPanel, rightRootHorizontal, rightStartBox);

                vm.leftRootNode = leftRoot;
                vm.rightRootNode = rightRoot;

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
    private void AddNode(INode parent)
    {
        if (parent is not Node parentNode)
            return;

        TextBox textBox = new TextBox
        {
            Height = 40,
            Width = 30,
            MinWidth = 0,
            MaxWidth = 30,
            Watermark = "...",
            Margin = Avalonia.Thickness.Parse("3,10,3,10")
        };
        Node node = new Node(parentNode, textBox);
        Button plus = new Button
        {
            Content = "+",
            Opacity = 0.5,
            Margin = Avalonia.Thickness.Parse("5")
        };

        plus.Command = vm.NewRow;
        plus.CommandParameter = node;
        parentNode.AddChildNode(node);

        if (node.GetDepthOfThisNode() < 4)
            node.AddControlBelow(plus);
    }

    private void AddRow(INode nodeCalledOn)
    {
        if (nodeCalledOn is not Node node)
            return;

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
        node.AddControlBeside(plus);
        node.RemoveFromVertical(); //remove the button we just clicked

    }
}