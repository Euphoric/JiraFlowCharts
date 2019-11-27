using Jira.Querying;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace Jira.FlowCharts.Test
{
    public class CumulativeFlowAnalysisTest
    {
        private class FlatIssueBuilder
        {
            readonly Dictionary<int, AnalyzedIssue> _issues = new Dictionary<int, AnalyzedIssue>();

            DateTime _currentDateTime = new DateTime(2010, 07, 03);

            internal IEnumerable<AnalyzedIssue> BuildIssues()
            {
                return _issues.Values;
            }

            internal void CreateIssue(int id)
            {
                if (_issues.ContainsKey(id))
                    return;

                var analyzedIssue = new AnalyzedIssue { Key = "IS-" + id, SimplifiedStatusChanges = new Collection<CachedIssueStatusChange>() };
                _issues.Add(id, analyzedIssue);
            }

            internal void UpdateIssue(int id, string state)
            {
                CreateIssue(id);
                _issues[id].SimplifiedStatusChanges.Add(new CachedIssueStatusChange(_currentDateTime, state));
            }

            internal void ForwardTime(TimeSpan time)
            {
                _currentDateTime = _currentDateTime.Add(time);
            }
        }

        private const string DevState = "In dev";
        private const string QaState = "In QA";
        private const string DoneState = "Done";

        [Fact]
        public void Empty_issues()
        {
            var builder = new FlatIssueBuilder();
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new []{ DevState, QaState });
            Assert.Empty(cfa.Changes);
            Assert.Equal(new[] { QaState, DevState }, cfa.States);
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

            Assert.Equal(new[] { QaState, DevState }, cfa.States);

            Assert.Equal(2, cfa.Changes.Length);

            var firstChange = cfa.Changes[0];
            Assert.Equal(new DateTime(2010, 07, 03), firstChange.Date);
            Assert.Equal(new int[] { 0, 1 }, firstChange.StateCounts);

            var secondChange = cfa.Changes[1];
            Assert.Equal(new DateTime(2010, 07, 04), secondChange.Date);
            Assert.Equal(new int[] { 1, 0 }, secondChange.StateCounts);
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

        [Fact]
        public void Changes_are_ordered_by_date()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(2, DevState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(1, QaState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(2, QaState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(3, DevState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState, QaState });

            Assert.Equal(5, cfa.Changes.Length);

            var dates = cfa.Changes.Select(x => x.Date).ToArray();

            Assert.Equal(dates.OrderBy(x => x), dates);
        }

        [Fact]
        public void Uses_simplified_status_changes()
        {
            var issues = new List<AnalyzedIssue>
            {
                new AnalyzedIssue
                {
                    Key = "IS-1",
                    StatusChanges = new Collection<CachedIssueStatusChange>()
                    {
                        new CachedIssueStatusChange(new DateTime(2019, 1, 1), DevState),
                        new CachedIssueStatusChange(new DateTime(2019, 1, 2), QaState),
                        new CachedIssueStatusChange(new DateTime(2019, 1, 3), DevState),
                        new CachedIssueStatusChange(new DateTime(2019, 1, 4), QaState),
                    },
                    SimplifiedStatusChanges = new Collection<CachedIssueStatusChange>()
                    {
                        new CachedIssueStatusChange(new DateTime(2019, 1, 1), DevState),
                        new CachedIssueStatusChange(new DateTime(2019, 1, 2), QaState)
                    }
                }
            };
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(issues, new[] { DevState, QaState, DoneState });

            Assert.Equal(2, cfa.Changes.Length);

            var firstChange = cfa.Changes[0];
            Assert.Equal(new DateTime(2019, 1, 1), firstChange.Date);
            Assert.Equal(new int[] { 0, 0, 1 }, firstChange.StateCounts);

            var secondChange = cfa.Changes[1];
            Assert.Equal(new DateTime(2019, 1, 2), secondChange.Date);
            Assert.Equal(new int[] { 0, 1, 0 }, secondChange.StateCounts);
        }

        [Fact]
        public void Issue_that_skips_state()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, QaState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState, QaState, DoneState });

            var change = Assert.Single(cfa.Changes);
            Assert.Equal(new DateTime(2010, 07, 03), change.Date);
            Assert.Equal(new int[] { 0, 1, 0 }, change.StateCounts);
        }

        [Fact]
        public void Issue_that_skips_state_while_other_issues_are_in_progress()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(4, DevState);
            builder.UpdateIssue(5, DevState);
            builder.UpdateIssue(6, DevState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(4, QaState);
            builder.UpdateIssue(5, QaState);
            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(4, DoneState);

            builder.ForwardTime(TimeSpan.FromDays(1));
            builder.UpdateIssue(1, QaState);

            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState, QaState, DoneState });

            Assert.Equal(4, cfa.Changes.Length);

            var allStates = cfa.Changes[2];
            Assert.Equal(new DateTime(2010, 07, 05), allStates.Date);
            Assert.Equal(new int[] { 1, 1, 1 }, allStates.StateCounts);

            var skippedDevChange = cfa.Changes[3];
            Assert.Equal(new DateTime(2010, 07, 06), skippedDevChange.Date);
            Assert.Equal(new int[] { 1, 2, 1 }, skippedDevChange.StateCounts);
        }

        [Fact]
        public void From_date_filter_results_in_empty_changes()
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState, QaState, DoneState }, new DateTime(2010, 07, 06));
            Assert.Empty(cfa.Changes);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public void From_date_shows_only_changes_from_given_date(int offsetDays)
        {
            var builder = new FlatIssueBuilder();
            builder.UpdateIssue(1, DevState);
            builder.ForwardTime(TimeSpan.FromDays(offsetDays));
            builder.UpdateIssue(2, DevState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState, QaState, DoneState }, new DateTime(2010, 07, 05));

            var change = Assert.Single(cfa.Changes);
            Assert.Equal(new DateTime(2010, 07, 03+offsetDays), change.Date);
            Assert.Equal(new int[] { 0, 0, 2 }, change.StateCounts);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public void Done_issues_before_from_date_are_ignored(int doneStatesBefore)
        {
            var builder = new FlatIssueBuilder();
            for (int i = 0; i < doneStatesBefore; i++)
            {
                builder.UpdateIssue(i, DoneState);
            }
            builder.ForwardTime(TimeSpan.FromDays(3));
            builder.UpdateIssue(100, DoneState);
            CumulativeFlowAnalysis cfa = new CumulativeFlowAnalysis(builder.BuildIssues(), new[] { DevState, QaState, DoneState }, new DateTime(2010, 07, 05));

            var change = Assert.Single(cfa.Changes);
            Assert.Equal(new DateTime(2010, 07, 06), change.Date);
            Assert.Equal(new int[] { 1, 0, 0 }, change.StateCounts);
        }
    }
}
