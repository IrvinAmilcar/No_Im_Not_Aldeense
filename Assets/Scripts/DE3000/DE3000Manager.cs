using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using XCharts.Runtime;
using DG.Tweening;

public class DE3000Manager : MonoBehaviour
{
    public static DE3000Manager Instance { get; private set; }

    public enum ScanMode { Idle, Thermal, Retinal, Neural, History }

    [Header("Hierarquia Principal")]
    public GameObject de3000Panel;
    public GameObject de3000Background;

    [Header("Visualização XCharts")]
    public LineChart thermalChart;
    public BarChart retinalChart;
    public BarChart neuralChart;
    public BarChart historyChart;

    [Header("UI de Texto")]
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI globalProbText;
    public TextMeshProUGUI batteryText;
    public TextMeshProUGUI statusText;

    [Header("Configurações")]
    public float pGlobal = 50f;
    public float batteryLevel = 100f;

    private const float BASE_COST = 10f;
    private const float MULT_THERMAL = 0.5f;
    private const float MULT_RETINAL = 1.0f;
    private const float MULT_NEURAL = 1.5f;

    private ScanMode currentMode = ScanMode.Idle;
    private bool isScanning = false;
    private VisitorProfile activeProfile;
    private bool activeIsHuman;

    private float deltaThermal = 0;
    private float deltaRetinal = 0;
    private float deltaNeural = 0;

    // Animação
    private RectTransform panelRect;
    private float offScreenY = -1200f;
    private float targetY;

    void Awake()
    {
        Instance = this;
        if (de3000Panel)
        {
            panelRect = de3000Panel.GetComponent<RectTransform>();
            if (panelRect) targetY = panelRect.anchoredPosition.y;
        }
    }

    void Start()
    {
        if (panelRect)
        {
            Vector2 pos = panelRect.anchoredPosition;
            pos.y = offScreenY;
            panelRect.anchoredPosition = pos;
        }
        batteryLevel = 100f;
    }

    // --- ATIVAÇÃO ---

    // --- Variáveis de Dificuldade ---
    private int currentDayIndex = 0;
    private float mimicFactor = 0f; // 0 = Fácil (Óbvio), 1 = Pesadelo (Indistinguível)

    public void ActivateDevice(VisitorProfile profile, bool isHuman)
    {
        activeProfile = profile;
        activeIsHuman = isHuman;

        // 1. Pega o dia atual do Gerente
        if (DayCycleManager.Instance != null)
        {
            currentDayIndex = DayCycleManager.Instance.GetCurrentDayIndex();
        }
        else
        {
            currentDayIndex = 0; // Fallback para Dia 1
        }

        // 2. Calcula o Fator de Camuflagem (Mimic Factor)
        // Dia 1 (Index 0) -> 0.0
        // Dia 3 (Index 2) -> 0.5
        // Dia 5 (Index 4) -> 1.0
        // Dividimos por 4f pois são 5 dias (0 a 4). Ajuste se tiver mais dias.
        mimicFactor = Mathf.Clamp01(currentDayIndex / 4f);

        Debug.Log($"[DE3000] Dia {currentDayIndex + 1}. Nível de Camuflagem: {mimicFactor * 100}%");

        // (Resto do código de inicialização igual...)
        pGlobal = 50f;
        deltaThermal = 0; deltaRetinal = 0; deltaNeural = 0;
        CleanChart(thermalChart); CleanChart(retinalChart); CleanChart(neuralChart); CleanChart(historyChart);
        UpdateBatteryUI(); UpdateGlobalProbUI(); SwitchMode(ScanMode.Idle);

        if (de3000Background) de3000Background.SetActive(true);
        if (de3000Panel) de3000Panel.SetActive(true);

        if (panelRect)
        {
            Vector2 pos = panelRect.anchoredPosition;
            pos.y = offScreenY;
            panelRect.anchoredPosition = pos;
            panelRect.DOAnchorPosY(targetY, 1.2f).SetEase(Ease.OutBack);
        }
    }

