using CSCore.Codecs.FLAC;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using third_year_project.Services;
using third_year_project.Views;

namespace third_year_project.ViewModels
{
    internal class SandboxPageViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> HomeClick { get; }

        public ReactiveCommand<Unit, Unit> ConfirmClick { get; }


        public ReactiveCommand<Node, Unit> NewRow { get; }
        public ReactiveCommand<Node, Unit> NewNode { get; }

        private int leftRows, rightRows = 1;

        public Interaction<Node, Unit> AddNodeInView { get; }
        public Interaction<Node, Unit> AddRowInView { get; }

        public Node leftRootNode { get; set; }
        public Node rightRootNode { get; set; }

        public MainWindowViewModel mwvm;

        public SandboxPageViewModel(MainWindowViewModel mainWindowVM)
        {
            HomeClick = ReactiveCommand.Create(() =>
            {
                //Console.WriteLine("Returning to home page");
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            ConfirmClick = ReactiveCommand.Create(() =>
            {
                ConfirmDiagram();
            }, outputScheduler: AvaloniaScheduler.Instance);

            AddNodeInView = new Interaction<Node, Unit>();
            AddRowInView = new Interaction<Node, Unit>();

            NewRow = ReactiveCommand.Create<Node>(async args =>
            {
                await AddRowInView.Handle(args);
            }, outputScheduler: AvaloniaScheduler.Instance);

            NewNode = ReactiveCommand.Create<Node>(async args =>
            {
                await AddNodeInView.Handle( args);
            }, outputScheduler: AvaloniaScheduler.Instance);

            mwvm = mainWindowVM;
        }

        private void ConfirmDiagram()
        {
            //validate the input TODO
            Console.WriteLine($"from vm children count is {leftRootNode.GetTotalChildrenCount()}");
            List<List<int>> leftStructure = new List<List<int>>();
            leftStructure.Add(new List<int>());
            leftStructure[0].Add(leftRootNode.GetTotalChildrenCount());
            leftStructure = GetChildrenRecursive(leftRootNode, leftStructure, 1);

            List<List<int>> rightStructure = new List<List<int>>();
            rightStructure.Add(new List<int>());
            rightStructure[0].Add(rightRootNode.GetTotalChildrenCount());
            rightStructure = GetChildrenRecursive(rightRootNode, rightStructure, 1);

            int rowCount = 0;
            bool validDiagram = true;
            foreach (List<int> row in leftStructure) //check they all add up to the same value
            {
                if(row.Sum() != leftRootNode.GetTotalChildrenCount() && row.Sum() != 0)
                {
                    validDiagram = false;
                }
                if(row.Count <= rowCount * 2 && row.Count != 0)
                {
                    validDiagram = false;
                    rowCount = row.Count;
                }
            }
            rowCount = 0;
            foreach (List<int> row in rightStructure) //check they all add up to the same value
            {
                if (row.Sum() != rightRootNode.GetTotalChildrenCount() && row.Sum() != 0)
                {
                    validDiagram = false;
                }
                if (row.Count <= rowCount * 2 && row.Count != 0)
                {
                    validDiagram = false;
                    rowCount = row.Count;
                }
            }
            if(leftStructure.Count != rightStructure.Count)
            {
                validDiagram = false;
            }
            if(leftStructure.Count < 3)
            {
                validDiagram = false;
            }

            if(validDiagram)
            {
                List<int[][]> structure = new List<int[][]>();
                List<int[]> left = [];
                foreach(List<int> row in leftStructure)
                {
                    if(row.Count > 0)
                    left.Add(row.ToArray());
                }
                structure.Add(left.ToArray());

                List<int[]> right = [];
                foreach (List<int> row in rightStructure)
                {
                    if(row.Count > 0)
                    right.Add(row.ToArray());
                }
                structure.Add(right.ToArray());
                mwvm.CurrentPage = new LearnPageViewModel(mwvm, structure);
            }
            else
            {
                Console.WriteLine("invalid trees");
            }
        }
        private List<List<int>> GetChildrenRecursive(Node node, List<List<int>> struc, int depth)
        {
            List<int> rowList = [];
            while (struc.Count <= depth)
            {
                struc.Add(new List<int>());
            }
            foreach (var child in node.GetChildren())
            {
                if (child.GetTotalChildrenCount() > 0)
                {
                    struc[depth].Add(child.GetTotalChildrenCount());
                }   
                struc = GetChildrenRecursive(child, struc, depth + 1);
               
            }
            return struc;
           
        }

    }


}
