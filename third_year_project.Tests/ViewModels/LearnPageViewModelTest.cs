using Avalonia;
using Avalonia.Controls;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using third_year_project.Controls;
using third_year_project.Services;
using third_year_project.ViewModels;

namespace third_year_project.Tests.ViewModels
{
    public class LearnPageViewModelTest
    {
        private static List<int[][]> MakeSimpleRhythm()
        {
            return new List<int[][]>
            {
                new int[][]
                {
                    new int[] {8},
                    new int[] {4,4},
                    new int[] {1,1,1,1,1,1,1,1}
                },
                new int[][]
                {
                    new int[] {7},
                    new int[] {4,3},
                    new int[] {1,1,1,1,1,1,1}
                }
            };
        }

        //create an instance without running ctor and inject required private fields using some tricks i found online
        private static LearnPageViewModel CreateVmWithInjectedFields()
        {
            var vm = (LearnPageViewModel)FormatterServices.GetUninitializedObject(typeof(LearnPageViewModel));

            //create a tree like the vm expects
            var treeGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, *"),
                ColumnSpacing = 25
            };
            var treeBorder = new Border
            {
                Padding = new Thickness(25),
                Child = treeGrid
            };

            //create the clock
            var clockBorder = new Border
            {
                Padding = new Thickness(25)
            };

            var vmType = typeof(LearnPageViewModel);
            var treeField = vmType.GetField("tree", BindingFlags.NonPublic | BindingFlags.Instance);
            var clockField = vmType.GetField("clock", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(treeField);
            Assert.NotNull(clockField);

            treeField.SetValue(vm, treeBorder);
            clockField.SetValue(vm, clockBorder);

            return vm;
        }

        //for testing some switchable control logic
        private class TestSwitchableControl : Control, SwitchableControl
        {
            public bool WasSwitched { get; private set; }
            public void OnControlSwitched() => WasSwitched = true;
        }

        //create a view model with moclks
        private static LearnPageViewModel CreateVmWithMocks(out Mock<ISoundPlayer> mockSound, out Mock<IAppDispatcher> mockDispatcher, bool performAcquire = false)
        {
            mockSound = new Mock<ISoundPlayer>();
            mockDispatcher = new Mock<IAppDispatcher>();

            // Default setups used by VM constructor / StartBeepingCycles
            mockSound.Setup(s => s.TryAcquire(It.IsAny<object>())).Returns(true);
            mockSound.Setup(s => s.Initialize(It.IsAny<object>()));
            mockSound.Setup(s => s.Release(It.IsAny<object>()));
            mockSound.Setup(s => s.ScheduleNote(It.IsAny<object>(), It.IsAny<long>(), It.IsAny<Note>()));
            mockSound.Setup(s => s.MsToSample(It.IsAny<double>())).Returns<double>(ms => (long)(ms * 44.1));
            mockSound.Setup(s => s.GetCurrentSample()).Returns(0L);
            mockSound.Setup(s => s.Stop(It.IsAny<object>()));

            var mainVm = new MainWindowViewModel();
            var vm = new LearnPageViewModel(mainVm, MakeSimpleRhythm(), mockSound.Object, mockDispatcher.Object, performAcquire);

            return vm;
        }

        [Fact]
        public void SetRhythmToDisplaySetsPatterns()
        {
            //setup
            var rhythm = MakeSimpleRhythm();
            var vm = CreateVmWithInjectedFields();

            vm.setRhythmToDisplay(rhythm);

            Assert.Equal(new int[] { 4, 4 }, vm.GetLeftPattern());
            Assert.Equal(new int[] { 4, 3 }, vm.GetRightPattern());
        }

        [Fact]
        public void SetRhythmToDisplay_MakesATree()
        {
            var rhythm = MakeSimpleRhythm();
            var vm = CreateVmWithInjectedFields();

            vm.setRhythmToDisplay(rhythm);

            var treeField = typeof(LearnPageViewModel).GetField("tree", BindingFlags.NonPublic | BindingFlags.Instance);
            var treeBorder = (Border)treeField.GetValue(vm);
            var treeGrid = (Grid)treeBorder.Child;
            Assert.Equal(2, treeGrid.Children.Count);
            Assert.IsType<TreeDiagram>(treeGrid.Children[0]);
            Assert.IsType<TreeDiagram>(treeGrid.Children[1]);
        }

