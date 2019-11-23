using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DynamicData;
using Jira.Querying;

namespace Jira.FlowCharts
{
    public class TasksSource
    {
        private readonly ITasksSourceJiraCacheAdapter _jiraCache;
        private readonly IStatesRepository _statesRepository;
        private IMapper _mapper;

        public ObservableCollection<string> AvailableStates { get; }
        public ObservableCollection<string> FilteredStates { get; }
        public ObservableCollection<string> ResetStates { get; }
        
        public TasksSource(ITasksSourceJiraCacheAdapter jiraCacheAdapter, IStatesRepository statesRepository)
        {
            _jiraCache = jiraCacheAdapter;
            _statesRepository = statesRepository;

            AvailableStates = new ObservableCollection<string>();
            FilteredStates = new ObservableCollection<string>();
            ResetStates = new ObservableCollection<string>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CachedIssue, AnalyzedIssue>();
            });

            _mapper = config.CreateMapper();
        }

        public async Task<string[]> GetAllStates()
        {
            return await _jiraCache.GetAllStates();
        }

        public async Task ReloadStates()
        {
            AvailableStates.Clear();
            FilteredStates.Clear();
            ResetStates.Clear();

            var allStates = await GetAllStates();
            var filteredStates = _statesRepository.GetFilteredStates();
            var resetStates = _statesRepository.GetResetStates();

            AvailableStates.AddRange(allStates.Except(filteredStates).Except(resetStates));
            FilteredStates.AddRange(filteredStates);
            ResetStates.AddRange(resetStates);
        }

        public void AddFilteredState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Remove(state);
            FilteredStates.Add(state);
            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        public void RemoveFilteredState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Add(state);
            FilteredStates.Remove(state);
            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        public void AddResetState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Remove(state);
            ResetStates.Add(state);
            _statesRepository.SetResetStates(ResetStates.ToArray());
        }

        public void RemoveResetState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Add(state);
            ResetStates.Remove(state);
            _statesRepository.SetResetStates(ResetStates.ToArray());
        }

        public void MoveFilteredStateLower(string state)
        {
            if (state == null)
                return;

            var stateAt = FilteredStates.IndexOf(state);
            if (stateAt == 0)
                return;

            stateAt--;
            var reinsertState = FilteredStates[stateAt];
            FilteredStates.RemoveAt(stateAt);
            FilteredStates.Insert(stateAt + 1, reinsertState);

            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        public void MoveFilteredStateHigher(string state)
        {
            if (state == null)
                return;

            var stateAt = FilteredStates.IndexOf(state);
            if (stateAt == FilteredStates.Count-1)
                return;

            stateAt++;
            var reinsertState = FilteredStates[stateAt];
            FilteredStates.RemoveAt(stateAt);
            FilteredStates.Insert(stateAt - 1, reinsertState);

            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        public async Task<IEnumerable<AnalyzedIssue>> GetAllIssues()
        {
            List<CachedIssue> issues = await _jiraCache.GetIssues();

            List<AnalyzedIssue> analyzedIssues = _mapper.Map<List<AnalyzedIssue>>(issues);

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(FilteredStates.ToArray(), ResetStates.ToArray());
            var finishedState = FilteredStates.LastOrDefault();

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
                item.IsValid = IsValidIssue(item);
            }

            return analyzedIssues;
        }
        private static bool IsValidIssue(AnalyzedIssue issue)
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
                .Where(x => x.IsValid);

            return stories;
        }

        public async Task<FinishedIssue[]> GetAllFinishedStories()
        {
            IEnumerable<AnalyzedIssue> stories = await GetStories();

            FinishedIssue[] finishedStories = stories
                .Where(x => x.Duration != null)
                .Select(x => CalculateDuration(x))
                .ToArray();

            return finishedStories;
        }

        public async Task<FinishedIssue[]> GetLatestFinishedStories()
        {
            FinishedIssue[] finishedStories = await GetAllFinishedStories();

            DateTime startDate = DateTime.Now.AddMonths(-12);

            FinishedIssue[] finishedStoriesLast = finishedStories
                .Where(x => x.Ended > startDate).ToArray();

            return finishedStoriesLast;
        }

        private static FinishedIssue CalculateDuration(AnalyzedIssue issue)
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
                StatusChanges = issue.SimplifiedStatusChanges,
                IsValid = issue.IsValid
            };

            return flowIssue;
        }

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectName, ICacheUpdateProgress cacheUpdateProgress)
        {
            await _jiraCache.UpdateIssues(jiraLoginParameters, projectName, cacheUpdateProgress);
        }
    }
}