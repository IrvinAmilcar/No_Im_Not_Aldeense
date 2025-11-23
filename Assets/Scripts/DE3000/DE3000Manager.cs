using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using XCharts.Runtime;
using DG.Tweening;

public class DE3000Manager : MonoBehaviour
{
    public static DE3000Manager Instance { get; private set; }

    public enum ScanMode { Idle, Thermal, Retinal, Neural, History }

    [Header("Hierarquia Principal")]
    public GameObject de3000Panel;
    public GameObject de3000Background;

    [Header("Sistema de Ajuda")]
    public GameObject helpContainer;
    public CanvasGroup helpCanvasGroup;
    public TextMeshProUGUI helpBodyText;
    public ScrollRect helpScrollRect;

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

    [Header("Configurações de Gameplay")]
    public float pGlobal = 50f;
    public float batteryLevel = 100f;

    // Custos de Bateria
    private const float BASE_COST = 10f;
    private const float MULT_THERMAL = 0.5f; // 5%
    private const float MULT_RETINAL = 1.0f; // 10%
    private const float MULT_NEURAL = 1.5f;  // 15%

    // Estado Interno
    private ScanMode currentMode = ScanMode.Idle;
    private bool isScanning = false;
    private bool isHelpOpen = false;

    private VisitorProfile activeProfile;
    private bool activeIsHuman;

    // Dificuldade Dinâmica
    private int currentDayIndex = 0;
    private float mimicFactor = 0f;

    // Histórico de Deltas
    private float deltaThermal = 0;
    private float deltaRetinal = 0;
    private float deltaNeural = 0;

    // Animação
    private RectTransform animationRect;
    private float offScreenY = -1200f;
    private float targetY;

    // Textos de Ajuda
    [TextArea(3, 5)] private string txtHelpIdle = "MANUAL GERAL:\n\nSelecione um modo acima para iniciar a verificação.\n\nO DE3000 calcula a probabilidade baseada em 3 testes distintos.\n\nUse os botões acima para ler sobre cada teste específico.";
    [TextArea(3, 5)] private string txtHelpThermal = "ANÁLISE TÉRMICA:\n\nBaseada na Curva Gaussiana.\n\nHumanos mantêm temperatura constante (~36.5ºC).\n\nImpostores tendem a ser frios (<34ºC) ou apresentar variações anormais.\n\nObserve se o ponto vermelho se alinha com o topo da curva.";
    [TextArea(3, 5)] private string txtHelpRetinal = "ANÁLISE RETINAL:\n\nBaseada na Distribuição Binomial.\n\nDetecta micro-movimentos oculares involuntários.\n\nHumanos: 7 a 10 movimentos (Alta frequência).\n\nImpostores: 0 a 4 movimentos (Olhar fixo/morto).";
    [TextArea(3, 5)] private string txtHelpNeural = "RESSONÂNCIA NEURAL:\n\nAnalisa frequências cerebrais (Alpha, Beta, Gamma).\n\nHumanos apresentam picos específicos em 10Hz e 25Hz.\n\nImpostores podem apresentar 'Flatline' (linha reta) ou ruído uniforme sem picos definidos.";
    [TextArea(3, 5)] private string txtHelpHistory = "HISTÓRICO DE DADOS:\n\nMostra o impacto acumulado de cada teste na probabilidade global.\n\nBarras para CIMA: Aumentaram a chance de ser humano.\n\nBarras para BAIXO: Diminuíram a chance (indicativo de impostor).";

    void Awake()
    {
        Instance = this;

        if (de3000Background)
        {
            animationRect = de3000Background.GetComponent<RectTransform>();
            if (animationRect) targetY = animationRect.anchoredPosition.y;
        }
        else if (de3000Panel)
        {
            animationRect = de3000Panel.GetComponent<RectTransform>();
            if (animationRect) targetY = animationRect.anchoredPosition.y;
        }

        if (helpContainer) helpContainer.SetActive(false);
        if (helpCanvasGroup) helpCanvasGroup.alpha = 0;
    }

    void Start()
    {
        if (animationRect)
        {
            Vector2 pos = animationRect.anchoredPosition;
            pos.y = offScreenY;
            animationRect.anchoredPosition = pos;
        }

        batteryLevel = 100f;
    }

    // --- ATIVAÇÃO ---

    public void ActivateDevice(VisitorProfile profile, bool isHuman)
    {
        activeProfile = profile;
        activeIsHuman = isHuman;

        if (DayCycleManager.Instance != null) currentDayIndex = DayCycleManager.Instance.GetCurrentDayIndex();
        else currentDayIndex = 0;

        mimicFactor = Mathf.Clamp01(currentDayIndex / 4f);

        pGlobal = 50f;
        deltaThermal = 0; deltaRetinal = 0; deltaNeural = 0;

        CleanChart(thermalChart);
        CleanChart(retinalChart);
        CleanChart(neuralChart);
        CleanChart(historyChart);

        UpdateBatteryUI();
        UpdateGlobalProbUI();
        SwitchMode(ScanMode.Idle);

        isHelpOpen = false;
        if (helpContainer) helpContainer.SetActive(false);

        if (de3000Background) de3000Background.SetActive(true);
        if (de3000Panel) de3000Panel.SetActive(true);

        if (animationRect)
        {
            animationRect.DOKill();
            Vector2 pos = animationRect.anchoredPosition;
            pos.y = offScreenY;
            animationRect.anchoredPosition = pos;
            animationRect.DOAnchorPosY(targetY, 1.2f).SetEase(Ease.OutBack);
        }
    }

