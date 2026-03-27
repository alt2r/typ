using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using third_year_project.Services;
using third_year_project.ViewModels;

namespace third_year_project.Tests.ViewModels
{
        // lightweight test double implementing INode (avoids Avalonia)
    internal class TestNode : INode
    {
        public List<INode> Children { get; } = new List<INode>();
        public void AddChild(TestNode child) => Children.Add(child);
        public List<INode> GetChildren() => Children;
        public int GetTotalChildrenCount()
        {
            if (!Children.Any()) return 1;
            int total = 0;
            foreach (var c in Children) total += c.GetTotalChildrenCount();
            return total;
        }
    }

    public class SandboxPageViewModelTests
    {
        [Fact]
        public void HomeClick_Sets_CurrentPage_To_HomePage()
        {
            var main = new MainWindowViewModel();
            var vm = new SandboxPageViewModel(main, onConfirm: null, outputScheduler: Scheduler.Immediate);

            vm.HomeClick.Execute().Subscribe();

            Assert.IsType<HomePageViewModel>(main.CurrentPage);
        }

        [Fact]
        public void ConfirmClick_WithValidSymmetricTrees_InvokesOnConfirmWithExpectedStructure()
        {
            // Build symmetric tree with depth 3 and root total 4:
            // root -> two children -> each has two leaves (4 total)
            var leftRoot = new TestNode();
            var l1 = new TestNode();
            var l2 = new TestNode();
            l1.AddChild(new TestNode());
            l1.AddChild(new TestNode());
            l2.AddChild(new TestNode());
            l2.AddChild(new TestNode());
            leftRoot.AddChild(l1);
            leftRoot.AddChild(l2);

            var rightRoot = new TestNode();
            var r1 = new TestNode();
            var r2 = new TestNode();
            r1.AddChild(new TestNode());
            r1.AddChild(new TestNode());
            r2.AddChild(new TestNode());
            r2.AddChild(new TestNode());
            rightRoot.AddChild(r1);
            rightRoot.AddChild(r2);

            List<int[][]>? captured = null;
            var main = new MainWindowViewModel();
            var vm = new SandboxPageViewModel(main, onConfirm: s => captured = s, outputScheduler: Scheduler.Immediate);
            vm.leftRootNode = leftRoot;
            vm.rightRootNode = rightRoot;

            vm.ConfirmClick.Execute().Subscribe();

            Assert.NotNull(captured);
            Assert.Equal(2, captured.Count);
            // first number in first row is the root total
            Assert.Equal(4, captured[0][0][0]);
            Assert.Equal(4, captured[1][0][0]);
        }

        [Fact]
        public void ConfirmClick_MismatchedDepthOrRowCounts_DoNotInvokeOnConfirm()
        {
            // left deeper (depth 3)
            var leftRoot = new TestNode();
            var l1 = new TestNode();
            l1.AddChild(new TestNode());
            l1.AddChild(new TestNode());
            leftRoot.AddChild(l1);

            // right shallower (depth 2)
            var rightRoot = new TestNode();
            rightRoot.AddChild(new TestNode());

            bool invoked = false;
            var main = new MainWindowViewModel();
            var vm = new SandboxPageViewModel(main, onConfirm: s => invoked = true, outputScheduler: Scheduler.Immediate);
            vm.leftRootNode = leftRoot;
            vm.rightRootNode = rightRoot;

            vm.ConfirmClick.Execute().Subscribe();

            Assert.False(invoked);
        }

        [Fact]
        public void ConfirmClick_TooShallowDiagram_IsRejected()
        {
            // both roots but too shallow (depth < 2)
            var leftRoot = new TestNode();
            var rightRoot = new TestNode();

            bool invoked = false;
            var main = new MainWindowViewModel();
            var vm = new SandboxPageViewModel(main, onConfirm: s => invoked = true, outputScheduler: Scheduler.Immediate);
            vm.leftRootNode = leftRoot;
            vm.rightRootNode = rightRoot;

            vm.ConfirmClick.Execute().Subscribe();

            Assert.False(invoked);
        }

        [Fact]
        public void ConfirmClick_NullRoots_DoesNotThrow_AndDoesNotInvoke()
        {
            bool invoked = false;
            var main = new MainWindowViewModel();
            var vm = new SandboxPageViewModel(main, onConfirm: s => invoked = true, outputScheduler: Scheduler.Immediate);
            vm.leftRootNode = null;
            vm.rightRootNode = null;

            // should not throw
            vm.ConfirmClick.Execute().Subscribe();

            Assert.False(invoked);
        }

        [Fact]
        public void NewRow_Triggers_AddRowInView_Interaction()
        {
            var main = new MainWindowViewModel();
            var vm = new SandboxPageViewModel(main, onConfirm: null, outputScheduler: Scheduler.Immediate);

            bool handlerCalled = false;
            vm.AddRowInView.RegisterHandler(h =>
            {
                handlerCalled = true;
                h.SetOutput(Unit.Default);
            });

            var testNode = new TestNode();
            vm.NewRow.Execute(testNode).Subscribe();

            Assert.True(handlerCalled);
        }

        [Fact]
        public void NewNode_Triggers_AddNodeInView_Interaction()
        {
            var main = new MainWindowViewModel();
            var vm = new SandboxPageViewModel(main, onConfirm: null, outputScheduler: Scheduler.Immediate);

            bool handlerCalled = false;
            vm.AddNodeInView.RegisterHandler(h =>
            {
                handlerCalled = true;
                h.SetOutput(Unit.Default);
            });

            var testNode = new TestNode();
            vm.NewNode.Execute(testNode).Subscribe();

            Assert.True(handlerCalled);
        }

        [Fact]
        public void ConfirmDiagram_Produces_Correct_PerRow_Arrays_For_NontrivialTree()
        {
            //root total = 4
            //level 1 row: [4]
            //level 2 row: [2,2]
            //level 3 row: [1,1,1,1]
            var leftRoot = new TestNode();
            var a = new TestNode();
            var b = new TestNode();
            a.AddChild(new TestNode());
            a.AddChild(new TestNode());
            b.AddChild(new TestNode());
            b.AddChild(new TestNode());
            leftRoot.AddChild(a);
            leftRoot.AddChild(b);

            var rightRoot = new TestNode();
            var c = new TestNode();
            var d = new TestNode();
            c.AddChild(new TestNode());
            c.AddChild(new TestNode());
            d.AddChild(new TestNode());
            d.AddChild(new TestNode());
            rightRoot.AddChild(c);
            rightRoot.AddChild(d);

            List<int[][]>? captured = null;
            var main = new MainWindowViewModel();
            var vm = new SandboxPageViewModel(main, onConfirm: s => captured = s, outputScheduler: Scheduler.Immediate);
            vm.leftRootNode = leftRoot;
            vm.rightRootNode = rightRoot;

            vm.ConfirmClick.Execute().Subscribe();

            Assert.NotNull(captured);
            var left = captured![0];
            //expected left[0] contains root total 4
            Assert.Equal(4, left[0][0]);
            //expected second row [2,2]
            Assert.Contains(left, arr => arr.SequenceEqual(new[] { 2, 2 }));
            //verify the flattened totals equal 4
            var summed = left.Select(arr => arr.Sum()).Where(s => s > 0).ToArray();
            Assert.Contains(4, summed);
        }
    }
}

