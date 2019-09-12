using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Jira.FlowCharts.Test
{
    public class CumulativeFlowAnalysisTest
    {
        private const string DevState = "In dev";

        [Fact]
        public void Empty_issues()
        {
            FlatIssue[] issues = new FlatIssue[0];
            CumulativeFlowAnalysis cfwm = new CumulativeFlowAnalysis(issues);
            Assert.Empty(cfwm.Changes);
            Assert.Empty(cfwm.States);
        }

        [Fact]
        public void Single_not_started_issue()
        {
            FlatIssue[] issues = new FlatIssue[]{
                new FlatIssue(){Key = "IS-1", StatusChanges = new Collection<FlatIssueStatusChange>()}
            };
            CumulativeFlowAnalysis cfwm = new CumulativeFlowAnalysis(issues);
            Assert.Empty(cfwm.Changes);
            Assert.Empty(cfwm.States);
        }

        [Fact]
        public void Single_started_issue()
        {
            FlatIssue[] issues = new FlatIssue[]{
                new FlatIssue(){Key = "IS-1", StatusChanges = new Collection<FlatIssueStatusChange>{ new FlatIssueStatusChange(new DateTime(2010, 07, 03), DevState) } }
            };
            CumulativeFlowAnalysis cfwm = new CumulativeFlowAnalysis(issues);

            Assert.Equal(new[] { DevState }, cfwm.States);

            var change = Assert.Single(cfwm.Changes);
            Assert.Equal(new DateTime(2010, 07, 03), change.Date);
            Assert.Equal(1, change.StateCounts[0]);
        }

        [Fact]
        public void Multiple_started_issues()
        {
            FlatIssue[] issues = new FlatIssue[]{
                new FlatIssue(){Key = "IS-1", StatusChanges = new Collection<FlatIssueStatusChange>{ new FlatIssueStatusChange(new DateTime(2010, 07, 03), DevState) } },
                new FlatIssue(){Key = "IS-2", StatusChanges = new Collection<FlatIssueStatusChange>{ new FlatIssueStatusChange(new DateTime(2010, 07, 04), DevState) } }
            };
            CumulativeFlowAnalysis cfwm = new CumulativeFlowAnalysis(issues);

            Assert.Equal(new[] { DevState }, cfwm.States);

            Assert.Equal(2, cfwm.Changes.Length);

            var firstChange = cfwm.Changes[0];
            Assert.Equal(new DateTime(2010, 07, 03), firstChange.Date);
            Assert.Equal(1, firstChange.StateCounts[0]);

            var secondChange = cfwm.Changes[1];
            Assert.Equal(new DateTime(2010, 07, 04), secondChange.Date);
            Assert.Equal(2, secondChange.StateCounts[0]);
        }
    }
}
