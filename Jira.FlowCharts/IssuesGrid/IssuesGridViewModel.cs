using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Caliburn.Micro;
using DynamicData;
using TimeSpan = System.TimeSpan;

namespace Jira.FlowCharts.IssuesGrid
{
    class IssuesGridViewModel : Screen
    {
        private readonly TasksSource _tasksSource;
        private readonly IStateFilteringProvider _stateFilteringProvider;
        private readonly ICurrentProject _currentProject;

        public IssuesGridViewModel(TasksSource tasksSource, IStateFilteringProvider stateFilteringProvider, ICurrentProject currentProject)
        {
            _tasksSource = tasksSource;
            _stateFilteringProvider = stateFilteringProvider;
            _currentProject = currentProject;
            DisplayName = "Issues grid";

            Issues = new ObservableCollection<dynamic>();
        }

        public ObservableCollection<dynamic> Issues { get; }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            Issues.Clear();

            var stateFilteringParameter = await _stateFilteringProvider.GetStateFilteringParameter();
            var allIssues = await _tasksSource.GetAllIssues(_currentProject.ProjectKey,stateFilteringParameter);

            var mapper = new Mapper(new MapperConfiguration(cfg => { }));
            Issues.AddRange(allIssues.Select(issue => ToDynamicRow(issue, mapper)));
        }

        private dynamic ToDynamicRow(AnalyzedIssue issue, Mapper mapper)
        {
            dynamic row = mapper.Map<ExpandoObject>(issue);

            row.OriginalEstimate = issue.OriginalEstimate.HasValue ? TimeSpan.FromMinutes(issue.OriginalEstimate.Value).TotalDays : (double?)null;
            row.TimeSpent = issue.TimeSpent.HasValue ? TimeSpan.FromMinutes(issue.TimeSpent.Value).TotalDays : (double?)null;
            row.IsValid = TasksSource.IsValidIssue(issue);
            row.DurationDays = issue.Duration?.TotalDays;

            return row;
        }
    }
}
