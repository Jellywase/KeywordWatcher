using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal class KeywordAnalyzer
    {
        IReadOnlyCollectedData[] cdSequence = null;
        object lockObj = new object();
        IReadOnlyAnalyzedData lastAD;
        const int leastCumulative = 2;
        public int cumulative
        {
            get 
            {
                lock (lockObj)
                {
                    return _cumulative;
                }
            }
            set 
            {
                lock (lockObj)
                {
                    _cumulative = int.Clamp(value, leastCumulative, int.MaxValue);
                    ResizeCDSequence();
                }
            }
        }
        int _cumulative;

        public KeywordAnalyzer(int cumulative)
        {
            this.cumulative = cumulative;
        }
        public async Task<AnalyzeResult> AnalyzeData(IReadOnlyCollectedData cd)
        {
            AnalyzeResult result = new AnalyzeResult();
            List<Exception> exceptions = new();
            result.exceptions = exceptions;

            try
            {
                UpdateCDSequence(cd);
                result.ad = await AnalyzeCumulativeData();
                result.isSuccessful = true;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                result.isSuccessful = false;
            }
            return result;
        }

        void ResizeCDSequence()
        {
            lock (lockObj)
            {
                if (cdSequence == null)
                {
                    cdSequence = new IReadOnlyCollectedData[cumulative];
                    return;
                }
                if (cdSequence.Length != cumulative)
                {
                    var newSequence = new IReadOnlyCollectedData[cumulative];
                    for (int i = 0; i < Math.Min(cdSequence.Length, newSequence.Length); i++)
                    {
                        newSequence[i] = cdSequence[i];
                    }
                    cdSequence = newSequence;
                }
            }
        }

        void UpdateCDSequence(IReadOnlyCollectedData cd)
        {
            lock (lockObj)
            {
                if (cdSequence is null || cdSequence.Length < leastCumulative)
                { throw new InvalidOperationException("CD sequence is invalid"); }

                for (int i = cdSequence.Length - 1; i > 0; i--)
                {
                    cdSequence[i] = cdSequence[i - 1];
                }
                cdSequence[0] = cd;
            }
        }
        async Task<IReadOnlyAnalyzedData> AnalyzeCumulativeData()
        {
            lock (lockObj)
            {
                if (cdSequence is null || cdSequence.Length < leastCumulative)
                { throw new InvalidOperationException("CD sequence is invalid"); }

                // 첫번째 CD 캐싱.
                var frontCD = cdSequence[0];
                if (frontCD == null)
                { throw new InvalidOperationException("Sequence need at least 1 CollectedData"); }

                // 두번째 CD 캐싱.
                var secondCD = cdSequence[1];
                if (secondCD == null)
                    // 두번째 CD가 없는 경우 빈 CD 할당
                { cdSequence[1] = secondCD = new CollectedData(frontCD.time, frontCD.name, frontCD.description); }

                // 목표 cumulative보다 축적된 cumulative가 적은 경우 대응하여 actualCumulative 로 대체.
                int actualCumulative = leastCumulative;
                for (int i = leastCumulative; i < cumulative; i++)
                {
                    if (cdSequence[i] == null)
                    {
                        actualCumulative = i;
                        break; 
                    }
                }

                AnalyzedData ad = new(frontCD.name, frontCD.time, actualCumulative);

                // AnalyzedKeyword 인스턴스를 만들고 키워드 빈도를 누적 집계
                Dictionary<string, AnalyzedKeyword> analyzedKeywords = new();
                for (int i = 0; i < actualCumulative; i++)
                {
                    var cd = cdSequence[i];

                    foreach (var kvp in cd.keywords)
                    {
                        string keyword = kvp.Key;
                        var kd = kvp.Value;

                        if (!analyzedKeywords.TryGetValue(keyword, out var ak))
                        {
                            ak = new AnalyzedKeyword(keyword);
                        }
                        ak.totalF += kd.frequency;
                        ak.totalSqrF += (int)Sqr(kd.frequency);
                        var r = cd.GetRatio(keyword);
                        ak.totalR += r;
                        ak.totalSqrR += Sqr(r);
                    }
                }

                // 키워드 집계로부터 통계산출
                foreach (var kvp in analyzedKeywords)
                {
                    var keyword = kvp.Key;
                    var ak = kvp.Value;

                    // 누계 마킹
                    ak.cumulative = actualCumulative;

                    // 키워드 평균빈도
                    ak.avgF = ak.totalF / actualCumulative;
                    ak.avgR = ak.totalR / actualCumulative;

                    // 키워드 분산
                    ak.varF = (ak.totalSqrF / actualCumulative) - Sqr(ak.avgF);
                    ak.varR = (ak.totalSqrR / actualCumulative) - Sqr(ak.varR);

                    // 키워드 표준편차
                    ak.stdDevF = MathF.Sqrt(ak.varF);
                    ak.stdDevR = MathF.Sqrt(ak.varR);

                    float r = frontCD.GetRatio(keyword);

                    float r_1 = secondCD.GetRatio(keyword);

                    // 점수 산출
                    ak.score = Scoring2(0.7f, 0.3f, r, r_1, ak.avgR, ak.stdDevR);

                    ad.AddAnalyzedKeyword(ak);
                }
                return ad;
            }
        }

        float Scoring1(float alpha, float beta, float f, float f_1, float avgF, float stdDevF)
        {
            float result = alpha * (f - avgF) / stdDevF + beta * (f - f_1) / (f_1 + 1);
            return result;
        }
        float Scoring2(float alpha, float beta, float r, float r_1, float avgR, float stdDevR)
        {
            float epsilon = 1e-6f;

            float result = alpha * (r - avgR) / stdDevR + beta * MathF.Log(1 + (r - r_1) / (r_1 + epsilon));
            return result;
        }

        float Sqr(float x)
        { return x * x; }

        public class AnalyzeResult
        {
            [JsonInclude]
            public IReadOnlyAnalyzedData ad;
            [JsonInclude]
            public IReadOnlyList<Exception> exceptions;
            [JsonInclude]
            public long loopID = 0;
            [JsonInclude]
            public bool isSuccessful;
        }
    }
}
