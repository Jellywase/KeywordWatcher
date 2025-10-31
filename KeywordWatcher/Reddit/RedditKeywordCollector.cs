using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace KeywordWatcher.Reddit
{
    internal abstract class RedditKeywordCollector : KeywordCollector
    {
        readonly RedditApp redditApp;
        const int retryLimit = 3;
        protected abstract string subreddit { get; }
        bool initialCollect = true;

        public RedditKeywordCollector(RedditApp redditApp)
        {
            this.redditApp = redditApp;
        }
        public async override Task<CollectResult> CollectData(CancellationToken ct, int collectPeriod)
        {
            if (initialCollect)
            { initialCollect = false; }
            else
            { await Task.Delay(collectPeriod); }

            KeywordCollector.CollectResult result = new();
            CollectedData newCD = new CollectedData(DateTime.Now, $"Reddit {subreddit}", "");
            List<Exception> exceptions = new();
            result.cd = newCD;
            result.exceptions = exceptions;

            try
            {
                // Hot 게시물 받아오기
                JsonNode hotPostsJson = null;
                int retryCount = 0;
                while (retryCount < retryLimit)
                {
                    try
                    {
                        hotPostsJson = await redditApp.GetHotPosts(subreddit);
                        if (hotPostsJson == null)
                        { throw new Exception(); }
                        break;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(new Exception($"Can't get hot posts from reddit app. Retried {retryCount}. ex - ", ex));
                        retryCount++;
                    }
                }
                if (hotPostsJson == null)
                { throw new Exception("Can't get hot posts from reddit app. Retry count exhausted."); }

                // 제목과 내용, 스코어 추출.
                JsonArray posts = hotPostsJson["data"]["children"].AsArray();
                List<string> targets = new();
                List<int> scores = new();
                foreach (JsonNode post in posts)
                {
                    var postData = post["data"];
                    string selftext = postData["selftext"].GetValue<string>();
                    string title = postData["title"].GetValue<string>();
                    int score = postData["score"].GetValue<int>();
                    bool stickied = postData["stickied"].GetValue<bool>();
                    bool is_self = postData["is_self"].GetValue<bool>();

                    if (stickied || !is_self)
                    { continue; }

                    string target = selftext + " " + title;
                    targets.Add(target);
                    scores.Add(score);
                }

                // 배치로 분석
                IEnumerable<IEnumerable<CatalystProvider.TaggedKeyword>> analyzedTargets = await CatalystProvider.Analyze(targets);

                // Collect. score만큼 카운팅
                int targetIndex = 0;
                foreach (var analyzedTarget in analyzedTargets)
                {
                    var distinctedAnalyzedTarget = analyzedTarget.DistinctBy((tk) => tk.keyword);
                    int score = scores[targetIndex];
                    foreach (var tk in distinctedAnalyzedTarget)
                    {
                        if (tk.tag is Catalyst.PartOfSpeech.NOUN or Catalyst.PartOfSpeech.PROPN)
                        { newCD.AddKeywordCount(tk.keyword, score); }
                    }
                    newCD.N += score;
                    targetIndex++;
                }

                result.isSuccessful = true;
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                result.isSuccessful = false;
            }

            return result;
        }
    }
}
