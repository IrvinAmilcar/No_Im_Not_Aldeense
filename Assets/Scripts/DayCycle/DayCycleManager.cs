using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DialogSystem;
using System.Linq;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    public static event System.Action<int> OnDayStarted;

    [System.Serializable]
    public struct WindowDayConfig
    {
        public string windowID;
        [TextArea(3, 5)] public string[] dialogue;
    }

    [System.Serializable]
    public struct DayConfig
    {
        public string dayName;
        public List<VisitorProfile> visitorsForThisDay;
        public DayMusicSetup musicSetup;
        [TextArea] public string wakeUpMessage;
        [TextArea(3, 5)] public string[] radioDialogue;
        public WindowDayConfig[] windowDialogues;
    }

    [Header("Configuração dos Dias")]
    public DayConfig[] allDays;

    [Header("Configuração de Visitantes")]
    public AudioClip defaultKnockingSound; // Renomeado para deixar claro que é o padrão
    public float minArrivalDelay = 2f;
    public float maxArrivalDelay = 5f;

    [Header("Referências")]
    public RadioInteraction radioInteraction;
    public BasePeekInteraction[] allPeekInteractions;

    private int currentDayIndex = 0;
    private Queue<VisitorProfile> dailyQueue = new Queue<VisitorProfile>();
    private int visitorsProcessedToday = 0;

    public bool IsVisitorWaiting { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() { StartDay(0); }

    private void StartDay(int dayIndex)
    {
        currentDayIndex = dayIndex;
        visitorsProcessedToday = 0;
        dailyQueue.Clear();
        IsVisitorWaiting = false;

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
        Debug.Log($"Iniciando {config.dayName}. Visitantes: {dailyQueue.Count}");

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

        IsVisitorWaiting = true;

        // --- LÓGICA DE SOM PERSONALIZADO ---
        if (GameAudioManager.Instance != null)
        {
            // Espia quem é o próximo sem remover da fila
            VisitorProfile nextVisitor = dailyQueue.Peek();

            // Decide qual som usar: o do perfil ou o padrão
            AudioClip soundToPlay = nextVisitor.specificKnockSound != null ? nextVisitor.specificKnockSound : defaultKnockingSound;

            if (soundToPlay != null)
                GameAudioManager.Instance.PlayKnocking(soundToPlay);
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
        if (dailyQueue.Count > 0) dailyQueue.Dequeue();

        visitorsProcessedToday++;
        IsVisitorWaiting = false;

        if (dailyQueue.Count == 0)
        {
            Debug.Log("Todos atendidos.");
            if (DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage("O silêncio voltou... Acho que posso dormir agora.", 3f);
        }
        else
        {
            StartCoroutine(ScheduleNextVisitor());
        }
    }

    public bool CanAdvanceDay() { return dailyQueue.Count == 0; }

    public IEnumerator AdvanceToNextDaySequence()
    {
        if (CameraFader.Instance != null) yield return StartCoroutine(CameraFader.Instance.Fade(1f, 2f));
        else yield return new WaitForSeconds(1f);

        yield return new WaitForSeconds(2f);
        currentDayIndex++;

        if (GeneratorManager.Instance != null) GeneratorManager.Instance.TransitionToNextDay();

        StartDay(currentDayIndex);

        if (CameraFader.Instance != null) yield return StartCoroutine(CameraFader.Instance.Fade(0f, 2f));

        if (currentDayIndex < allDays.Length)
        {
            string msg = allDays[currentDayIndex].wakeUpMessage;
            if (!string.IsNullOrEmpty(msg) && DialogManager.Instance != null)
                DialogManager.Instance.ShowMessage(msg, 4f);
        }
    }

    public int GetCurrentDayIndex() { return currentDayIndex; }
}