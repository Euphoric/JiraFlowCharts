using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jira.Querying;
using Jira.Querying.Sqlite;

namespace Jira.FlowCharts
{
    public class TasksSource
    {
        public string[] States { get; }
        public string[] ResetStates { get; }

        public TasksSource()
        {
            States = new[] { "Ready For Dev", "In Dev", "Ready for Peer Review", "Ready for QA", "In QA", "Ready for Done", "Done" };
            ResetStates = new[] { "On Hold", "Not Started", "Withdrawn" };
        }

        private static async Task<List<CachedIssue>> RetrieveIssues()
        {
            using (var cacheRepo = new SqliteJiraLocalCacheRepository(@"../../../Data/issuesCache.db"))
            {
                await cacheRepo.Initialize();

                return (await cacheRepo.GetIssues()).ToList();
            }
        }

        public async Task<IEnumerable<CachedIssue>> GetAllTasks()
        {
            var issues = await RetrieveIssues();

            IEnumerable<CachedIssue> stories = issues
                .Where(x => x.Type == "Story" || x.Type == "Bug")
                .Where(x => x.Resolution != "Cancelled" && x.Resolution != "Duplicate")
                .Where(x => x.Status != "Withdrawn" && x.Status != "On Hold");

            return stories;
        }

        public async Task<FlowIssue[]> GetFinishedTasks()
        {
            IEnumerable<CachedIssue> stories = await GetAllTasks();

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(States, ResetStates);

            DateTime startDate = DateTime.Now.AddMonths(-12);

            FlowIssue[] finishedStories = stories
                .Where(x => x.Status == "Done")
                .Select(x => CalculateDuration(x, simplify))
                .Where(x => x.End > startDate).ToArray();

            return finishedStories;
        }

        private static FlowIssue CalculateDuration(CachedIssue issue, SimplifyStateChangeOrder simplify)
        {
            var simplifiedIssues = simplify.FilterStatusChanges(issue.StatusChanges);

            var startTime = simplifiedIssues.First().ChangeTime;
            var doneTime = simplifiedIssues.Last().ChangeTime;

            TimeSpan duration = doneTime - startTime;

            var flowIssue = new FlowIssue()
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
    }
}