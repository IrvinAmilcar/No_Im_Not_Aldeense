using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using XCharts.Runtime; // XCharts 3.x

public class DE3000Manager : MonoBehaviour
{
    public static DE3000Manager Instance { get; private set; }

    // Adicionei "Idle" para o estado inicial vazio
    public enum ScanMode { Idle, Thermal, Retinal, Neural, History }

    [Header("Hierarquia Principal")]
    public GameObject de3000Panel;
    public GameObject de3000Background;

    [Header("Visualização XCharts (Arraste os objetos)")]
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

    private ScanMode currentMode = ScanMode.Idle; // Começa Ocioso
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

        pGlobal = 50f;
        deltaThermal = 0; deltaRetinal = 0; deltaNeural = 0;

        if (de3000Background) de3000Background.SetActive(true);
        if (de3000Panel) de3000Panel.SetActive(true);

        UpdateBatteryUI();
        UpdateGlobalProbUI();

        // --- CORREÇÃO 1: Começa no modo IDLE (Sem gráficos) ---
        SwitchMode(ScanMode.Idle);
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

        // Esconde tudo primeiro
        if (thermalChart) thermalChart.gameObject.SetActive(false);
        if (retinalChart) retinalChart.gameObject.SetActive(false);
        if (neuralChart) neuralChart.gameObject.SetActive(false);
        if (historyChart) historyChart.gameObject.SetActive(false);

        if (statusText) statusText.text = "PRONTO";

        // Configura o modo
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

    // --- IMPLEMENTAÇÃO DOS GRÁFICOS (CORRIGIDA) ---

    private float PerformThermalScan()
    {
        float reading = activeIsHuman
            ? StatisticalUtils.RandomNormal(activeProfile.meanTemp, activeProfile.tempStdDev)
            : Random.Range(30.0f, 33.0f);

        if (thermalChart != null)
        {
            thermalChart.RemoveAllSerie();

            // Série 0: A Curva (Referência)
            var lineSerie = thermalChart.AddSerie<Line>("Referencia");
            lineSerie.symbol.show = false; // Sem bolinhas na linha
            lineSerie.lineStyle.width = 2f;
            lineSerie.lineStyle.type = LineStyle.Type.Solid; // Linha sólida suave

            // Série 1: O Ponto (Resultado)
            var pointSerie = thermalChart.AddSerie<Scatter>("Leitura");
            pointSerie.symbol.size = 20f;
            pointSerie.symbol.type = SymbolType.Circle;
            pointSerie.itemStyle.color = Color.red;

            thermalChart.ClearData();

            // Desenha a curva (de 32 a 41 graus)
            for (float i = 32f; i <= 41f; i += 0.1f) // 0.1f para ficar mais suave
            {
                float y = StatisticalUtils.NormalPDF(i, 36.5f, 0.5f);
                thermalChart.AddData(0, i, y);
            }

            // Desenha o ponto exato
            float readingY = StatisticalUtils.NormalPDF(reading, 36.5f, 0.5f);

            // Se a leitura for muito longe (impostor frio), o Y é quase zero, 
            // mas garantimos que ele apareça no gráfico
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
            // Cria a série de barras
            var barSerie = retinalChart.AddSerie<Bar>("Acertos");
            barSerie.itemStyle.color = new Color(0f, 1f, 0f, 0.7f); // Verde
            // Configura a largura da barra para ficar bonita
            barSerie.barWidth = 40f;

            retinalChart.ClearData();

            // --- CORREÇÃO: Adiciona explicitamente na Categoria 0 com Valor 'successes' ---
            // O primeiro '0' é o índice da série. 
            // O segundo '0' é a posição X (primeira barra).
            // O terceiro é o valor Y (altura).
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
            // --- CORREÇÃO CRÍTICA ---
            neuralChart.RemoveAllSerie();
            var barSerie = neuralChart.AddSerie<Bar>("Frequencias");
            barSerie.itemStyle.color = new Color(0.5f, 0f, 1f, 0.8f); // Roxo/Azul

            neuralChart.ClearData();
            for (int i = 0; i < 5; i++)
            {
                // No XCharts 3, para categorias, usamos o index X e o valor Y
                neuralChart.AddData(0, i, pattern[i]);
            }
        }

        return StatisticalUtils.CalculateNeuralDelta(flatline, activeIsHuman);
    }

    private void UpdateHistoryChart()
    {
        if (historyChart != null)
        {
            historyChart.RemoveAllSerie();
            var barSerie = historyChart.AddSerie<Bar>("Historico");
            barSerie.label.show = true; // Mostra o número na barra
            barSerie.label.position = LabelStyle.Position.Top;

            historyChart.ClearData();

            // Adiciona dados (Index X 0, 1, 2 correspondem às categorias)
            historyChart.AddData(0, 0, deltaThermal);
            historyChart.AddData(0, 1, deltaRetinal);
            historyChart.AddData(0, 2, deltaNeural);
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
            globalProbText.text = $"{pGlobal:F0}%";
            if (pGlobal > 80) globalProbText.color = Color.green;
            else if (pGlobal < 40) globalProbText.color = Color.red;
            else globalProbText.color = Color.yellow;
        }
    }
}