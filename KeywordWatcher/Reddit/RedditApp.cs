using AngleSharp.Dom.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

internal class RedditApp : IDisposable
{
    HttpClient httpClient;
    readonly string userAgent = "CollectingBot/1.0 by jellywase";
    readonly string clientID = "Jq0IUrEJj9uXIiUidIehGw";
    readonly string clientSecret = "9Ar6jfjDdP-Nz8rJdM7YnJiZID9EJQ";
    readonly string username = "jellywase";
    string state => "MyState";
    readonly string redirectUri = "http://localhost:5001/oauth";
    readonly string accessTokenUrl = "https://www.reddit.com/api/v1/access_token";

    string accessToken = string.Empty;
    string refreshToken = string.Empty;

    public RedditApp()
    {
        // 레딧 장려 가이드라인으로 인해 새로운 httpclient 생성.
        this.httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", $"windows:redditapp:v0.0.1(by / u / {username})");
    }

    public async Task Initialize()
    {
        // 인증 코드 받기
        var builder = new UriBuilder("https://www.reddit.com/api/v1/authorize");
        var query = HttpUtility.ParseQueryString(string.Empty);

        string cachedState = this.state;
        query["client_id"] = clientID;
        query["response_type"] = "code";
        query["state"] = cachedState;
        query["redirect_uri"] = redirectUri;
        query["duration"] = "permanent";
        query["scope"] = "read";

        builder.Query = query.ToString();
        string authUrl = builder.ToString();

        Process.Start(new ProcessStartInfo
        {
            FileName = authUrl,
            UseShellExecute = true
        });

        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri + "/");
        listener.Start();

        var context = await listener.GetContextAsync();
        string authCode = context.Request.QueryString["code"] ?? string.Empty;
        string resState = context.Request.QueryString["state"] ?? string.Empty;
        listener.Stop();
        if (resState != cachedState)
        {
            throw new Exception("State 값이 일치하지 않습니다.");
        }

        // 액세스 토큰 받기
        var values = new Dictionary<string, string>
        {
            {"grant_type", "authorization_code" },
            {"code", authCode },
            {"redirect_uri", redirectUri }
        };
        using var content = new FormUrlEncodedContent(values);

        using var response = await httpClient.PostAsync(accessTokenUrl, content);
        string responseString = await response.Content.ReadAsStringAsync();

        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseString);

        accessToken = responseJson.GetProperty("access_token").GetString() ?? string.Empty;
        refreshToken = responseJson.GetProperty("refresh_token").GetString() ?? string.Empty;
    }

    async Task RefreshAccessToken()
    {
        var refreshValues = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

        using var client = new HttpClient();
        using var refreshContent = new FormUrlEncodedContent(refreshValues);
        using var refreshResponse = await client.PostAsync(accessTokenUrl, refreshContent);

        if (!refreshResponse.IsSuccessStatusCode)
        {
            throw new Exception("RedditApp 액세스 토큰 갱신 실패.");
        }

        string responseString = await refreshResponse.Content.ReadAsStringAsync();
        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseString);

        accessToken = responseJson.GetProperty("access_token").GetString() ?? string.Empty;
    }

    public async Task A()
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.GetAsync("https://oauth.reddit.com/redditdev+cats/comments/");
    }

    public void Dispose()
    {
        httpClient?.Dispose();
    }
}
