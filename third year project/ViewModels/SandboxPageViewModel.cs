using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using third_year_project.Services;

namespace third_year_project.ViewModels
{
    internal class SandboxPageViewModel : ReactiveObject
    {
        public ReactiveCommand<Unit, Unit> HomeClick { get; }

        public ReactiveCommand<Side, Unit> NewRow { get; }
        public ReactiveCommand<NodeCreationArgs, Unit> NewNode { get; }

        public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

        private int leftRows, rightRows = 1;

        public Interaction<string, Unit> AddNodeInView { get; }
        public Interaction<string, Unit> AddRowInView { get; }

        public Interaction<string, Unit> ConfirmInView { get; }

        public SandboxPageViewModel(MainWindowViewModel mainWindowVM)
        {
            HomeClick = ReactiveCommand.Create(() =>
            {
                //Console.WriteLine("Returning to home page");
                mainWindowVM.CurrentPage = new HomePageViewModel(mainWindowVM);
            }, outputScheduler: AvaloniaScheduler.Instance);

            AddNodeInView = new Interaction<string, Unit>();
            AddRowInView = new Interaction<string, Unit>();
            ConfirmInView = new Interaction<string, Unit>();

            NewRow = ReactiveCommand.Create<Side>(async side =>
            {
                await AddRowInView.Handle($"{(int)side}");
            }, outputScheduler: AvaloniaScheduler.Instance);

            NewNode = ReactiveCommand.Create<NodeCreationArgs>(async args =>
            {
                await AddNodeInView.Handle($"{(int)args.side}{args.row}");
            }, outputScheduler: AvaloniaScheduler.Instance);

            ConfirmCommand = ReactiveCommand.Create<Unit>(async args => {
                await ConfirmInView.Handle("");
            }, outputScheduler: AvaloniaScheduler.Instance);
        }
    }

    public class NodeCreationArgs
    {
        public Side side { get; init; }
        public int row { get; init; }
    } //this is some avalonia tech that wouldve been helpful to know months ago but now im actually getting good
}
