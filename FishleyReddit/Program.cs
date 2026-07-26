using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Xml;
using Reddit;

const string ConfigPath = "/home/ubre/Desktop/FishleyReddit/config.json";
const string SeenPostsPath = "/home/ubre/Desktop/FishleyReddit/seen_posts.json";
const string RssFeedUrl = "https://sbox.game/rss/news";
const int CheckIntervalMinutes = 5;

if (!File.Exists(ConfigPath))
{
    Console.WriteLine($"Config not found at {ConfigPath}");
    return;
}

var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ConfigPath))!;
string Cfg(string key) => config.TryGetValue(key, out var v) ? v : throw new Exception($"Missing config key: {key}");

var appId       = Cfg("RedditAppId");
var appSecret   = Cfg("RedditAppSecret");
var username    = Cfg("RedditUsername");
var password    = Cfg("RedditPassword");
var userAgent   = Cfg("RedditUserAgent");
var subreddit   = Cfg("Subreddit");

var seenUrls  = File.Exists(SeenPostsPath)
    ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(SeenPostsPath))!
    : new HashSet<string>();

bool firstRun = !File.Exists(SeenPostsPath);

string? accessToken = null;
DateTime tokenExpiry = DateTime.MinValue;
RedditClient? reddit = null;

Console.WriteLine("FishleyReddit starting...");

while (true)
{
    try
    {
        await EnsureAuthenticated();

        var items = FetchFeed();

        if (firstRun)
        {
            foreach (var item in items)
                seenUrls.Add(item.Url);
            SaveSeen();
            firstRun = false;
            Console.WriteLine($"First run: seeded {seenUrls.Count} existing posts. Will post new ones going forward.");
        }
        else
        {
            var newItems = items.Where(i => !seenUrls.Contains(i.Url)).ToList();

            foreach (var item in newItems)
            {
                Console.WriteLine($"Posting: {item.Title}");
                reddit!.Subreddit(subreddit).About().LinkPost(title: item.Title, url: item.Url).Submit();
                seenUrls.Add(item.Url);
                SaveSeen();
                await Task.Delay(2000);
            }

            if (newItems.Count > 0)
                Console.WriteLine($"Posted {newItems.Count} new item(s).");
            else
                Console.WriteLine("No new posts.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Error: {ex.Message}");
    }

    await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes));
}

async Task EnsureAuthenticated()
{
    if (accessToken != null && DateTime.UtcNow < tokenExpiry)
        return;

    Console.WriteLine("Authenticating with Reddit...");
    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{appId}:{appSecret}")));

    var resp = await http.PostAsync("https://www.reddit.com/api/v1/access_token",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"]   = username,
            ["password"]   = password,
        }));

    var data = JsonSerializer.Deserialize<JsonElement>(await resp.Content.ReadAsStringAsync());
    accessToken = data.GetProperty("access_token").GetString()!;
    tokenExpiry = DateTime.UtcNow.AddMinutes(50);
    reddit = new RedditClient(appId: appId, appSecret: appSecret, accessToken: accessToken, userAgent: userAgent);
    Console.WriteLine("Authenticated.");
}

List<(string Title, string Url)> FetchFeed()
{
    using var reader = XmlReader.Create(RssFeedUrl);
    var feed = SyndicationFeed.Load(reader);
    return feed.Items
        .Select(item => (
            Title: item.Title.Text,
            Url:   item.Links.FirstOrDefault()?.Uri.ToString() ?? ""))
        .Where(x => !string.IsNullOrEmpty(x.Url))
        .ToList();
}

void SaveSeen() => File.WriteAllText(SeenPostsPath, JsonSerializer.Serialize(seenUrls));
