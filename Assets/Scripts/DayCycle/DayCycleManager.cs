using UnityEngine;
using System.Collections; 
using System.Collections.Generic;
using DialogSystem; 
using System.Linq; // NECESSÁRIO para o .FirstOrDefault()

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    // --- Estrutura para configurar o diálogo de CADA janela por dia ---
    [System.Serializable]
    public struct WindowDayConfig
    {
        [Tooltip("Deve ser IGUAL ao 'Window ID' da janela no script BasePeekInteraction.")]
        public string windowID;
        
        [Tooltip("O diálogo da Janela para este dia.")]
        [TextArea(3, 5)] 
        public string[] dialogue; 
    }

    [System.Serializable]
    public struct DayConfig
    {
        public string dayName; // Ex: "Dia 1"
        public List<VisitorProfile> visitorsForThisDay;
        public DayMusicSetup musicSetup; // Música do dia (opcional)
        [TextArea] public string wakeUpMessage; // Mensagem ao acordar (opcional)
        
        [Tooltip("O diálogo (reportagem) que será exibido ao interagir com o rádio neste dia.")]
        [TextArea(3, 5)] 
        public string[] radioDialogue; 
        
        [Tooltip("As configurações de diálogo para TODAS as janelas (e objetos peek) neste dia.")]
        public WindowDayConfig[] windowDialogues; 
    }

    [Header("Configuração dos Dias")]
    public DayConfig[] allDays;

    [Header("Referências")]
    public RadioInteraction radioInteraction; // Para resetar o rádio (opcional)
    [Tooltip("Todas as instâncias de BasePeekInteraction na cena (janelas, etc.)")]
    public BasePeekInteraction[] allPeekInteractions;

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
            return;
        }

        DayConfig config = allDays[dayIndex];

        // 1. Enfileira os visitantes (Lógica Original)
        foreach (var v in config.visitorsForThisDay)
        {
            dailyQueue.Enqueue(v);
        }

        // 2. Configura Música (Lógica Original)
        if (GameAudioManager.Instance != null && config.musicSetup != null)
        {
            GameAudioManager.Instance.LoadDayMusic(config.musicSetup);
        }

        // 3. Reseta e configura o Rádio
        if (radioInteraction != null)
        {
            radioInteraction.ResetForNewDay();
            
            // --- CONFIGURAR DIÁLOGO DO RÁDIO ---
            if (config.radioDialogue != null && config.radioDialogue.Length > 0)
            {
                radioInteraction.SetDailyDialogue(config.radioDialogue);
            }
            else
            {
                radioInteraction.SetDailyDialogue(new string[] { "O rádio está mudo." });
            }
        }

        // --- CORREÇÃO DE SETUP: GARANTE AS REFERÊNCIAS ---
        if (allPeekInteractions == null || allPeekInteractions.Length == 0)
        {
            // Tenta encontrar todas as interações de peek (janelas e portas) automaticamente
            allPeekInteractions = FindObjectsOfType<BasePeekInteraction>();
            if (allPeekInteractions.Length > 0)
            {
                Debug.Log($"[DayCycleManager] Encontrou {allPeekInteractions.Length} interações Peek na cena. (Auto-Find)");
            }
        }
        // --------------------------------------------------


        // --- CONFIGURAR DIÁLOGOS DAS JANELAS ---
        if (allPeekInteractions != null && config.windowDialogues != null)
        {
            foreach (BasePeekInteraction peekInteraction in allPeekInteractions)
            {
                // Ignora a DoorInteraction, que usa o PeepholeManager para seu diálogo.
                if (peekInteraction is DoorInteraction) continue;

                // Procura a configuração de diálogo para o ID desta janela
                WindowDayConfig? dialogueConfig = config.windowDialogues
                    .FirstOrDefault(d => d.windowID == peekInteraction.windowID);

                if (dialogueConfig.HasValue && dialogueConfig.Value.dialogue != null && dialogueConfig.Value.dialogue.Length > 0)
                {
                    peekInteraction.SetDailyDialogue(dialogueConfig.Value.dialogue);
                }
                else
                {
                    // Diálogo padrão para janelas não configuradas (ou com diálogo vazio)
                    peekInteraction.SetDailyDialogue(new string[] { $"[Janela {peekInteraction.windowID}]: Eu não vejo nada de novo por aqui." });
                }
            }
        }
        // ----------------------------------------

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

    // --- Lógica de Dormir / Avançar Dia (Lógica Original) ---

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

        // 4. Aplica penalidades no Gerador (Se existir o Manager) (Lógica Original)
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

    // permitir acesso externo ao dia atual
    public int GetCurrentDayIndex()
    {
        return currentDayIndex;
    }
}