        [Fact]
        public void SetRhythmToDisplay_AttachesClockAndTreeDiagram()
        {
            var rhythm = MakeSimpleRhythm();
            var vm = CreateVmWithInjectedFields();

            vm.setRhythmToDisplay(rhythm);

            var clockField = typeof(LearnPageViewModel).GetField("clock", BindingFlags.NonPublic | BindingFlags.Instance);
            var clockBorder = (Border)clockField.GetValue(vm);

            var treeField = typeof(LearnPageViewModel).GetField("tree", BindingFlags.NonPublic | BindingFlags.Instance);
            var treeBorder = (Border)treeField.GetValue(vm);
            var treeGrid = (Grid)treeBorder.Child;

            Assert.NotNull(clockBorder.Child);
            Assert.IsType<ClockDiagram>(clockBorder.Child);

            Assert.NotNull(treeGrid.Children[0]);
            Assert.IsType<TreeDiagram>(treeGrid.Children[0]);
        }

        [Fact]
        public void SetRhythmToDisplay_ClearsPreviousChildren()
        {
            var rhythm = MakeSimpleRhythm();
            var vm = CreateVmWithInjectedFields();

            //call twice with different rhythms and ensure replacement
            vm.setRhythmToDisplay(rhythm);
            var firstTreeField = typeof(LearnPageViewModel).GetField("tree", BindingFlags.NonPublic | BindingFlags.Instance);
            var firstTreeGrid = (Grid)((Border)firstTreeField.GetValue(vm)).Child;
            Assert.Equal(2, firstTreeGrid.Children.Count);

            var altRhythm = new List<int[][]>
            {
                new int[][]
                {
                    new int[] {4}, new int[] {2, 2}, new int[] {1, 1, 1, 1}
                },
                new int[][]
                {
                    new int[] {5}, new int[] {3, 2}, new int[] {1, 1, 1, 1, 1}
                }
            };
            vm.setRhythmToDisplay(altRhythm);

            var secondTreeGrid = (Grid)((Border)firstTreeField.GetValue(vm)).Child;
            Assert.Equal(2, secondTreeGrid.Children.Count);
            Assert.IsType<TreeDiagram>(secondTreeGrid.Children[0]);
            Assert.IsType<TreeDiagram>(secondTreeGrid.Children[1]);
        }

        [Fact]
        public void SoundPlayed_Event_Posts_To_Dispatcher()
        {
            var mockSound = new Mock<ISoundPlayer>();
            var mockDispatcher = new Mock<IAppDispatcher>();
            var mainVm = new MainWindowViewModel();
            var rhythm = MakeSimpleRhythm();

            //build vm with mnock everything and dont call acquire so we can raise the event without worrying about ownership
            var vm = new LearnPageViewModel(mainVm, rhythm, mockSound.Object, mockDispatcher.Object, performAcquire: false);

            //do the thing in sound player
            mockSound.Raise(s => s.SoundPlayed += null, 440.0);

            mockDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
        }

