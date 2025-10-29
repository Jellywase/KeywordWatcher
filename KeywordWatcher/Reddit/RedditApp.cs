using AngleSharp.Dom.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

internal class RedditApp : IDisposable
{
    readonly HttpClient httpClient;
    readonly SemaphoreSlim refreshTokenSS = new SemaphoreSlim(1, 1);

    readonly string clientID = "Jq0IUrEJj9uXIiUidIehGw";
    readonly string clientSecret = "9Ar6jfjDdP-Nz8rJdM7YnJiZID9EJQ";
    readonly string username = "jellywase";
    string state => "MyState";
    readonly string redirectUri = "http://localhost:5001/oauth";
    readonly string accessTokenUrl = "https://www.reddit.com/api/v1/access_token";

    object accessTokenLock = new();
    string accessToken
    {
        get
        {
            lock (accessTokenLock)
            {
                return _accessToken;
            }
        }
        set
        {
            lock (accessTokenLock)
            {
                _accessToken = value;
                accessTokenGettedTime = DateTime.Now;
            }
        }
    }
    string _accessToken;
    string refreshToken = string.Empty;
    DateTime accessTokenGettedTime;
    const int accessTokenExpirateAfter = 1; // hour
    bool isAccessTokenExpired
    {
        get
        {
            lock (accessTokenLock)
            {
                return DateTime.Now > accessTokenGettedTime.AddHours(accessTokenExpirateAfter);
            }
        }
    }

    bool initialized = false;

    readonly APIUsage apiUsage = new APIUsage(90);

    public RedditApp()
    {
        // 레딧 가이드라인으로 인해 새로운 httpclient 생성.
        this.httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", $"windows.redditapp.v1.0.0 (by /u/{username})");
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
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientID}:{clientSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

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
        initialized = true;
    }

    async Task RefreshAccessToken()
    {
        if (!isAccessTokenExpired)
        { return; }

        await refreshTokenSS.WaitAsync();

        if (!isAccessTokenExpired)
        { return; }

        try
        {
            var refreshValues = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            using var refreshContent = new FormUrlEncodedContent(refreshValues);
            using var refreshResponse = await httpClient.PostAsync(accessTokenUrl, refreshContent);

            if (!refreshResponse.IsSuccessStatusCode)
            {
                throw new Exception("RedditApp 액세스 토큰 갱신 실패.");
            }

            string responseString = await refreshResponse.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonNode>(responseString);

            accessToken = responseJson?["access_token"]?.GetValue<string>() ?? string.Empty;
        }
        catch
        { throw; }
        finally
        {
            refreshTokenSS.Release();
        }
    }

    public async Task<JsonNode> GetHotPosts(string subreddit)
    {
        return await GetDataInternal($"https://oauth.reddit.com/r/{subreddit}/hot");
    }

    async Task<JsonNode> GetDataInternal(string apiEndpoint)
    {
        if (!initialized)
        { throw new Exception("Reddit app not initialized"); }

        JsonNode result = null;
        var request = new HttpRequestMessage(HttpMethod.Get, apiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await Task.Delay(apiUsage.WhatMSShouldIWait());
        apiUsage.RecordUse();
        var response = await httpClient.SendAsync(request);


        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                string responseString = await response.Content.ReadAsStringAsync();
                result = JsonSerializer.Deserialize<JsonNode>(responseString);
                if (result == null)
                { throw new Exception($"Reddit api status code is OK, but response string can't be parsed to json? - {responseString} "); }
                break;

            case HttpStatusCode.Unauthorized:
                await RefreshAccessToken();
                result = await GetDataInternal(apiEndpoint);
                break;

            default:
                throw new Exception($"Unexpected response: status code - {response.StatusCode}, reason - {response.ReasonPhrase}");
                
        }
        return result;
    }

    public void Dispose()
    {
        httpClient?.Dispose();
        refreshTokenSS.Dispose();
    }

    class APIUsage
    {
        List<DateTime> history = new();
        object lockObj = new();
        readonly int limitPerMin;
        public APIUsage(int limitPerMin)
        {
            this.limitPerMin = limitPerMin;
        }
        public int WhatMSShouldIWait()
        { 
            lock (lockObj)
            {
                var now = DateTime.UtcNow;
                TrimHistory(now);
                if (history.Count < limitPerMin)
                { return 0; }
                else
                {
                    // boundary index부터 끝까지 요소 = limitPerMin
                    int boundaryIndex = history.Count - limitPerMin;
                    DateTime boundaryDate = history[boundaryIndex];
                    DateTime oneMinuteAgo = now.AddMinutes(-1);
                    return (int)(boundaryDate - oneMinuteAgo).TotalMilliseconds + 1;
                }
            }
        }
        public void RecordUse()
        {
            lock (lockObj)
            {
                var now = DateTime.Now;
                TrimHistory(now);
                history.Add(now);
            }
        }
        /// <summary>
        /// 현재기준 1분 안의 Date만 남김.
        /// </summary>
        void TrimHistory(DateTime now)
        {
            var oneMinuteAgo = now.AddMinutes(-1);
            while (history.Count > 0 && history[0] < oneMinuteAgo)
            {
                history.RemoveAt(0);
            }
        }
    }
}
