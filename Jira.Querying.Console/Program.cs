using System;
using System.Threading.Tasks;
using Jira.Querying;
using Jira.Querying.Sqlite;
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

            using (JiraLocalCache jiraLocalCache = new JiraLocalCache(new SqliteJiraLocalCacheRepository(@"../../../../Data/issuesCache.db")))
            {
                await jiraLocalCache.Initialize();

                await jiraLocalCache.Update(client, lastUpdate);
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
