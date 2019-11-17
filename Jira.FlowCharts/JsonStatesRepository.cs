using Newtonsoft.Json;
using System.IO;

namespace Jira.FlowCharts
{
    public class JsonStatesRepository : IStatesRepository
    {
        private class JsonStructure
        {
            public string[] FilteredStates { get; set; }
            public string[] ResetStates { get; set; }
        }

        private readonly string _file;

        public JsonStatesRepository(string file)
        {
            _file = file;
        }

        private JsonStructure CurrentStructure()
        {
            JsonStructure jsonStructure;
            if (!File.Exists(_file))
            {
                jsonStructure = new JsonStructure()
                {
                    FilteredStates = new string[0],
                    ResetStates = new string[0]
                };
            }
            else
            {
                jsonStructure = JsonConvert.DeserializeObject<JsonStructure>(File.ReadAllText(_file));
            }

            return jsonStructure;
        }

        private void SaveNewStructure(JsonStructure structure)
        {
            File.WriteAllText(_file, JsonConvert.SerializeObject(structure));
        }

        public string[] GetFilteredStates()
        {
            JsonStructure jsonStructure = CurrentStructure();
            return jsonStructure.FilteredStates;
        }

        public void SetFilteredStates(string[] states)
        {
            var structure = CurrentStructure();
            structure.FilteredStates = states;
            SaveNewStructure(structure);
        }

        public string[] GetResetStates()
        {
            JsonStructure jsonStructure = CurrentStructure();
            return jsonStructure.ResetStates;
        }

        public void SetResetStates(string[] states)
        {
            var structure = CurrentStructure();
            structure.ResetStates = states;
            SaveNewStructure(structure);
        }
    }
}