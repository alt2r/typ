using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using third_year_project.Controls;
using Xunit;

namespace third_year_project.Tests.Controls
{
    public class TreeDiagramTests
    {
        //private static object GetPrivateField(object instance, string name)
        //{
        //    var t = instance.GetType();
        //    var f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        //    if (f == null) throw new InvalidOperationException($"Field '{name}' not found on {t.FullName}");
        //    return f.GetValue(instance);
        //}

        private static object InvokePrivateMethod(object instance, string name, params object[] args)
        {
            var t = instance.GetType();
            var m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null) throw new InvalidOperationException($"Method '{name}' not found on {t.FullName}");
            return m.Invoke(instance, args);
        }

        [Fact]
        public void MeasureReturnsExpectedSize()
        {
            var structure = new int[][] { new[] { 4 }, new[] { 2, 2 }, new[] { 1, 1, 1, 1 } };
            var td = new TreeDiagram(structure, "c4");

            td.Measure(new Size(400, 300));

            Assert.Equal(400, td.DesiredSize.Width, 3);
            Assert.Equal(300, td.DesiredSize.Height, 3);
        }

        [Fact]
        public void MeasureReturnsDefaultSizeForInfiniteSize()
        {
            var structure = new int[][] { new[] { 3 }, new[] { 1, 1, 1 } };
            var td = new TreeDiagram(structure, "c4");

            td.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(100, td.DesiredSize.Width, 3);
            Assert.Equal(100, td.DesiredSize.Height, 3);
        }

        [Fact]
        public void DiagramNodeBasicTreeOperations()
        {
            //        root
            //       /    \
            //     n1      n2
            //    /  \    /  \
            //  l1  l2  l3  l4 
            var root = new DiagramNode(4);
            var n1 = new DiagramNode(2);
            var n2 = new DiagramNode(2);
            var l1 = new DiagramNode(1);
            var l2 = new DiagramNode(1);
            var l3 = new DiagramNode(1);
            var l4 = new DiagramNode(1);

            n1.AddChildren(l1, l2);
            n2.AddChildren(l3, l4);
            root.AddChildren(n1, n2);

            Assert.Equal(2, root.getDepthOfSubtree());

            var bottom = root.GetBottomRowNodes();
            Assert.Equal(4, bottom.Count);
            Assert.Contains(l1, bottom);
            Assert.Contains(l2, bottom);
            Assert.Contains(l3, bottom);
            Assert.Contains(l4, bottom);

            Assert.Equal(root, n1.GetParent().GetParent()); //n1 parent is root (root parent is itself)
            Assert.Equal(n1, l1.GetParent());
            Assert.Equal(n2, l3.GetParent());

            var p = new Point(10, 20);
            l1.setThisPoint(p);
            Assert.Equal(p, l1.getThisPoint());

            l1.setCurrentFlashStep(3);
            Assert.Equal(3, l1.getCurrentFlashStep());
        }

        [Fact]
        public void BuildTreeCreatesCorrectLeafCountAndValidatesStructure()
        {
            var validStructure = new int[][] { new[] { 4 }, new[] { 2, 2 }, new[] { 1, 1, 1, 1 } };
            var td = new TreeDiagram(validStructure, "c4");

            var rootObj = InvokePrivateMethod(td, "buildTree");
            Assert.NotNull(rootObj);
            var root = Assert.IsType<DiagramNode>(rootObj);

            var bottomNodes = root.GetBottomRowNodes();
            Assert.Equal(4, bottomNodes.Count);

            var invalidStructure = new int[][] { new[] { 4 }, new[] { 3, 1 }, new[] { 1, 1, 1 } }; 
            var tdInvalid = new TreeDiagram(invalidStructure, "c4");
            var ex = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(tdInvalid, "buildTree"));
            Assert.IsType<ArgumentException>(ex.InnerException);
        }
    }
}
