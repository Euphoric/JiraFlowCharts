using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jira.Querying;
using KellermanSoftware.CompareNetObjects;
using Xunit;

namespace Jira.FlowCharts
{
    public class TasksSourceIssueAnalysisTest
    {
        private class TestJiraCacheAdapter : ITasksSourceJiraCacheAdapter
        {
            public Task<string[]> GetAllStates()
            {
                throw new NotSupportedException("Use different test class.");
            }

            public List<CachedIssue> Issues { get; set; } = new List<CachedIssue>();

            public Task<List<CachedIssue>> GetIssues()
            {
                return Task.FromResult(Issues);
            }

            public Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress, DateTime startUpdateDate)
            {
                throw new NotSupportedException("Use different test class.");
            }
        }

        private readonly TestJiraCacheAdapter _jiraCacheAdapter;
        private readonly TasksSource _tasksSource;
        private readonly CompareLogic _compareLogic;

        public TasksSourceIssueAnalysisTest()
        {
            _jiraCacheAdapter = new TestJiraCacheAdapter();
            _tasksSource = new TasksSource(_jiraCacheAdapter);

            _compareLogic = new CompareLogic(new ComparisonConfig() { IgnoreObjectTypes = true, MaxDifferences = 3 });
        }

        [Fact]
        public async Task Retrieves_no_issues()
        {
            var stateFiltering = new StateFilteringParameter(new string[0], new string[0]);

            var issues = await _tasksSource.GetAllIssues(stateFiltering);

            Assert.Empty(issues);
        }

        [Theory]
        [AutoFixture.Xunit2.InlineAutoData]
        public async Task Retrieves_issue(CachedIssue issue)
        {
            var stateFiltering = new StateFilteringParameter(new string[0], new string[0]);

            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetAllIssues(stateFiltering);

            _compareLogic.AssertEqual<object>(issues, _jiraCacheAdapter.Issues);
        }

        [Fact]
        public async Task Analyzed_issue_contains_simplified_states()
        {
            var stateFiltering = new StateFilteringParameter(new []{"A", "C"}, new []{"D"});

            var issue = new CachedIssue()
            {
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 1, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 1, 2), "D"),

                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 2), "B"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 3), "C"),
                }
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetAllIssues(stateFiltering);

            var analyzedIssue = Assert.Single(issues);

            var expectedIssue = new AnalyzedIssue()
            {
                StatusChanges = issue.StatusChanges,
                SimplifiedStatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 3), "C"),
                },
                Started = new DateTime(2012, 2, 1),
                Ended = new DateTime(2012, 2, 3),
                Duration = TimeSpan.FromDays(2)
            };

            _compareLogic.AssertEqual<object>(expectedIssue, analyzedIssue);
        }

        [Fact]
        public async Task Non_started_issue_is_analyzed()
        {
            var stateFiltering = new StateFilteringParameter(new []{"B"}, new string[0]);

            var issue = new CachedIssue()
            {
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 1, 1), "A"),
                }
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetAllIssues(stateFiltering);

            var analyzedIssue = Assert.Single(issues);

            var expectedIssue = new AnalyzedIssue()
            {
                StatusChanges = issue.StatusChanges,
                SimplifiedStatusChanges = new Collection<CachedIssueStatusChange>()
                { },
                Started = null,
                Ended = null,
                Duration = null
            };

            _compareLogic.AssertEqual<object>(expectedIssue, analyzedIssue);
        }

        [Fact]
        public async Task Issue_that_doesnt_have_last_state_is_not_finished()
        {
            var stateFiltering = new StateFilteringParameter(new []{"A", "C"}, new string[0]);

            var issue = new CachedIssue()
            {
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 2), "B"),
                }
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetAllIssues(stateFiltering);

            var analyzedIssue = Assert.Single(issues);

            var expectedIssue = new AnalyzedIssue()
            {
                StatusChanges = issue.StatusChanges,
                SimplifiedStatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A")
                },
                Started = new DateTime(2012, 2, 1),
                Ended = null,
                Duration = null
            };

            _compareLogic.AssertEqual<object>(expectedIssue, analyzedIssue);
        }

        [Fact]
        public async Task Analyzed_as_valid_issue()
        {
            var stateFiltering = new StateFilteringParameter(new string[0], new string[0]);

            var issue = new CachedIssue()
            {
                Type = "Story",
                StatusChanges = new Collection<CachedIssueStatusChange>()
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetAllIssues(stateFiltering);

            var analyzedIssue = Assert.Single(issues);

            var expectedIssue = new AnalyzedIssue()
            {
                Type = "Story",
                StatusChanges = issue.StatusChanges,
                SimplifiedStatusChanges = new Collection<CachedIssueStatusChange>()
            };

            _compareLogic.AssertEqual<object>(expectedIssue, analyzedIssue);
        }

        [Fact]
        public async Task Finished_issue_is_analyzed()
        {
            var stateFiltering = new StateFilteringParameter(new []{"A", "C"}, new string[0]);

            var issue = new CachedIssue()
            {
                Key = "AC-1",
                Title = "Title",
                Type = "Story",
                StoryPoints = 12,
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 2), "B"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 3), "C"),
                }
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetLatestFinishedStories(new IssuesFromParameters(new DateTime(2012,1 , 1)), stateFiltering);

            var analyzedIssue = Assert.Single(issues);

            var expectedIssue = new FinishedIssue()
            {
                Key = issue.Key,
                Title = issue.Title,
                Type = issue.Type,
                StoryPoints = issue.StoryPoints,
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 3), "C"),
                },
                Started = new DateTime(2012, 2, 1),
                Ended = new DateTime(2012, 2, 3),
                Duration = TimeSpan.FromDays(2)
            };

            _compareLogic.AssertEqual(expectedIssue, analyzedIssue);
        }

        [Fact]
        public async Task Finished_story_must_be_valid()
        {
            var stateFiltering = new StateFilteringParameter(new []{"A", "C"}, new string[0]);

            var issue = new CachedIssue()
            {
                Type = "NotStory",
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 2), "B"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 3), "C"),
                }
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetLatestFinishedStories(new IssuesFromParameters(null), stateFiltering);

            Assert.Empty(issues);
        }


        [Fact]
        public async Task Finished_story_must_have_start()
        {
            var stateFiltering = new StateFilteringParameter(new []{"A", "C"}, new string[0]);

            var issue = new CachedIssue()
            {
                Type = "Story",
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 2), "B"),
                }
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetLatestFinishedStories(new IssuesFromParameters(null), stateFiltering);

            Assert.Empty(issues);
        }

        [Fact]
        public async Task Finished_story_must_have_end()
        {
            var stateFiltering = new StateFilteringParameter(new []{"A", "C"}, new string[0]);

            var issue = new CachedIssue()
            {
                Type = "Story",
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 2), "B"),
                }
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetLatestFinishedStories(new IssuesFromParameters(null), stateFiltering);

            Assert.Empty(issues);
        }

        [Fact]
        public async Task Filters_out_old_issues()
        {
            var stateFiltering = new StateFilteringParameter(new []{"A", "C"}, new string[0]);
            var issuesFrom = new DateTime(2012, 2, 3).AddSeconds(1);

            var issue = new CachedIssue()
            {
                Key = "AC-1",
                Title = "Title",
                Type = "Story",
                StoryPoints = 12,
                StatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 2), "B"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 3), "C"),
                }
            };
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetLatestFinishedStories(new IssuesFromParameters(issuesFrom), stateFiltering);

            Assert.Empty(issues);
        }
    }
}
