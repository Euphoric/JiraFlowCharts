namespace Jira.FlowCharts
{
    public interface IStatesRepository
    {
        string[] GetFilteredStates();

        void SetFilteredStates(string[] states);

        string[] GetResetStates();

        void SetResetStates(string[] states);
    }
}