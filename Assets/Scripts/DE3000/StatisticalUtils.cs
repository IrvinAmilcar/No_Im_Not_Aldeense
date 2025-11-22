using UnityEngine;

public static class StatisticalUtils
{
    // --- DISTRIBUIÇÃO NORMAL (Térmica) ---

    public static float NormalPDF(float x, float mean, float stdDev)
    {
        float variance = stdDev * stdDev;
        // Z-Score ao quadrado e negativo: -0.5 * ((x - mean)/stdDev)^2
        float exponent = -0.5f * Mathf.Pow(x - mean, 2) / variance;

        // Constante de Normalização: 1 / (sigma * sqrt(2 * PI))
        // Na sua imagem: 1 / (0.5 * 2.506...) = 1 / 1.253 = 0.798
        float coefficient = 1f / (stdDev * Mathf.Sqrt(2 * Mathf.PI));

        return coefficient * Mathf.Exp(exponent);
    }

    // Gera temperatura com "ruído" para dificultar
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

    // --- SISTEMA DE DELTAS (BALANCEAMENTO DE GAMEPLAY) ---
    // Aqui está o segredo da dificuldade. Valores menores e áreas cinzas.

    public static float CalculateThermalDelta(float temp)
    {
        // Humano Ideal (36.2 a 36.8)
        if (temp >= 36.2f && temp <= 36.8f) return Random.Range(8f, 12f);

        // Zona Cinza (Pode ser Humano com frio/febre OU Impostor bem disfarçado)
        // 35.5 a 36.2 (Hipotermia leve) ou 36.8 a 37.5 (Estado febril)
        if ((temp >= 35.5f && temp < 36.2f) || (temp > 36.8f && temp <= 37.5f))
            return Random.Range(-3f, 5f); // Gera DÚVIDA (pode subir ou descer pouco)

        // Febre Alta (Provavelmente humano doente, mas arriscado)
        if (temp > 37.5f) return Random.Range(2f, 6f);

        // Frio Anormal (Provável Impostor, mas não garantido)
        if (temp < 35.5f && temp > 34.0f) return Random.Range(-10f, -5f);

        // Morto/Impostor Óbvio (< 34.0)
        return Random.Range(-20f, -15f);
    }

    public static float CalculateRetinalDelta(int successes)
    {
        // Humano Saudável (8-10) - Olhos vivos
        if (successes >= 8) return Random.Range(8f, 12f);

        // Zona de Tensão (6-7) - Humano cansado ou Impostor aprendendo?
        if (successes >= 6) return Random.Range(0f, 5f);

        // Zona de Perigo (4-5) - Muito suspeito
        if (successes >= 4) return Random.Range(-8f, -2f);

        // Olhar de Peixe Morto (0-3)
        return Random.Range(-20f, -15f);
    }

    public static float CalculateNeuralDelta(bool isFlatline, bool isHumanPattern)
    {
        // Padrão Humano Claro
        if (isHumanPattern) return Random.Range(10f, 15f);

        // Flatline (Morte Cerebral ou Impostor)
        if (isFlatline) return Random.Range(-25f, -15f);

        // Padrão Caótico (Stress ou Interferência)
        // Nem sobe muito, nem desce muito. Deixa o jogador na mão.
        return Random.Range(-5f, 5f);
    }
}