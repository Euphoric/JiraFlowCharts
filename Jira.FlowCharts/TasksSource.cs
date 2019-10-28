using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Jira.Querying;
using Jira.Querying.Sqlite;

namespace Jira.FlowCharts
{
    public class TasksSource
    {
        private readonly ITasksSourceJiraCacheAdapter _jiraCache;

        public string[] States { get; }
        public string[] ResetStates { get; }

        public TasksSource(ITasksSourceJiraCacheAdapter jiraCacheAdapter)
        {
            _jiraCache = jiraCacheAdapter;

            States = new[] { "Ready For Dev", "In Dev", "Ready for Peer Review", "Ready for QA", "In QA", "Ready for Done", "Done" };
            ResetStates = new[] { "On Hold", "Not Started", "Withdrawn" };
        }

        public async Task<IEnumerable<CachedIssue>> GetAllIssues()
        {
            return await _jiraCache.GetIssues();
        }

        public async Task<IEnumerable<CachedIssue>> GetStories()
        {
            var issues = await GetAllIssues();

            IEnumerable<CachedIssue> stories = issues
                .Where(x => x.Type == "Story" || x.Type == "Bug")
                .Where(x => x.Resolution != "Cancelled" && x.Resolution != "Duplicate")
                .Where(x => x.Status != "Withdrawn" && x.Status != "On Hold");

            return stories;
        }

        public async Task<FinishedTask[]> GetFinishedStories()
        {
            IEnumerable<CachedIssue> stories = await GetStories();

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(States, ResetStates);

            DateTime startDate = DateTime.Now.AddMonths(-12);

            FinishedTask[] finishedStories = stories
                .Where(x => x.Status == "Done")
                .Select(x => CalculateDuration(x, simplify))
                .Where(x => x.End > startDate).ToArray();

            return finishedStories;
        }

        private static FinishedTask CalculateDuration(CachedIssue issue, SimplifyStateChangeOrder simplify)
        {
            var simplifiedIssues = simplify.FilterStatusChanges(issue.StatusChanges);

            var startTime = simplifiedIssues.First().ChangeTime;
            var doneTime = simplifiedIssues.Last().ChangeTime;

            TimeSpan duration = doneTime - startTime;

            var flowIssue = new FinishedTask()
            {
                Key = issue.Key,
                Title = issue.Title,
                Type = issue.Type,
                Start = startTime,
                End = doneTime,
                Duration = duration.TotalDays,
                StoryPoints = issue.StoryPoints,
                TimeSpent = issue.TimeSpent
            };

            return flowIssue;
        }

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters)
        {
            await _jiraCache.UpdateIssues(jiraLoginParameters);
        }
    }
}