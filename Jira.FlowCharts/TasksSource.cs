using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Jira.Querying;

namespace Jira.FlowCharts
{
    public class TasksSource
    {
        private readonly ITasksSourceJiraCacheAdapter _jiraCache;
        private readonly IMapper _mapper;

        public TasksSource(ITasksSourceJiraCacheAdapter jiraCacheAdapter)
        {
            _jiraCache = jiraCacheAdapter;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CachedIssue, AnalyzedIssue>();
            });

            _mapper = config.CreateMapper();
        }

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectName, ICacheUpdateProgress cacheUpdateProgress)
        {
            await _jiraCache.UpdateIssues(jiraLoginParameters, projectName, cacheUpdateProgress);
        }

        public async Task<IEnumerable<AnalyzedIssue>> GetAllIssues(StateFilteringParameter stateFiltering)
        {
            List<CachedIssue> issues = await _jiraCache.GetIssues();

            List<AnalyzedIssue> analyzedIssues = _mapper.Map<List<AnalyzedIssue>>(issues);

            SimplifyStateChangeOrder simplify =
                new SimplifyStateChangeOrder(stateFiltering.FilteredStates, stateFiltering.ResetStates);
            var finishedState = stateFiltering.FilteredStates.LastOrDefault();

            foreach (var item in analyzedIssues)
            {
                item.SimplifiedStatusChanges =
                    new Collection<CachedIssueStatusChange>(simplify.FilterStatusChanges(item.StatusChanges).ToList());

                item.Started = item.SimplifiedStatusChanges.FirstOrDefault()?.ChangeTime;

                CachedIssueStatusChange lastState = item.SimplifiedStatusChanges.LastOrDefault();
                if (lastState != null && lastState.State == finishedState)
                {
                    item.Ended = lastState.ChangeTime;
                }

                item.Duration = item.Ended - item.Started;
            }

            return analyzedIssues;
        }

        public static bool IsValidIssue(AnalyzedIssue issue)
        {
            // TODO : Automated tests and ability to change for user

            return
                (issue.Type == "Story" || issue.Type == "Bug") &&
                (issue.Resolution != "Cancelled" && issue.Resolution != "Duplicate") &&
                (issue.Status != "Withdrawn" && issue.Status != "On Hold");
        }

        public async Task<IEnumerable<AnalyzedIssue>> GetStories(StateFilteringParameter stateFiltering)
        {
            var issues = await GetAllIssues(stateFiltering);

            IEnumerable<AnalyzedIssue> stories = issues
                .Where(IsValidIssue);

            return stories;
        }

        private async Task<IEnumerable<AnalyzedIssue>> GetLatestStories(IssuesFromParameters parameters, StateFilteringParameter stateFiltering)
        {
            IEnumerable<AnalyzedIssue> stories = (await GetStories(stateFiltering)).ToArray();

            var latestStories = stories
                    .Where(x => parameters.IssuesFrom == null || x.Ended >= parameters.IssuesFrom)
                    .ToArray();

            return latestStories;
        }

        public async Task<IEnumerable<FinishedIssue>> GetLatestFinishedStories(IssuesFromParameters parameters, StateFilteringParameter stateFiltering)
        {
            var latestStories = await GetLatestStories(parameters, stateFiltering);

            return OfFinishedStories(latestStories);
        }

        public async Task<IEnumerable<FinishedIssue>> GetFinishedStories(StateFilteringParameter stateFiltering)
        {
            var stories = await GetStories(stateFiltering);

            return OfFinishedStories(stories);
        }

        private static IEnumerable<FinishedIssue> OfFinishedStories(IEnumerable<AnalyzedIssue> latestStories)
        {
            FinishedIssue[] finishedStoriesLast =
                latestStories
                    .Where(x => x.Duration != null)
                    .Select(MapToFinishedIssue)
                    .ToArray();

            return finishedStoriesLast;
        }

        private static FinishedIssue MapToFinishedIssue(AnalyzedIssue issue)
        {
            var flowIssue = new FinishedIssue()
            {
                Key = issue.Key,
                Title = issue.Title,
                Type = issue.Type,
                Started = issue.Started.Value,
                Ended = issue.Ended.Value,
                Duration = issue.Duration.Value,
                StoryPoints = issue.StoryPoints,
                StatusChanges = issue.SimplifiedStatusChanges
            };

            return flowIssue;
        }

        public async Task<IEnumerable<ProjectStatistics>> GetProjectsStatistics()
        {
            var allIssues = await _jiraCache.GetIssues();

            return 
                allIssues
                .GroupBy(x=>x.Key.Split('-')[0])
                .Select(grp=> new ProjectStatistics(grp.Key, grp.Count(), grp.Max(x => x.Updated)));
        }
    }
}