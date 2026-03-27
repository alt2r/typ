using Avalonia.Threading;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using third_year_project.Services;
using third_year_project.ViewModels;

namespace third_year_project.Tests.ViewModels
{
    public class PracticePageViewModelTests
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

        private static PracticePageViewModel CreateVmWithMocks(out Mock<ISoundPlayer> mockSound, out Mock<IAppDispatcher> mockDispatcher, bool performAcquire = false)
        {
            mockSound = new Mock<ISoundPlayer>(MockBehavior.Strict);
            mockDispatcher = new Mock<IAppDispatcher>(MockBehavior.Strict);

            mockSound.Setup(s => s.TryAcquire(It.IsAny<object>())).Returns(true);
            mockSound.Setup(s => s.Initialize(It.IsAny<object>()));
            mockSound.Setup(s => s.Release(It.IsAny<object>()));
            mockSound.Setup(s => s.ScheduleNote(It.IsAny<object>(), It.IsAny<long>(), It.IsAny<Note>()));
            mockSound.Setup(s => s.MsToSample(It.IsAny<double>())).Returns<double>(ms => (long)(ms * 44.1));
            mockSound.Setup(s => s.GetCurrentSample()).Returns(0L);
            mockSound.Setup(s => s.Stop(It.IsAny<object>()));
            mockSound.Setup(s => s.PlayLiveNote(It.IsAny<Note>()));
            mockSound.Setup(s => s.SampleToMs(It.IsAny<long>())).Returns<long>(samples => samples / 44.1);

            var mainVm = new MainWindowViewModel();
            var vm = new PracticePageViewModel(mainVm, MakeSimpleRhythm(), mockSound.Object, mockDispatcher.Object, performAcquire);
            return vm;
        }

