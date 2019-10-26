using Caliburn.Micro;
using Jira.FlowCharts.JiraUpdate;
using Jira.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jira.FlowCharts
{
    public class JiraUpdateViewModelTest
    {
        [Fact]
        public async Task Activating_updates_current_status()
        {
            var repository = JiraLocalCache.CreateMemoryRepository();
            var tasksSource = new TasksSource(() => repository);
            var vm = new JiraUpdateViewModel(tasksSource);

            await (vm as IScreen).ActivateAsync();

            Assert.Equal(0, vm.CachedIssuesCount);
            Assert.Null(vm.LastUpdatedIssue);
        }
    }
}