    public void DeactivateDevice()
    {
        if (animationRect)
        {
            animationRect.DOAnchorPosY(offScreenY, 0.4f)
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

    public void ForceClose()
    {
        if (animationRect)
        {
            animationRect.DOKill();
            Vector2 pos = animationRect.anchoredPosition;
            pos.y = offScreenY;
            animationRect.anchoredPosition = pos;
        }
        if (de3000Background) de3000Background.SetActive(false);
    }

    public void ForceHide()
    {
        if (animationRect) animationRect.DOKill();
        if (de3000Background) de3000Background.SetActive(false);
    }

    // --- AJUDA ---

    public void OnClick_Help()
    {
        if (isScanning) return;
        if (isHelpOpen) CloseHelp();
        else OpenHelp();
    }

    private void OpenHelp()
    {
        if (!helpContainer || !helpCanvasGroup) return;
        isHelpOpen = true;
        helpContainer.SetActive(true);
        UpdateHelpText(currentMode);
        helpCanvasGroup.alpha = 0;
        helpCanvasGroup.DOFade(1, 0.3f);
        helpContainer.transform.localScale = Vector3.one * 0.9f;
        helpContainer.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    private void CloseHelp()
    {
        if (!helpContainer || !helpCanvasGroup) return;
        isHelpOpen = false;
        helpCanvasGroup.DOFade(0, 0.2f).OnComplete(() => { helpContainer.SetActive(false); });
    }

    private void UpdateHelpText(ScanMode mode)
    {
        if (!helpBodyText) return;
        if (helpScrollRect) helpScrollRect.verticalNormalizedPosition = 1f;

        switch (mode)
        {
            case ScanMode.Idle: helpBodyText.text = txtHelpIdle; break;
            case ScanMode.Thermal: helpBodyText.text = txtHelpThermal; break;
            case ScanMode.Retinal: helpBodyText.text = txtHelpRetinal; break;
            case ScanMode.Neural: helpBodyText.text = txtHelpNeural; break;
            case ScanMode.History: helpBodyText.text = txtHelpHistory; break;
        }
    }

    // --- CONTROLES ---

    public void OnClick_ModeThermal()
    {
        if (!isScanning) { SwitchMode(ScanMode.Thermal); if (isHelpOpen) UpdateHelpText(ScanMode.Thermal); }
    }
    public void OnClick_ModeRetinal()
    {
        if (!isScanning) { SwitchMode(ScanMode.Retinal); if (isHelpOpen) UpdateHelpText(ScanMode.Retinal); }
    }
    public void OnClick_ModeNeural()
    {
        if (!isScanning) { SwitchMode(ScanMode.Neural); if (isHelpOpen) UpdateHelpText(ScanMode.Neural); }
    }
    public void OnClick_GraphHistory()
    {
        if (!isScanning) { SwitchMode(ScanMode.History); UpdateHistoryChart(); if (isHelpOpen) UpdateHelpText(ScanMode.History); }
    }

    public void OnClick_Back()
    {
        if (isHelpOpen) CloseHelp();
        else DeactivateDevice();
    }

    public void OnClick_Scan()
    {
        if (isScanning || currentMode == ScanMode.History || currentMode == ScanMode.Idle || isHelpOpen) return;

        float cost = GetScanCost();
        if (batteryLevel < cost)
        {
            if (statusText) statusText.text = "BATERIA INSUFICIENTE";
            return;
        }

        StartCoroutine(ScanRoutine());
    }

    // --- LÓGICA PRINCIPAL ---

    private void SwitchMode(ScanMode mode)
    {
        currentMode = mode;

        if (thermalChart) thermalChart.gameObject.SetActive(false);
        if (retinalChart) retinalChart.gameObject.SetActive(false);
        if (neuralChart) neuralChart.gameObject.SetActive(false);
        if (historyChart) historyChart.gameObject.SetActive(false);

        // --- CÁLCULO E EXIBIÇÃO DO CUSTO DE BATERIA ---
        float cost = GetScanCost();
        string statusMessage = "PRONTO";

        // Se houver custo, mostra na tela (ex: "PRONTO (-5%)")
        if (cost > 0)
        {
            statusMessage = $"PRONTO (-{cost:F0}%) de Bateria";
        }

        if (statusText) statusText.text = statusMessage;

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
                // Histórico não tem custo, removemos o texto de custo
                if (statusText) statusText.text = "VISUALIZANDO";
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

    // --- MODOS DE SCAN ---

    private float PerformThermalScan()
    {
        float reading;
        if (activeIsHuman) reading = StatisticalUtils.RandomNormal(activeProfile.meanTemp, activeProfile.tempStdDev);
        else
        {
            float baseTemp = Mathf.Lerp(32.0f, 36.0f, mimicFactor);
            reading = baseTemp + Random.Range(-0.3f, 0.5f);
        }

        if (thermalChart != null)
        {
            thermalChart.RemoveAllSerie();
            var lineSerie = thermalChart.AddSerie<Line>("Referencia");
            lineSerie.symbol.show = false; lineSerie.lineStyle.width = 2f;
            var pointSerie = thermalChart.AddSerie<Scatter>("Leitura");
            pointSerie.symbol.size = 8f; pointSerie.symbol.type = SymbolType.Circle; pointSerie.itemStyle.color = Color.red;

            thermalChart.ClearData();
            for (float i = 32f; i <= 41f; i += 0.1f) thermalChart.AddData(0, i, StatisticalUtils.NormalPDF(i, 36.5f, 0.5f));
            float readingY = StatisticalUtils.NormalPDF(reading, 36.5f, 0.5f);
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
            float pImpostor = Mathf.Lerp(0.2f, 0.7f, mimicFactor);
            for (int i = 0; i < 10; i++) if (Random.value < pImpostor) successes++;
        }

        if (retinalChart != null)
        {
            retinalChart.RemoveAllSerie();
            var barSerie = retinalChart.AddSerie<Bar>("Acertos");
            barSerie.itemStyle.color = new Color(0f, 1f, 0f, 0.7f); barSerie.barWidth = 40f;
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
            pattern[0] = Random.Range(0.1f, 0.3f); pattern[1] = Random.Range(0.7f, 0.9f);
            pattern[2] = Random.Range(0.5f, 0.7f); pattern[3] = Random.Range(0.2f, 0.4f);
            pattern[4] = Random.Range(0.6f, 0.8f); isHumanPattern = true;
        }
        else
        {
            if (mimicFactor < 0.3f)
            {
                float noise = Random.Range(0.1f, 0.2f);
                for (int i = 0; i < 5; i++) pattern[i] = noise;
                flatline = true;
            }
            else
            {
                pattern[0] = Random.Range(0.1f, 0.4f);
                pattern[1] = Mathf.Lerp(0.3f, 0.8f, mimicFactor) + Random.Range(-0.1f, 0.1f);
                pattern[2] = Random.Range(0.4f, 0.6f);
                pattern[3] = Random.Range(0.2f, 0.5f);
                pattern[4] = Mathf.Lerp(0.3f, 0.7f, mimicFactor) + Random.Range(-0.1f, 0.1f);
            }
        }

        if (neuralChart != null)
        {
            neuralChart.RemoveAllSerie(); neuralChart.ClearData();
            var xAxis = neuralChart.EnsureChartComponent<XAxis>();
            if (xAxis != null)
            {
                xAxis.type = Axis.AxisType.Category; xAxis.data.Clear();
                xAxis.data.Add("5Hz"); xAxis.data.Add("10Hz"); xAxis.data.Add("15Hz"); xAxis.data.Add("20Hz"); xAxis.data.Add("25Hz");
                xAxis.axisLabel.show = true; xAxis.axisLabel.interval = 0; xAxis.axisLabel.textStyle.color = Color.white;
            }
            var barSerie = neuralChart.AddSerie<Bar>("Frequencias");
            barSerie.itemStyle.color = new Color(0.5f, 0f, 1f, 0.8f);
            for (int i = 0; i < 5; i++) neuralChart.AddData(0, i, pattern[i]);
            neuralChart.RefreshChart();
        }
        return StatisticalUtils.CalculateNeuralDelta(flatline, isHumanPattern);
    }

    private void UpdateHistoryChart()
    {
        if (historyChart == null) return;
        historyChart.RemoveAllSerie(); historyChart.ClearData();
        var xAxis = historyChart.EnsureChartComponent<XAxis>();
        var yAxis = historyChart.EnsureChartComponent<YAxis>();

        if (xAxis != null && yAxis != null)
        {
            xAxis.type = Axis.AxisType.Category;
            if (xAxis.data != null)
            {
                xAxis.data.Clear(); xAxis.data.Add("Térmica"); xAxis.data.Add("Retina"); xAxis.data.Add("Neural");
                xAxis.axisLabel.textStyle.color = Color.white;
            }
            yAxis.type = Axis.AxisType.Value;
            var barSerie = historyChart.AddSerie<Bar>("Historico");
            if (barSerie != null)
            {
                barSerie.itemStyle.color = new Color(1f, 0.8f, 0f, 0.8f);
                if (barSerie.label != null) { barSerie.label.show = true; barSerie.label.position = LabelStyle.Position.Top; barSerie.label.textStyle.color = Color.white; }
                historyChart.AddData(0, deltaThermal); historyChart.AddData(0, deltaRetinal); historyChart.AddData(0, deltaNeural);
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

    private void CleanChart(BaseChart chart) { if (chart != null) { chart.RemoveAllSerie(); chart.ClearData(); } }
}