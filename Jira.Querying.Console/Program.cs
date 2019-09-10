using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;
using Atlassian.Jira;
using Newtonsoft.Json;
using Jira.Querying;
using Microsoft.Extensions.Configuration;

namespace JiraParse
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await MainInner();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static async Task MainInner()
        {
            IConfigurationRoot configuration = CreateConfiguration();

            var section = configuration.GetSection("jira");
            
            // create a connection to JIRA using the Rest client
            var client = new JiraClient(section.GetValue<string>("url"), section.GetValue<string>("userName"), section.GetValue<string>("password"));

            DateTime lastUpdate = DateTime.Now.AddYears(-1);

            JiraLocalCache jiraLocalCache = new JiraLocalCache(client, lastUpdate);

            await jiraLocalCache.Update();

            var updatedIssues = jiraLocalCache.Issues;

            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(@"issues.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, updatedIssues);
            }

            Console.WriteLine("Finished");
        }

        private static IConfigurationRoot CreateConfiguration()
        {
            var builder = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json",
                                     optional: false,
                                     reloadOnChange: true);
            builder.AddUserSecrets<Program>(true);

            var configuration = builder.Build();
            return configuration;
        }
    }
}
