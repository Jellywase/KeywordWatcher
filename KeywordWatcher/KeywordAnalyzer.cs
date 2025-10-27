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
                while (actualCumulative < cumulative && cdSequence[actualCumulative] != null)
                {
                    actualCumulative++;
                }

                AnalyzedData ad = new(frontCD.name, frontCD, actualCumulative);

                // AnalyzedKeyword 인스턴스를 만들고 키워드 빈도를 누적 집계
                for (int i = 0; i < actualCumulative; i++)
                {
                    var cd = cdSequence[i];

                    foreach (var kvp in cd.keywords)
                    {
                        string keyword = kvp.Key;
                        var kd = kvp.Value;

                        AnalyzedKeyword ak;
                        if (!ad.analyzedKeywords.TryGetValue(keyword, out var rak))
                        {
                            rak = ak = new AnalyzedKeyword(keyword);
                            ad.AddAnalyzedKeyword(ak);
                        }
                        ak = (AnalyzedKeyword)rak;
                        ak.totalF += kd.frequency;
                        ak.totalSqrF += (int)Sqr(kd.frequency);
                        var r = cd.GetRatio(keyword);
                        ak.totalR += r;
                        ak.totalSqrR += Sqr(r);

                        // 선두 빈도 저장
                        if (i == 0)
                        {
                            ak.frontF = kd.frequency;
                        }
                    }
                }

                // 키워드 집계로부터 통계산출
                foreach (var kvp in ad.analyzedKeywords)
                {
                    var keyword = kvp.Key;
                    var ak = (AnalyzedKeyword)kvp.Value;

                    // 누계 마킹
                    ak.cumulative = actualCumulative;

                    // 키워드 평균빈도
                    ak.avgF = (float)ak.totalF / actualCumulative;
                    ak.avgR = (float)ak.totalR / actualCumulative;

                    // 키워드 분산
                    ak.varF = ((float)ak.totalSqrF / actualCumulative) - Sqr(ak.avgF);
                    ak.varR = (ak.totalSqrR / actualCumulative) - Sqr(ak.varR);

                    // 키워드 표준편차
                    ak.stdDevF = MathF.Sqrt(ak.varF);
                    ak.stdDevR = MathF.Sqrt(ak.varR);

                    // 빈도 비율
                    float r = frontCD.GetRatio(keyword);
                    float r_1 = secondCD.GetRatio(keyword);

                    // EMAR
                    float lastEMAR;
                    if (lastAD == null)
                    { lastEMAR = 0; }
                    else
                    { lastEMAR = lastAD.analyzedKeywords.TryGetValue(keyword, out var lak) ? lak.emaR : 0; }
                    ak.emaR = GetEMAR(2f / (cumulative + 1f), lastEMAR, r);

                    // 점수 산출
                    ak.score = Scoring1(0.5f, 0.5f, r, r_1, ak.avgR, ak.stdDevR);

                    ad.AddAnalyzedKeyword(ak);
                }
                return ad;
            }
        }
        float Scoring1(float alpha, float beta, float r, float r_1, float avgR, float stdDevR)
        {

            float x = (r - avgR) / MathF.Sqrt(stdDevR);
            float y = (r - r_1) / MathF.Sqrt((r + r_1) / 2);
            float result = alpha * x + beta * y;
            return result;
        }
        float Scoring2(float alpha, float beta, float r, float r_1, float avgR, float stdDevR)
        {

            float x = (r - avgR) / MathF.Sqrt(stdDevR);
            float y = (r - r_1) / MathF.Sqrt((r + r_1) / 2);
            float result = alpha * x + beta * y;
            return result;
        }

        float Scoring3(float alpha, float beta, float f, float f_1, float avgF, float stdDevF)
        {
            float result = alpha * (f - avgF) / stdDevF + beta * (f - f_1) / (f_1 + 1);
            return result;
        }

        float Scoring4(float alpha, float beta, float r, float r_1, float avgR, float stdDevR)
        {
            float epsilon = 1e-6f;
            float result = (1 - MathF.Pow(r - 1, 4)) * (alpha * (r - avgR) / stdDevR + beta * MathF.Log(1 + (r - r_1) / (r_1 + epsilon)));
            return result;
        }

        float Sqr(float x)
        { return x * x; }

        float GetEMAR(float lambda, float lastEMAR, float r)
        {
            return lambda * r + (1 - lambda) * lastEMAR;
        }

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
