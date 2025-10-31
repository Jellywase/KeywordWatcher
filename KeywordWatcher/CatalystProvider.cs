using Catalyst;
using Mosaik.Core;
using NetKiwi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal static class CatalystProvider
    {
        static object lockObj = new();
        static bool initialized = false;

        static void Initialize()
        {
            lock (lockObj)
            {
                if (initialized)
                { return; }
                Storage.Current = new DiskStorage("catalyst-models");
                Catalyst.Models.English.Register();
                initialized = true;
            }
        }

        public static async Task<IEnumerable<TaggedKeyword>> Analyze(string target, Language language = Language.English)
        {
            lock (lockObj)
            {
                if (!initialized)
                { Initialize(); }
            }

            var nlp = await Pipeline.ForAsync(language);
            var doc = new Document(target, language);
            nlp.ProcessSingle(doc);

            List<TaggedKeyword> result = new();

            foreach (var sentence in doc.TokensData)
            {
                foreach (var token in sentence)
                {
                    string word = target[token.LowerBound..(token.UpperBound+1)];
                    result.Add(new TaggedKeyword(word, token.Tag));
                }
            }
            return result;
        }

        public static async Task<IEnumerable<IEnumerable<TaggedKeyword>>> Analyze(IEnumerable<string> targets, Language language = Language.English)
        {
            lock (lockObj)
            {
                if (!initialized)
                { Initialize(); }
            }

            var nlp = await Pipeline.ForAsync(language);
            List<Document> docs = new List<Document>();
            foreach (var target in targets)
            {
                docs.Add(new Document(target, language));
            }
            // ToArray를 하지 않으면 docs에 TokenData가 반영되지 않는 버그 있음.
            nlp.Process(docs).ToArray();

            List<List<TaggedKeyword>> result = new();

            var targetsEnumerator = targets.GetEnumerator();
            foreach (var doc in docs)
            {
                List<TaggedKeyword> taggedKeywords = new();
                result.Add(taggedKeywords);
                targetsEnumerator.MoveNext();
                string target = targetsEnumerator.Current;
                foreach (var sentence in doc.TokensData)
                {
                    foreach (var token in sentence)
                    {
                        string word = target[token.LowerBound..(token.UpperBound + 1)];
                        taggedKeywords.Add(new TaggedKeyword(word, token.Tag));
                    }
                }
            }
            targetsEnumerator.Dispose();
            return result;
        }

        public struct TaggedKeyword
        {
            public string keyword { get; }
            public PartOfSpeech tag { get; }
            public TaggedKeyword(string keyword, PartOfSpeech tag)
            {
                this.keyword = keyword;
                this.tag = tag;
            }
        }
    }
}
