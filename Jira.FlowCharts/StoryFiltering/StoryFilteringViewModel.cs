using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jira.FlowCharts.StoryFiltering
{
    public class StoryFilteringViewModel : ReactiveScreen
    {
        public StoryFilteringViewModel(TasksSource source)
        {
            DisplayName = "Story and state filtering";
        }
    }
}
