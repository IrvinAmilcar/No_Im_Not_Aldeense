using UnityEngine;

public static class StatisticalUtils
{
    // --- DISTRIBUIÇÃO NORMAL (Térmica) ---
    public static float NormalPDF(float x, float mean, float stdDev)
    {
        float variance = stdDev * stdDev;
        float exponent = -0.5f * Mathf.Pow(x - mean, 2) / variance;
        float coefficient = 1f / (stdDev * Mathf.Sqrt(2 * Mathf.PI));
        return coefficient * Mathf.Exp(exponent);
    }

    public static float RandomNormal(float mean, float stdDev)
    {
        float u1 = 1.0f - Random.value;
        float u2 = 1.0f - Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        return mean + stdDev * randStdNormal;
    }

    // --- DISTRIBUIÇÃO BINOMIAL (Retina) ---
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
        for (int i = 1; i <= k; ++i) res = res * (n - i + 1) / i;
        return res;
    }

    // --- CÁLCULO DE DELTA (O quanto a probabilidade muda) ---
    // Baseado nas regras da página 3 do PDF
    public static float CalculateThermalDelta(float temp)
    {
        if (temp >= 35.5f && temp <= 37.5f) return Random.Range(10f, 20f); // Humano ideal
        if (temp > 37.5f && temp <= 39.0f) return Random.Range(0f, 5f);    // Febre
        if (temp < 34.0f) return Random.Range(-30f, -20f);                 // Muito frio (Impostor)
        return -5f; // Indeterminado
    }

    public static float CalculateRetinalDelta(int successes)
    {
        if (successes >= 7) return Random.Range(10f, 20f); // Humano (7-9)
        if (successes >= 5) return Random.Range(0f, 10f);  // Duvidoso (5-6)
        return Random.Range(-30f, -20f);                   // Impostor (0-3)
    }

    public static float CalculateNeuralDelta(bool isFlatline, bool isHumanPattern)
    {
        if (isFlatline) return Random.Range(-30f, -20f);
        if (isHumanPattern) return Random.Range(10f, 20f);
        return Random.Range(-5f, 5f); // Stress
    }
}