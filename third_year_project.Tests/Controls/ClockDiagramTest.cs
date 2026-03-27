using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using third_year_project.Controls;
using Xunit;

namespace third_year_project.Tests.Controls
{
    public class ClockDiagramTests
    {
        private static object GetPrivateField(object instance, string name)
        {
            var t = instance.GetType();
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) throw new InvalidOperationException($"Field '{name}' not found on {t.FullName}");
            return f.GetValue(instance);
        }

        //private static void SetPrivateField(object instance, string name, object value)
        //{
        //    var t = instance.GetType();
        //    var f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        //    if (f == null) throw new InvalidOperationException($"Field '{name}' not found on {t.FullName}");
        //    f.SetValue(instance, value);
        //}

        [Fact]
        public void MeasureReturnsCorrectSize()
        {
            var structure = new int[][] { new[] { 4, 4 }, new[] { 4, 4 } };
            var notes = new string[] { "c4", "f4" };
            var cd = new ClockDiagram(structure, notes);

            //measure is public and it calls measureoverride automatically
            cd.Measure(new Size(400, 300));

            Assert.Equal(400, cd.DesiredSize.Width, 3); // tolerance via integer equality
            Assert.Equal(300, cd.DesiredSize.Height, 3);
        }

        [Fact]
        public void MeasureReturnsDefaultSizeForInfiniteAvailableSize()
        {
            var structure = new int[][] { new[] { 3 }, new[] { 4 } };
            var notes = new string[] { "c4", "f4" };
            var cd = new ClockDiagram(structure, notes);

            cd.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(100, cd.DesiredSize.Width, 3);
            Assert.Equal(100, cd.DesiredSize.Height, 3);
        }

        [Fact]
        public void ConstructorSetsBeatCountsMatchingStructure()
        {
            var structure = new int[][] { new[] { 1, 2, 3 }, new[] { 4, 1 } };
            var notes = new string[] { "c4", "f4" };
            var cd = new ClockDiagram(structure, notes);

            var beatCountsObj = GetPrivateField(cd, "beatCounts");
            var beatCounts = (System.Collections.IList)beatCountsObj;

            Assert.Equal(structure.Length, beatCounts.Count);
            Assert.Equal(structure[0].Sum(), Convert.ToInt32(beatCounts[0]));
            Assert.Equal(structure[1].Sum(), Convert.ToInt32(beatCounts[1]));
        }

        [Fact]
        public void OnControlSwitchedResetsThetasToTop()
        {
            var structure = new int[][] { new[] { 4, 4 }, new[] { 4 } };
            var notes = new string[] { "c4", "f4" };
            var cd = new ClockDiagram(structure, notes);

            var thetasObj = GetPrivateField(cd, "thetas");
            var thetas = (System.Collections.IList)thetasObj;
            for (int i = 0; i < thetas.Count; i++)
            {
                thetas[i] = 0.0f;
            }
            cd.OnControlSwitched();

            double expected = -0.5 * Math.PI;
            for (int i = 0; i < thetas.Count; i++)
            {
                var val = Convert.ToDouble(thetas[i]);
                Assert.InRange(val, expected - 1e-6, expected + 1e-6);
            }
        }

        [Fact]
        public void SetBpm_UpdatesBpm()
        {
            var structure = new int[][] { new[] { 4 } };
            var notes = new string[] { "c4" };
            var cd = new ClockDiagram(structure, notes);

            cd.SetBpm(90);

            var bpmObj = GetPrivateField(cd, "bpm");
            var bpm = Convert.ToInt32(bpmObj);
            Assert.Equal(90, bpm);
        }
    }
}
