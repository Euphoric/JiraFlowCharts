﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jira.Querying;
using Jira.Querying.Sqlite;
using ReactiveUI;

namespace Jira.FlowCharts
{
    public class MainViewModel : ReactiveObject
    {
        private CycleTimeScatterplotViewModel cycleTimeScatterplotViewModel;
        private CycleTimeHistogramViewModel cycleTimeHistogramViewModel;
        private StoryPointCycleTimeViewModel storyPointCycleTimeViewModel;
        private CumulativeFlowViewModel cumulativeFlowViewModel;
        private SimulationViewModel simulationViewModel;

        public async Task Initialize()
        {
            var issues = await RetrieveIssues();

            DateTime startDate = DateTime.Now.AddMonths(-12);

            var stories = issues
                .Where(x => x.Type == "Story" || x.Type == "Bug")
                .Where(x => x.Resolution != "Cancelled" && x.Resolution != "Duplicate")
                .Where(x => x.Status != "Withdrawn" && x.Status != "On Hold");

            var states = new[] { "Ready For Dev", "In Dev", "Ready for Peer Review", "Ready for QA", "In QA", "Ready for Done", "Done" };
            var resetStates = new[] { "On Hold", "Not Started", "Withdrawn" };
            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(states, resetStates);

            var finishedStories = stories
                .Where(x => x.Status == "Done")
                .Select(x => CalculateDuration(x, simplify))
                .Where(x => x.End > startDate).ToArray();

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

            CycleTimeScatterplotViewModel = new CycleTimeScatterplotViewModel(finishedStories);
            CycleTimeHistogramViewModel = new CycleTimeHistogramViewModel(finishedStories);
            StoryPointCycleTimeViewModel = new StoryPointCycleTimeViewModel(finishedStories);
            CumulativeFlowViewModel = new CumulativeFlowViewModel(stories, states);
            SimulationViewModel = new SimulationViewModel(finishedStories);
        }

        public CycleTimeScatterplotViewModel CycleTimeScatterplotViewModel { get => cycleTimeScatterplotViewModel; private set => this.RaiseAndSetIfChanged(ref cycleTimeScatterplotViewModel, value); }
        public CycleTimeHistogramViewModel CycleTimeHistogramViewModel { get => cycleTimeHistogramViewModel; private set => this.RaiseAndSetIfChanged(ref cycleTimeHistogramViewModel, value); }
        public StoryPointCycleTimeViewModel StoryPointCycleTimeViewModel { get => storyPointCycleTimeViewModel; private set => this.RaiseAndSetIfChanged(ref storyPointCycleTimeViewModel, value); }
        public CumulativeFlowViewModel CumulativeFlowViewModel { get => cumulativeFlowViewModel; private set => this.RaiseAndSetIfChanged(ref cumulativeFlowViewModel, value); }
        public SimulationViewModel SimulationViewModel { get => simulationViewModel; private set => this.RaiseAndSetIfChanged(ref simulationViewModel, value); }

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
