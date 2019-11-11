using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using Jira.Querying;

namespace Jira.FlowCharts
{
    public class TasksSource
    {
        private readonly ITasksSourceJiraCacheAdapter _jiraCache;
        private readonly IStatesRepository _statesRepository;

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

        internal void AddFilteredState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Remove(state);
            FilteredStates.Add(state);
            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        internal void RemoveFilteredState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Add(state);
            FilteredStates.Remove(state);
            _statesRepository.SetFilteredStates(FilteredStates.ToArray());
        }

        internal void AddResetState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Remove(state);
            ResetStates.Add(state);
            _statesRepository.SetResetStates(ResetStates.ToArray());
        }

        internal void RemoveResetState(string state)
        {
            if (state == null)
                return;

            AvailableStates.Add(state);
            ResetStates.Remove(state);
            _statesRepository.SetResetStates(ResetStates.ToArray());
        }

        internal void MoveFilteredStateLower(string state)
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

        internal void MoveFilteredStateHigher(string state)
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

        public async Task<FinishedTask[]> GetFinishedStories()
        {
            IEnumerable<CachedIssue> stories = await GetStories();

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(FilteredStates.ToArray(), ResetStates.ToArray());

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

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectName, ICacheUpdateProgress cacheUpdateProgress)
        {
            await _jiraCache.UpdateIssues(jiraLoginParameters, projectName, cacheUpdateProgress);
        }
    }
}