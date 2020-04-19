using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jira.Querying;
using Xunit;

namespace Jira.FlowCharts
{
    public class TestJiraCacheAdapter : ITasksSourceJiraCacheAdapter
    {
        public JiraLoginParameters ExpectedLoginParameters { get; internal set; }

        readonly List<CachedIssue> _issues = new List<CachedIssue>();

        public Task<List<CachedIssue>> GetIssues()
        {
            return Task.FromResult(_issues);
        }

        public Task<List<CachedIssue>> GetIssues(string projectKey)
        {
            return Task.FromResult(_issues);
        }

        public List<CachedIssue> IssuesToUpdateWith { get; } = new List<CachedIssue>();

        public string ExpectedProjectKey { get; internal set; }

        public Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress, DateTime startUpdateDate)
        {
            if (ExpectedLoginParameters != null)
            {
                Assert.Equal(ExpectedLoginParameters.JiraUrl, jiraLoginParameters.JiraUrl);
                Assert.Equal(ExpectedLoginParameters.JiraUsername, jiraLoginParameters.JiraUsername);
                Assert.Equal(ExpectedLoginParameters.JiraPassword, jiraLoginParameters.JiraPassword);
                Assert.Equal(ExpectedProjectKey, projectKey);
            }

            foreach (var item in IssuesToUpdateWith)
            {
                cacheUpdateProgress.UpdatedIssue(item.Key, item.Updated.Value);
            }

            _issues.AddRange(IssuesToUpdateWith);
            IssuesToUpdateWith.Clear();

            return Task.CompletedTask;
        }

        public string[] AllStates { get; set; } = new string[0];

        public Task<string[]> GetAllStates()
        {
            return Task.FromResult(AllStates);
        }
    }
}