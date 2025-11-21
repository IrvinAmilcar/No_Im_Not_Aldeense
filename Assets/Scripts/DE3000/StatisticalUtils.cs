using UnityEngine;

public static class StatisticalUtils
{
    // --- DISTRIBUIÇÃO NORMAL (GAUSSIANA) ---
    // Fórmula: f(x) = (1 / (sigma * sqrt(2*pi))) * e^(-0.5 * ((x - mu) / sigma)^2)
    public static float NormalPDF(float x, float mean, float stdDev)
    {
        float variance = stdDev * stdDev;
        float exponent = -0.5f * Mathf.Pow(x - mean, 2) / variance;
        float coefficient = 1f / (stdDev * Mathf.Sqrt(2 * Mathf.PI));
        return coefficient * Mathf.Exp(exponent);
    }

    // --- DISTRIBUIÇÃO BINOMIAL ---
    // Fórmula: P(k) = C(n, k) * p^k * (1-p)^(n-k)
    public static float BinomialProbability(int k, int n, float p)
    {
        if (k < 0 || k > n) return 0;
        return Combination(n, k) * Mathf.Pow(p, k) * Mathf.Pow(1 - p, n - k);
    }

    // Combinação: C(n, k) = n! / (k! * (n-k)!)
    private static long Combination(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        if (k == 0 || k == n) return 1;
        if (k > n / 2) k = n - k;

        long res = 1;
        for (int i = 1; i <= k; ++i)
        {
            res = res * (n - i + 1) / i;
        }
        return res;
    }

    // --- CORRELAÇÃO (Para o modo Neural) ---
    // Calcula quão parecido o histograma atual é com o padrão humano
    public static float CalculateCorrelation(float[] dataA, float[] dataB)
    {
        if (dataA.Length != dataB.Length) return 0;

        float sum = 0;
        for (int i = 0; i < dataA.Length; i++)
        {
            // Similaridade simples baseada na diferença absoluta invertida
            float diff = Mathf.Abs(dataA[i] - dataB[i]);
            sum += (1f - Mathf.Clamp01(diff));
        }
        return sum / dataA.Length; // Retorna 0 a 1
    }
}