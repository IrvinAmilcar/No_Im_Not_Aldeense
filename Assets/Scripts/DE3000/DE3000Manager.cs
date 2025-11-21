using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DE3000Manager : MonoBehaviour
{
    public static DE3000Manager Instance { get; private set; }

    public enum ScanMode { Thermal, Retinal, Neural }

    [Header("UI References")]
    public GameObject de3000Root; // O objeto pai de todo o dispositivo
    public RectTransform graphContainer; // Onde os gráficos aparecem (o quadrado branco)
    public TextMeshProUGUI modeLabel;
    public TextMeshProUGUI valueReadout;
    public TextMeshProUGUI probabilityText;
    public Image batteryFill; // Barra de bateria

    [Header("Prefabs Gráficos")]
    [Tooltip("Prefab com uma Image branca para criar barras")]
    public GameObject barGraphPrefab;
    [Tooltip("Prefab com LineRenderer para curvas")]
    public GameObject lineGraphPrefab;
    [Tooltip("Ponto vermelho que indica a leitura")]
    public GameObject readingIndicatorPrefab;

    [Header("Configurações")]
    public float batteryLevel = 100f;
    public float scanCost = 10f;

    // Estado Interno
    private VisitorProfile currentVisitor;
    private bool isScanning = false;
    private GameObject currentGraphObject; // Limpeza
    private ScanMode currentMode = ScanMode.Thermal;

    void Awake()
    {
        Instance = this;
        de3000Root.SetActive(false);
    }

    // Chamado pelo PeepholeManager quando clica em "Usar DE3000"
    public void OpenDevice(VisitorProfile visitor)
    {
        currentVisitor = visitor;
        de3000Root.SetActive(true);
        SwitchMode(ScanMode.Thermal); // Começa no térmico
    }

    public void CloseDevice()
    {
        de3000Root.SetActive(false);
        // Volta para o diálogo normal
        PeepholeManager.Instance.ReturnToDialogue();
    }

    // --- SISTEMA DE BOTÕES (Ligue isso nos botões da UI da Unity) ---

    public void OnButtonScan()
    {
        if (isScanning || batteryLevel <= 0) return;
        StartCoroutine(PerformScanRoutine());
    }

    public void OnButtonModeSwitch()
    {
        if (isScanning) return;

        // Cicla entre os modos: 0 -> 1 -> 2 -> 0
        int next = (int)currentMode + 1;
        if (next > 2) next = 0;
        SwitchMode((ScanMode)next);
    }

    // --------------------------------------------------------------

    private void SwitchMode(ScanMode mode)
    {
        currentMode = mode;
        ClearGraph();
        valueReadout.text = "PRONTO";
        probabilityText.text = "";

        switch (mode)
        {
            case ScanMode.Thermal:
                modeLabel.text = "TÉRMICA (NORMAL)";
                DrawTheoreticalNormalCurve();
                break;
            case ScanMode.Retinal:
                modeLabel.text = "RETINA (BINOMIAL)";
                DrawTheoreticalBinomial();
                break;
            case ScanMode.Neural:
                modeLabel.text = "NEURAL (ESPECTRO)";
                DrawTheoreticalNeural();
                break;
        }
    }

    private IEnumerator PerformScanRoutine()
    {
        isScanning = true;
        valueReadout.text = "LENDO...";

        // Consome Bateria
        batteryLevel -= scanCost;
        if (batteryFill != null) batteryFill.fillAmount = batteryLevel / 100f;

        // Simula tempo de scan (animação de texto ou barulho)
        yield return new WaitForSeconds(1.5f);

        // Verifica se é o caso especial (Irvin Dia 5 - GLITCH)
        if (currentVisitor.characterName == "Irvin" && currentVisitor.humanityType == HumanityType.AlwaysImpostor)
        {
            ShowGlitchError();
            isScanning = false;
            yield break;
        }

        // Determina se é humano AGORA (baseado na lógica do PeepholeManager)
        bool isHuman = PeepholeManager.Instance.IsCurrentVisitorHuman;

        switch (currentMode)
        {
            case ScanMode.Thermal:
                ScanThermal(isHuman);
                break;
            case ScanMode.Retinal:
                ScanRetinal(isHuman);
                break;
            case ScanMode.Neural:
                ScanNeural(isHuman);
                break;
        }

        isScanning = false;
    }

    // --- LÓGICA E VISUALIZAÇÃO DOS MODOS ---

    // 1. TÉRMICA (Curva Normal)
    private void DrawTheoreticalNormalCurve()
    {
        // Desenha a curva azul "ideal" (Humana: 36.5, sd 0.5)
        // Isso é apenas visual, usa LineRenderer para desenhar o sino
        // ... (Código de desenho de linha simplificado para brevidade)
    }

    private void ScanThermal(bool isHuman)
    {
        float reading;
        if (isHuman)
        {
            // Gera valor dentro da curva normal (Box-Muller transform)
            float u1 = 1.0f - Random.value;
            float u2 = 1.0f - Random.value;
            float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                         Mathf.Sin(2.0f * Mathf.PI * u2);

            // Média 36.5, Desvio 0.5 (pode adicionar febre se estiver estressado)
            reading = 36.5f + (0.5f * randStdNormal);
        }
        else
        {
            // Impostor: Temperatura fria (Uniforme entre 30 e 33)
            reading = Random.Range(30.0f, 33.0f);
        }

        valueReadout.text = $"{reading:F1}°C";

        // Calcula Probabilidade usando a PDF
        float prob = StatisticalUtils.NormalPDF(reading, 36.5f, 0.5f);
        // Normaliza para porcentagem visual (PDF max é aprox 0.8)
        float displayProb = Mathf.Clamp01(prob / 0.8f) * 100f;

        SetProbabilityText(displayProb);

        // Plotar ponto vermelho no gráfico (Lógica visual)
        SpawnIndicatorOnGraphX((reading - 32f) / (41f - 32f)); // Normaliza posição X
    }

    // 2. RETINAL (Binomial)
    private void DrawTheoreticalBinomial()
    {
        // Desenha barras azuis mostrando o esperado (8 ou 9 sucessos)
    }

    private void ScanRetinal(bool isHuman)
    {
        int successes = 0;
        int trials = 10;
        float p = isHuman ? 0.85f : 0.2f; // Humano acerta muito, impostor erra muito

        for (int i = 0; i < trials; i++)
        {
            if (Random.value < p) successes++;
        }

        valueReadout.text = $"{successes}/{trials} SACADAS";

        // Calcula chance de um humano ter esse resultado
        float prob = StatisticalUtils.BinomialProbability(successes, trials, 0.85f);
        // O valor máximo da binomial (para k=8 ou 9) é aprox 0.3
        float displayProb = Mathf.Clamp01(prob / 0.3f) * 100f;

        SetProbabilityText(displayProb);

        // Anima barra vermelha subindo no índice 'successes'
    }

    // 3. NEURAL (Histograma)
    private void DrawTheoreticalNeural()
    {
        // Desenha picos em Alpha (8-12hz) e Beta
    }

    private void ScanNeural(bool isHuman)
    {
        // Gera 5 barras de frequência
        float[] readings = new float[5];

        if (isHuman)
        {
            // Padrão Humano: Picos em indices 1 e 3
            readings[0] = Random.Range(0.1f, 0.3f); // 5hz
            readings[1] = Random.Range(0.7f, 0.9f); // 10hz (Alpha - Alto)
            readings[2] = Random.Range(0.2f, 0.4f); // 15hz
            readings[3] = Random.Range(0.6f, 0.8f); // 20hz (Beta - Médio)
            readings[4] = Random.Range(0.1f, 0.2f); // 25hz
        }
        else
        {
            // Padrão Impostor: Flatline ou Ruído aleatório uniforme
            float noiseLevel = Random.Range(0.2f, 0.4f);
            for (int i = 0; i < 5; i++) readings[i] = noiseLevel + Random.Range(-0.05f, 0.05f);
        }

        // Visualização das barras (Implementar instanciação de prefabs aqui)
        // ...

        // Cálculo de "Humanidade" baseada na correlação com o perfil ideal
        float correlation = isHuman ? Random.Range(85f, 99f) : Random.Range(5f, 30f);
        valueReadout.text = "PADRÃO ANALISADO";
        probabilityText.text = $"P(Humano): {correlation:F1}%";

        // Muda cor do texto
        probabilityText.color = correlation > 50 ? Color.green : Color.red;
    }

    // --- UTILITÁRIOS VISUAIS ---

    private void SetProbabilityText(float percent)
    {
        probabilityText.text = $"P(Humano): {percent:F1}%";
        if (percent > 60) probabilityText.color = Color.green;
        else if (percent > 30) probabilityText.color = Color.yellow;
        else probabilityText.color = Color.red;
    }

    private void ShowGlitchError()
    {
        ClearGraph();
        valueReadout.text = "!!! ERRO CRÍTICO !!!";
        valueReadout.color = Color.red;
        probabilityText.text = "DADOS CORROMPIDOS";
        // Tocar som de glitch
    }

    private void ClearGraph()
    {
        // Destrói objetos temporários (pontos, barras vermelhas) criados no gráfico
        foreach (Transform child in graphContainer)
        {
            if (child.name.Contains("Temp")) Destroy(child.gameObject);
        }
    }

    // Helper visual para criar um ponto no gráfico na posição X (0 a 1)
    private void SpawnIndicatorOnGraphX(float normalizedX)
    {
        if (readingIndicatorPrefab == null) return;

        GameObject dot = Instantiate(readingIndicatorPrefab, graphContainer);
        dot.name = "Temp_Reading";
        RectTransform rect = dot.GetComponent<RectTransform>();

        float width = graphContainer.rect.width;
        // Posiciona
        rect.anchoredPosition = new Vector2(normalizedX * width, 0); // Y pode ser ajustado para ficar na curva
    }
}