    public void DeactivateDevice()
    {
        // Animação de saída normal (com callback para voltar ao diálogo)
        if (panelRect)
        {
            panelRect.DOAnchorPosY(offScreenY, 0.4f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (de3000Background) de3000Background.SetActive(false);
                    if (PeepholeManager.Instance != null)
                        PeepholeManager.Instance.ReturnToDialogue();
                });
        }
        else
        {
            ForceClose();
            if (PeepholeManager.Instance != null)
                PeepholeManager.Instance.ReturnToDialogue();
        }
    }

    // --- NOVO MÉTODO: FECHAMENTO INSTANTÂNEO (Para quando chega visita) ---
    public void ForceClose()
    {
        // Mata a animação atual para impedir que o OnComplete rode e sobrescreva o texto
        if (panelRect)
        {
            panelRect.DOKill();
            Vector2 pos = panelRect.anchoredPosition;
            pos.y = offScreenY;
            panelRect.anchoredPosition = pos;
        }

        if (de3000Background) de3000Background.SetActive(false);
    }

    // --- RESTO DO SCRIPT (IGUAL) ---

    private void CleanChart(BaseChart chart)
    {
        if (chart != null) { chart.RemoveAllSerie(); chart.ClearData(); }
    }

    public void OnClick_ModeThermal() { if (!isScanning) SwitchMode(ScanMode.Thermal); }
    public void OnClick_ModeRetinal() { if (!isScanning) SwitchMode(ScanMode.Retinal); }
    public void OnClick_ModeNeural() { if (!isScanning) SwitchMode(ScanMode.Neural); }

    public void OnClick_GraphHistory()
    {
        if (!isScanning)
        {
            SwitchMode(ScanMode.History);
            UpdateHistoryChart();
        }
    }

    public void OnClick_Scan()
    {
        if (isScanning || currentMode == ScanMode.History || currentMode == ScanMode.Idle) return;

        float cost = GetScanCost();
        if (batteryLevel < cost)
        {
            if (statusText) statusText.text = "BATERIA INSUFICIENTE";
            return;
        }

        StartCoroutine(ScanRoutine());
    }

    public void OnClick_Back() { DeactivateDevice(); }

    private void SwitchMode(ScanMode mode)
    {
        currentMode = mode;

        if (thermalChart) thermalChart.gameObject.SetActive(false);
        if (retinalChart) retinalChart.gameObject.SetActive(false);
        if (neuralChart) neuralChart.gameObject.SetActive(false);
        if (historyChart) historyChart.gameObject.SetActive(false);

        if (statusText) statusText.text = "PRONTO";

        switch (mode)
        {
            case ScanMode.Idle:
                if (modeText) modeText.text = "DE-3000";
                if (statusText) statusText.text = "SELECIONE UM MODO";
                break;
            case ScanMode.Thermal:
                if (thermalChart) thermalChart.gameObject.SetActive(true);
                if (modeText) modeText.text = "TÉRMICA";
                break;
            case ScanMode.Retinal:
                if (retinalChart) retinalChart.gameObject.SetActive(true);
                if (modeText) modeText.text = "RETINA";
                break;
            case ScanMode.Neural:
                if (neuralChart) neuralChart.gameObject.SetActive(true);
                if (modeText) modeText.text = "NEURAL";
                break;
            case ScanMode.History:
                if (historyChart) historyChart.gameObject.SetActive(true);
                if (modeText) modeText.text = "HISTÓRICO";
                break;
        }
    }

    private float GetScanCost()
    {
        switch (currentMode)
        {
            case ScanMode.Thermal: return BASE_COST * MULT_THERMAL;
            case ScanMode.Retinal: return BASE_COST * MULT_RETINAL;
            case ScanMode.Neural: return BASE_COST * MULT_NEURAL;
            default: return 0;
        }
    }

    private IEnumerator ScanRoutine()
    {
        isScanning = true;

        batteryLevel -= GetScanCost();
        if (batteryLevel < 0) batteryLevel = 0;
        UpdateBatteryUI();

        if (statusText) statusText.text = "ANALISANDO...";
        yield return new WaitForSeconds(0.5f);
        if (statusText) statusText.text = "";
        yield return new WaitForSeconds(1.0f);

        float delta = 0;

        switch (currentMode)
        {
            case ScanMode.Thermal:
                delta = PerformThermalScan();
                deltaThermal += delta;
                break;
            case ScanMode.Retinal:
                delta = PerformRetinalScan();
                deltaRetinal += delta;
                break;
            case ScanMode.Neural:
                delta = PerformNeuralScan();
                deltaNeural += delta;
                break;
        }

        pGlobal = Mathf.Clamp(pGlobal + delta, 5f, 95f);
        UpdateGlobalProbUI();

        if (statusText) statusText.text = $"DELTA: {(delta >= 0 ? "+" : "")}{delta:F0}%";

        isScanning = false;
    }

    private float PerformThermalScan()
    {
        float reading;

        if (activeIsHuman)
        {
            reading = StatisticalUtils.RandomNormal(activeProfile.meanTemp, activeProfile.tempStdDev);
        }
        else
        {
            // IMPOSTOR EVOLUTIVO
            // Dia 1: Base 32.0ºC (Frio, fácil de ver)
            // Dia 5: Base 36.0ºC (Quase humano, sobrepõe com 'frio/doente')
            float baseTemp = Mathf.Lerp(32.0f, 36.0f, mimicFactor);

            // Adiciona ruído para não ficar um número fixo
            reading = baseTemp + Random.Range(-0.3f, 0.5f);
        }

        // (Visualização Gráfica - Mantenha o código anterior aqui...)
        if (thermalChart != null)
        {
            thermalChart.RemoveAllSerie();
            var lineSerie = thermalChart.AddSerie<Line>("Referencia");
            lineSerie.symbol.show = false; lineSerie.lineStyle.width = 2f;
            var pointSerie = thermalChart.AddSerie<Scatter>("Leitura");
            pointSerie.symbol.size = 20f; pointSerie.itemStyle.color = Color.red;
            thermalChart.ClearData();
            for (float i = 32f; i <= 41f; i += 0.1f) thermalChart.AddData(0, i, StatisticalUtils.NormalPDF(i, 36.5f, 0.5f));
            float readingY = StatisticalUtils.NormalPDF(reading, 36.5f, 0.5f);
            if (readingY < 0.01f) readingY = 0.01f;
            thermalChart.AddData(1, reading, readingY);
        }

        return StatisticalUtils.CalculateThermalDelta(reading);
    }

    private float PerformRetinalScan()
    {
        int successes = 0;

        if (activeIsHuman)
        {
            float p = activeProfile.retinalProbability;
            for (int i = 0; i < 10; i++) if (Random.value < p) successes++;
        }
        else
        {
            // IMPOSTOR EVOLUTIVO
            // Dia 1: Probabilidade baixa (0.2) -> Gera ~2 sucessos (Óbvio)
            // Dia 5: Probabilidade alta (0.7) -> Gera ~7 sucessos (Confunde com humano cansado)
            float pImpostor = Mathf.Lerp(0.2f, 0.7f, mimicFactor);

            for (int i = 0; i < 10; i++) if (Random.value < pImpostor) successes++;
        }

        // (Visualização Gráfica igual...)
        if (retinalChart != null)
        {
            retinalChart.RemoveAllSerie();
            var barSerie = retinalChart.AddSerie<Bar>("Acertos");
            barSerie.itemStyle.color = new Color(0f, 1f, 0f, 0.7f);
            barSerie.barWidth = 40f;
            retinalChart.ClearData();
            retinalChart.AddData(0, 0, successes);
        }

        return StatisticalUtils.CalculateRetinalDelta(successes);
    }

    private float PerformNeuralScan()
    {
        float[] pattern = new float[5];
        bool flatline = false;
        bool isHumanPattern = false;

        if (activeIsHuman)
        {
            // Padrão Humano: Picos em 10Hz e 25Hz
            pattern[0] = Random.Range(0.1f, 0.3f);
            pattern[1] = Random.Range(0.7f, 0.9f);
            pattern[2] = Random.Range(0.5f, 0.7f);
            pattern[3] = Random.Range(0.2f, 0.4f);
            pattern[4] = Random.Range(0.6f, 0.8f);
            isHumanPattern = true;
        }
        else
        {
            // IMPOSTOR EVOLUTIVO
            if (mimicFactor < 0.3f) // Dia 1-2 (Fácil)
            {
                // Flatline ou Ruído Baixo
                float noise = Random.Range(0.1f, 0.2f);
                for (int i = 0; i < 5; i++) pattern[i] = noise;
                flatline = true;
            }
            else
            {
                // Dia 3-5 (Difícil)
                // Tenta imitar os picos humanos, mas com "falhas"
                pattern[0] = Random.Range(0.1f, 0.4f);

                // Tenta imitar o pico de 10Hz (Alpha), mas varia conforme a camuflagem
                // Quanto maior o mimicFactor, mais perto de 0.8 ele chega
                pattern[1] = Mathf.Lerp(0.3f, 0.8f, mimicFactor) + Random.Range(-0.1f, 0.1f);

                pattern[2] = Random.Range(0.4f, 0.6f);
                pattern[3] = Random.Range(0.2f, 0.5f);

                // Tenta imitar pico de 25Hz
                pattern[4] = Mathf.Lerp(0.3f, 0.7f, mimicFactor) + Random.Range(-0.1f, 0.1f);
            }
        }

        // (Visualização Gráfica igual...)
        if (neuralChart != null)
        {
            neuralChart.RemoveAllSerie();
            var barSerie = neuralChart.AddSerie<Bar>("Frequencias");
            barSerie.itemStyle.color = new Color(0.5f, 0f, 1f, 0.8f);
            neuralChart.ClearData();
            for (int i = 0; i < 5; i++) neuralChart.AddData(0, i, pattern[i]);
        }

        return StatisticalUtils.CalculateNeuralDelta(flatline, isHumanPattern);
    }

    private void UpdateHistoryChart()
    {
        if (historyChart == null) return;
        historyChart.RemoveAllSerie();
        historyChart.ClearData();

        var xAxis = historyChart.EnsureChartComponent<XAxis>();
        var yAxis = historyChart.EnsureChartComponent<YAxis>();

        if (xAxis != null && yAxis != null)
        {
            xAxis.type = Axis.AxisType.Category;
            if (xAxis.data != null) { xAxis.data.Clear(); xAxis.data.Add("Térmica"); xAxis.data.Add("Retina"); xAxis.data.Add("Neural"); }
            yAxis.type = Axis.AxisType.Value;

            var barSerie = historyChart.AddSerie<Bar>("Historico");
            if (barSerie != null)
            {
                barSerie.itemStyle.color = new Color(1f, 0.8f, 0f, 0.8f);
                if (barSerie.label != null) { barSerie.label.show = true; barSerie.label.position = LabelStyle.Position.Top; }

                historyChart.AddData(0, deltaThermal);
                historyChart.AddData(0, deltaRetinal);
                historyChart.AddData(0, deltaNeural);
                historyChart.RefreshChart();
            }
        }
    }

    private void UpdateBatteryUI()
    {
        if (batteryText) { batteryText.text = $"{batteryLevel:F0}%"; batteryText.color = batteryLevel < 20 ? Color.red : Color.green; }
    }

    private void UpdateGlobalProbUI()
    {
        if (globalProbText)
        {
            globalProbText.text = $"P(Humano): {pGlobal:F0}%";
            if (pGlobal > 80) globalProbText.color = Color.green;
            else if (pGlobal < 40) globalProbText.color = Color.red;
            else globalProbText.color = Color.yellow;
        }
    }
}