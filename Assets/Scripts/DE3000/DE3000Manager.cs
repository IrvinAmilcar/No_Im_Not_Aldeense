using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using XCharts.Runtime; // XCharts 3.x

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

    // Histórico
    private float deltaThermal = 0;
    private float deltaRetinal = 0;
    private float deltaNeural = 0;

    void Awake() { Instance = this; }

    void Start()
    {
        if (de3000Background) de3000Background.SetActive(false);
        batteryLevel = 100f;
    }

    // --- INICIALIZAÇÃO ---

    public void ActivateDevice(VisitorProfile profile, bool isHuman)
    {
        activeProfile = profile;
        activeIsHuman = isHuman;

        // --- CORREÇÃO: Reseta variáveis de histórico ---
        pGlobal = 50f;
        deltaThermal = 0; deltaRetinal = 0; deltaNeural = 0;

        // --- CORREÇÃO: Limpa TODOS os gráficos visualmente ao iniciar ---
        // Isso impede que o gráfico do personagem anterior "pisque" na tela
        CleanChart(thermalChart);
        CleanChart(retinalChart);
        CleanChart(neuralChart);
        CleanChart(historyChart);

        if (de3000Background) de3000Background.SetActive(true);
        if (de3000Panel) de3000Panel.SetActive(true);

        UpdateBatteryUI();
        UpdateGlobalProbUI();

        SwitchMode(ScanMode.Idle);
    }

    // Método auxiliar para limpar gráficos com segurança
    private void CleanChart(BaseChart chart)
    {
        if (chart != null)
        {
            chart.RemoveAllSerie();
            chart.ClearData();
        }
    }

    public void DeactivateDevice()
    {
        if (de3000Background) de3000Background.SetActive(false);
        if (PeepholeManager.Instance != null) PeepholeManager.Instance.ReturnToDialogue();
    }

    // --- BOTÕES ---

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

    // --- LÓGICA CORE ---

    private void SwitchMode(ScanMode mode)
    {
        currentMode = mode;

        // Esconde tudo
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

        pGlobal = Mathf.Clamp(pGlobal + delta, 0f, 100f);
        UpdateGlobalProbUI();

        if (statusText) statusText.text = $"DELTA: {(delta >= 0 ? "+" : "")}{delta:F0}%";

        isScanning = false;
    }

    // --- IMPLEMENTAÇÃO DOS GRÁFICOS ---

    private float PerformThermalScan()
    {
        float reading = activeIsHuman
            ? StatisticalUtils.RandomNormal(activeProfile.meanTemp, activeProfile.tempStdDev)
            : Random.Range(30.0f, 33.0f);

        if (thermalChart != null)
        {
            thermalChart.RemoveAllSerie();

            var lineSerie = thermalChart.AddSerie<Line>("Referencia");
            lineSerie.symbol.show = false;
            lineSerie.lineStyle.width = 2f;

            var pointSerie = thermalChart.AddSerie<Scatter>("Leitura");
            pointSerie.symbol.size = 20f;
            pointSerie.itemStyle.color = Color.red;

            thermalChart.ClearData();
            for (float i = 32f; i <= 41f; i += 0.1f)
            {
                float y = StatisticalUtils.NormalPDF(i, 36.5f, 0.5f);
                thermalChart.AddData(0, i, y);
            }

            float readingY = StatisticalUtils.NormalPDF(reading, 36.5f, 0.5f);
            if (readingY < 0.01f) readingY = 0.01f;
            thermalChart.AddData(1, reading, readingY);
        }

        return StatisticalUtils.CalculateThermalDelta(reading);
    }

    private float PerformRetinalScan()
    {
        int successes = 0;
        float p = activeIsHuman ? activeProfile.retinalProbability : 0.2f;
        for (int i = 0; i < 10; i++) if (Random.value < p) successes++;

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
        bool flatline = !activeIsHuman;

        if (activeIsHuman)
        {
            pattern[0] = Random.Range(0.1f, 0.3f);
            pattern[1] = Random.Range(0.7f, 0.9f);
            pattern[2] = Random.Range(0.6f, 0.8f);
            pattern[3] = Random.Range(0.2f, 0.4f);
            pattern[4] = Random.Range(0.5f, 0.7f);
        }
        else
        {
            for (int i = 0; i < 5; i++) pattern[i] = Random.Range(0.2f, 0.3f);
        }

        if (neuralChart != null)
        {
            neuralChart.RemoveAllSerie();
            var barSerie = neuralChart.AddSerie<Bar>("Frequencias");
            barSerie.itemStyle.color = new Color(0.5f, 0f, 1f, 0.8f);

            neuralChart.ClearData();
            for (int i = 0; i < 5; i++)
            {
                neuralChart.AddData(0, i, pattern[i]);
            }
        }

        return StatisticalUtils.CalculateNeuralDelta(flatline, activeIsHuman);
    }

    // --- CORREÇÃO DO NULL REFERENCE (HistoryChart) ---
    // --- CORREÇÃO: Método Blindado contra NullReference ---
    private void UpdateHistoryChart()
    {
        // 1. Segurança básica
        if (historyChart == null) return;

        // 2. Limpeza Total
        historyChart.RemoveAllSerie();
        historyChart.ClearData();

        // 3. Garantir que os Eixos existem (Cria se não existirem)
        var xAxis = historyChart.EnsureChartComponent<XAxis>();
        var yAxis = historyChart.EnsureChartComponent<YAxis>();

        if (xAxis == null || yAxis == null)
        {
            Debug.LogError("Erro: Não foi possível criar os eixos do HistoryChart.");
            return;
        }

        // 4. Configurar Eixo X (Categorias) com segurança
        xAxis.type = Axis.AxisType.Category;
        if (xAxis.data != null)
        {
            xAxis.data.Clear();
            xAxis.data.Add("Térmica");
            xAxis.data.Add("Retina");
            xAxis.data.Add("Neural");
        }

        // 5. Configurar Eixo Y (Valores)
        yAxis.type = Axis.AxisType.Value;

        // 6. Adicionar a Série e verificar se foi criada
        var barSerie = historyChart.AddSerie<Bar>("Historico");

        if (barSerie != null)
        {
            // Configura visual da barra
            barSerie.itemStyle.color = new Color(1f, 0.8f, 0f, 0.8f); // Amarelo/Laranja

            // Verificação extra para o Label (causa comum do erro)
            if (barSerie.label != null)
            {
                barSerie.label.show = true;
                barSerie.label.position = LabelStyle.Position.Top;
            }

            // 7. Adicionar os Dados (Sequencialmente: 0=Térmica, 1=Retina, 2=Neural)
            // AddData(SerieIndex, Valor) -> O XCharts distribui nas categorias automaticamente
            historyChart.AddData(0, deltaThermal);
            historyChart.AddData(0, deltaRetinal);
            historyChart.AddData(0, deltaNeural);

            // Força atualização visual
            historyChart.RefreshChart();
        }
        else
        {
            Debug.LogError("Erro: Falha ao criar a série 'Bar' no HistoryChart.");
        }
    }

    // --- UI ---

    private void UpdateBatteryUI()
    {
        if (batteryText)
        {
            batteryText.text = $"{batteryLevel:F0}%";
            batteryText.color = batteryLevel < 20 ? Color.red : Color.green;
        }
    }

    private void UpdateGlobalProbUI()
    {
        if (globalProbText)
        {
            // --- CORREÇÃO: Texto mais explicativo ---
            globalProbText.text = $"P(Humano): {pGlobal:F0}%";

            if (pGlobal > 80) globalProbText.color = Color.green;
            else if (pGlobal < 40) globalProbText.color = Color.red;
            else globalProbText.color = Color.yellow;
        }
    }
}