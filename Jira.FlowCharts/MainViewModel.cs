using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Jira.Querying;
using Jira.Querying.Sqlite;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private async Task InitializeAsync()
        {
            var issues = await RetrieveIssues();

            var stories = issues
                .Where(x => x.Type == "Story" || x.Type == "Bug")
                .Where(x => x.Resolution != "Cancelled" && x.Resolution != "Duplicate")
                .Where(x => x.Status != "Withdrawn" && x.Status != "On Hold");

            var states = new[] { "Ready For Dev", "In Dev", "Ready for Peer Review", "Ready for QA", "In QA", "Ready for Done", "Done" };
            var resetStates = new[] { "On Hold", "Not Started", "Withdrawn" };
            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(states, resetStates);

            DateTime startDate = DateTime.Now.AddMonths(-12);

            var flowTasks = stories
                .Select(x => CreateFlowIssue(x, simplify))
                .Where(x => x.End > startDate)
                .ToArray();

            Items.Add(new CumulativeFlowViewModel(stories, states));
            Items.Add(new CycleTimeScatterplotViewModel(flowTasks));
            Items.Add(new CycleTimeHistogramViewModel(flowTasks));
            Items.Add(new StoryPointCycleTimeViewModel(flowTasks));
            Items.Add(new SimulationViewModel(flowTasks));

            //List<double> daysItTakesToFinish = new List<double>();
            //for (int i = -200; i < 0; i++)
            //{
            //    var sinceTime = DateTime.Now.AddDays(i);

            //    var storiesFinishedSince = finishedStories.Where(x => x.Start > sinceTime).OrderBy(x => x.End).ToArray();
            //    if (storiesFinishedSince.Length < 10)
            //        break;
            //    var lastFinishedStory = storiesFinishedSince.Take(10).Last();

            //    var timeItTakesToFinishStories = (lastFinishedStory.End - sinceTime).TotalDays;
            //    daysItTakesToFinish.Add(timeItTakesToFinishStories);
            //}
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
        }

        private static FlowIssue CreateFlowIssue(CachedIssue issue, SimplifyStateChangeOrder simplify)
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
                TimeSpent = issue.TimeSpent,
                IsDone = issue.Status == "Done"
            };

            return flowIssue;
        }

        private static async Task<List<CachedIssue>> RetrieveIssues()
        {
            using (var cacheRepo = new SqliteJiraLocalCacheRepository(@"../../../Data/issuesCache.db"))
            {
                await cacheRepo.Initialize();

                return (await cacheRepo.GetIssues()).ToList();
            }
        }
    }
}
