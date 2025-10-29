using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    public static class Tester
    {
        public static async Task Test()
        {
            RedditApp redditApp = new();
            await redditApp.Initialize();
            var json = await redditApp.GetHotPosts("stocks");
        }

        internal static IStopwatch CreateStopwatch()
        { return new Tester.Stopwatch(); }

        internal interface IStopwatch
        {
            public void Start();
            public float Stop();
            public float Stop(Action<float> action);
            public void Reset();
        }

        internal class Stopwatch : IStopwatch
        {
            System.Diagnostics.Stopwatch sw = new();
            public void Start()
            {
                sw.Start();
            }

            public float Stop()
            {
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }

            public float Stop(Action<float> action)
            {
                sw.Stop();
                action?.Invoke(sw.ElapsedMilliseconds);
                return sw.ElapsedMilliseconds;
            }
            public void Reset()
            {  sw.Reset(); }
        }
        internal class VoidStopwatch : IStopwatch
        {
            public void Start()
            { }

            public float Stop()
            { return -1; }

            public float Stop(Action<float> action)
            { return -1; }
            public void Reset()
            { }
        }
    }
}
