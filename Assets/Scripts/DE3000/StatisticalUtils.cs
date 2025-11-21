using UnityEngine;

public static class StatisticalUtils
{
    // --- DISTRIBUIÇÃO NORMAL (GAUSSIANA) ---
    // Fórmula da densidade de probabilidade: f(x)
    // Usada para desenhar a curva azul no gráfico
    public static float NormalPDF(float x, float mean, float stdDev)
    {
        float variance = stdDev * stdDev;
        float exponent = -0.5f * Mathf.Pow(x - mean, 2) / variance;
        float coefficient = 1f / (stdDev * Mathf.Sqrt(2 * Mathf.PI));
        return coefficient * Mathf.Exp(exponent);
    }

    // Gera um número aleatório seguindo uma distribuição normal (Box-Muller)
    // Usada para gerar a temperatura do visitante humano
    public static float RandomNormal(float mean, float stdDev)
    {
        float u1 = 1.0f - Random.value; // Uniforme(0,1]
        float u2 = 1.0f - Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                              Mathf.Sin(2.0f * Mathf.PI * u2);
        return mean + stdDev * randStdNormal;
    }

    // --- DISTRIBUIÇÃO BINOMIAL ---
    // Calcula a chance de ter exatamente 'k' sucessos em 'n' tentativas
    public static float BinomialProbability(int k, int n, float p)
    {
        if (k < 0 || k > n) return 0;
        return Combination(n, k) * Mathf.Pow(p, k) * Mathf.Pow(1 - p, n - k);
    }

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
    // Compara dois arrays de floats e retorna o quão parecidos são (0 a 1)
    public static float CalculateSimilarity(float[] dataA, float[] dataB)
    {
        if (dataA.Length != dataB.Length) return 0;

        float totalDiff = 0;
        for (int i = 0; i < dataA.Length; i++)
        {
            totalDiff += Mathf.Abs(dataA[i] - dataB[i]);
        }

        // Quanto menor a diferença, maior a similaridade
        // 5 barras, max diff é 5 (se um for 0 e outro 1). Normaliza.
        return Mathf.Clamp01(1f - (totalDiff / 2.5f));
    }
}