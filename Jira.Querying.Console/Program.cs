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
            var jiraUrl = section.GetValue<string>("url");
            var username = section.GetValue<string>("userName");
            var password = section.GetValue<string>("password");
            var client = new JiraClient(jiraUrl, username, password);

            DateTime lastUpdate = DateTime.Now.AddYears(-1);

            using (JiraLocalCache jiraLocalCache = new JiraLocalCache(new SqliteJiraLocalCacheRepository(@"../../../../Data/issuesCache.db")))
            {
                await jiraLocalCache.Update(client, lastUpdate, "AC");
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
