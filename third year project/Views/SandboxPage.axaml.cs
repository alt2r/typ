using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;
using System.Reactive;
using third_year_project.Services;
using third_year_project.ViewModels;

namespace third_year_project.Views;

public partial class SandboxPage : UserControl
{
    SandboxPageViewModel vm;
    List<StackPanel> leftRows = new List<StackPanel>();

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
                    var args = interaction.Input;
                    interaction.SetOutput(Unit.Default);
                    AddNode((Side)((int)args[0] - 48), (int)args[1] - 48); //ascii conversion since int parse is being difficult
                });

                vm.AddRowInView.RegisterHandler(interaction =>
                {
                    Side side = (Side)((int)interaction.Input[0] - 48);
                    interaction.SetOutput(Unit.Default);
                    AddRow(side);
                });

                vm.ConfirmInView.RegisterHandler(interaction =>
                {
                    Console.WriteLine("confirming");
                    interaction.SetOutput(Unit.Default);
                });

                textBoxReferencesLeft[0, 0] = leftStartBox;
            }
        };
        leftRows.Add(leftRow0); //jsut for testing 
    }
    private void AddNode(Side side, int row)
    {
        if(leftRows.Count > row - 1)
        {
           // row = row + 1; //think this is bc top row doesnt count?
        }
        
        Console.WriteLine(row);
        TextBox node = new TextBox
        {
            Height = 40,
            Watermark = "...",
            Margin = Avalonia.Thickness.Parse("5")
        };

        
        if (side == Side.Left)
        {
            Console.WriteLine($"row: {row}");
            leftRows[row].Children.Insert(leftRows[row].Children.Count - 1, node);
            textBoxReferencesLeft[row, leftRows[row].Children.Count - 2] = node;
            Console.WriteLine($"adding text box at {row}, {leftRows[row].Children.Count - 2}");
            if (leftRows[row].Children.Count > 8)   //bear in mind that this is fucked and needs to be redone after the showcase
            {
                leftRows[row].Children.RemoveAt(8);
            }
            
        }
        //mainGridRight.Children.Add(node);
    }

    private void AddRow(Side side)
    {
        StackPanel row = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        Button plus = new Button
        {
            Content = "+",
            Opacity = 0.5,
            Margin = Avalonia.Thickness.Parse("5")
        };
        //plus.Bind(Button.CommandProperty, new Binding(nameof(vm.NewNode)) { Source = vm });
        plus.Command = vm.NewNode;
        Grid.SetRow(row, mainGridLeft.Children.Count - 1);
        if (side == Side.Left)
        {
            plus.CommandParameter = new NodeCreationArgs { side = Side.Left, row = leftRows.Count };
            row.Children.Add(plus);
            mainGridLeft.Children.Add(row);
            leftRows.Add(row);

        }
        else
        {
            mainGridRight.Children.Add(row);
        }

        if (leftRows.Count <= 4)
        {
            Grid.SetRow(addNewRowButtonLeft, leftRows.Count);
        }
        else
        {
            mainGridLeft.Children.Remove(addNewRowButtonLeft);
        }
        Console.WriteLine("hey");
        Console.WriteLine(plus.CommandParameter);
        AddNode(side, leftRows.Count - 1);
    }

}