using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DE3000Manager : MonoBehaviour
{
    public static DE3000Manager Instance { get; private set; }

    public enum ScanMode { Thermal, Retinal, Neural }

    [Header("UI References")]
    public GameObject de3000Panel;      // O objeto que tem este script
    public GameObject de3000Background; // O PAI deste objeto (Fundo escuro)

    public RectTransform graphContainer;

    public TextMeshProUGUI modeText;
    public TextMeshProUGUI valueText;
    public TextMeshProUGUI probabilityText;
    public TextMeshProUGUI batteryText;

    [Header("Prefabs Visuais")]
    public GameObject lineRendererPrefab;
    public GameObject barPrefab;
    public GameObject pointPrefab;

    [Header("Configurações")]
    public float batteryLevel = 100f;
    public float costPerScan = 10f;

    private ScanMode currentMode = ScanMode.Thermal;
    private bool isScanning = false;
    private VisitorProfile activeProfile;
    private bool activeIsHuman;

    void Awake()
    {
        Instance = this;
        // Não alteramos SetActive aqui para evitar conflitos de inicialização
    }

    void Start()
    {
        // Garante que comece desligado no início do jogo
        if (de3000Background) de3000Background.SetActive(false);
        // Se o painel for filho, ele desliga junto automaticamente
    }

    public void ActivateDevice(VisitorProfile profile, bool isHuman)
    {
        activeProfile = profile;
        activeIsHuman = isHuman;

        // 1. Ligar o PAI primeiro (Obrigatório na Unity)
        if (de3000Background) de3000Background.SetActive(true);

        // 2. Ligar o Painel (caso esteja desligado individualmente)
        if (de3000Panel) de3000Panel.SetActive(true);

        SwitchMode(ScanMode.Thermal);
        UpdateBatteryUI();
    }

    public void DeactivateDevice()
    {
        // Desligar o PAI desliga tudo
        if (de3000Background) de3000Background.SetActive(false);

        // Avisa o Peephole para voltar
        if (PeepholeManager.Instance != null)
            PeepholeManager.Instance.ReturnToDialogue();
    }

    // --- BOTÕES ---

    public void OnClick_Thermal() { if (!isScanning) SwitchMode(ScanMode.Thermal); }
    public void OnClick_Retinal() { if (!isScanning) SwitchMode(ScanMode.Retinal); }
    public void OnClick_Neural() { if (!isScanning) SwitchMode(ScanMode.Neural); }

    public void OnClick_SCAN()
    {
        if (isScanning) return;

        if (batteryLevel < costPerScan)
        {
            if (valueText) valueText.text = "SEM BATERIA";
            return;
        }

        StartCoroutine(ScanRoutine());
    }

    public void OnClick_Back()
    {
        DeactivateDevice();
    }

    public void OnClick_Reset()
    {
        SwitchMode(currentMode);
    }

    // --- LÓGICA INTERNA ---

    private void SwitchMode(ScanMode mode)
    {
        currentMode = mode;
        ClearGraph();
        if (valueText) valueText.text = "---";
        if (probabilityText) probabilityText.text = "PRONTO";

        if (modeText)
        {
            switch (mode)
            {
                case ScanMode.Thermal: modeText.text = "TÉRMICA"; break;
                case ScanMode.Retinal: modeText.text = "RETINA"; break;
                case ScanMode.Neural: modeText.text = "NEURAL"; break;
            }
        }
    }

    private void UpdateBatteryUI()
    {
        if (batteryText)
        {
            batteryText.text = $"{batteryLevel:F0}%";
            if (batteryLevel < 20) batteryText.color = Color.red;
            else batteryText.color = Color.green;
        }
    }

    private IEnumerator ScanRoutine()
    {
        isScanning = true;
        if (valueText) valueText.text = "LENDO...";

        batteryLevel -= costPerScan;
        if (batteryLevel < 0) batteryLevel = 0;
        UpdateBatteryUI();

        yield return new WaitForSeconds(1.5f);

        if (activeProfile != null && activeProfile.characterName == "Irvin" && !activeProfile.isScannable)
        {
            if (valueText) valueText.text = "ERRO";
            if (probabilityText) probabilityText.text = "DADOS CORROMPIDOS";
            isScanning = false;
            yield break;
        }

        switch (currentMode)
        {
            case ScanMode.Thermal: GenerateThermalResult(); break;
            case ScanMode.Retinal: GenerateRetinalResult(); break;
            case ScanMode.Neural: GenerateNeuralResult(); break;
        }

        isScanning = false;
    }

    // (MÉTODOS DE GRÁFICO MANTIDOS IDÊNTICOS - Copie se necessário ou mantenha os existentes)
    // Certifique-se de que StatisticalUtils está no projeto

    void GenerateThermalResult()
    {
        float reading = activeIsHuman ? StatisticalUtils.RandomNormal(activeProfile.meanTemp, activeProfile.tempStdDev) : Random.Range(30.0f, 33.0f);
        valueText.text = $"{reading:F1}°C";
        float percent = Mathf.Clamp01(StatisticalUtils.NormalPDF(reading, 36.5f, 0.5f) / 0.8f) * 100f;
        SetProbabilityLabel(percent);
        float t = Mathf.InverseLerp(32f, 41f, reading);
        SpawnPointOnGraph(t, percent / 100f);
    }

    void GenerateRetinalResult()
    {
        int successes = 0;
        float p = activeIsHuman ? activeProfile.retinalProbability : 0.2f;
        for (int i = 0; i < 10; i++) if (Random.value < p) successes++;
        valueText.text = $"{successes}/10 SUCESSOS";
        float percent = Mathf.Clamp01(StatisticalUtils.BinomialProbability(successes, 10, activeProfile.retinalProbability) / 0.3f) * 100f;
        SetProbabilityLabel(percent);
    }

    void GenerateNeuralResult()
    {
        float[] basePattern = activeIsHuman ? activeProfile.neuralPattern : new float[] { 0.2f, 0.8f, 0.3f, 0.7f, 0.2f };
        float[] current = new float[5];
        for (int i = 0; i < 5; i++) current[i] = activeIsHuman ? Mathf.Clamp01(basePattern[i] + Random.Range(-0.15f, 0.15f)) : Random.Range(0.3f, 0.4f);
        float similarity = StatisticalUtils.CalculateSimilarity(current, basePattern);
        valueText.text = "PADRÃO CAPTURADO";
        SetProbabilityLabel(similarity * 100f);
    }

    void SetProbabilityLabel(float percent)
    {
        probabilityText.text = $"P(Humano): {percent:F1}%";
        if (percent > 60) probabilityText.color = Color.green;
        else if (percent > 30) probabilityText.color = Color.yellow;
        else probabilityText.color = Color.red;
    }

    void ClearGraph() { if (graphContainer) foreach (Transform child in graphContainer) Destroy(child.gameObject); }

    void SpawnPointOnGraph(float x, float y)
    {
        if (!pointPrefab || !graphContainer) return;
        GameObject p = Instantiate(pointPrefab, graphContainer);
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(x, y);
        rt.anchoredPosition = Vector2.zero;
    }
}