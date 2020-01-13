using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jira.FlowCharts
{
    
    public class DurationPercentilesTest
    {
        [Fact]
        public void Empty_input_is_error()
        {
            var ex = Assert.Throws<ArgumentException>(() => new DurationPercentiles(new double[0]));
        }

        [Fact]
        public void Contains_input_durations()
        {
            var dp = new DurationPercentiles(new double[]{1, 2, 3});
            Assert.Equal(new double[] { 1, 2, 3 }, dp.Durations);
        }

        [Fact]
        public void Orders_durations()
        {
            var dp = new DurationPercentiles(new double[] { 3, 1, 2 });
            Assert.Equal(new double[] { 1, 2, 3 }, dp.Durations);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(0.5, 2)]
        [InlineData(1, 3)]
        public void Returns_exact_durations_for_exact_percentiles(double percentile, double expectedDuration)
        {
            var dp = new DurationPercentiles(new double[] { 3, 1, 2 });
            Assert.Equal(expectedDuration, dp.DurationAtPercentile(percentile));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(0.25, 3)]
        [InlineData(0.5, 5)]
        [InlineData(0.75, 6)]
        [InlineData(1, 8)]
        public void Returns_exact_durations_for_exact_percentiles_2(double percentile, double expectedDuration)
        {
            var dp = new DurationPercentiles(new double[] { 1, 3, 5, 6, 8 });
            Assert.Equal(expectedDuration, dp.DurationAtPercentile(percentile));
        }

        [Theory]
        [InlineData(0.1, 1)]
        [InlineData(0.2, 3)]
        [InlineData(0.3, 3)]
        public void Returns_exact_durations_for_inexact_percentiles(double percentile, double expectedDuration)
        {
            var dp = new DurationPercentiles(new double[] { 1, 3, 5, 6, 8 });
            Assert.Equal(expectedDuration, dp.DurationAtPercentile(percentile));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 0.5)]
        [InlineData(3, 1)]
        public void Returns_exact_percentiles_for_exact_durations(double duration, double expectedPercentile)
        {
            var dp = new DurationPercentiles(new double[] { 3, 1, 2 });
            Assert.Equal(expectedPercentile, dp.PercentileAtDuration(duration));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(3, 0.25)]
        [InlineData(5, 0.5)]
        [InlineData(6, 0.75)]
        [InlineData(8, 1)]
        public void Returns_exact_percentiles_for_exact_durations_2(double percentile, double expectedDuration)
        {
            var dp = new DurationPercentiles(new double[] { 1, 3, 5, 6, 8 });
            Assert.Equal(expectedDuration, dp.PercentileAtDuration(percentile));
        }

        [Theory]
        [InlineData(1.5, 0)]
        [InlineData(5.3, 0.5)]
        public void Returns_exact_percentiles_for_inexact_durations(double percentile, double expectedDuration)
        {
            var dp = new DurationPercentiles(new double[] { 1, 3, 5, 6, 8 });
            Assert.Equal(expectedDuration, dp.PercentileAtDuration(percentile));
        }

        [Theory]
        [InlineData(10, 1)]
        public void Percentile_over_maximum_is_1(double percentile, double expectedDuration)
        {
            var dp = new DurationPercentiles(new double[] { 1, 3, 5, 6, 8 });
            Assert.Equal(expectedDuration, dp.PercentileAtDuration(percentile));
        }
    }
}
