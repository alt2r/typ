using global::third_year_project.ViewModels;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using third_year_project.ViewModels;
using Xunit;

namespace third_year_project.Tests
{
    public class LevelSelectViewModelTests
    {
        [Fact] //double checking is good
        public void HomeClickSetsCurrentPageToHomePage()
        {
            var main = new MainWindowViewModel();
            var vm = new LevelSelectViewModel(main, learnPage: true, outputScheduler: Scheduler.Immediate);

            // Execute synchronously
            vm.HomeClick.Execute().Subscribe();

            Assert.IsType<HomePageViewModel>(main.CurrentPage);
        }

        [Fact] //figured we only need to test one level creation since the method is shared, and the levels are just data
        public void BuildRhythmHasExpectedStructure()
        {
            var rhythm = LevelSelectViewModel.BuildRhythm("1");

            Assert.Equal(2, rhythm.Count);

            Assert.Equal(3, rhythm[0].Length);
            Assert.Equal(3, rhythm[1].Length);
            Assert.Equal(6, rhythm[0][0][0]);

            Assert.Equal(new int[] { 3, 3 }, rhythm[0][1]);

            Assert.Equal(6, rhythm[0][2].Length);
            Assert.All(rhythm[0][2], v => Assert.Equal(1, v));
        }

        [Fact]
        public void BuildRhythmInvalidLevelFallsBackToDefaultPattern()
        {
            var rhythm = LevelSelectViewModel.BuildRhythm("not-a-level");

            Assert.Single(rhythm);

            Assert.Equal(16, rhythm[0][0][0]);
        }
    }
}

