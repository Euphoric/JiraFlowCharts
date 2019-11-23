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
                throw new NotSupportedException("Use differnet test class.");
            }

            public List<CachedIssue> Issues { get; set; } = new List<CachedIssue>();

            public Task<List<CachedIssue>> GetIssues()
            {
                return Task.FromResult(Issues);
            }

            public Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress)
            {
                throw new NotSupportedException("Use differnet test class.");
            }
        }

        private readonly TestJiraCacheAdapter _jiraCacheAdapter;
        private readonly MemoryStatesRepository _statesRepository;
        private readonly TasksSource _tasksSource;
        private readonly CompareLogic _compareLogic;

        public TasksSourceIssueAnalysisTest()
        {
            _jiraCacheAdapter = new TestJiraCacheAdapter();
            _statesRepository = new MemoryStatesRepository(new string[0], new string[0]);
            _tasksSource = new TasksSource(_jiraCacheAdapter, _statesRepository);

            _compareLogic = new CompareLogic(new ComparisonConfig() { IgnoreObjectTypes = true, MaxDifferences = 3 });
        }

        [Fact]
        public async Task Retrieves_no_issues()
        {
            var issues = await _tasksSource.GetAllIssues();

            Assert.Empty(issues);
        }

        [Theory]
        [AutoFixture.Xunit2.InlineAutoData]
        public async Task Retrieves_issue(CachedIssue issue)
        {
            _jiraCacheAdapter.Issues.Add(issue);
            var issues = await _tasksSource.GetAllIssues();

            _compareLogic.AssertEqual<object>(issues, _jiraCacheAdapter.Issues);
        }

        [Fact]
        public async Task Analyzed_issue_contains_simplified_states()
        {
            _tasksSource.AddFilteredState("A");
            _tasksSource.AddFilteredState("C");
            _tasksSource.AddResetState("D");

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
            var issues = await _tasksSource.GetAllIssues();

            var analyzedIssue = Assert.Single(issues);

            var expectedIssue = new AnalyzedIssue()
            {
                StatusChanges = issue.StatusChanges,
                SimplifiedStatusChanges = new Collection<CachedIssueStatusChange>()
                {
                    new CachedIssueStatusChange(new DateTime(2012, 2, 1), "A"),
                    new CachedIssueStatusChange(new DateTime(2012, 2, 3), "C"),
                }
            };

            _compareLogic.AssertEqual<object>(expectedIssue, analyzedIssue);
        }
    }
}
