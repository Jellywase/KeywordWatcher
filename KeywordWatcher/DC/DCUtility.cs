using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;

namespace KeywordWatcher.DC
{
    internal static class DCUtility
    {
        /// <summary>
        /// Extracts title and content
        /// </summary>
        /// <param name="formString"></param>
        /// <returns></returns>
        public static async Task<string> ParseFromHTML(string formString)
        {
            using var context = BrowsingContext.New(Configuration.Default);
            using var doc = await context.OpenAsync(req => req.Content(formString));

            // 제목 추출
            string title = doc.QuerySelector("title")?.TextContent
                ?? doc.QuerySelector("meta[name='title']")?.GetAttribute("content")
                ?? "";

            // 본문 추출 (dcinside 기준)
            string content = doc.QuerySelector(".write_div")?.TextContent ?? "";

            // 정리
            title = System.Net.WebUtility.HtmlDecode(title).Trim();
            content = System.Net.WebUtility.HtmlDecode(content);
            content = Regex.Replace(content, @"\s+", " ").Trim();
            return title + " " + content;
        }
    }
}
