using Jira.Querying;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Jira.FlowCharts.Test
{
    public class SimplifyStateChangeOrderTest
    {
        private const string DevState = "In dev";
        private const string QaState = "In QA";
        private const string DoneState = "Done";

        [Fact]
        public void Ignores_all_but_first_transition_into_state()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 03), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 05), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 06), QaState),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder();
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            List<CachedIssueStatusChange> expectedChanges = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 03), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 05), QaState),
            };

            simplified.ShouldCompare(expectedChanges);
        }

        [Fact]
        public void Going_back_to_previous_state_is_ignored()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 03), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 05), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 06), QaState),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder();
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            List<CachedIssueStatusChange> expectedChanges = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 03), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), QaState),
            };

            simplified.ShouldCompare(expectedChanges);
        }
    }
}
