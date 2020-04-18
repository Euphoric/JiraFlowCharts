﻿using System.Collections.Generic;
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

        private StateFiltering StateFiltering { get; }

        public TasksSource(ITasksSourceJiraCacheAdapter jiraCacheAdapter, StateFiltering stateFiltering)
        {
            _jiraCache = jiraCacheAdapter;

            StateFiltering = stateFiltering;

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

        public async Task<IEnumerable<AnalyzedIssue>> GetAllIssues()
        {
            List<CachedIssue> issues = await _jiraCache.GetIssues();

            List<AnalyzedIssue> analyzedIssues = _mapper.Map<List<AnalyzedIssue>>(issues);

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(StateFiltering.FilteredStates.ToArray(), StateFiltering.ResetStates.ToArray());
            var finishedState = StateFiltering.FilteredStates.LastOrDefault();

            foreach (var item in analyzedIssues)
            {
                item.SimplifiedStatusChanges = new Collection<CachedIssueStatusChange>(simplify.FilterStatusChanges(item.StatusChanges).ToList());

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

        public async Task<IEnumerable<AnalyzedIssue>> GetStories()
        {
            var issues = await GetAllIssues();

            IEnumerable<AnalyzedIssue> stories = issues
                .Where(IsValidIssue);

            return stories;
        }

        private async Task<IEnumerable<AnalyzedIssue>> GetLatestStories(IssuesFromParameters parameters)
        {
            IEnumerable<AnalyzedIssue> stories = (await GetStories()).ToArray();

            var latestStories = stories
                    .Where(x => parameters.IssuesFrom == null || x.Ended >= parameters.IssuesFrom)
                    .ToArray();

            return latestStories;
        }

        public async Task<IEnumerable<FinishedIssue>> GetLatestFinishedStories(IssuesFromParameters parameters)
        {
            var latestStories = await GetLatestStories(parameters);

            return OfFinishedStories(latestStories);
        }

        public async Task<IEnumerable<FinishedIssue>> GetFinishedStories()
        {
            var stories = await GetStories();

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
    }
}