        [Fact]
        public void StartBeepingCycles_Calls_Initialize_And_StopBeepingCalls_Stop()
        {
            var mockSound = new Mock<ISoundPlayer>();
            var mockDispatcher = new Mock<IAppDispatcher>();
            var mainVm = new MainWindowViewModel();
            var rhythm = MakeSimpleRhythm();

            var vm = new LearnPageViewModel(mainVm, rhythm, mockSound.Object, mockDispatcher.Object, performAcquire: false);

            vm.StartBeepingCycles();

            //make sure were only starting initially one time (we were apparently starting twice for this entire project :D )
            mockSound.VerifyAdd(m => m.SoundPlayed += It.IsAny<Action<double>>(), Times.Once);

            mockSound.Verify(s => s.Initialize(It.IsAny<object>()), Times.Once);

            vm.StopBeepingCycles();
            mockSound.Verify(s => s.Stop(It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public void HomeClick_And_PracticeClick_Change_MainWindowPage_And_Release_SoundPlayer_On_Practice()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);
            var mainVm = new MainWindowViewModel();

            var vm2 = new LearnPageViewModel(mainVm, MakeSimpleRhythm(), mockSound.Object, mockDispatcher.Object, performAcquire: false);

            vm2.HomeClick.Execute().Subscribe();
            Assert.IsType<HomePageViewModel>(mainVm.CurrentPage);

            mockSound.Invocations.Clear(); //clear previous invocations to isolate the next part of the test
            mockSound.Setup(s => s.Release(It.IsAny<object>())).Verifiable();

            vm2.PracticeClick.Execute().Subscribe();

            mockSound.Verify(s => s.Release(It.IsAny<object>()), Times.Once);
            Assert.IsType<PracticePageViewModel>(mainVm.CurrentPage);
        }

        [Fact]
        public void OnViewClosed_Should_Stop_And_Release_SoundPlayer()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);
            vm.StartBeepingCycles();

            mockSound.Invocations.Clear();
            mockSound.Setup(s => s.Stop(It.IsAny<object>())).Verifiable();
            mockSound.Setup(s => s.Release(It.IsAny<object>())).Verifiable();

            vm.OnViewClosed();

            mockSound.Verify(s => s.Stop(It.IsAny<object>()), Times.Once);
            mockSound.Verify(s => s.Release(It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public void SoundPlayedWhenRaisedUsesDispatcherAndSubscriptionIsAdded()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);

            //verify vm subscribes to soundplayed exactly one time
            mockSound.VerifyAdd(s => s.SoundPlayed += It.IsAny<Action<double>>(), Times.Once);

            mockDispatcher.Setup(d => d.Post(It.IsAny<Action>())).Verifiable();

            mockSound.Raise(s => s.SoundPlayed += null, 440.0);

            mockDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
        }

        [Fact] //this actually wasnt passing either as i had coded things in a weird way. fixed now tho
        public void SoundPlayedAfterOnViewClosedShouldNotInvokeDispatcher()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);

            //make sure dispatcher is being called before the view is cloased
            mockDispatcher.Setup(d => d.Post(It.IsAny<Action>())).Verifiable();
            mockSound.Raise(s => s.SoundPlayed += null, 440.0);
            mockDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
            mockDispatcher.Invocations.Clear();

            vm.OnViewClosed();

            //make sure on view closed unsubscribes from things properly
            mockSound.Raise(s => s.SoundPlayed += null, 440.0);
            mockDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Never);
        }

        [Fact]
        public void BeepingStartsOnLoad()
        {
            var mainVm = new MainWindowViewModel();
            var rhythm = MakeSimpleRhythm();

            var mockSound = new Mock<ISoundPlayer>();
            var mockDispatcher = new Mock<IAppDispatcher>();
            mockSound.Setup(s => s.TryAcquire(It.IsAny<object>())).Returns(true);
            mockSound.Setup(s => s.Initialize(It.IsAny<object>()));
            mockSound.Setup(s => s.MsToSample(It.IsAny<double>())).Returns<double>(ms => (long)(ms * 44.1));
            mockSound.Setup(s => s.GetCurrentSample()).Returns(0L);
            mockSound.Setup(s => s.ScheduleNote(It.IsAny<object>(), It.IsAny<long>(), It.IsAny<Note>()));

            var learnVm = new LearnPageViewModel(mainVm, rhythm, mockSound.Object, mockDispatcher.Object, performAcquire: true);

            var learnLeftField = typeof(LearnPageViewModel).GetField("leftTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            var learnRightField = typeof(LearnPageViewModel).GetField("rightTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(learnLeftField);
            Assert.NotNull(learnRightField);

            var learnLeftTimer = learnLeftField.GetValue(learnVm);
            var learnRightTimer = learnRightField.GetValue(learnVm);
            Assert.NotNull(learnLeftTimer);
            Assert.NotNull(learnRightTimer);

        }
    }
}
