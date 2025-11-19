using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DialogSystem;
using System.Linq; // Necessário para usar o .FirstOrDefault()

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    // --- NOVA ESTRUTURA PARA CONFIGURAR O DIÁLOGO DA JANELA ---
    [System.Serializable]
    public struct WindowDayConfig
    {
        [Tooltip("Deve ser IGUAL ao 'Window ID' da janela no script BasePeekInteraction.")]
        public string windowID;
        
        [Tooltip("O diálogo da Janela para este dia.")]
        [TextArea(3, 5)] 
        public string[] dialogue; 
    }
    // --------------------------------------------------------

    [System.Serializable]
    public struct DayConfig
    {
        public string dayName; // Ex: "Dia 1"
        public int expectedVisitors; // Quantas pessoas/eventos precisam passar pela porta
        public DayMusicSetup musicSetup; // Música deste dia (opcional)
        [TextArea] public string wakeUpMessage; // Mensagem ao acordar (ex: Rádio do Dia X)
        
        // --- CAMPO ATUALIZADO (Array de configurações) ---
        [Tooltip("As configurações de diálogo para TODAS as janelas neste dia.")]
        public WindowDayConfig[] windowDialogues; 
        // ------------------------------------------------
    }

    [Header("Configuração dos 5 Dias")]
    public DayConfig[] allDays;

    [Header("Referências")]
    public RadioInteraction radioInteraction; // Para resetar o rádio
    
    // --- REFERÊNCIA ATUALIZADA (Array de janelas) ---
    [Tooltip("Arrastar TODAS as janelas (BasePeekInteraction) da cena aqui.")]
    public BasePeekInteraction[] allWindows; 
    // ------------------------------------------------

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

        // 1. Configura Música e Rádio
        if (GameAudioManager.Instance != null && config.musicSetup != null)
        {
            GameAudioManager.Instance.LoadDayMusic(config.musicSetup);
        }
        if (radioInteraction != null)
        {
            radioInteraction.ResetForNewDay();
        }

        // --- LÓGICA NOVA: CONFIGURAR DIÁLOGOS DAS MÚLTIPLAS JANELAS ---
        if (allWindows != null && config.windowDialogues != null)
        {
            foreach (BasePeekInteraction window in allWindows)
            {
                // Tenta encontrar a configuração de diálogo correspondente pelo WindowID
                WindowDayConfig? dialogueConfig = config.windowDialogues
                    .FirstOrDefault(d => d.windowID == window.windowID);

                if (dialogueConfig.HasValue && dialogueConfig.Value.dialogue != null)
                {
                    // Atribui o diálogo específico
                    window.SetDailyDialogue(dialogueConfig.Value.dialogue);
                }
                else
                {
                    // Atribui um diálogo padrão se não for configurado para esta janela
                    window.SetDailyDialogue(new string[] { $"[Janela {window.windowID}]: Eu não vejo nada de novo por aqui." });
                }
            }
        }
        // ------------------------------------------------------------

        // 3. (IMPORTANTE) Aqui você avisaria seu "VisitorSpawner" para começar a lógica do novo dia
        // Ex: VisitorSpawner.Instance.SetDay(dayIndex + 1);
    }
}