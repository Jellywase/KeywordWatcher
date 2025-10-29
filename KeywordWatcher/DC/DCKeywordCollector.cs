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
        const long maxPostCount = 100000;
        long currentPostID = -1;

        public DCKeywordCollector(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }


        public override async Task<KeywordCollector.CollectResult> CollectData(CancellationToken ct, int collectPeriod)
        {
            KeywordCollector.CollectResult result = new();
            CollectedData newCD = new CollectedData(DateTime.Now, $"DC {boardCode}", "");
            List<Exception> exceptions = new();
            result.cd = newCD;
            result.exceptions = exceptions;

            try
            {
                if (currentPostID == -1)
                {
                    currentPostID = await GetFrontPostID();
                }
                await Task.Delay(collectPeriod, ct);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
                result.isSuccessful = false;
                return result;
            }


            try
            {
                var sw = Tester.CreateStopwatch();
                sw.Start();
                var fetchPostsResult = await FetchPosts(newCD);
                sw.Stop((ms) => Console.WriteLine($"DC FetchPosts : {ms}ms, N? = {newCD.N}"));
                sw.Reset();

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
            currentPostID = Math.Max(frontPostID - maxPostCount, currentPostID);

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
                            cd.N++;
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
            long result = -1;
            const int retryLimit = 5;
            int retryCount = 0;
            while (retryCount < retryLimit)
            {
                try
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
                        if (long.TryParse(htmlString[newPostIDIndex..endOfNewPostIDIndex], out long postID))
                        { postIDs.Add(postID); }
                    }

                    // 종종 response로 status는 OK를 반환하지만 content string은 빈 문자열인 경우가 있음.
                    if (postIDs.Count == 0 && (await response.Content.ReadAsByteArrayAsync()).Length == 0)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        result = postIDs.Max();
                        break;
                    }
                }
                catch (Exception e)
                {
                    retryCount++;
                }
            }
            if (result == -1)
            { throw new Exception("Can't get dc front post id."); }
            return result;
        }


        void ExtractKeywords(CollectedData cd, string target, bool dup = false)
        {
            var results = KiwiProvider.kiwi.Analyze(target);
            foreach (var result in results)
            {
                var distinctedMorphs = result.morphs.DistinctBy((token) => token.form);
                foreach (var token in distinctedMorphs)
                {
                    if (token.tag is "NNG" or "NNP" or "SL") // NNG: 일반 명사, NNP: 고유 명사, SL: 알파벳(티커)
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
