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
using System.Reactive.Concurrency;

namespace third_year_project.ViewModels
{
    public class SandboxPageViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> HomeClick { get; }

        public ReactiveCommand<Unit, Unit> ConfirmClick { get; }


        public ReactiveCommand<INode, Unit> NewRow { get; }
        public ReactiveCommand<INode, Unit> NewNode { get; }

        private int leftRows, rightRows = 1;

        public Interaction<INode, Unit> AddNodeInView { get; }
        public Interaction<INode, Unit> AddRowInView { get; }

        public INode? leftRootNode { get; set; }
        public INode? rightRootNode { get; set; }

        public MainWindowViewModel mwvm;

        private readonly Action<List<int[][]>>? onConfirm;

        private bool isNotValidDiagram = false;
        public bool IsNotValidDiagram
        {
            get => isNotValidDiagram;
            set => this.RaiseAndSetIfChanged(ref isNotValidDiagram, value);
        }

        public SandboxPageViewModel(MainWindowViewModel mainWindowVM, Action<List<int[][]>>? onConfirm = null, IScheduler? outputScheduler = null)
        {
            var scheduler = outputScheduler ?? AvaloniaScheduler.Instance;

            HomeClick = ReactiveCommand.Create(() =>
            {
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: scheduler);

            this.onConfirm = onConfirm;

            ConfirmClick = ReactiveCommand.Create(() =>
            {
                ConfirmDiagram();
            }, outputScheduler: scheduler);

            AddNodeInView = new Interaction<INode, Unit>();
            AddRowInView = new Interaction<INode, Unit>();

            NewRow = ReactiveCommand.Create<INode>(async args =>
            {
                await AddRowInView.Handle(args);
            }, outputScheduler: scheduler);

            NewNode = ReactiveCommand.Create<INode>(async args =>
            {
                await AddNodeInView.Handle(args);
            }, outputScheduler: scheduler);

            mwvm = mainWindowVM;
        }

        private void ConfirmDiagram()
        {
            if (leftRootNode == null || rightRootNode == null)
            {
                return;
            }
            List<List<int>> leftStructure = new List<List<int>>();
            leftStructure.Add(new List<int>());
            leftStructure[0].Add(leftRootNode.GetTotalChildrenCount());
            leftStructure = GetChildrenRecursive(leftRootNode, leftStructure, 1);

            List<List<int>> rightStructure = new List<List<int>>();
            rightStructure.Add(new List<int>());
            rightStructure[0].Add(rightRootNode.GetTotalChildrenCount());
            rightStructure = GetChildrenRecursive(rightRootNode, rightStructure, 1);

            //trim the trailing empty rows added by the recursive builder
            TrimEmptyRows(leftStructure);
            TrimEmptyRows(rightStructure);

            bool validDiagram = true;
            foreach (List<int> row in leftStructure) //check they all add up to the same value
            {
                if(row.Sum() != leftRootNode.GetTotalChildrenCount() && row.Sum() != 0)
                {
                    validDiagram = false;
                }
                // keep existing heuristic; consider revisiting this logic in future
            }

            foreach (List<int> row in rightStructure) //check they all add up to the same value
            {
                if (row.Sum() != rightRootNode.GetTotalChildrenCount() && row.Sum() != 0)
                {
                    validDiagram = false;
                }
            }
            if(leftStructure.Count != rightStructure.Count)
            {
                validDiagram = false;
            }
            if(leftStructure.Count < 2)
            {
                validDiagram = false;
            }

            if(validDiagram)
            {
                List<int[][]> structure = new List<int[][]>();
                List<int[]> left = new List<int[]>();
                foreach(List<int> row in leftStructure)
                {
                    if(row.Count > 0)
                        left.Add(row.ToArray());
                }
                structure.Add(left.ToArray());

                List<int[]> right = new List<int[]>();
                foreach (List<int> row in rightStructure)
                {
                    if(row.Count > 0)
                        right.Add(row.ToArray());
                }
                structure.Add(right.ToArray());

                if (onConfirm != null)
                {
                    onConfirm(structure);
                }
                else
                {
                    mwvm.CurrentPage = new LearnPageViewModel(mwvm, structure);
                }
            }
            else
            {
                IsNotValidDiagram = true;
            }
        }

        private void TrimEmptyRows(List<List<int>> struc)
        {
            //remove trailing empty rows that were introduced by recursion but contain no data
            while (struc.Count > 0 && struc.Last().Count == 0)
            {
                struc.RemoveAt(struc.Count - 1);
            }
        }

        private List<List<int>> GetChildrenRecursive(INode node, List<List<int>> struc, int depth)
        {
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
