using System;
using System.Collections.Generic;
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
    }
}
