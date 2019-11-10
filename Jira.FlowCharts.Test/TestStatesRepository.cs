using System;
using System.Linq;

namespace Jira.FlowCharts
{
    public class TestStatesRepository : IStatesRepository
    {
        public string[] FilteredStates { get; set; } = new string[0];
        public string[] ResetStates { get; set; } = new string[0];

        public string[] GetFilteredStates()
        {
            return FilteredStates;
        }

        public void SetFilteredStates(string[] states)
        {
            FilteredStates = states;
        }

        public string[] GetResetStates()
        {
            return ResetStates;
        }

        public void SetResetStates(string[] states)
        {
            ResetStates = states;
        }
    }
}