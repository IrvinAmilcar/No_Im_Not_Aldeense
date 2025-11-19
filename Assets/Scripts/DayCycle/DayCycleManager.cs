using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DialogSystem;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    [System.Serializable]
    public struct DayConfig
    {
        public string dayName; // Ex: "Dia 1"
        public int expectedVisitors; // Quantas pessoas/eventos precisam passar pela porta
        public DayMusicSetup musicSetup; // Música deste dia (opcional)
        [TextArea] public string wakeUpMessage; // Mensagem ao acordar (ex: Rádio do Dia X)
    }

    [Header("Configuração dos 5 Dias")]
    public DayConfig[] allDays;

    [Header("Referências")]
    public RadioInteraction radioInteraction; // Para resetar o rádio

    // Estado Atual
    private int currentDayIndex = 0; // 0 = Dia 1
    private int visitorsProcessedToday = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Inicializa o Dia 1
        StartDay(0);
    }

    /// <summary>
    /// Chama toda vez que um visitante vai embora (Aceito ou Rejeitado).
    /// Você deve chamar isso no seu script de Decisão/VisitorManager.
    /// </summary>
    public void RegisterVisitorProcessed()
    {
        visitorsProcessedToday++;
        Debug.Log($"Visitante processado. Progresso: {visitorsProcessedToday}/{allDays[currentDayIndex].expectedVisitors}");

        if (visitorsProcessedToday >= allDays[currentDayIndex].expectedVisitors)
        {
            // Feedback visual ou sonoro que o dia pode ser encerrado
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage("O silêncio voltou... Acho que posso dormir agora.", 3f);
        }
    }

    /// <summary>
    /// Verifica se pode dormir.
    /// </summary>
    public bool CanAdvanceDay()
    {
        return visitorsProcessedToday >= allDays[currentDayIndex].expectedVisitors;
    }

    /// <summary>
    /// A sequência de dormir e acordar no próximo dia.
    /// </summary>
    public IEnumerator AdvanceToNextDaySequence()
    {
        // 1. Fade Out (Escurece a tela)
        yield return StartCoroutine(CameraFader.Instance.Fade(1f, 2f));

        // 2. Lógica de Passagem de Tempo
        yield return new WaitForSeconds(2f); // Tempo "dormindo"

        // Atualiza índice do dia
        currentDayIndex++;

        if (currentDayIndex >= allDays.Length)
        {
            Debug.Log("FIM DE JOGO - Sobreviveu aos 5 dias!");
            // Aqui você chamaria a cena de Vitória ou Créditos
            yield break;
        }

        // 3. Aplica penalidades no Gerador
        if (GeneratorManager.Instance != null)
        {
            GeneratorManager.Instance.TransitionToNextDay(); // Remove 50% energia
        }

        // 4. Prepara o novo dia
        StartDay(currentDayIndex);

        // 5. Fade In (Clareia a tela)
        yield return StartCoroutine(CameraFader.Instance.Fade(0f, 2f));

        // 6. Mensagem de bom dia (opcional)
        string msg = allDays[currentDayIndex].wakeUpMessage;
        if (!string.IsNullOrEmpty(msg) && DialogManager.Instance != null)
        {
            DialogManager.Instance.ShowMessage(msg, 4f);
        }
    }

    private void StartDay(int dayIndex)
    {
        visitorsProcessedToday = 0;
        DayConfig config = allDays[dayIndex];

        Debug.Log($"--- INICIANDO {config.dayName} ---");

        // 1. Configura Música
        if (GameAudioManager.Instance != null && config.musicSetup != null)
        {
            GameAudioManager.Instance.LoadDayMusic(config.musicSetup);
        }

        // 2. Reseta o Rádio para ser usado novamente
        if (radioInteraction != null)
        {
            radioInteraction.ResetForNewDay();
            // Opcional: Se você quiser mudar o diálogo do rádio por dia, faria aqui:
            // radioInteraction.SetDailyDialogue(novasPaginas);
        }

        // 3. (IMPORTANTE) Aqui você avisaria seu "VisitorSpawner" para começar a lógica do novo dia
        // Ex: VisitorSpawner.Instance.SetDay(dayIndex + 1);
    }
}