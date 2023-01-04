using System.Net;
using System.Text.Json;

namespace ActionActivator
{
    class Program
    {
        static Conf _conf;
        static HttpClient _scClient;

        static async Task Main()
        {
            _conf = Deserialize<Conf>(GetEnvValue("CONF"));
            if (!string.IsNullOrWhiteSpace(_conf.ScKey))
            {
                _scClient = new HttpClient();
            }

            Console.WriteLine("ActionActivator开始运行...");
            foreach (Activator act in _conf.Acts)
            {
                string owner = act.User;
                Console.WriteLine($"共 {_conf.Acts.Length} 个账号，正在运行{owner}...");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", owner);
                client.DefaultRequestHeaders.Add("Authorization", $"token {act.Token}");

                foreach (Repo repo in act.Repos)
                {
                    string badInfo = null;
                    try
                    {

                        var httpResponseMessage = await client.PutAsync($"https://api.github.com/repos/{owner}/{repo.Name}/actions/workflows/{repo.WorkflowFileName}/enable", null);
                        if (httpResponseMessage.StatusCode != HttpStatusCode.NoContent)
                        {//请求失败
                            badInfo = $"请求失败. code: {httpResponseMessage.StatusCode}, msg: {await httpResponseMessage.Content.ReadAsStringAsync()}";
                        }
                    }
                    catch (Exception ex)
                    {
                        badInfo = $"ex: {ex.Message}";
                    }
                    await Notify($"{repo.Name}...{badInfo ?? "ok"}", badInfo != null);
                }
            }
            Console.WriteLine("ActionActivator运行完毕");
        }

        static async Task Notify(string msg, bool isFailed = false)
        {
            Console.WriteLine(msg);
            if (_conf.ScType == "Always" || (isFailed && _conf.ScType == "Failed"))
            {
                await _scClient?.GetAsync($"https://sc.ftqq.com/{_conf.ScKey}.send?text={msg}");
            }
        }

        static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);

        static string GetEnvValue(string key) => Environment.GetEnvironmentVariable(key);

        #region Conf

        class Conf
        {
            public string ScKey { get; set; }
            public string ScType { get; set; }
            public Activator[] Acts { get; set; }
        }

        class Activator
        {
            public string User { get; set; }
            public string Token { get; set; }
            public Repo[] Repos { get; set; }
        }

        class Repo
        {
            public string Name { get; set; }
            public string WorkflowFileName { get; set; } = "main.yml";
        }

        #endregion
    }
}
