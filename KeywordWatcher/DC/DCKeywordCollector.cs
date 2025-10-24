using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NetKiwi;
using NetKiwi.Backend;

namespace KeywordWatcher.DC
{
    internal abstract class DCKeywordCollector : KeywordCollector
    {
        protected abstract string boardCode { get; }
        readonly HttpClient httpClient;
        long currentPostID = 0;

        public DCKeywordCollector(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }


        public override async Task<KeywordCollector.CollectResult> CollectData()
        {
            KeywordCollector.CollectResult result = new();
            CollectedData newCD = new CollectedData(DateTime.Now, $"DC {boardCode}", "");
            List<Exception> exceptions = new();
            result.cd = newCD;
            result.exceptions = exceptions;

            try
            {
                var fetchPostsResult = await FetchPosts(newCD);
                exceptions.AddRange(fetchPostsResult.exceptions);
                result.isSuccessful = true;
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                result.isSuccessful = false;
            }
            return result;
        }

        async Task<FetchPostsResult> FetchPosts(CollectedData cd)
        {
            FetchPostsResult result = new();
            List<Exception> exceptions = new();
            result.exceptions = exceptions;

            long frontPostID = await GetFrontPostID();
            const int retryLimit = 3;
            int retryCount = 0;
            while (currentPostID <= frontPostID)
            {
                try
                {
                    using var response = await httpClient.GetAsync($"https://gall.dcinside.com/mgallery/board/view/?id={boardCode}&no={currentPostID}&page=1");
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            currentPostID++;
                            break;

                        case HttpStatusCode.TooManyRequests:
                            await Task.Delay(500);
                            throw new Exception($"Rate limited.");

                        case HttpStatusCode.OK:
                            var htmlString = await response.Content.ReadAsStringAsync();
                            var titleAndContent = await DCUtility.ParseFromHTML(htmlString);
                            ExtractKeywords(cd, titleAndContent);
                            cd.CountN();
                            currentPostID++;
                            break;
                    }
                    retryCount = 0;
                }
                catch (Exception e)
                {
                    exceptions.Add(new Exception($"Error fetching {boardCode} post {currentPostID}. ", e));
                    if (retryCount < retryLimit)
                    { retryCount++; }
                    else
                    { currentPostID++; }
                }
            }
            return result;
        }

        async Task<long> GetFrontPostID()
        {
            using var response = await httpClient.GetAsync($"https://gall.dcinside.com/mgallery/board/lists/?id={boardCode}");
            var htmlString = await response.Content.ReadAsStringAsync();
            const string prefix = "data-no=\"";

            List<long> postIDs = new();
            int newPrefixIndex = 0;
            int newSearchIndex = 0;
            while ((newPrefixIndex = htmlString.IndexOf(prefix, newSearchIndex)) != -1)
            {
                int newPostIDIndex = newPrefixIndex + prefix.Length;
                int endOfNewPostIDIndex = htmlString.IndexOf('"', newPostIDIndex);
                newSearchIndex = endOfNewPostIDIndex + 1;
                if (long.TryParse(htmlString[newPostIDIndex..endOfNewPostIDIndex], out long postID));
                { postIDs.Add(postID); }
            }
            long max = postIDs.Max();
            return max;
        }


        void ExtractKeywords(CollectedData cd, string target, bool dup = false)
        {
            var results = KiwiProvider.kiwi.Analyze(target);
            foreach (var result in results)
            {
                var distinctedMorphs = result.morphs.DistinctBy((token) => token.form);
                foreach (var token in distinctedMorphs)
                {
                    if (token.tag.StartsWith("N"))
                    {
                        cd.AddKeywordCount(token.form, 1);
                    }
                }
            }
        }

        public class FetchPostsResult
        {
            public IReadOnlyList<Exception> exceptions;
        }
    }
}
