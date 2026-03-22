namespace third_year_project.Tests.Services;

using third_year_project.Services;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

public class NodeTest //mostly a ui service so we dont need that many tests 
{
    [Fact]
    public void NewNodeHasNoChildren()
    {
        Node node = new Node(null, new TextBox());

        Assert.Empty(node.GetChildren());
    }
    [Fact]
    public void NewNodeHasDepth1()
    {
        Node node = new Node(null, new TextBox());
        Assert.Equal(1, node.GetDepth());
    }
    [Fact]
    public void AddChildAddsOneChild()
    {
        Node node = new Node(null, new TextBox());
        node.AddChildNode(new Node(node, new TextBox()));

        Assert.Single(node.GetChildren()); //assert that count is 1
    }
    [Fact]
    public void AddingChildIncrementsDepth() //get depth isnt actually used its for something i didnt have time to finish
    {
        Node node = new Node(null, new TextBox());
        node.AddChildNode(new Node(node, new TextBox()));

        Assert.Equal(2, node.GetDepth());
    }

    [Fact]
    public void ManualConstructorTest()
    {
        Panel horizontal = new StackPanel();
        Panel vertical = new StackPanel();
        TextBox textBox = new TextBox();
        Node node = new Node(null, vertical, horizontal, textBox);

        Assert.Empty(node.GetChildren());
        Assert.Equal(1, node.GetDepth());

    }
    [Fact]
    public void CannotAddNullChild()
    {
        Node node = new Node(null, new TextBox());
        Assert.Throws<ArgumentNullException>(() => node.AddChildNode(null));
    }
    [Fact]
    public void CannotAddNullControl()
    {
        Node node = new Node(null, new TextBox());
        Assert.Throws<ArgumentNullException>(() => node.AddControlBelow(null));
        Assert.Throws<ArgumentNullException>(() => node.AddControlBeside(null));
    }

}
