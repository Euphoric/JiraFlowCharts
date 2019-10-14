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
        private const string OnHoldState = "On Hold";

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

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState });
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

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            List<CachedIssueStatusChange> expectedChanges = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 03), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), QaState),
            };

            simplified.ShouldCompare(expectedChanges);
        }

        [Fact]
        public void Should_not_include_undefined_state()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 03), "UnknownState"),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            Assert.Empty(simplified);
        }

        [Fact]
        public void Going_back_to_previously_unvisited_state_is_removed()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 05), DevState),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            List<CachedIssueStatusChange> expectedChanges = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), QaState),
            };

            simplified.ShouldCompare(expectedChanges);
        }

        [Fact]
        public void Going_back_to_previously_unvisited_state_multiple_is_removed()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), "State4"),
                new CachedIssueStatusChange(new DateTime(2010, 07, 05), "State1"),
                new CachedIssueStatusChange(new DateTime(2010, 07, 06), "State2"),
                new CachedIssueStatusChange(new DateTime(2010, 07, 07), "State3"),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { "State1", "State2", "State3" , "State4" });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            List<CachedIssueStatusChange> expectedChanges = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), "State4"),
            };

            simplified.ShouldCompare(expectedChanges);
        }

        [Fact]
        public void Ignore_states_before_reset_state()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 05), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 06), OnHoldState),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState }, new[] { OnHoldState });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            Assert.Empty(simplified);
        }

        [Fact]
        public void Ignore_states_before_reset_state_and_returns_next_state()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 05), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 06), OnHoldState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 07), DevState),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState }, new[] { OnHoldState });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            List<CachedIssueStatusChange> expectedChanges = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 07), DevState),
            };

            simplified.ShouldCompare(expectedChanges);
        }

        [Fact]
        public void Ignore_states_before_reset_state_and_returns_following_states_simplified()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 05), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 06), OnHoldState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 07), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 08), DevState),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState }, new[] { OnHoldState });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            List<CachedIssueStatusChange> expectedChanges = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 07), QaState),
            };

            simplified.ShouldCompare(expectedChanges);
        }

        [Fact]
        public void Takes_last_reset_state()
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 04), DevState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 06), OnHoldState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 07), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 08), OnHoldState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 09), DevState),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState }, new[] { OnHoldState });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            List<CachedIssueStatusChange> expectedChanges = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 09), DevState),
            };

            simplified.ShouldCompare(expectedChanges);
        }

        [Theory]
        [InlineData("Reset1")]
        [InlineData("Reset2")]
        [InlineData("Reset3")]
        public void Handles_multiple_reset_states(string inputState)
        {
            List<CachedIssueStatusChange> changes = new List<CachedIssueStatusChange>()
            {
                new CachedIssueStatusChange(new DateTime(2010, 07, 07), QaState),
                new CachedIssueStatusChange(new DateTime(2010, 07, 08), inputState),
            };

            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(new[] { DevState, QaState, DoneState }, new[] { "Reset1", "Reset2", "Reset3" });
            var simplified = simplify.FilterStatusChanges(changes).ToList();

            Assert.Empty(simplified);
        }
    }
}
