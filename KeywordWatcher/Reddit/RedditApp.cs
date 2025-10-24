using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.WebRequestMethods;

namespace KeywordWatcher.Reddit
{
    internal class RedditApp
    {
        HttpClient httpClient;
        readonly string userAgent = "CollectingBot/1.0 by jellywase";
        readonly string clientID = "Jq0IUrEJj9uXIiUidIehGw";
        readonly string clientSecret = "9Ar6jfjDdP-Nz8rJdM7YnJiZID9EJQ";
        readonly string username = "jellywase";
        readonly string password = "3461lsls*";

        public RedditApp(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task Connect()
        {
            



















            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

            var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientID}:{clientSecret}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password)
        });
        }
    }
}