        private static T GetPrivateField<T>(object instance, string name)
        {
            var fi = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)fi.GetValue(instance);
        }

        private static void SetPrivateField<T>(object instance, string name, T value)
        {
            var fi = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            fi.SetValue(instance, value);
        }

        [Fact]
        public void StartBeepingCyclesCreatesTimersAndCallsInitialize()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);

            vm.StartBeepingCycles();

            mockSound.Verify(s => s.Initialize(It.IsAny<object>()), Times.Once);

            var leftTimer = GetPrivateField<DispatcherTimer>(vm, "leftTimer");
            var rightTimer = GetPrivateField<DispatcherTimer>(vm, "rightTimer");
            Assert.NotNull(leftTimer);
            Assert.NotNull(rightTimer);
        }

        [Fact]
        public void StartBeepingCyclesCallingTwiceDoesNotRemoveTimers()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);

            vm.StartBeepingCycles();
            vm.StartBeepingCycles(); //should not create duplicate timers - timers remain present

            var leftTimer = GetPrivateField<DispatcherTimer>(vm, "leftTimer");
            var rightTimer = GetPrivateField<DispatcherTimer>(vm, "rightTimer");
            Assert.NotNull(leftTimer);
            Assert.NotNull(rightTimer);

            mockSound.Verify(s => s.Initialize(It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public void StopBeepingCycles_CallsStopAndStopsTimers()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);

            vm.StartBeepingCycles();
            mockSound.Invocations.Clear();
            mockSound.Setup(s => s.Stop(It.IsAny<object>())).Verifiable();

            vm.StopBeepingCycles();

            mockSound.Verify(s => s.Stop(It.IsAny<object>()), Times.Once);

            var leftTimer = GetPrivateField<DispatcherTimer>(vm, "leftTimer");
            var rightTimer = GetPrivateField<DispatcherTimer>(vm, "rightTimer");
            Assert.Null(leftTimer);
            Assert.Null(rightTimer);
        }

        [Fact]
        public void StopBeepingCycles_CallsStopEvenIfNotStarted()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);

            mockSound.Invocations.Clear();
            mockSound.Setup(s => s.Stop(It.IsAny<object>())).Verifiable();

            vm.StopBeepingCycles();

            mockSound.Verify(s => s.Stop(It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public void ToggleLeft_Toggles_LeftMuted_And_Stops_And_Restarts()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);
            vm.StartBeepingCycles();
            var leftMutedBefore = vm.LeftMuted;

            vm.ToggleLeft();

            Assert.NotEqual(leftMutedBefore, vm.LeftMuted);
            var leftTimerAfterToggle = GetPrivateField<DispatcherTimer>(vm, "leftTimer");
            Assert.Null(leftTimerAfterToggle);

            vm.ToggleLeft();
            var leftTimerRestarted = GetPrivateField<DispatcherTimer>(vm, "leftTimer");
            Assert.NotNull(leftTimerRestarted);
        }

        [Fact]
        public void ToggleRightTogglesRightMutedAndStopsAndRestarts()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);
            vm.StartBeepingCycles();
            var rightMutedBefore = vm.RightMuted;

            vm.ToggleRight();

            Assert.NotEqual(rightMutedBefore, vm.RightMuted);
            var rightTimerAfterToggle = GetPrivateField<DispatcherTimer>(vm, "rightTimer");
            Assert.Null(rightTimerAfterToggle);

            vm.ToggleRight();
            var rightTimerRestarted = GetPrivateField<DispatcherTimer>(vm, "rightTimer");
            Assert.NotNull(rightTimerRestarted);
        }

        [Fact]
        public void SwapSidesSwapsPatternsAndRestartsCycles()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);

            var leftPatternBefore = GetPrivateField<int[]>(vm, "leftPattern");
            var rightPatternBefore = GetPrivateField<int[]>(vm, "rightPattern");

            vm.StartBeepingCycles();
            vm.SwapSides();

            var leftPatternAfter = GetPrivateField<int[]>(vm, "leftPattern");
            var rightPatternAfter = GetPrivateField<int[]>(vm, "rightPattern");

            Assert.Equal(rightPatternBefore, leftPatternAfter);
            Assert.Equal(leftPatternBefore, rightPatternAfter);

            //check timers are still runnig after swap
            var leftTimer = GetPrivateField<DispatcherTimer>(vm, "leftTimer");
            var rightTimer = GetPrivateField<DispatcherTimer>(vm, "rightTimer");
            Assert.NotNull(leftTimer);
            Assert.NotNull(rightTimer);
        }

        [Fact]
        public void OnKeyDownLeftCallsPlayLiveWhenNotPlaying()
        {
            var vm = CreateVmWithMocks(out var mockSound, out var mockDispatcher, performAcquire: false);

            //start sample wants to be 0. duh
            SetPrivateField(vm, "startSample", 0L);
            mockSound.Setup(s => s.GetCurrentSample()).Returns(10000L);

            SetPrivateField(vm, "leftOn", false);

            mockSound.Invocations.Clear();
            mockSound.Setup(s => s.PlayLiveNote(It.IsAny<Note>())).Verifiable();
            mockSound.Setup(s => s.MsToSample(It.IsAny<double>())).Returns<double>(ms => (long)(ms * 44.1));
            mockSound.Setup(s => s.SampleToMs(It.IsAny<long>())).Returns<long>(samples => samples / 44.1);

            var result = vm.OnKeyDown(vm.GetLeftKey());

            mockSound.Verify(s => s.PlayLiveNote(It.IsAny<Note>()), Times.Once);
            Assert.IsType<double>(result);
        }

        [Fact]
        public void BeepingStartsOnLoad()
        {
            var mainVm = new MainWindowViewModel();
            var rhythm = MakeSimpleRhythm();

            var mockSound2 = new Mock<ISoundPlayer>();
            var mockDispatcher2 = new Mock<IAppDispatcher>();
            mockSound2.Setup(s => s.TryAcquire(It.IsAny<object>())).Returns(true);
            mockSound2.Setup(s => s.Initialize(It.IsAny<object>()));
            mockSound2.Setup(s => s.MsToSample(It.IsAny<double>())).Returns<double>(ms => (long)(ms * 44.1));
            mockSound2.Setup(s => s.GetCurrentSample()).Returns(0L);
            mockSound2.Setup(s => s.ScheduleNote(It.IsAny<object>(), It.IsAny<long>(), It.IsAny<Note>()));

            var practiceVm = new PracticePageViewModel(mainVm, rhythm, mockSound2.Object, mockDispatcher2.Object, performAcquire: true);

            var pracLeftField = typeof(PracticePageViewModel).GetField("leftTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            var pracRightField = typeof(PracticePageViewModel).GetField("rightTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(pracLeftField);
            Assert.NotNull(pracRightField);

            var pracLeftTimer = pracLeftField.GetValue(practiceVm);
            var pracRightTimer = pracRightField.GetValue(practiceVm);
            Assert.NotNull(pracLeftTimer);
            Assert.NotNull(pracRightTimer);
        }
    }
}
