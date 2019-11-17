using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jira.FlowCharts
{
    public class JsonStatesRepositoryTest : StatesRepositoryTest, IDisposable
    {
        private readonly string _fileName;

        public JsonStatesRepositoryTest()
        {
            _fileName = Path.GetRandomFileName();
        }

        public void Dispose()
        {
            if (File.Exists(_fileName))
            {
                File.Delete(_fileName);
            }
        }

        protected override IStatesRepository CreateRepository()
        {
            return new JsonStatesRepository(_fileName);
        }
    }

    public class MemoryStatesRepositoryTest : StatesRepositoryTest
    {
        MemoryStatesRepository _repository;

        public MemoryStatesRepositoryTest()
        {
            _repository = new MemoryStatesRepository(new string[0], new string[0]);
        }

        protected override IStatesRepository CreateRepository()
        {
            return _repository;
        }
    }

    public abstract class StatesRepositoryTest
    {
        protected abstract IStatesRepository CreateRepository();

        IStatesRepository Repository => CreateRepository();

        [Fact]
        public void Is_empty_after_creation()
        {
            Assert.Empty(Repository.GetFilteredStates());
            Assert.Empty(Repository.GetResetStates());
        }

        [Fact]
        public void Setting_filtered_state_stays_can_be_retrieved()
        {
            string[] states = new string[] { "A", "B", "C" };
            
            Repository.SetFilteredStates(states);

            Assert.Equal(states, Repository.GetFilteredStates());
            Assert.Empty(Repository.GetResetStates());
        }

        [Fact]
        public void Setting_reset_state_stays_can_be_retrieved()
        {
            string[] states = new string[] { "A", "B", "C" };

            Repository.SetResetStates(states);

            Assert.Equal(states, Repository.GetResetStates());
            Assert.Empty(Repository.GetFilteredStates());
        }

        [Fact]
        public void Setting_all_states_can_be_retrieved()
        {
            string[] filteredStates = new string[] { "E", "F", "G" };
            string[] resetStates = new string[] { "A", "B", "C" };

            Repository.SetFilteredStates(filteredStates);
            Repository.SetResetStates(resetStates);

            Assert.Equal(filteredStates, Repository.GetFilteredStates());
            Assert.Equal(resetStates, Repository.GetResetStates());

            Repository.SetResetStates(resetStates);
            Repository.SetFilteredStates(filteredStates);

            Assert.Equal(filteredStates, Repository.GetFilteredStates());
            Assert.Equal(resetStates, Repository.GetResetStates());
        }
    }
}
