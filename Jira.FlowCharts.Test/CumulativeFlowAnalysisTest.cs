using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Jira.FlowCharts.Test
{
    public class CumulativeFlowAnalysisTest
    {
        private class FlatIssueBuilder
        {
            Dictionary<int, FlatIssue> issues = new Dictionary<int, FlatIssue>();

            DateTime currentDateTime = new DateTime(2010, 07, 03);

            internal IEnumerable<FlatIssue> BuildIssues()
            {
                return issues.Values;
            }

            internal void CreateIssue(int id)
            {
                if (issues.ContainsKey(id))
                    return;

                issues.Add(id, new FlatIssue() { Key = (string)("IS-" + id), StatusChanges = new Collection<FlatIssueStatusChange>() });
            }

            internal void UpdateIssue(int id, string state)
            {
                CreateIssue(id);
                issues[id].StatusChanges.Add(new FlatIssueStatusChange(currentDateTime, state));
            }

            internal void ForwardTime(TimeSpan time)
            {
                currentDateTime = currentDateTime.Add(time);
            }
        }

        private const string DevState = "In dev";
        private const string QaState = "In QA";

        [Fact]
        public void Empty_issues()
        {
            var builder = new FlatIssueBuilder();
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new []{ DevState, QaState });
            Assert.Empty(cfa.Changes);
            Assert.Equal(new[] { DevState, QaState }, cfa.States);
        }

        [Fact]
        public void Single_not_started_issue()
        {
            var builder = new FlatIssueBuilder();
            builder.CreateIssue(1);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState });
            Assert.Empty(cfa.Changes);
            Assert.Equal(new[] { DevState }, cfa.States);
        }

        [Fact]
        public void Single_started_issue()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState });

            Assert.Equal(new[] { DevState }, cfa.States);

            var change = Assert.Single(cfa.Changes);
            Assert.Equal(new DateTime(2010, 07, 03), change.Date);
            Assert.Equal(1, change.StateCounts[0]);
        }

        [Fact]
        public void Multiple_started_issues()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(2, DevState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState });

            Assert.Equal(new[] { DevState }, cfa.States);

            Assert.Equal(2, cfa.Changes.Length);

            var firstChange = cfa.Changes[0];
            Assert.Equal(new DateTime(2010, 07, 03), firstChange.Date);
            Assert.Equal(new int[] { 1 }, firstChange.StateCounts);

            var secondChange = cfa.Changes[1];
            Assert.Equal(new DateTime(2010, 07, 04), secondChange.Date);
            Assert.Equal(new int[] { 2 }, secondChange.StateCounts);
        }

        [Fact]
        public void First_issue_updated()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(1, QaState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState, QaState });

            Assert.Equal(new[] { DevState, QaState }, cfa.States);

            Assert.Equal(2, cfa.Changes.Length);

            var firstChange = cfa.Changes[0];
            Assert.Equal(new DateTime(2010, 07, 03), firstChange.Date);
            Assert.Equal(new int[] { 1, 0 }, firstChange.StateCounts);

            var secondChange = cfa.Changes[1];
            Assert.Equal(new DateTime(2010, 07, 04), secondChange.Date);
            Assert.Equal(new int[] { 0, 1 }, secondChange.StateCounts);
        }

        [Fact]
        public void Multiple_issues_in_same_day()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            builder.UpdateIssue(2, DevState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState });

            var firstChange =  Assert.Single(cfa.Changes);

            Assert.Equal(new DateTime(2010, 07, 03), firstChange.Date);
            Assert.Equal(new int[] { 2 }, firstChange.StateCounts);
        }

        [Fact]
        public void Groups_issues_in_single_day()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            builder.ForwardTime(TimeSpan.FromDays(0.5));
            builder.UpdateIssue(2, DevState);
            builder.ForwardTime(TimeSpan.FromDays(0.5));
            builder.UpdateIssue(3, DevState);
            builder.ForwardTime(TimeSpan.FromDays(0.5));
            builder.UpdateIssue(4, DevState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState });

            Assert.Equal(2, cfa.Changes.Length);

            var firstChange = cfa.Changes[0];
            Assert.Equal(new DateTime(2010, 07, 03), firstChange.Date);
            Assert.Equal(new int[] { 2 }, firstChange.StateCounts);

            var secondChange = cfa.Changes[1];
            Assert.Equal(new DateTime(2010, 07, 04), secondChange.Date);
            Assert.Equal(new int[] { 4 }, secondChange.StateCounts);
        }

        [Fact]
        public void Ignores_unspecified_state()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { QaState });

            Assert.Empty(cfa.Changes);
        }
    }
}
