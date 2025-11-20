using UnityEngine;
using System.Collections; // Necessário para Corrotinas (IEnumerator)
using System.Collections.Generic;
using DialogSystem; // Necessário se for mostrar mensagens de "Bom dia"

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    [System.Serializable]
    public struct DayConfig
    {
        public string dayName; // Ex: "Dia 1"
        public List<VisitorProfile> visitorsForThisDay;
        public DayMusicSetup musicSetup; // Música do dia (opcional)
        [TextArea] public string wakeUpMessage; // Mensagem ao acordar (opcional)
    }

    [Header("Configuração dos Dias")]
    public DayConfig[] allDays;

    [Header("Referências")]
    public RadioInteraction radioInteraction; // Para resetar o rádio (opcional)

    // Estado Interno
    private int currentDayIndex = 0;
    private Queue<VisitorProfile> dailyQueue = new Queue<VisitorProfile>();
    private int visitorsProcessedToday = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        StartDay(0); // Começa no Dia 1 (index 0)
    }

    private void StartDay(int dayIndex)
    {
        currentDayIndex = dayIndex;
        visitorsProcessedToday = 0;
        dailyQueue.Clear();

        if (dayIndex >= allDays.Length)
        {
            Debug.Log("Fim de Jogo! Você sobreviveu.");
            // Aqui você pode carregar uma cena de vitória
            return;
        }

        DayConfig config = allDays[dayIndex];

        // 1. Enfileira os visitantes
        foreach (var v in config.visitorsForThisDay)
        {
            dailyQueue.Enqueue(v);
        }

        // 2. Configura Música (se tiver o sistema de áudio)
        if (GameAudioManager.Instance != null && config.musicSetup != null)
        {
            GameAudioManager.Instance.LoadDayMusic(config.musicSetup);
        }

        // 3. Reseta o Rádio (se tiver referência)
        if (radioInteraction != null)
        {
            radioInteraction.ResetForNewDay();
        }

        Debug.Log($"Iniciando {config.dayName}. Visitantes na fila: {dailyQueue.Count}");
    }

    // --- Interação com a Porta / Olho Mágico ---

    public void CheckDoorForVisitor()
    {
        if (dailyQueue.Count > 0)
        {
            VisitorProfile nextVisitor = dailyQueue.Peek();
            PeepholeManager.Instance.StartEncounter(nextVisitor);
        }
        else
        {
            Debug.Log("Ninguém na porta.");
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage("Silêncio total lá fora.", 2f);
        }
    }

    public void RegisterVisitorProcessed()
    {
        if (dailyQueue.Count > 0)
        {
            dailyQueue.Dequeue();
        }

        visitorsProcessedToday++;

        if (dailyQueue.Count == 0)
        {
            Debug.Log("Todos os visitantes do dia foram atendidos. Pode dormir.");
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage("O silêncio voltou... Acho que posso dormir agora.", 3f);
        }
    }

    // --- Lógica de Dormir / Avançar Dia (Corrige o Erro CS1061) ---

    /// <summary>
    /// Verifica se o jogador pode dormir (se a fila de visitantes está vazia).
    /// </summary>
    public bool CanAdvanceDay()
    {
        return dailyQueue.Count == 0;
    }

    /// <summary>
    /// Sequência completa de dormir: Fade Out -> Espera -> Perde Energia -> Novo Dia -> Fade In.
    /// </summary>
    public IEnumerator AdvanceToNextDaySequence()
    {
        // 1. Fade Out (Escurece a tela)
        if (CameraFader.Instance != null)
            yield return StartCoroutine(CameraFader.Instance.Fade(1f, 2f));
        else
            yield return new WaitForSeconds(1f); // Fallback se não tiver fader

        // 2. Lógica de Passagem de Tempo
        yield return new WaitForSeconds(2f); // Tempo "dormindo"

        // 3. Atualiza índice do dia
        currentDayIndex++;

        // 4. Aplica penalidades no Gerador (Se existir o Manager)
        if (GeneratorManager.Instance != null)
        {
            GeneratorManager.Instance.TransitionToNextDay();
        }

        // 5. Inicia o novo dia (Lógica interna)
        StartDay(currentDayIndex);

        // 6. Fade In (Clareia a tela)
        if (CameraFader.Instance != null)
            yield return StartCoroutine(CameraFader.Instance.Fade(0f, 2f));

        // 7. Mensagem de bom dia (Opcional)
        if (currentDayIndex < allDays.Length)
        {
            string msg = allDays[currentDayIndex].wakeUpMessage;
            if (!string.IsNullOrEmpty(msg) && DialogManager.Instance != null)
            {
                DialogManager.Instance.ShowMessage(msg, 4f);
            }
        }
    }
}