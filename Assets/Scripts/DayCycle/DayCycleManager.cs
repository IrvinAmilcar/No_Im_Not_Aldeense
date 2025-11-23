using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DialogSystem;
using System.Linq;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }
    public static event System.Action<int> OnDayStarted;

    [System.Serializable] public struct WindowDayConfig { public string windowID; [TextArea(3, 5)] public string[] dialogue; }

    [System.Serializable]
    public struct DayConfig
    {
        public string dayName;
        public List<VisitorProfile> visitorsForThisDay;
        public DayMusicSetup musicSetup;

        [Header("Mensagens de Início (Sequência)")]
        [Tooltip("Lista de frases que aparecerão uma por uma no início do dia.")]
        [TextArea(2, 4)] public string[] wakeUpMessages; // --- MUDANÇA: Agora é uma lista (Array) ---

        [TextArea(3, 5)] public string[] radioDialogue;
        public WindowDayConfig[] windowDialogues;
    }

    [Header("Configuração dos Dias")]
    public DayConfig[] allDays;

    [Header("Configuração de Visitantes")]
    public AudioClip knockingSound;
    public float minArrivalDelay = 2f;
    public float maxArrivalDelay = 5f;

    [Header("Referências")]
    public RadioInteraction radioInteraction;
    public BasePeekInteraction[] allPeekInteractions;

    // Estado Interno
    private int currentDayIndex = 0;
    private Queue<VisitorProfile> dailyQueue = new Queue<VisitorProfile>();
    private int visitorsProcessedToday = 0;

    public bool IsVisitorWaiting { get; private set; } = false;
    public bool IsGameOver { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Inicia o Dia 1
        StartDay(0);
        // Inicia a sequência de mensagens do Dia 1
        StartCoroutine(DayStartSequence());
    }

    private void StartDay(int dayIndex)
    {
        if (GeneratorManager.Instance != null && GeneratorManager.Instance.IsBroken) return;

        currentDayIndex = dayIndex;
        visitorsProcessedToday = 0;
        dailyQueue.Clear();
        IsVisitorWaiting = false;
        IsGameOver = false;

        if (dayIndex >= allDays.Length) { Debug.Log("Fim de Jogo!"); return; }

        DayConfig config = allDays[dayIndex];

        foreach (var v in config.visitorsForThisDay) dailyQueue.Enqueue(v);

        if (GameAudioManager.Instance != null && config.musicSetup != null)
            GameAudioManager.Instance.LoadDayMusic(config.musicSetup);

        if (radioInteraction != null)
        {
            radioInteraction.ResetForNewDay();
            if (config.radioDialogue != null && config.radioDialogue.Length > 0)
                radioInteraction.SetDailyDialogue(config.radioDialogue);
            else
                radioInteraction.SetDailyDialogue(new string[] { "O rádio está mudo." });
        }

        if (allPeekInteractions == null || allPeekInteractions.Length == 0)
            allPeekInteractions = FindObjectsOfType<BasePeekInteraction>();

        if (allPeekInteractions != null && config.windowDialogues != null)
        {
            foreach (BasePeekInteraction peekInteraction in allPeekInteractions)
            {
                if (peekInteraction is DoorInteraction) continue;
                WindowDayConfig? dialogueConfig = config.windowDialogues.FirstOrDefault(d => d.windowID == peekInteraction.windowID);
                if (dialogueConfig.HasValue && dialogueConfig.Value.dialogue != null && dialogueConfig.Value.dialogue.Length > 0)
                    peekInteraction.SetDailyDialogue(dialogueConfig.Value.dialogue);
                else
                    peekInteraction.SetDailyDialogue(new string[] { $"[Janela {peekInteraction.windowID}]: Eu não vejo nada de novo por aqui." });
            }
        }

        OnDayStarted?.Invoke(currentDayIndex);

        // NOTA: Removemos o agendamento imediato aqui. 
        // Agora ele acontece APÓS as mensagens terminarem, no DayStartSequence.
    }

    // --- SEQUÊNCIA DE MENSAGENS (PARCELADA) ---
    private IEnumerator DayStartSequence()
    {
        if (currentDayIndex >= allDays.Length) yield break;

        DayConfig config = allDays[currentDayIndex];

        // Se houver mensagens na lista
        if (config.wakeUpMessages != null && config.wakeUpMessages.Length > 0)
        {
            // Pequeno delay inicial para o fade da câmera terminar
            yield return new WaitForSeconds(1.0f);

            // Loop por cada frase da lista
            foreach (string message in config.wakeUpMessages)
            {
                if (string.IsNullOrWhiteSpace(message)) continue;

                // Calcula tempo de leitura (Mínimo 3s + tempo pelo tamanho do texto)
                float msgDuration = Mathf.Max(3.0f, 2.0f + (message.Length * 0.06f));

                // Exibe a frase
                if (DialogManager.Instance != null)
                    DialogManager.Instance.ShowMessage(message, msgDuration);

                // Espera a mensagem sumir + um pequeno respiro antes da próxima
                // (msgDuration é o tempo que ela fica na tela, + 1.0s para o fade out e silêncio)
                yield return new WaitForSeconds(msgDuration + 1.0f);
            }
        }
        else
        {
            // Se não tiver mensagem, só espera um pouco
            yield return new WaitForSeconds(1.5f);
        }

        // SÓ AGORA os visitantes começam a chegar
        Debug.Log($"[Dia {currentDayIndex + 1}] Mensagens finalizadas. Iniciando fila de visitantes.");
        StartCoroutine(ScheduleNextVisitor());
    }

    private IEnumerator ScheduleNextVisitor()
    {
        if (dailyQueue.Count == 0)
        {
            IsVisitorWaiting = false;
            yield break;
        }

        float delay = Random.Range(minArrivalDelay, maxArrivalDelay);
        yield return new WaitForSeconds(delay);

        if (IsGameOver) yield break;

        IsVisitorWaiting = true;

        if (GameAudioManager.Instance != null)
        {
            VisitorProfile nextVisitor = dailyQueue.Peek();
            AudioClip sound = nextVisitor.specificKnockSound != null ? nextVisitor.specificKnockSound : knockingSound;
            GameAudioManager.Instance.PlayKnocking(sound);
        }
    }

    public void CheckDoorForVisitor()
    {
        if (GameAudioManager.Instance != null) GameAudioManager.Instance.StopKnocking();

        if (dailyQueue.Count > 0)
        {
            VisitorProfile nextVisitor = dailyQueue.Peek();
            PeepholeManager.Instance.StartEncounter(nextVisitor);
        }
    }

    public void RegisterVisitorProcessed()
    {
        if (IsGameOver) return;

        if (dailyQueue.Count > 0) dailyQueue.Dequeue();

        visitorsProcessedToday++;
        IsVisitorWaiting = false;

        if (dailyQueue.Count == 0)
        {
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage("O silêncio voltou... Acho que posso dormir agora.", 3f);
        }
        else
        {
            StartCoroutine(ScheduleNextVisitor());
        }
    }

    public void TriggerGameOverEvent(VisitorProfile paleMan)
    {
        Debug.Log("DAYCYCLE: Modo Game Over Ativado.");
        StopAllCoroutines();
        dailyQueue.Clear();
        dailyQueue.Enqueue(paleMan);
        IsGameOver = true;
        IsVisitorWaiting = true;

        if (GameAudioManager.Instance != null)
        {
            AudioClip sound = paleMan.specificKnockSound != null ? paleMan.specificKnockSound : knockingSound;
            GameAudioManager.Instance.PlayKnocking(sound);
        }
    }

    public bool CanAdvanceDay() { return dailyQueue.Count == 0 && !IsGameOver; }

    public IEnumerator AdvanceToNextDaySequence()
    {
        if (CameraFader.Instance != null) yield return StartCoroutine(CameraFader.Instance.Fade(1f, 2f));
        else yield return new WaitForSeconds(1f);

        yield return new WaitForSeconds(2f);
        currentDayIndex++;

        if (GeneratorManager.Instance != null) GeneratorManager.Instance.TransitionToNextDay();

        StartDay(currentDayIndex);

        if (CameraFader.Instance != null) yield return StartCoroutine(CameraFader.Instance.Fade(0f, 2f));

        // Inicia a Sequência de Mensagens do novo dia
        if (currentDayIndex < allDays.Length && !IsGameOver)
        {
            yield return StartCoroutine(DayStartSequence());
        }
    }

    public int GetCurrentDayIndex() { return currentDayIndex; }
}