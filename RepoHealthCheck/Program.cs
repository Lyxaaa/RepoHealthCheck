using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;

namespace RepoHealthCheck
{
    internal static class Program
    {
        private static IConfiguration Configuration { get; set; } = null!;

        private static string azureDevOpsUrl { get; set; }

        // Set this up on https://{YourOrgDomainHere}.visualstudio.com/_usersSettings/tokens
        private static string personalAccessToken { get; set; } = null!;
        private static string commitHistoryFilePath { get; set; } = null!;

        private static List<string> previousCommitHashes = new List<string>();
        private static Form1 form;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static async Task Main()
        {
            Configuration = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .Build();

            personalAccessToken = Configuration["PersonalAccessToken"];
            azureDevOpsUrl = Configuration["AzureDevOpsUrl"];
            commitHistoryFilePath = $"{Configuration["CommitHistoryFilePath"]}\\commitHistory.txt";

            ApplicationConfiguration.Initialize();
            LoadCommitHistory();
            form = new Form1();
            Application.Run(form);
        }

        private static void LoadCommitHistory()
        {
            if (File.Exists(commitHistoryFilePath))
            {
                previousCommitHashes = File.ReadAllLines(commitHistoryFilePath).ToList();
            }
        }

        private static void SaveCommitHistory()
        {
            File.WriteAllLines(commitHistoryFilePath, previousCommitHashes);
        }

        public static async Task CheckCommitsAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

                    string response = await client.GetStringAsync(azureDevOpsUrl);
                    Commits? commitsResponse = JsonSerializer.Deserialize<Commits>(response);

                    List<Commit> commits = commitsResponse?.Value ?? new List<Commit>();

                    if (commits.Any())
                    {
                        string oldestCommitId = commits.Last().CommitId;
                        int sliceIndex = previousCommitHashes.IndexOf(oldestCommitId);

                        if (sliceIndex != -1)
                        {
                            string[] localSlice = previousCommitHashes.Take(sliceIndex + 1).ToArray();

                            string[]? missingCommits = localSlice.Except(commits.Select(c => c.CommitId)).ToArray();
                            if (!missingCommits.Any())
                            {
                                string latestCommitId = commits.First().CommitId;
                                form.LogMessage($"Commit check successful. All commits are present and in order. Latest commit ID: {latestCommitId} ({commits.First().Comment})");
                            }
                            else
                            {
                                form.LogError($"Commit check failed. Commits are missing or out of order. Missing commits: {string.Join(", ", missingCommits)}");
                            }

                            previousCommitHashes = commits.Select(c => c.CommitId).ToList();
                            SaveCommitHistory();
                        }
                        else
                        {
                            form.LogError("Commit check failed. Oldest commit from the new request not found in local history.");
                        }
                    }
                    else
                    {
                        form.LogError("Commit check failed. No commits returned in the new request.");
                    }
                }
            }
            catch (Exception ex)
            {
                form.LogError($"Error during commit check: {ex.Message}");
            }
        }

        private class Commits
        {
            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("value")]
            public List<Commit> Value { get; set; }
        }

        private class Commit
        {
            [JsonPropertyName("commitId")]
            public string CommitId { get; set; }

            [JsonPropertyName("author")]
            public Author Author { get; set; }

            [JsonPropertyName("committer")]
            public Committer Committer { get; set; }

            [JsonPropertyName("comment")]
            public string Comment { get; set; }

            [JsonPropertyName("commentTruncated")]
            public bool CommentTruncated { get; set; }

            [JsonPropertyName("changeCounts")]
            public ChangeCounts ChangeCounts { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("remoteUrl")]
            public string RemoteUrl { get; set; }
        }

        private class Author
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("date")]
            public DateTime Date { get; set; }
        }

        private class Committer
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("date")]
            public DateTime Date { get; set; }
        }

        private class ChangeCounts
        {
            [JsonPropertyName("Add")]
            public int Add { get; set; }

            [JsonPropertyName("Edit")]
            public int Edit { get; set; }

            [JsonPropertyName("Delete")]
            public int Delete { get; set; }
        }
    }
}